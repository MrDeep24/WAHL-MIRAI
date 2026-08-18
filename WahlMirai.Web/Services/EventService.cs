using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class EventService : IEventService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;

    public EventService(WahlMiraiDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<VotingEvent>> GetEventsAsync()
    {
        var events = await _context.VotingEvents
            .Where(e => e.Status != "ELIMINADO")
            .Include(e => e.Candidates)
            .Include(e => e.EventGrades).ThenInclude(eg => eg.Grade)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
            
        await UpdateEventStatusesAsync(events);
        return events;
    }

    public async Task<VotingEvent?> GetEventByIdAsync(uint id)
    {
        var ev = await _context.VotingEvents
            .Include(e => e.Candidates.Where(c => c.Status != "RECHAZADO"))
                .ThenInclude(c => c.CandidateProposals)
            .Include(e => e.Candidates)
                .ThenInclude(c => c.Voter)
            .Include(e => e.EventGrades)
                .ThenInclude(eg => eg.Grade)
            .FirstOrDefaultAsync(e => e.Id == id && e.Status != "ELIMINADO");
            
        if (ev != null)
        {
            await UpdateEventStatusesAsync(new[] { ev });
        }
        return ev;
    }

    public async Task<VotingEvent> CreateEventAsync(VotingEvent newEvent, List<byte> gradeIds, string clientIp)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (newEvent.StartDate < today)
            throw new ArgumentException("La fecha de inicio del proceso electoral debe ser a partir de la fecha actual.");

        if (newEvent.EndDate < newEvent.StartDate || (newEvent.EndDate == newEvent.StartDate && newEvent.EndTime <= newEvent.StartTime))
            throw new ArgumentException("La fecha/hora de fin debe ser posterior a la de inicio.");

        newEvent.Status = "PROGRAMADA";
        newEvent.CreatedAt = DateTime.UtcNow;

        _context.VotingEvents.Add(newEvent);
        await _context.SaveChangesAsync();

        // Sync event_grades
        if (gradeIds != null && gradeIds.Any())
        {
            var eventGrades = gradeIds.Select(gid => new EventGrade { VotingEventId = newEvent.Id, GradeId = gid }).ToList();
            _context.EventGrades.AddRange(eventGrades);
        }

        // Voto en blanco autogenerado SOLO para tipo PERSONAS
        if (newEvent.ElectionType == "PERSONAS")
        {
            var blankVote = new Candidate
            {
                VotingEventId = newEvent.Id,
                Name = "Voto en Blanco",
                IsBlankVote = true,
                Status = "APROBADO",
                EnrolledAt = DateTime.UtcNow
            };
            _context.Candidates.Add(blankVote);
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("EVENT_CREATED", (int)newEvent.CreatedByVoterId, "voting_events", (int)newEvent.Id, null, null, null, null, clientIp);
        
        return newEvent;
    }

    public async Task<VotingEvent?> UpdateEventAsync(VotingEvent updatedEvent, List<byte> gradeIds, string clientIp)
    {
        var existing = await _context.VotingEvents
            .Include(e => e.EventGrades)
            .FirstOrDefaultAsync(e => e.Id == updatedEvent.Id && e.Status != "ELIMINADO");

        if (existing == null) return null;

        var today = DateOnly.FromDateTime(DateTime.Now);
        if (updatedEvent.StartDate < today && updatedEvent.StartDate != existing.StartDate)
            throw new ArgumentException("La fecha de inicio del proceso electoral debe ser a partir de la fecha actual.");

        if (updatedEvent.EndDate < updatedEvent.StartDate || (updatedEvent.EndDate == updatedEvent.StartDate && updatedEvent.EndTime <= updatedEvent.StartTime))
            throw new ArgumentException("La fecha/hora de fin debe ser posterior a la de inicio.");

        var oldValues = $"Title:{existing.Title}, Start:{existing.StartDate}, End:{existing.EndDate}";

        existing.Title = updatedEvent.Title;
        existing.Description = updatedEvent.Description;
        existing.StartDate = updatedEvent.StartDate;
        existing.StartTime = updatedEvent.StartTime;
        existing.EndDate = updatedEvent.EndDate;
        existing.EndTime = updatedEvent.EndTime;
        
        // Sync grades
        if (gradeIds != null)
        {
            _context.EventGrades.RemoveRange(existing.EventGrades);
            var newGrades = gradeIds.Select(gid => new EventGrade { VotingEventId = existing.Id, GradeId = gid }).ToList();
            _context.EventGrades.AddRange(newGrades);
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("EVENT_UPDATED", (int)updatedEvent.CreatedByVoterId, "voting_events", (int)existing.Id, null, oldValues, $"Title:{existing.Title}, Start:{existing.StartDate}, End:{existing.EndDate}", null, clientIp);

        return existing;
    }

    public async Task<bool> SoftDeleteEventAsync(uint id, string clientIp)
    {
        var ev = await _context.VotingEvents.FindAsync(id);
        if (ev == null || ev.Status == "ELIMINADO") return false;

        ev.Status = "ELIMINADO";
        ev.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("EVENT_DELETED", null, "voting_events", (int)id, "status", "PROGRAMADA/ACTIVA/FINALIZADA", "ELIMINADO", null, clientIp);
        
        return true;
    }

    public async Task<Candidate> AddCandidateAsync(uint eventId, uint voterId, string? slogan, string? photoUrl, string clientIp)
    {
        var ev = await _context.VotingEvents.FindAsync(eventId);
        if (ev == null || ev.Status != "PROGRAMADA")
            throw new InvalidOperationException("Solo se pueden añadir candidatos cuando el evento está en estado PROGRAMADA.");

        if (ev.ElectionType != "PERSONAS")
            throw new InvalidOperationException("Este método solo es válido para elecciones de tipo PERSONAS.");

        var voter = await _context.Voters.Include(v => v.Grade).FirstOrDefaultAsync(v => v.Id == voterId);
        if (voter == null || voter.Status != "ACTIVO")
            throw new InvalidOperationException("El elector no existe o no está ACTIVO.");

        var candidate = new Candidate
        {
            VotingEventId = eventId,
            VoterId = voterId,
            Name = voter.FullName,
            Slogan = slogan,
            PhotoUrl = photoUrl,
            Status = "APROBADO",
            IsBlankVote = false,
            EnrolledAt = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CANDIDATE_ADDED", null, "candidates", (int)candidate.Id, null, null, $"VoterId: {voterId}", null, clientIp);

        return candidate;
    }

    public async Task<Candidate> AddProposalOptionAsync(uint eventId, string name, string? slogan, string? photoUrl, string clientIp)
    {
        var ev = await _context.VotingEvents.FindAsync(eventId);
        if (ev == null || ev.Status != "PROGRAMADA")
            throw new InvalidOperationException("Solo se pueden añadir opciones cuando el evento está en estado PROGRAMADA.");
        
        if (ev.ElectionType != "TEMAS")
            throw new InvalidOperationException("Este método solo es válido para elecciones de tipo TEMAS.");

        var candidate = new Candidate
        {
            VotingEventId = eventId,
            VoterId = null,
            Name = name,
            Slogan = slogan,
            PhotoUrl = photoUrl,
            Status = "APROBADO",
            IsBlankVote = false,
            EnrolledAt = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PROPOSAL_OPTION_ADDED", null, "candidates", (int)candidate.Id, null, null, $"Name: {name}", null, clientIp);

        return candidate;
    }

    public async Task<List<VoterSearchResultDto>> SearchVoterAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return new List<VoterSearchResultDto>();

        var query = _context.Voters
            .Include(v => v.Grade)
            .Where(v => v.Status == "ACTIVO" && v.RoleId == 2);

        string termLower = term.ToLower();
        string hashedDoc = ComputeSha256Hash(term);

        var matches = await query.Where(v => v.FullName.ToLower().Contains(termLower) || v.DocumentHash == hashedDoc)
            .Take(10)
            .ToListAsync();

        return matches.Select(v => new VoterSearchResultDto
        {
            Id = v.Id,
            FullName = v.FullName,
            Documento = DecryptDocument(v.EncryptedDocument),
            GradeName = v.Grade?.Name ?? "N/A"
        }).ToList();
    }
    
    private string DecryptDocument(string encryptedDocument)
    {
        if (string.IsNullOrEmpty(encryptedDocument)) return "";
        if (encryptedDocument.StartsWith("PENDIENTE_CIFRAR:"))
            return encryptedDocument.Substring(17);
            
        // Simulando descifrado, como especificado, mostramos solo los últimos 4 dígitos o algo legible.
        // Como no tenemos la clave de cifrado aquí, es una simplificación
        if (encryptedDocument.Length > 4)
            return "******" + encryptedDocument.Substring(encryptedDocument.Length - 4);
            
        return encryptedDocument;
    }
    
    private string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    private async Task UpdateEventStatusesAsync(IEnumerable<VotingEvent> events)
    {
        var now = DateTime.Now;
        var currentDate = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);
        bool hasChanges = false;

        foreach (var ve in events)
        {
            if (ve.Status == "PROGRAMADA" && (currentDate > ve.StartDate || (currentDate == ve.StartDate && currentTime >= ve.StartTime)))
            {
                ve.Status = "ACTIVA";
                hasChanges = true;
            }
            if ((ve.Status == "PROGRAMADA" || ve.Status == "ACTIVA") && (currentDate > ve.EndDate || (currentDate == ve.EndDate && currentTime >= ve.EndTime)))
            {
                ve.Status = "FINALIZADA";
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }
    }
}

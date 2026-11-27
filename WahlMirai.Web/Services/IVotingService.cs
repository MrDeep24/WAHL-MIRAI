using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WahlMirai.Web.Models;

using Microsoft.AspNetCore.SignalR;
using WahlMirai.Web.Hubs;

namespace WahlMirai.Web.Services;

/// <summary>
/// Resultado detallado del intento de emisión de voto.
/// </summary>
public sealed record CastVoteResult(bool Success, string? ErrorCode = null, string? ErrorMessage = null);

public interface IVotingService
{
    Task<bool> HasVotedAsync(int voterId, int eventId);
    Task<bool> CastVoteAsync(int voterId, int eventId, int candidateId, string ipAddress);

    /// <summary>
    /// Emite un voto validando elector, proceso electoral, etapa ACTIVA, grado habilitado y candidato aprobado.
    /// Implementación por defecto delega en <see cref="CastVoteAsync"/> para mantener compatibilidad con dobles de prueba.
    /// </summary>
    async Task<CastVoteResult> CastVoteDetailedAsync(int voterId, int eventId, int candidateId, string ipAddress)
    {
        var success = await CastVoteAsync(voterId, eventId, candidateId, ipAddress);
        return success
            ? new CastVoteResult(true, null, "¡Tu voto ha sido registrado exitosamente!")
            : new CastVoteResult(false, "VOTE_FAILED", "Hubo un problema al registrar tu voto. Es posible que ya hayas participado.");
    }

    Task<List<VotingEvent>> GetActiveEventsForVoterAsync(int voterId);
    /// <summary>
    /// Returns all voting events visible on the elector dashboard:
    /// ACTIVA events (where the elector can still vote) AND FINALIZADA events
    /// (where the elector's grade is enabled in event_grades — RN-4.1).
    /// Events with status ELIMINADO are never included.
    /// </summary>
    Task<List<VotingEvent>> GetEventsForVoterDashboardAsync(int voterId);
    Task<List<Candidate>> GetCandidatesForEventAsync(int eventId);
    Task<string?> GetVoterGradeNameAsync(int voterId);
}

public class VotingService : IVotingService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IHubContext<ResultsHub> _hubContext;
    private readonly TimeProvider _timeProvider;

    public VotingService(WahlMiraiDbContext context, IAuditService auditService, IHubContext<ResultsHub> hubContext, TimeProvider? timeProvider = null)
    {
        _context = context;
        _auditService = auditService;
        _hubContext = hubContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<bool> HasVotedAsync(int voterId, int eventId)
    {
        return await _context.VoterEventParticipations
            .AnyAsync(p => p.VoterId == (uint)voterId && p.VotingEventId == (uint)eventId);
    }

    /// <summary>
    /// Registra un voto de forma anónima (RN-3) garantizando unicidad por elector y evento.
    /// </summary>
    public async Task<bool> CastVoteAsync(int voterId, int eventId, int candidateId, string ipAddress)
    {
        var result = await CastVoteDetailedAsync(voterId, eventId, candidateId, ipAddress);
        return result.Success;
    }

    /// <summary>
    /// Valida elector activo, evento no eliminado, grado habilitado en event_grades, etapa ACTIVA,
    /// candidato aprobado perteneciente al evento (incluyendo voto en blanco) y no duplicidad antes de persistir.
    /// </summary>
    public async Task<CastVoteResult> CastVoteDetailedAsync(int voterId, int eventId, int candidateId, string ipAddress)
    {
        if (voterId <= 0 || eventId <= 0 || candidateId <= 0)
        {
            return new CastVoteResult(false, "INVALID_REQUEST", "Datos de votación inválidos.");
        }

        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.Status != "ACTIVO" || voter.DeletedAt != null)
        {
            return new CastVoteResult(false, "VOTER_INELIGIBLE", "Tu cuenta no está activa o no está habilitada para votar.");
        }

        var votingEvent = await _context.VotingEvents.FindAsync((uint)eventId);
        if (votingEvent == null || votingEvent.Status == "ELIMINADO" || votingEvent.DeletedAt != null)
        {
            return new CastVoteResult(false, "EVENT_NOT_FOUND", "El proceso electoral no está disponible para tu grado o no existe.");
        }

        if (voter.GradeId == null)
        {
            return new CastVoteResult(false, "GRADE_NOT_ELIGIBLE", "El proceso electoral no está disponible para tu grado.");
        }

        var isGradeAllowed = await _context.EventGrades.AnyAsync(eg =>
            eg.VotingEventId == (uint)eventId && eg.GradeId == voter.GradeId.Value);
        if (!isGradeAllowed)
        {
            return new CastVoteResult(false, "GRADE_NOT_ELIGIBLE", "El proceso electoral no está disponible para tu grado.");
        }

        var localNow = _timeProvider.GetLocalNow().DateTime;
        if (!string.Equals(CalculateStatus(votingEvent, localNow), "ACTIVA", StringComparison.Ordinal))
        {
            return new CastVoteResult(false, "EVENT_NOT_ACTIVE", "La votación no se encuentra en etapa ACTIVA.");
        }

        var isCandidateValid = await _context.Candidates.AnyAsync(c =>
            c.Id == (uint)candidateId &&
            c.VotingEventId == (uint)eventId &&
            c.Status == "APROBADO");
        if (!isCandidateValid)
        {
            return new CastVoteResult(false, "INVALID_CANDIDATE", "El candidato seleccionado no es válido para esta elección.");
        }

        if (await HasVotedAsync(voterId, eventId))
        {
            return new CastVoteResult(false, "ALREADY_VOTED", "Ya has participado en esta elección.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create anonymous vote (RN-3: without any voter/user reference)
            var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            var voteData = $"{eventId}-{candidateId}-{utcNow.Ticks}-{Guid.NewGuid()}";
            var voteHash = HashVote(voteData);

            var vote = new Vote
            {
                VotingEventId = (uint)eventId,
                CandidateId = (uint)candidateId,
                VoteHash = voteHash,
                VotedAt = utcNow
            };

            _context.Votes.Add(vote);

            // Record participation separately to enforce anti-duplicate (uq_ep_user_event) while maintaining anonymity
            var participation = new VoterEventParticipation
            {
                VoterId = (uint)voterId,
                VotingEventId = (uint)eventId,
                ParticipatedAt = utcNow
            };

            _context.VoterEventParticipations.Add(participation);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogAsync("VOTE_CAST", voterId, "votes", null, null, null, null, $"Event: {eventId}", ipAddress);

            // Notificar cambios en tiempo real vía SignalR
            try
            {
                var liveCounts = await _context.VwVoteCounts
                    .Where(v => v.EventId == eventId)
                    .OrderByDescending(v => v.TotalVotes)
                    .Select(v => new {
                        candidateId = v.CandidateId,
                        candidateName = v.CandidateName,
                        totalVotes = v.TotalVotes
                    })
                    .ToListAsync();

                long totalVotes = liveCounts.Sum(c => c.totalVotes);

                await _hubContext.Clients.Group($"Event_{eventId}").SendAsync("ReceiveResultsUpdate", new {
                    eventId = eventId,
                    totalVotes = totalVotes,
                    candidates = liveCounts
                });
            }
            catch
            {
                // Fallback silencioso si falla la notificación en tiempo real
            }

            return new CastVoteResult(true, null, "¡Tu voto ha sido registrado exitosamente!");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return new CastVoteResult(false, "ALREADY_VOTED", "Ya has participado en esta elección o no es posible registrar el voto.");
        }
    }

    public async Task<List<VotingEvent>> GetActiveEventsForVoterAsync(int voterId)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.GradeId == null) return new List<VotingEvent>();

        var events = await _context.VotingEvents
            .Include(ve => ve.EventGrades)
            .Where(ve => ve.Status != "ELIMINADO" && ve.EventGrades.Any(eg => eg.GradeId == voter.GradeId))
            .ToListAsync();

        var now = _timeProvider.GetLocalNow().DateTime;
        bool hasChanges = false;

        foreach (var ve in events)
        {
            var calculated = CalculateStatus(ve, now);
            if (ve.Status != calculated)
            {
                ve.Status = calculated;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }

        return events.Where(ve => ve.Status == "ACTIVA").ToList();
    }

    public async Task<List<VotingEvent>> GetEventsForVoterDashboardAsync(int voterId)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.GradeId == null) return new List<VotingEvent>();

        // Fetch all non-deleted events where the voter's grade is enabled.
        var candidates = await _context.VotingEvents
            .Include(ve => ve.EventGrades)
            .Where(ve =>
                ve.Status != "ELIMINADO" &&
                ve.EventGrades.Any(eg => eg.GradeId == voter.GradeId))
            .ToListAsync();

        var now = _timeProvider.GetLocalNow().DateTime;
        bool hasChanges = false;

        foreach (var ve in candidates)
        {
            var calculated = CalculateStatus(ve, now);
            if (ve.Status != calculated)
            {
                ve.Status = calculated;
                hasChanges = true;
            }
        }

        if (hasChanges)
            await _context.SaveChangesAsync();

        return candidates
            .Where(ve => ve.Status == "INSCRIPCION" || ve.Status == "PROPUESTAS" || ve.Status == "ACTIVA" || ve.Status == "FINALIZADA")
            .ToList();
    }

    private static int GetStatusRank(string? status) => status switch
    {
        "INSCRIPCION" => 1,
        "PROPUESTAS" => 2,
        "ACTIVA" => 3,
        "FINALIZADA" => 4,
        _ => 0
    };

    private static string CalculateStatus(VotingEvent ve, DateTime now)
    {
        if (ve.Status == "ELIMINADO") return "ELIMINADO";

        var regStart = ve.RegistrationStartDate.ToDateTime(ve.RegistrationStartTime);
        var propStart = ve.ProposalsStartDate.ToDateTime(ve.ProposalsStartTime);
        var votStart = ve.VotingStartDate.ToDateTime(ve.VotingStartTime);
        var votEnd = ve.VotingEndDate.ToDateTime(ve.VotingEndTime);

        string computed;
        if (now >= votEnd) computed = "FINALIZADA";
        else if (now >= votStart) computed = "ACTIVA";
        else if (now >= propStart) computed = "PROPUESTAS";
        else if (now >= regStart) computed = "INSCRIPCION";
        else computed = "PROGRAMADA";

        int currentRank = GetStatusRank(ve.Status);
        int computedRank = GetStatusRank(computed);

        if (computedRank < currentRank)
        {
            return ve.Status;
        }

        if (computed == "PROGRAMADA" && (now >= regStart || currentRank > 0))
        {
            return ve.Status is "INSCRIPCION" or "PROPUESTAS" or "ACTIVA" or "FINALIZADA"
                ? ve.Status
                : "INSCRIPCION";
        }

        return computed;
    }

    public async Task<List<Candidate>> GetCandidatesForEventAsync(int eventId)
    {
        return await _context.Candidates
            .Include(c => c.CandidateProposals)
            .Include(c => c.Voter!)
                .ThenInclude(v => v.Grade!)
            .Where(c => c.VotingEventId == (uint)eventId && c.Status == "APROBADO")
            .ToListAsync();
    }

    public async Task<string?> GetVoterGradeNameAsync(int voterId)
    {
        var voter = await _context.Voters
            .Include(v => v.Grade)
            .FirstOrDefaultAsync(v => v.Id == (uint)voterId);
        return voter?.Grade?.Name;
    }

    private string HashVote(string data)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}

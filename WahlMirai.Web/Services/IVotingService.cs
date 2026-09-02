using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WahlMirai.Web.Models;

using Microsoft.AspNetCore.SignalR;
using WahlMirai.Web.Hubs;

namespace WahlMirai.Web.Services;

public interface IVotingService
{
    Task<bool> HasVotedAsync(int voterId, int eventId);
    Task<bool> CastVoteAsync(int voterId, int eventId, int candidateId, string ipAddress);
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

    public VotingService(WahlMiraiDbContext context, IAuditService auditService, IHubContext<ResultsHub> hubContext)
    {
        _context = context;
        _auditService = auditService;
        _hubContext = hubContext;
    }

    public async Task<bool> HasVotedAsync(int voterId, int eventId)
    {
        return await _context.VoterEventParticipations
            .AnyAsync(p => p.VoterId == (uint)voterId && p.VotingEventId == (uint)eventId);
    }

    public async Task<bool> CastVoteAsync(int voterId, int eventId, int candidateId, string ipAddress)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.Status != "ACTIVO")
        {
            return false;
        }

        if (await HasVotedAsync(voterId, eventId))
        {
            return false;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create anonymous vote
            var voteData = $"{eventId}-{candidateId}-{DateTime.UtcNow.Ticks}-{Guid.NewGuid()}";
            var voteHash = HashVote(voteData);

            var vote = new Vote
            {
                VotingEventId = (uint)eventId,
                CandidateId = (uint)candidateId,
                VoteHash = voteHash,
                VotedAt = DateTime.UtcNow
            };
            
            _context.Votes.Add(vote);

            // Record participation separately to enforce anti-duplicate but maintain anonymity
            var participation = new VoterEventParticipation
            {
                VoterId = (uint)voterId,
                VotingEventId = (uint)eventId,
                ParticipatedAt = DateTime.UtcNow
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

            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return false;
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

        var now = DateTime.Now;
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

        var now = DateTime.Now;
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

    private static string CalculateStatus(VotingEvent ve, DateTime now)
    {
        var regStart = ve.RegistrationStartDate.ToDateTime(ve.RegistrationStartTime);
        var regEnd = ve.RegistrationEndDate.ToDateTime(ve.RegistrationEndTime);
        var propStart = ve.ProposalsStartDate.ToDateTime(ve.ProposalsStartTime);
        var propEnd = ve.ProposalsEndDate.ToDateTime(ve.ProposalsEndTime);
        var votStart = ve.VotingStartDate.ToDateTime(ve.VotingStartTime);
        var votEnd = ve.VotingEndDate.ToDateTime(ve.VotingEndTime);

        if (now >= votEnd) return "FINALIZADA";
        if (now >= votStart && now < votEnd) return "ACTIVA";
        if (now >= propStart && now < propEnd) return "PROPUESTAS";
        if (now >= regStart && now < regEnd) return "INSCRIPCION";
        return "PROGRAMADA";
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

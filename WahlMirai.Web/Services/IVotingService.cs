using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public interface IVotingService
{
    Task<bool> HasVotedAsync(int voterId, int eventId);
    Task<bool> CastVoteAsync(int voterId, int eventId, int candidateId, string ipAddress);
    Task<List<VotingEvent>> GetActiveEventsForVoterAsync(int voterId);
    Task<List<Candidate>> GetCandidatesForEventAsync(int eventId);
}

public class VotingService : IVotingService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;

    public VotingService(WahlMiraiDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<bool> HasVotedAsync(int voterId, int eventId)
    {
        return await _context.VoterEventParticipations
            .AnyAsync(p => p.VoterId == (uint)voterId && p.VotingEventId == (uint)eventId);
    }

    public async Task<bool> CastVoteAsync(int voterId, int eventId, int candidateId, string ipAddress)
    {
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

        return await _context.VotingEvents
            .Include(ve => ve.EventGrades)
            .Where(ve => ve.Status == "ACTIVA" && ve.EventGrades.Any(eg => eg.GradeId == voter.GradeId))
            .ToListAsync();
    }

    public async Task<List<Candidate>> GetCandidatesForEventAsync(int eventId)
    {
        return await _context.Candidates
            .Include(c => c.CandidateProposals)
            .Where(c => c.VotingEventId == (uint)eventId && c.Status == "APROBADO")
            .ToListAsync();
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

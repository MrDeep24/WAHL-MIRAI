using System.Collections.Generic;
using System.Threading.Tasks;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class VoterSearchResultDto
{
    public uint Id { get; set; }
    public string Documento { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string GradeName { get; set; } = string.Empty;
}

public interface IEventService
{
    Task<List<VotingEvent>> GetEventsAsync();
    Task<VotingEvent?> GetEventByIdAsync(uint id);
    Task<VotingEvent> CreateEventAsync(VotingEvent newEvent, List<byte> gradeIds, string clientIp);
    Task<VotingEvent?> UpdateEventAsync(VotingEvent updatedEvent, List<byte> gradeIds, string clientIp);
    Task<bool> SoftDeleteEventAsync(uint id, string clientIp);
    Task<Candidate> AddCandidateAsync(uint eventId, uint voterId, string? slogan, string? photoUrl, string clientIp);
    Task<Candidate> AddProposalOptionAsync(uint eventId, string name, string? slogan, string? photoUrl, string clientIp);
    Task<List<VoterSearchResultDto>> SearchVoterAsync(string term);
}

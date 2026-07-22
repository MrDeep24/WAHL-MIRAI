using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public interface IAuditService
{
    Task LogAsync(string action, int? voterId, string targetEntity, int? targetId,
        string? fieldName, string? oldValue, string? newValue, string? details, string? ipAddress);
}

public class AuditService : IAuditService
{
    private readonly WahlMiraiDbContext _context;

    public AuditService(WahlMiraiDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string action, int? voterId, string targetEntity, int? targetId,
        string? fieldName, string? oldValue, string? newValue, string? details, string? ipAddress)
    {
        var log = new AuditLog
        {
            Action       = action,
            VoterId      = voterId.HasValue ? (uint?)voterId.Value : null,
            TargetEntity = targetEntity,
            TargetId     = targetId,
            FieldName    = fieldName,
            OldValue     = oldValue,
            NewValue     = newValue,
            Details      = details,
            IpAddress    = ipAddress,
            OccurredAt   = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}

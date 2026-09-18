using System.Collections.Concurrent;

namespace WahlMirai.Web.Services;

public record SessionEntry(string Stamp, SessionInvalidationReason? PendingReason);

public class ActiveSessionStore : IActiveSessionStore
{
    private readonly ConcurrentDictionary<uint, SessionEntry> _sessions = new();

    public void RegisterSession(uint userId, string sessionStamp)
    {
        _sessions[userId] = new SessionEntry(sessionStamp, SessionInvalidationReason.ReplacedByNewDevice);
    }

    public (bool IsValid, SessionInvalidationReason? Reason) IsSessionValid(uint userId, string sessionStamp)
    {
        if (string.IsNullOrWhiteSpace(sessionStamp))
        {
            return (false, SessionInvalidationReason.ReplacedByNewDevice);
        }

        if (!_sessions.TryGetValue(userId, out var entry))
        {
            return (false, SessionInvalidationReason.ReplacedByNewDevice);
        }

        if (entry.Stamp == sessionStamp)
        {
            return (true, null);
        }

        return (false, entry.PendingReason ?? SessionInvalidationReason.ReplacedByNewDevice);
    }

    public void InvalidateSession(uint userId, SessionInvalidationReason reason)
    {
        _sessions[userId] = new SessionEntry(string.Empty, reason);
    }
}

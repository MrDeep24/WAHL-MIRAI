using System.Collections.Concurrent;

namespace WahlMirai.Web.Services;

public interface IPendingPasswordStore
{
    void StorePassword(ulong emailQueueId, string plainTextPassword);
    bool TryGetPassword(ulong emailQueueId, out string? plainTextPassword);
    void RemovePassword(ulong emailQueueId);
}

public class PendingPasswordStore : IPendingPasswordStore
{
    private readonly ConcurrentDictionary<ulong, string> _store = new();

    public void StorePassword(ulong emailQueueId, string plainTextPassword)
    {
        _store[emailQueueId] = plainTextPassword;
    }

    public bool TryGetPassword(ulong emailQueueId, out string? plainTextPassword)
    {
        return _store.TryGetValue(emailQueueId, out plainTextPassword);
    }

    public void RemovePassword(ulong emailQueueId)
    {
        _store.TryRemove(emailQueueId, out _);
    }
}

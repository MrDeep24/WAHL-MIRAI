namespace WahlMirai.Web.Services;

public enum SessionInvalidationReason
{
    ReplacedByNewDevice,
    AccountDisabledOrDeleted,
    LoggedOut
}

public interface IActiveSessionStore
{
    void RegisterSession(uint userId, string sessionStamp);
    (bool IsValid, SessionInvalidationReason? Reason) IsSessionValid(uint userId, string sessionStamp);
    void InvalidateSession(uint userId, SessionInvalidationReason reason);
}

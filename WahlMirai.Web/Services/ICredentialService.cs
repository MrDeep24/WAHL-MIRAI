namespace WahlMirai.Web.Services;

public enum EmailType
{
    CREDENCIAL_INICIAL,
    RECUPERACION_ACCESO,
    REASIGNACION_ADMIN
}

public interface ICredentialService
{
    Task IssueNewPasswordAsync(int voterId, EmailType emailType, int? actorVoterId, CancellationToken ct = default);
}

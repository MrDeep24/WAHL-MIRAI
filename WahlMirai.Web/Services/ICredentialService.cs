namespace WahlMirai.Web.Services;

public enum EmailType
{
    RECUPERACION_ACCESO,
    REASIGNACION_ADMIN,
    CREDENCIAL_INICIAL,
    CAMBIO_PERFIL,
    RESPUESTA_PQR,
    CANDIDATURA_APROBADA,
    CANDIDATURA_RECHAZADA
}

public interface ICredentialService
{
    Task IssueNewPasswordAsync(int voterId, EmailType emailType, int? actorVoterId, CancellationToken ct = default);
}

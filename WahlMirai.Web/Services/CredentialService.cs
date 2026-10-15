// ============================================================
// Archivo:       CredentialService.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Genera y almacena hashes de nuevas contraseñas,
//                encola avisos, conserva la clave temporal, confirma el restablecimiento
//                por token efímero y registra auditoría.
// Requerimientos: RF-M01-02, RF-M07-02, RF-M09-01; RN-2, RN-9, RN-14, RN-15.
// ============================================================
using System.Security.Cryptography;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

/// <summary>
/// Implementa la emisión de contraseñas nuevas, su encolado para notificación,
/// la confirmación del restablecimiento por token efímero (RN-2, RN-14, RN-15) y la auditoría asociada.
/// </summary>
public class CredentialService : ICredentialService
{
    private readonly WahlMiraiDbContext _dbContext;
    private readonly IPendingPasswordStore _passwordStore;
    private readonly IAuditService _auditService;
    private readonly IPasswordResetTokenStore _tokenStore;
    private readonly ILoginAttemptStore _loginAttemptStore;
    private readonly IActiveSessionStore _sessionStore;

    /// <summary>Inicializa el servicio con el contexto, el almacén temporal, el servicio de auditoría, el almacén de tokens, el almacén de intentos y el almacén de sesiones.</summary>
    /// <param name="dbContext">Contexto usado para actualizar la cuenta y crear la notificación pendiente.</param>
    /// <param name="passwordStore">Almacén temporal para asociar el secreto en claro a la notificación.</param>
    /// <param name="auditService">Servicio que registra la operación de emisión.</param>
    /// <param name="tokenStore">Almacén de tokens de recuperación de acceso de un solo uso.</param>
    /// <param name="loginAttemptStore">Almacén de intentos de login para limpiar bloqueo tras restablecimiento (RN-15).</param>
    /// <param name="sessionStore">Almacén de sesiones activas para revocarlas tras restablecimiento (RN-14).</param>
    public CredentialService(
        WahlMiraiDbContext dbContext,
        IPendingPasswordStore passwordStore,
        IAuditService auditService,
        IPasswordResetTokenStore tokenStore,
        ILoginAttemptStore loginAttemptStore,
        IActiveSessionStore sessionStore)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordStore = passwordStore ?? throw new ArgumentNullException(nameof(passwordStore));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _loginAttemptStore = loginAttemptStore ?? throw new ArgumentNullException(nameof(loginAttemptStore));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    }

    /// <inheritdoc />
    public async Task IssueNewPasswordAsync(int voterId, EmailType emailType, int? actorVoterId, CancellationToken ct = default)
    {
        if (emailType == EmailType.RECUPERACION_ACCESO)
        {
            throw new ArgumentException("El tipo RECUPERACION_ACCESO ya no admite contraseñas generadas; use IssuePasswordResetTokenAsync.", nameof(emailType));
        }

        var voter = await _dbContext.Voters.FindAsync(new object[] { (uint)voterId }, ct);
        if (voter == null) return;

        // Generate strong password
        var plainTextPassword = GenerateStrongPassword(10);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword);

        voter.PasswordHash = passwordHash;
        
        // Enqueue email
        var emailQueue = new EmailQueue
        {
            VoterId = (uint)voterId,
            EmailType = emailType.ToString(),
            Status = "PENDIENTE",
            Attempts = 0,
            CreatedAt = DateTime.Now
        };

        _dbContext.EmailQueues.Add(emailQueue);

        await _dbContext.SaveChangesAsync(ct);

        // Store plaintext password in memory using the new ID
        _passwordStore.StorePassword(emailQueue.Id, plainTextPassword);

        // Audit log
        var action = emailType switch
        {
            EmailType.RECUPERACION_ACCESO => "PASSWORD_RECOVERY_REQUESTED",
            EmailType.REASIGNACION_ADMIN => "PASSWORD_REASSIGNED",
            _ => "PASSWORD_RESET"
        };

        await _auditService.LogAsync(
            action: action,
            voterId: actorVoterId,
            targetEntity: "users",
            targetId: voterId,
            fieldName: null,
            oldValue: null,
            newValue: null,
            details: $"{{\"email_type\":\"{emailType}\"}}",
            ipAddress: "System"
        );
    }

    /// <inheritdoc />
    public async Task IssuePasswordResetTokenAsync(int userId, int? actorUserId, CancellationToken ct = default)
    {
        var voter = await _dbContext.Voters.FindAsync(new object[] { (uint)userId }, ct);
        if (voter == null || voter.Status != "ACTIVO") return;

        var rawToken = _tokenStore.Issue((uint)userId);

        var emailQueue = new EmailQueue
        {
            VoterId = (uint)userId,
            EmailType = EmailType.RECUPERACION_ACCESO.ToString(),
            Status = "PENDIENTE",
            Attempts = 0,
            CreatedAt = DateTime.Now
        };

        _dbContext.EmailQueues.Add(emailQueue);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            _tokenStore.Revoke((uint)userId);
            throw;
        }

        _passwordStore.StorePassword(emailQueue.Id, rawToken);

        await _auditService.LogAsync(
            action: "PASSWORD_RECOVERY_REQUESTED",
            voterId: actorUserId,
            targetEntity: "users",
            targetId: userId,
            fieldName: null,
            oldValue: null,
            newValue: null,
            details: $"{{\"email_type\":\"{EmailType.RECUPERACION_ACCESO}\"}}",
            ipAddress: "System"
        );
    }

    /// <inheritdoc />
    public async Task<(bool Success, string ErrorMessage)> CompletePasswordResetAsync(
        string token, string newPassword, string? ipAddress, CancellationToken ct = default)
    {
        // Mensaje genérico único para todos los casos de rechazo (anti-enumeración)
        const string genericError = "El enlace de restablecimiento no es válido o ha expirado.";

        // 1. Consume el token atómicamente (RF-M01-02, RN-14)
        if (!_tokenStore.TryConsume(token, out var userId))
            return (false, genericError);

        // 2. Valida estado del usuario — misma respuesta genérica para usuario inexistente/inactivo (anti-enumeración)
        var voter = await _dbContext.Voters.FindAsync(new object[] { userId }, ct);
        if (voter == null || voter.Status != "ACTIVO" || voter.DeletedAt != null)
        {
            // El token ya fue consumido (no se puede reutilizar); revocar cualquier residuo por seguridad
            _tokenStore.Revoke(userId);
            return (false, genericError);
        }

        // 3. Actualizar hash de contraseña con BCrypt (mismo cost factor que el resto de la app)
        voter.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        voter.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        // 4. Limpiar bloqueo de login (RN-15): el restablecimiento por token debe ser independiente del lockout
        // NOTA: ILoginAttemptStore.RegisterSuccess acepta cualquier clave de bucket (hash de documento o profile:{id}).
        // Se usa el hash de documento del voter para limpiar el bucket del login.
        _loginAttemptStore.RegisterSuccess(voter.DocumentHash);

        // 5. Invalidar sesión activa (RN-14).
        // Razón: LoggedOut → redirect a /Auth/Login sin ?reason= → sin texto de error engañoso.
        // ReplacedByNewDevice → ?reason=device (incorrecto para restablecimiento).
        // AccountDisabledOrDeleted → ?reason=disabled (incorrecto y alarmante).
        _sessionStore.InvalidateSession(userId, SessionInvalidationReason.LoggedOut);

        // 6. Revocar cualquier token residual del usuario (por si acaso hubo reemisión concurrente)
        _tokenStore.Revoke(userId);

        // 7. Auditoría (RF-M01-02, RN-8)
        await _auditService.LogAsync(
            action: "PASSWORD_RESET_COMPLETED",
            voterId: (int)userId,
            targetEntity: "users",
            targetId: (int)userId,
            fieldName: "PasswordHash",
            oldValue: null,
            newValue: null,
            details: "{\"method\":\"reset_token\"}",
            ipAddress: ipAddress ?? "unknown"
        );

        return (true, string.Empty);
    }

    private string GenerateStrongPassword(int length)
    {
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()-=_+";
        
        var chars = new char[length];
        var random = RandomNumberGenerator.Create();
        
        chars[0] = lower[GetRandomInt(random, lower.Length)];
        chars[1] = upper[GetRandomInt(random, upper.Length)];
        chars[2] = digits[GetRandomInt(random, digits.Length)];
        chars[3] = special[GetRandomInt(random, special.Length)];

        const string all = lower + upper + digits + special;
        for (int i = 4; i < length; i++)
        {
            chars[i] = all[GetRandomInt(random, all.Length)];
        }

        return new string(chars.OrderBy(x => GetRandomInt(random, int.MaxValue)).ToArray());
    }

    private int GetRandomInt(RandomNumberGenerator random, int max)
    {
        var bytes = new byte[4];
        random.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0);
        return (int)(value % max);
    }
}

// ============================================================
// Archivo:       CredentialService.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Genera y almacena hashes de nuevas contraseñas,
//                encola avisos, conserva la clave temporal y registra auditoría.
// Requerimientos: RF-M01-02, RF-M07-02, RF-M09-01; apoyo para RN-8 y RN-9.
// ============================================================
using System.Security.Cryptography;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

/// <summary>
/// Implementa la emisión de contraseñas nuevas, su encolado para notificación y la auditoría asociada.
/// </summary>
public class CredentialService : ICredentialService
{
    private readonly WahlMiraiDbContext _dbContext;
    private readonly IPendingPasswordStore _passwordStore;
    private readonly IAuditService _auditService;
    private readonly IPasswordResetTokenStore? _tokenStore;

    /// <summary>Inicializa el servicio con el contexto, el almacén temporal, el servicio de auditoría y el almacén de tokens.</summary>
    /// <param name="dbContext">Contexto usado para actualizar la cuenta y crear la notificación pendiente.</param>
    /// <param name="passwordStore">Almacén temporal para asociar el secreto en claro a la notificación.</param>
    /// <param name="auditService">Servicio que registra la operación de emisión.</param>
    /// <param name="tokenStore">Almacén de tokens de recuperación de acceso de un solo uso.</param>
    public CredentialService(
        WahlMiraiDbContext dbContext,
        IPendingPasswordStore passwordStore,
        IAuditService auditService,
        IPasswordResetTokenStore? tokenStore = null)
    {
        _dbContext = dbContext;
        _passwordStore = passwordStore;
        _auditService = auditService;
        _tokenStore = tokenStore;
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

        if (_tokenStore == null)
        {
            throw new InvalidOperationException("IPasswordResetTokenStore no está configurado en CredentialService.");
        }

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

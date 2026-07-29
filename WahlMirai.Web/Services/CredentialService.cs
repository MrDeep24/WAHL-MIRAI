using System.Security.Cryptography;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class CredentialService : ICredentialService
{
    private readonly WahlMiraiDbContext _dbContext;
    private readonly IPendingPasswordStore _passwordStore;
    private readonly IAuditService _auditService;

    public CredentialService(WahlMiraiDbContext dbContext, IPendingPasswordStore passwordStore, IAuditService auditService)
    {
        _dbContext = dbContext;
        _passwordStore = passwordStore;
        _auditService = auditService;
    }

    public async Task IssueNewPasswordAsync(int voterId, EmailType emailType, int? actorVoterId, CancellationToken ct = default)
    {
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
            EmailType.CREDENCIAL_INICIAL => "PASSWORD_ASSIGNED_BULK",
            EmailType.RECUPERACION_ACCESO => "PASSWORD_RECOVERY_REQUESTED",
            EmailType.REASIGNACION_ADMIN => "PASSWORD_REASSIGNED",
            _ => "PASSWORD_RESET"
        };

        await _auditService.LogAsync(
            action: action,
            voterId: actorVoterId,
            targetEntity: "voters",
            targetId: voterId,
            fieldName: null,
            oldValue: null,
            newValue: null,
            details: $"{{\"email_type\":\"{emailType}\"}}",
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

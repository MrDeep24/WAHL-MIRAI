using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

// Moved here from AdminAccountServiceTests.cs and CensusWhitelistTests.cs - was duplicated identically in both.
internal sealed class FakeAuthService : IAuthService
{
    public Task<Voter?> ValidateLoginAsync(string document, string password, string? ipAddress) => Task.FromResult<Voter?>(null);
    public Task<bool> ChangePasswordAsync(int voterId, string newPassword, string ipAddress) => Task.FromResult(true);
    public string HashPassword(string password) => "password-hash";
    public string HashDocument(string document) => $"hash-{document}";
}

// Moved here from AdminAccountServiceTests.cs and CensusWhitelistTests.cs - was duplicated identically in both.
internal sealed class FakeEncryptionService : IDocumentEncryptionService
{
    public string Encrypt(string plainText) => $"enc-{plainText}";
    public string Decrypt(string cipherText) => cipherText.Replace("enc-", string.Empty);
}

// Moved here from AdminAccountServiceTests.cs and CensusWhitelistTests.cs - was duplicated identically in both.
internal sealed class FakeAuditService : IAuditService
{
    public List<string> Actions { get; } = new();
    public List<string?> IpAddresses { get; } = new();

    public Task LogAsync(string action, int? voterId, string targetEntity, int? targetId, string? fieldName, string? oldValue, string? newValue, string? details, string? ipAddress)
    {
        Actions.Add(action);
        IpAddresses.Add(ipAddress);
        return Task.CompletedTask;
    }
}

// Añadido para ProfileServiceTests: simula el almacén de intentos fallidos (RN-15)
// delegando en LoginAttemptStore con política inyectable para controlar el umbral en tests.
internal sealed class FakeLoginAttemptStore : ILoginAttemptStore
{
    private readonly LoginAttemptStore _inner;

    /// <param name="threshold">Número de fallos antes del bloqueo (default: 5, igual que producción).</param>
    public FakeLoginAttemptStore(int threshold = 5)
    {
        var policy = new LoginAttemptPolicy
        {
            LockoutThreshold = threshold,
            WindowDuration   = TimeSpan.FromMinutes(15)
        };
        _inner = new LoginAttemptStore(TimeProvider.System, policy);
    }

    public LoginAttemptResult RegisterFailure(string key)          => _inner.RegisterFailure(key);
    public void RegisterSuccess(string key)                         => _inner.RegisterSuccess(key);
    public bool IsLockedOut(string key, out TimeSpan remaining)     => _inner.IsLockedOut(key, out remaining);
}

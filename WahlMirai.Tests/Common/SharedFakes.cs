using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

// Moved here from AdminAccountServiceTests.cs and CensusWhitelistTests.cs - was duplicated identically in both.
internal sealed class FakeAuthService : IAuthService
{
    public Task<Voter?> ValidateLoginAsync(string document, string password) => Task.FromResult<Voter?>(null);
    public Task<string> GenerateInitialPasswordAsync(string document) => Task.FromResult("Pass123!");
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

    public Task LogAsync(string action, int? voterId, string targetEntity, int? targetId, string? fieldName, string? oldValue, string? newValue, string? details, string? ipAddress)
    {
        Actions.Add(action);
        return Task.CompletedTask;
    }
}

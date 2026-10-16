// ============================================================
// Archivo:       PasswordResetTokenStoreTests.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias de emisión, inspección, consumo,
//                expiración determinista y concurrencia de PasswordResetTokenStore.
// Requerimientos: RF-M01-02, RN-2, RN-14, RN-15.
// ============================================================
using Microsoft.Extensions.Caching.Memory;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias del almacén en memoria de tokens de restablecimiento (RF-M01-02, RN-14, RN-15).
/// </summary>
public class PasswordResetTokenStoreTests
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly DateTimeOffset _baseTime = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_PeekAndConsume_SingleUseWorksAsExpected()
    {
        var fakeTime = new FakeTimeProvider(_baseTime);
        var store = new PasswordResetTokenStore(_cache, fakeTime);
        const uint userId = 42;

        var token = store.Issue(userId);
        Assert.False(string.IsNullOrWhiteSpace(token));

        // Peek no consume el token
        Assert.True(store.TryPeek(token, out var peekUser1));
        Assert.Equal(userId, peekUser1);
        Assert.True(store.TryPeek(token, out var peekUser2));
        Assert.Equal(userId, peekUser2);

        // Primer consumo exitoso
        Assert.True(store.TryConsume(token, out var consumedUser));
        Assert.Equal(userId, consumedUser);

        // Segundo intento de consumo falla (single-use)
        Assert.False(store.TryConsume(token, out var secondConsumeUser));
        Assert.Equal(0u, secondConsumeUser);

        // Peek ya no lo encuentra
        Assert.False(store.TryPeek(token, out _));
    }

    [Fact]
    public void Issue_SecondIssueForSameUser_InvalidatesFirstToken()
    {
        var fakeTime = new FakeTimeProvider(_baseTime);
        var store = new PasswordResetTokenStore(_cache, fakeTime);
        const uint userId = 7;

        var token1 = store.Issue(userId);
        var token2 = store.Issue(userId);

        Assert.NotEqual(token1, token2);

        // El primer token queda invalidado de inmediato (RN-14)
        Assert.False(store.TryPeek(token1, out _));
        Assert.False(store.TryConsume(token1, out _));

        // El segundo token está vigente
        Assert.True(store.TryPeek(token2, out var activeUser));
        Assert.Equal(userId, activeUser);
        Assert.True(store.TryConsume(token2, out var consumedUser));
        Assert.Equal(userId, consumedUser);
    }

    [Fact]
    public void TryPeekAndConsume_ExpiredToken_ReturnsFalse()
    {
        var fakeTime = new FakeTimeProvider(_baseTime);
        var store = new PasswordResetTokenStore(_cache, fakeTime);
        const uint userId = 10;

        var token = store.Issue(userId);

        // Antes de la expiración (29 minutos)
        fakeTime.Advance(TimeSpan.FromMinutes(29));
        Assert.True(store.TryPeek(token, out _));

        // Expirado a los 31 minutos (TTL = 30 min)
        fakeTime.Advance(TimeSpan.FromMinutes(2));
        Assert.False(store.TryPeek(token, out var peekExpired));
        Assert.Equal(0u, peekExpired);
        Assert.False(store.TryConsume(token, out var consumeExpired));
        Assert.Equal(0u, consumeExpired);
    }

    [Fact]
    public void Revoke_RemovesActiveTokenForUser()
    {
        var fakeTime = new FakeTimeProvider(_baseTime);
        var store = new PasswordResetTokenStore(_cache, fakeTime);
        const uint userId = 15;

        var token = store.Issue(userId);
        Assert.True(store.TryPeek(token, out _));

        store.Revoke(userId);

        Assert.False(store.TryPeek(token, out _));
        Assert.False(store.TryConsume(token, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("token-inexistente-12345")]
    public void TryPeekAndConsume_InvalidOrUnknownTokens_ReturnFalse(string? token)
    {
        var fakeTime = new FakeTimeProvider(_baseTime);
        var store = new PasswordResetTokenStore(_cache, fakeTime);

        Assert.False(store.TryPeek(token!, out var peekUser));
        Assert.Equal(0u, peekUser);

        Assert.False(store.TryConsume(token!, out var consumeUser));
        Assert.Equal(0u, consumeUser);
    }

    [Fact]
    public async Task TryConsume_ConcurrentAttempts_OnlyOneWinner()
    {
        var fakeTime = new FakeTimeProvider(_baseTime);
        var store = new PasswordResetTokenStore(_cache, fakeTime);
        const uint userId = 99;

        var token = store.Issue(userId);

        const int concurrency = 20;
        var tasks = new Task<bool>[concurrency];

        for (int i = 0; i < concurrency; i++)
        {
            tasks[i] = Task.Run(() => store.TryConsume(token, out _));
        }

        var results = await Task.WhenAll(tasks);
        var successCount = results.Count(success => success);

        Assert.Equal(1, successCount);
    }
}

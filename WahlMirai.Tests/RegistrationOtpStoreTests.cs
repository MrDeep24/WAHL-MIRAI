// ============================================================
// Archivo:       RegistrationOtpStoreTests.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias de RegistrationOtpStore verificando
//                cooldown de 60s (A1), emisión de señuelos decoy (A2),
//                límite de 5 intentos, SHA-256 y consumo de un solo uso.
// Requerimientos: RF-M01-00, RN-1, RN-1.1, RN-15.
// ============================================================
using Microsoft.Extensions.Caching.Memory;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

public sealed class RegistrationOtpStoreTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly RegistrationOtpStore _store;

    public RegistrationOtpStoreTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));
        var cache = new MemoryCache(new MemoryCacheOptions());
        _store = new RegistrationOtpStore(cache, _timeProvider);
    }

    [Fact]
    public void IssueRegistrationTicket_ValidEntry_ReturnsTicketAndOtp()
    {
        var result = _store.IssueRegistrationTicket("12345678", 1, "student@example.com", "dochash");

        Assert.False(result.IsDecoy);
        Assert.False(result.IsCooldownActive);
        Assert.NotNull(result.Ticket);
        Assert.NotNull(result.RawOtp);
        Assert.Equal(6, result.RawOtp.Length);
        Assert.True(int.TryParse(result.RawOtp, out _));
        Assert.Equal("student@example.com", result.ContactEmail);
    }

    [Fact]
    public void IssueRegistrationTicket_Decoy_ReturnsDecoyResultWithoutOtp()
    {
        var result = _store.IssueRegistrationTicket("99999999", null, null, "dochash");

        Assert.True(result.IsDecoy);
        Assert.False(result.IsCooldownActive);
        Assert.NotNull(result.Ticket);
        Assert.Null(result.RawOtp);
    }

    [Fact]
    public void IssueRegistrationTicket_Enforces60sCooldown_PerEntry()
    {
        var first = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash");
        Assert.False(first.IsCooldownActive);
        Assert.NotNull(first.RawOtp);

        // Intento dentro del periodo de 60 segundos (A1)
        _timeProvider.Advance(TimeSpan.FromSeconds(30));
        var second = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash");
        Assert.True(second.IsCooldownActive);
        Assert.Null(second.RawOtp);

        // Luego de superar los 60 segundos, emite nuevo código
        _timeProvider.Advance(TimeSpan.FromSeconds(35));
        var third = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash");
        Assert.False(third.IsCooldownActive);
        Assert.NotNull(third.RawOtp);
    }

    [Fact]
    public void TryValidateOtp_ValidCode_VerifiesTicketAndAllowsSingleConsumption()
    {
        var issued = _store.IssueRegistrationTicket("12345678", 7, "student@example.com", "dochash");
        Assert.NotNull(issued.RawOtp);

        var valid = _store.TryValidateOtp(issued.Ticket, issued.RawOtp, out var error, out var isLockoutEligible);
        Assert.True(valid);
        Assert.Null(error);
        Assert.False(isLockoutEligible);

        // El ticket queda verificado pero aún no consumido
        Assert.True(_store.TryGetVerifiedTicket(issued.Ticket, out var whitelistId, out var document));
        Assert.Equal(7u, whitelistId);
        Assert.Equal("12345678", document);

        // Primer consumo atómico tiene éxito
        Assert.True(_store.TryConsumeVerifiedTicket(issued.Ticket, out var consumedId, out var consumedDoc));
        Assert.Equal(7u, consumedId);
        Assert.Equal("12345678", consumedDoc);

        // Segundo consumo falla (garantía de un solo uso)
        Assert.False(_store.TryConsumeVerifiedTicket(issued.Ticket, out _, out _));
        Assert.False(_store.TryGetVerifiedTicket(issued.Ticket, out _, out _));
    }

    [Fact]
    public void TryValidateOtp_DecoyTicket_AlwaysFails()
    {
        var decoy = _store.IssueRegistrationTicket("99999999", null, null, "dochash");

        var valid = _store.TryValidateOtp(decoy.Ticket, "123456", out var error, out _);
        Assert.False(valid);
        Assert.NotNull(error);

        // Ticket señuelo nunca puede ser verificado ni consumido (A2)
        Assert.False(_store.TryGetVerifiedTicket(decoy.Ticket, out _, out _));
        Assert.False(_store.TryConsumeVerifiedTicket(decoy.Ticket, out _, out _));
    }

    [Fact]
    public void TryValidateOtp_InvalidCode_FailsAndEnforcesAttemptLimit()
    {
        var issued = _store.IssueRegistrationTicket("12345678", 12, "student@example.com", "dochash");

        for (var i = 0; i < 4; i++)
        {
            var valid = _store.TryValidateOtp(issued.Ticket, "000000", out var error, out var isLockoutEligible);
            Assert.False(valid);
            Assert.True(isLockoutEligible);
            Assert.Contains("Intentos restantes", error);
        }

        // Intento 5 falla y agota el límite
        var finalAttempt = _store.TryValidateOtp(issued.Ticket, "000000", out var finalError, out var finalLockoutEligible);
        Assert.False(finalAttempt);
        Assert.True(finalLockoutEligible);
        Assert.Contains("Demasiados intentos fallidos", finalError);

        // Intento posterior con el código correcto ahora es rechazado
        var retryWithCorrect = _store.TryValidateOtp(issued.Ticket, issued.RawOtp!, out _, out _);
        Assert.False(retryWithCorrect);
    }

    [Fact]
    public void ResendOtp_EnforcesCooldown_AndReissuesAfterExpiration()
    {
        var issued = _store.IssueRegistrationTicket("12345678", 20, "student@example.com", "dochash");

        // Reenvío inmediato está en cooldown (A1)
        var immediateResend = _store.ResendOtp(issued.Ticket);
        Assert.False(immediateResend.Success);
        Assert.True(immediateResend.IsCooldownActive);
        Assert.Null(immediateResend.RawOtp);

        // Avanzar más allá de 60 segundos
        _timeProvider.Advance(TimeSpan.FromSeconds(65));
        var validResend = _store.ResendOtp(issued.Ticket);
        Assert.True(validResend.Success);
        Assert.False(validResend.IsCooldownActive);
        Assert.NotNull(validResend.RawOtp);

        // Nuevo código funciona
        var valid = _store.TryValidateOtp(issued.Ticket, validResend.RawOtp, out _, out _);
        Assert.True(valid);
    }
}

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

        // Intento dentro de los 60 segundos sin presentar la cookie válida (ajena/anónima):
        // NUNCA entrega el ticket existente; emite señuelo nuevo sin correo (A, B).
        _timeProvider.Advance(TimeSpan.FromSeconds(30));
        var second = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash");
        Assert.True(second.IsCooldownActive);
        Assert.True(second.IsDecoy);
        Assert.NotEqual(first.Ticket, second.Ticket);
        Assert.Null(second.RawOtp);
        Assert.Null(second.ContactEmail);

        // Intento dentro del cooldown presentando la cookie válida de esa entrada: debe conservarla
        var thirdWithCookie = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash", currentTicket: first.Ticket);
        Assert.True(thirdWithCookie.IsCooldownActive);
        Assert.False(thirdWithCookie.IsDecoy);
        Assert.Equal(first.Ticket, thirdWithCookie.Ticket);
        Assert.Null(thirdWithCookie.RawOtp);
        Assert.Equal("student@example.com", thirdWithCookie.ContactEmail);

        // Luego de superar los 60 segundos, emite nuevo código y revoca el ticket previo
        _timeProvider.Advance(TimeSpan.FromSeconds(35));
        var fourth = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash");
        Assert.False(fourth.IsCooldownActive);
        Assert.False(fourth.IsDecoy);
        Assert.NotNull(fourth.RawOtp);
        Assert.NotEqual(first.Ticket, fourth.Ticket);

        // El ticket original first.Ticket fue revocado tras el cooldown
        Assert.False(_store.TryGetVerifiedTicket(first.Ticket, out _, out _));
        Assert.False(_store.TryValidateOtp(first.Ticket, first.RawOtp!, out _, out _));
    }

    [Fact]
    public void IssueRegistrationTicket_IndistinguishableRealVsDecoy_WithinCooldown()
    {
        // 1. Entrada real que entra en cooldown
        var realFirst = _store.IssueRegistrationTicket("12345678", 33, "real@colegio.edu.co", "dochash_real");
        Assert.False(realFirst.IsCooldownActive);

        // 2. Petición a la entrada real dentro del cooldown sin cookie previa (atacante)
        _timeProvider.Advance(TimeSpan.FromSeconds(15));
        var realWithinCooldown = _store.IssueRegistrationTicket("12345678", 33, "real@colegio.edu.co", "dochash_real", currentTicket: null);

        // 3. Petición a una entrada inexistente / señuelo puro (decoy)
        var pureDecoy = _store.IssueRegistrationTicket("99999999", null, null, "dochash_fake", currentTicket: null);

        // Ambos emiten un token opaco de 64 hex, sin OTP y sin correo (indistinguibles externamente)
        Assert.NotNull(realWithinCooldown.Ticket);
        Assert.Equal(64, realWithinCooldown.Ticket.Length);
        Assert.Null(realWithinCooldown.RawOtp);
        Assert.Null(realWithinCooldown.ContactEmail);
        Assert.True(realWithinCooldown.IsDecoy);

        Assert.NotNull(pureDecoy.Ticket);
        Assert.Equal(64, pureDecoy.Ticket.Length);
        Assert.Null(pureDecoy.RawOtp);
        Assert.Null(pureDecoy.ContactEmail);
        Assert.True(pureDecoy.IsDecoy);

        // Ambos tickets fallan de forma indistinguible ante TryValidateOtp
        Assert.False(_store.TryValidateOtp(realWithinCooldown.Ticket, "123456", out var errReal, out _));
        Assert.False(_store.TryValidateOtp(pureDecoy.Ticket, "123456", out var errDecoy, out _));
        Assert.Equal("El código de verificación no es válido.", errReal);
        Assert.Equal("El código de verificación no es válido.", errDecoy);
    }

    [Fact]
    public void IssueRegistrationTicket_AfterOtpVerificationAndCooldownExpired_RevokesOldTicketAndIssuesNewUnverifiedTicket()
    {
        var issued = _store.IssueRegistrationTicket("12345678", 50, "student@example.com", "dochash");
        Assert.NotNull(issued.RawOtp);

        // Validar el código OTP: el ticket pasa a verificado
        var isValid = _store.TryValidateOtp(issued.Ticket, issued.RawOtp, out _, out _);
        Assert.True(isValid);
        Assert.True(_store.TryGetVerifiedTicket(issued.Ticket, out var verifiedId, out _));
        Assert.Equal(50u, verifiedId);

        // El usuario no completa la cuenta y pasan 65 segundos (expira cooldown)
        _timeProvider.Advance(TimeSpan.FromSeconds(65));

        // Vuelve a POST /Auth/Registro con el mismo documento
        var reissued = _store.IssueRegistrationTicket("12345678", 50, "student@example.com", "dochash");
        Assert.False(reissued.IsCooldownActive);
        Assert.NotNull(reissued.RawOtp);
        Assert.NotEqual(issued.Ticket, reissued.Ticket);

        // El ticket viejo quedó revocado (ya no puede usarse para completar)
        Assert.False(_store.TryGetVerifiedTicket(issued.Ticket, out _, out _));
        // El nuevo ticket aún NO está verificado
        Assert.False(_store.TryGetVerifiedTicket(reissued.Ticket, out _, out _));
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
    public void TryValidateOtp_AfterTtlExpiration_Fails()
    {
        var issued = _store.IssueRegistrationTicket("12345678", 80, "student@example.com", "dochash");
        _timeProvider.Advance(TimeSpan.FromMinutes(16)); // Excede TTL de 15 minutos

        var valid = _store.TryValidateOtp(issued.Ticket, issued.RawOtp!, out var error, out var isLockoutEligible);
        Assert.False(valid);
        Assert.True(isLockoutEligible);
        Assert.Contains("ha expirado", error);
        Assert.False(_store.TryGetVerifiedTicket(issued.Ticket, out _, out _));
        Assert.False(_store.TryConsumeVerifiedTicket(issued.Ticket, out _, out _));
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

    [Fact]
    public void ResendOtp_WhenAlreadyVerified_ReturnsError()
    {
        var issued = _store.IssueRegistrationTicket("12345678", 60, "student@example.com", "dochash");
        Assert.True(_store.TryValidateOtp(issued.Ticket, issued.RawOtp!, out _, out _));

        _timeProvider.Advance(TimeSpan.FromSeconds(65));
        var resend = _store.ResendOtp(issued.Ticket);
        Assert.False(resend.Success);
        Assert.Contains("ya ha sido verificado", resend.Message);
    }

    [Fact]
    public void ResendOtp_ChecksWhitelistCooldown_ConsistentWithEntry()
    {
        var issued = _store.IssueRegistrationTicket("12345678", 70, "student@example.com", "dochash");

        // Reenvío a los 20 segundos está en cooldown
        _timeProvider.Advance(TimeSpan.FromSeconds(20));
        var resend = _store.ResendOtp(issued.Ticket);
        Assert.False(resend.Success);
        Assert.True(resend.IsCooldownActive);
        Assert.Equal(40, resend.CooldownRemainingSeconds);
    }

    [Theory]
    [InlineData("carlos.santana@colegio.edu.co", "c***@c***.co")]
    [InlineData("a@dominio.com", "a@d***.com")]
    [InlineData("user@sub.domain.org", "u***@s***.org")]
    [InlineData(null, "***")]
    [InlineData("", "***")]
    [InlineData("invalido", "***")]
    public void MaskEmail_VariousFormats_MasksCorrectly(string? input, string expected)
    {
        var masked = _store.MaskEmail(input!);
        Assert.Equal(expected, masked);
    }

    [Fact]
    public void Revoke_RemovesTicketAndCooldownFromCache()
    {
        var issued = _store.IssueRegistrationTicket("12345678", 90, "student@example.com", "dochash");
        Assert.False(issued.IsCooldownActive);

        // Revocar el ticket
        _store.Revoke(issued.Ticket);

        // El ticket ya no existe
        Assert.False(_store.TryValidateOtp(issued.Ticket, issued.RawOtp!, out _, out _));

        // El cooldown fue removido, permitiendo emitir de inmediato sin esperar 60s
        var next = _store.IssueRegistrationTicket("12345678", 90, "student@example.com", "dochash");
        Assert.False(next.IsCooldownActive);
        Assert.NotNull(next.RawOtp);
    }
}

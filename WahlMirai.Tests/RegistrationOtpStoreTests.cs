// ============================================================
// Archivo:       RegistrationOtpStoreTests.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias de RegistrationOtpStore verificando
//                cooldown de 60s (A1), enmascaramiento parcial,
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
        var result = _store.IssueRegistrationTicket("12345678", 1, "student@example.com", "dochash", fullName: "Carlos Santana");

        Assert.False(result.IsCooldownActive);
        Assert.NotNull(result.Ticket);
        Assert.NotNull(result.RawOtp);
        Assert.Equal(6, result.RawOtp.Length);
        Assert.True(int.TryParse(result.RawOtp, out _));
        Assert.Equal("student@example.com", result.ContactEmail);
    }

    [Fact]
    public void IssueRegistrationTicket_InvalidArguments_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _store.IssueRegistrationTicket("12345678", 0, "student@example.com", "dochash"));

        Assert.Throws<ArgumentException>(() =>
            _store.IssueRegistrationTicket("12345678", 1, "", "dochash"));
    }

    [Fact]
    public void IssueRegistrationTicket_Enforces60sCooldown_PerEntry()
    {
        var first = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash");
        Assert.False(first.IsCooldownActive);
        Assert.NotNull(first.RawOtp);

        // Intento dentro de los 60 segundos sin presentar la cookie válida (ajena/anónima):
        // NO emite nuevo ticket, NO reemite OTP, NO revoca el ticket previo y reporta cooldown activo.
        _timeProvider.Advance(TimeSpan.FromSeconds(30));
        var second = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash");
        Assert.True(second.IsCooldownActive);
        Assert.False(second.HasValidSessionForEntry);
        Assert.Equal(string.Empty, second.Ticket);
        Assert.Null(second.RawOtp);
        Assert.Null(second.ContactEmail);
        Assert.Equal(30, second.CooldownRemainingSeconds);

        // Intento dentro del cooldown presentando la cookie válida de esa entrada: conserva la sesión
        var thirdWithCookie = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash", currentTicket: first.Ticket);
        Assert.True(thirdWithCookie.IsCooldownActive);
        Assert.True(thirdWithCookie.HasValidSessionForEntry);
        Assert.Equal(first.Ticket, thirdWithCookie.Ticket);
        Assert.Null(thirdWithCookie.RawOtp);
        Assert.Equal("student@example.com", thirdWithCookie.ContactEmail);
        Assert.Equal(30, thirdWithCookie.CooldownRemainingSeconds);

        // Luego de superar los 60 segundos, emite nuevo código y revoca el ticket previo
        _timeProvider.Advance(TimeSpan.FromSeconds(35));
        var fourth = _store.IssueRegistrationTicket("12345678", 5, "student@example.com", "dochash");
        Assert.False(fourth.IsCooldownActive);
        Assert.NotNull(fourth.RawOtp);
        Assert.NotEqual(first.Ticket, fourth.Ticket);

        // El ticket original first.Ticket fue revocado tras el cooldown
        Assert.False(_store.TryGetVerifiedTicket(first.Ticket, out _, out _));
        Assert.False(_store.TryValidateOtp(first.Ticket, first.RawOtp!, out _, out _));
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

    [Fact]
    public void TryGetTicketDisplayInfo_ValidTicket_ReturnsPrecomputedMaskedNameAndEmail()
    {
        var issued = _store.IssueRegistrationTicket(
            "12345678",
            42,
            "carlos.santana@colegio.edu.co",
            "dochash",
            fullName: "Carlos Santana");

        var found = _store.TryGetTicketDisplayInfo(issued.Ticket, out var maskedName, out var maskedEmail);

        Assert.True(found);
        Assert.Equal("Ca*** Sa***", maskedName);
        Assert.Equal("c***@c***.co", maskedEmail);
    }

    [Fact]
    public void TryGetTicketDisplayInfo_MissingOrExpiredTicket_ReturnsFalse()
    {
        var missingFound = _store.TryGetTicketDisplayInfo("nonexistent_ticket", out var missingName, out var missingEmail);
        Assert.False(missingFound);
        Assert.Equal(string.Empty, missingName);
        Assert.Equal(string.Empty, missingEmail);

        var issued = _store.IssueRegistrationTicket("12345678", 43, "test@example.com", "dochash", fullName: "Kevin");
        _timeProvider.Advance(TimeSpan.FromMinutes(16)); // Excede TTL de 15 minutos

        var expiredFound = _store.TryGetTicketDisplayInfo(issued.Ticket, out var expName, out var expEmail);
        Assert.False(expiredFound);
        Assert.Equal(string.Empty, expName);
        Assert.Equal(string.Empty, expEmail);
    }

    [Theory]
    [InlineData("Kevin López", "Ke*** Ló***")]
    [InlineData("Ana Y Carlos", "A*** Y*** Ca***")]
    [InlineData("Ángel Peña", "Án*** Pe***")]
    [InlineData("  Kevin   David   López  ", "Ke*** Da*** Ló***")]
    [InlineData("De Los Ríos", "D*** L*** Rí***")]
    [InlineData("María De Los Ángeles Pérez Gómez", "Ma*** D*** L*** Án*** Pé*** Gó***")]
    [InlineData(null, "***")]
    [InlineData("", "***")]
    [InlineData("   ", "***")]
    public void MaskFullName_VariousFormats_MasksCorrectly(string? input, string expected)
    {
        var masked = _store.MaskFullName(input);
        Assert.Equal(expected, masked);
    }

    [Theory]
    [InlineData("carlos.santana@colegio.edu.co", "c***@c***.co")]
    [InlineData("ab@colegio.edu.co", "a***@c***.co")]
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

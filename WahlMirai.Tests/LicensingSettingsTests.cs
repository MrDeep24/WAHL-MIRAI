using WahlMirai.Web.Models;
using Xunit;

namespace WahlMirai.Tests;

public class LicensingSettingsTests
{
    [Fact]
    public void DefaultConfiguration_HasValidHttpsUrl_AndSafeSourceCodeUrlReturnsIt()
    {
        var settings = new LicensingSettings();

        Assert.Equal("https://github.com/MrDeep24/WAHL-MIRAI", settings.SourceCodeUrl);
        Assert.Equal("https://github.com/MrDeep24/WAHL-MIRAI", settings.SafeSourceCodeUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void SafeSourceCodeUrl_NullEmptyOrWhitespace_ReturnsNull(string? rawUrl)
    {
        var settings = new LicensingSettings { SourceCodeUrl = rawUrl };

        Assert.Null(settings.SafeSourceCodeUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JAVASCRIPT:alert(document.cookie)")]
    [InlineData("ftp://ftp.example.com/repo")]
    [InlineData("file:///etc/passwd")]
    [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
    [InlineData("data:text/plain;charset=utf-8,hello")]
    [InlineData("mailto:admin@example.com")]
    [InlineData("tel:+1234567890")]
    public void SafeSourceCodeUrl_UnsafeOrDisallowedSchemes_ReturnsNull(string rawUrl)
    {
        var settings = new LicensingSettings { SourceCodeUrl = rawUrl };

        Assert.Null(settings.SafeSourceCodeUrl);
    }

    [Theory]
    [InlineData("/source/code")]
    [InlineData("relative/path/to/repo")]
    [InlineData("../parent/repo")]
    [InlineData("git@github.com:MrDeep24/WAHL-MIRAI.git")]
    [InlineData("not a url at all")]
    [InlineData("://missing-scheme")]
    public void SafeSourceCodeUrl_RelativeOrMalformedUrls_ReturnsNull(string rawUrl)
    {
        var settings = new LicensingSettings { SourceCodeUrl = rawUrl };

        Assert.Null(settings.SafeSourceCodeUrl);
    }

    [Fact]
    public void SafeSourceCodeUrl_ValidHttpUrl_ReturnsCanonicalUrl()
    {
        var settings = new LicensingSettings { SourceCodeUrl = "http://example.com/source" };

        Assert.Equal("http://example.com/source", settings.SafeSourceCodeUrl);
    }

    [Fact]
    public void SafeSourceCodeUrl_ValidHttpsUrl_ReturnsCanonicalUrl()
    {
        var settings = new LicensingSettings { SourceCodeUrl = "https://github.com/MrDeep24/WAHL-MIRAI" };

        Assert.Equal("https://github.com/MrDeep24/WAHL-MIRAI", settings.SafeSourceCodeUrl);
    }

    [Fact]
    public void SafeSourceCodeUrl_ValidUrlWithSurroundingWhitespace_TrimsAndAccepts()
    {
        // En la implementación actual, SourceCodeUrl.Trim() recorta espacios iniciales y finales
        // antes de invocar Uri.TryCreate, por lo que NO se rechaza sino que se normaliza recortado.
        const string rawWithSpaces = "   https://github.com/MrDeep24/WAHL-MIRAI   \r\n";
        var settings = new LicensingSettings { SourceCodeUrl = rawWithSpaces };

        Assert.Equal("https://github.com/MrDeep24/WAHL-MIRAI", settings.SafeSourceCodeUrl);
    }

    [Fact]
    public void SafeSourceCodeUrl_ValidHttpUrlWithSurroundingWhitespace_TrimsAndAccepts()
    {
        const string rawWithSpaces = " \t http://example.com/source/code \r\n ";
        var settings = new LicensingSettings { SourceCodeUrl = rawWithSpaces };

        Assert.Equal("http://example.com/source/code", settings.SafeSourceCodeUrl);
    }

    [Fact]
    public void SafeSourceCodeUrl_ValidUrlWithPortAndQuery_ReturnsCanonicalUrl()
    {
        const string rawUrl = "https://git.example.internal:8443/org/repo?ref=main#readme";
        var settings = new LicensingSettings { SourceCodeUrl = rawUrl };

        Assert.Equal("https://git.example.internal:8443/org/repo?ref=main#readme", settings.SafeSourceCodeUrl);
    }

    [Fact]
    public void SafeSourceCodeUrl_FailOpenBehavior_NeverThrowsOnArbitraryInputs()
    {
        // Fail-open: ante cualquier valor inesperado, SafeSourceCodeUrl retorna null sin lanzar
        // excepciones, permitiendo a las vistas y al arranque omitir el enlace de forma segura.
        var maliciousInputs = new[]
        {
            "javascript://alert(1)",
            "https://",
            "http://",
            new string('a', 10000),
            "\0https://evil.com",
            "http://[:::1]"
        };

        foreach (var input in maliciousInputs)
        {
            var settings = new LicensingSettings { SourceCodeUrl = input };
            var exception = Record.Exception(() => _ = settings.SafeSourceCodeUrl);

            Assert.Null(exception);
            // Si no fue una URL válida, retorna null
            if (settings.SafeSourceCodeUrl != null)
            {
                Assert.True(
                    settings.SafeSourceCodeUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    settings.SafeSourceCodeUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}

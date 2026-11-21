using System;
using System.Data.Common;
using System.Net.Sockets;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class StartupDiagnosticsTests
{
    private class FakeDbException : DbException
    {
        public int? Number { get; }

        public FakeDbException(string message, int errorCode, int? number = null)
            : base(message, errorCode)
        {
            Number = number ?? errorCode;
        }
    }

    [Fact]
    public void Describe_MissingConnectionString_ReturnsClearMessageAndAction()
    {
        // Arrange
        var ex = new DatabaseConfigurationException("La cadena de conexión 'ConnectionStrings:WahlMiraiDb' no está configurada o está vacía.");

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("ConnectionStrings:WahlMiraiDb", reason);
        Assert.Contains("vacía", reason);
        Assert.Contains("appsettings.Development.json", action);
    }

    [Fact]
    public void Describe_InvalidServerConfiguration_ReturnsClearMessageAndAction()
    {
        // Arrange
        var ex = new DatabaseConfigurationException("El tipo de servidor de base de datos 'Postgres' no es válido. Los valores soportados son 'MySql' y 'MariaDb'.");

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("Database:ServerType", reason);
        Assert.Contains("MySql", action);
        Assert.Contains("MariaDb", action);
        Assert.Contains("8.0.36", action);
    }

    [Fact]
    public void Describe_CredentialsRejected_Error1045_IdentifiedAccurately()
    {
        // Arrange
        var ex = new FakeDbException("Access denied for user 'root'@'localhost' (using password: YES)", 1045);

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("1045", reason);
        Assert.Contains("credenciales", reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("usuario y la contraseña", action, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Describe_DatabaseNotFound_Error1049_ReferencesV32Script()
    {
        // Arrange
        var ex = new FakeDbException("Unknown database 'wahl_mirai_db'", 1049);

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("1049", reason);
        Assert.Contains("no existe", reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/wahl_mirai_db_v3_2_completo.sql", action);
    }

    [Fact]
    public void Describe_TablesNotFound_Error1146_ReferencesV32Script()
    {
        // Arrange
        var ex = new FakeDbException("Table 'wahl_mirai_db.voters' doesn't exist", 1146);

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("1146", reason);
        Assert.Contains("tablas requeridas no existen", reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/wahl_mirai_db_v3_2_completo.sql", action);
    }

    [Fact]
    public void Describe_UninitializedConnectionString_ClassifiedAsMissingConnectionString()
    {
        // Arrange
        var ex = new InvalidOperationException("The ConnectionString property has not been initialized.");

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("ConnectionStrings:WahlMiraiDb", reason);
        Assert.Contains("vacía", reason);
        Assert.Contains("appsettings.Development.json", action);
    }

    [Fact]
    public void Describe_ServerUnreachable_Error2003_SuggestsStartingMySql()
    {
        // Arrange
        var ex = new FakeDbException("Unable to connect to any of the specified MySQL hosts.", 2003);

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("no se pudo conectar", reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("XAMPP", action);
        Assert.Contains("Start", action);
    }

    [Fact]
    public void Describe_SocketException_ClassifiedAsServerUnreachable()
    {
        // Arrange
        var inner = new SocketException((int)SocketError.ConnectionRefused);
        var ex = new InvalidOperationException("An error occurred while opening the connection", inner);

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("no se pudo conectar", reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("XAMPP", action);
    }

    [Fact]
    public void Describe_TimeoutException_ClassifiedAsServerUnreachable()
    {
        // Arrange
        var ex = new TimeoutException("The connection attempt timed out.");

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("no se pudo conectar", reason, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("XAMPP", action);
    }

    [Fact]
    public void Describe_GenericFallbackException_SanitizesMessageAndProvidesGuidance()
    {
        // Arrange
        var ex = new InvalidOperationException("Unexpected internal engine fault");

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.Contains("InvalidOperationException", reason);
        Assert.Contains("Unexpected internal engine fault", reason);
        Assert.Contains("logs", action);
    }

    [Fact]
    public void Describe_NeverLeaksConnectionStringOrPasswords()
    {
        // Arrange
        const string sensitivePassword = "UltraSecretPassword999!";
        var ex = new InvalidOperationException(
            $"Error connecting with connection string: Server=127.0.0.1;Port=3306;Database=secret;User Id=dbadmin;Password={sensitivePassword};");

        // Act
        var (reason, action) = StartupDiagnostics.Describe(ex);

        // Assert
        Assert.DoesNotContain(sensitivePassword, reason);
        Assert.DoesNotContain(sensitivePassword, action);
        Assert.DoesNotContain("User Id=dbadmin", reason);
    }

    [Fact]
    public void FormatMessage_MatchesRequiredConsoleBoxStyle()
    {
        // Arrange
        const string reason = "no se pudo conectar con el servidor de base de datos.";
        const string action = "verifica que MySQL esté iniciado.";

        // Act
        var formatted = StartupDiagnostics.FormatMessage(reason, action);

        // Assert
        Assert.Contains("==================================================", formatted);
        Assert.Contains("Wahl Mirai no pudo iniciar", formatted);
        Assert.Contains($"Motivo: {reason}", formatted);
        Assert.Contains($"Qué hacer: {action}", formatted);
    }

    [Fact]
    public void ReportAndExit_CallsExitActionWithExitCode1()
    {
        // Arrange
        var ex = new TimeoutException("Connection timed out");
        int? capturedCode = null;

        // Act
        StartupDiagnostics.ReportAndExit(ex, code => capturedCode = code);

        // Assert
        Assert.Equal(1, capturedCode);
    }

    [Fact]
    public void IsDatabaseConnectionOrConfigException_ClassifiesCorrectly()
    {
        // Database related exceptions: must return true
        Assert.True(StartupDiagnostics.IsDatabaseConnectionOrConfigException(
            new DatabaseConfigurationException("Missing connection string")));
        Assert.True(StartupDiagnostics.IsDatabaseConnectionOrConfigException(
            new SocketException((int)SocketError.ConnectionRefused)));
        Assert.True(StartupDiagnostics.IsDatabaseConnectionOrConfigException(
            new TimeoutException("Connection timeout")));
        Assert.True(StartupDiagnostics.IsDatabaseConnectionOrConfigException(
            new FakeDbException("Denied", 1045)));
        Assert.True(StartupDiagnostics.IsDatabaseConnectionOrConfigException(
            new InvalidOperationException("Error resolving ServerVersion.AutoDetect")));

        // Unrelated exceptions: must return false (do not hide logic bugs)
        Assert.False(StartupDiagnostics.IsDatabaseConnectionOrConfigException(
            new NullReferenceException("Object reference not set to an instance of an object")));
        Assert.False(StartupDiagnostics.IsDatabaseConnectionOrConfigException(
            new ArgumentNullException("parameter")));
        Assert.False(StartupDiagnostics.IsDatabaseConnectionOrConfigException(
            new DivideByZeroException()));
    }
}

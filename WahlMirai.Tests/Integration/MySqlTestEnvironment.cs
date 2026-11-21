using System;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace WahlMirai.Tests.Integration;

/// <summary>
/// Provee detección centralizada y en caché de la disponibilidad del servidor MySQL/MariaDB
/// para pruebas de integración reales (sin alterar la base de desarrollo wahl_mirai_db).
/// </summary>
public static class MySqlTestEnvironment
{
    public const string EnvironmentVariableName = "WAHLMIRAI_TEST_DB";
    public const string DefaultSkipReason =
        "Pruebas de integración omitidas: no se pudo conectar a MySQL/MariaDB (¿está XAMPP iniciado y WAHLMIRAI_TEST_DB definida?).";

    private static readonly Lazy<ServerProbeResult> _probe = new(ProbeServer);

    public static bool IsServerAvailable => _probe.Value.IsAvailable;
    public static string? SkipReason => _probe.Value.SkipReason;
    public static string? ServerConnectionString => _probe.Value.ServerConnectionString;
    public static string? ServerVersion => _probe.Value.ServerVersion;

    public static ServerProbeResult ProbeServer()
    {
        string? rawConnStr = Environment.GetEnvironmentVariable(EnvironmentVariableName, EnvironmentVariableTarget.Process);
        if (string.IsNullOrWhiteSpace(rawConnStr))
        {
            rawConnStr = Environment.GetEnvironmentVariable(EnvironmentVariableName, EnvironmentVariableTarget.User);
        }
        if (string.IsNullOrWhiteSpace(rawConnStr))
        {
            rawConnStr = Environment.GetEnvironmentVariable(EnvironmentVariableName, EnvironmentVariableTarget.Machine);
        }

        // Si no está en variable de entorno, revisar User-Secrets o appsettings de prueba como respaldo
        if (string.IsNullOrWhiteSpace(rawConnStr))
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddUserSecrets<MySqlDatabaseFixture>(optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                rawConnStr = config.GetConnectionString("WAHLMIRAI_TEST_DB")
                             ?? config["WAHLMIRAI_TEST_DB"];
            }
            catch
            {
                // Ignorar fallas al resolver configuración de respaldo
            }
        }

        if (string.IsNullOrWhiteSpace(rawConnStr))
        {
            return new ServerProbeResult(false, DefaultSkipReason, null, null);
        }

        rawConnStr = rawConnStr.Trim().Trim('"', '\'');

        try
        {
            var builder = new MySqlConnectionStringBuilder(rawConnStr)
            {
                // Timeout corto (3s) para no bloquear corridas cuando el servidor no responde
                ConnectionTimeout = 3,
                // Garantizar que la verificación inicial de servidor no dependa de una base existente
                Database = string.Empty
            };

            using var conn = new MySqlConnection(builder.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT VERSION();";
            var version = cmd.ExecuteScalar()?.ToString() ?? "Unknown";

            return new ServerProbeResult(true, null, builder.ConnectionString, version);
        }
        catch
        {
            return new ServerProbeResult(false, DefaultSkipReason, null, null);
        }
    }

    public sealed record ServerProbeResult(
        bool IsAvailable,
        string? SkipReason,
        string? ServerConnectionString,
        string? ServerVersion);
}

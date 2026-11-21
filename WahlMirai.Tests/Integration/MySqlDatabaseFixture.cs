using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests.Integration;

public sealed class DdlExecutionFailure
{
    public required string Statement { get; init; }
    public required int ErrorNumber { get; init; }
    public required string ErrorMessage { get; init; }
    public required string ServerVersion { get; init; }

    public override string ToString() =>
        $"[Error {ErrorNumber}] {ErrorMessage} (Server: {ServerVersion}) -> Stmt: {Statement.Trim()}";
}

/// <summary>
/// Fixture de colección para pruebas de integración contra MySQL/MariaDB real.
/// Crea una base de datos temporal con prefijo 'wahl_mirai_test_', ejecuta el DDL consolidado,
/// y garantiza el aislamiento y eliminación total al finalizar.
/// </summary>
public class MySqlDatabaseFixture : IAsyncLifetime
{
    public const string TestDatabasePrefix = "wahl_mirai_test_";
    public const string ProductionDatabaseName = "wahl_mirai_db";

    private bool _databaseCreated = false;

    public string DatabaseName { get; }
    public string? ConnectionString { get; private set; }
    public string? ServerVersion { get; private set; }
    public List<DdlExecutionFailure> FailedDdlStatements { get; } = new();

    public MySqlDatabaseFixture()
    {
        // Generar nombre único: wahl_mirai_test_<guid sin guiones>
        DatabaseName = $"{TestDatabasePrefix}{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Valida que el nombre de base de datos sea estrictamente temporal y seguro.
    /// Impide cualquier operación sobre 'wahl_mirai_db' o bases fuera del prefijo de prueba.
    /// </summary>
    public static void ValidateTestDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("El nombre de la base de datos de prueba no puede estar vacío.");
        }

        if (string.Equals(databaseName, ProductionDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Operación abortada por seguridad: el fixture de integración jamás debe apuntar a la base de datos '{ProductionDatabaseName}'.");
        }

        if (!databaseName.StartsWith(TestDatabasePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Operación abortada por seguridad: el nombre de la base '{databaseName}' no inicia con el prefijo obligatorio '{TestDatabasePrefix}'.");
        }

        if (!Regex.IsMatch(databaseName, @"^[a-zA-Z0-9_]+$"))
        {
            throw new InvalidOperationException(
                $"Operación abortada por seguridad: el nombre de la base '{databaseName}' contiene caracteres no permitidos.");
        }
    }

    /// <summary>
    /// Elimina comentarios iniciales (-- , # y /* ... */) para inspeccionar la instrucción real.
    /// </summary>
    public static string StripLeadingComments(string sql)
    {
        var span = sql.AsSpan().TrimStart();
        while (true)
        {
            if (span.StartsWith("--") || span.StartsWith("#"))
            {
                var newlineIdx = span.IndexOf('\n');
                if (newlineIdx < 0) return string.Empty;
                span = span[(newlineIdx + 1)..].TrimStart();
            }
            else if (span.StartsWith("/*"))
            {
                var endIdx = span.IndexOf("*/");
                if (endIdx < 0) return string.Empty;
                span = span[(endIdx + 2)..].TrimStart();
            }
            else
            {
                break;
            }
        }
        return span.ToString();
    }

    /// <summary>
    /// Determina si una sentencia SQL es una instrucción a nivel de base global (DROP DATABASE, CREATE DATABASE, USE).
    /// </summary>
    public static bool IsDatabaseLevelCommand(string sql)
    {
        var clean = StripLeadingComments(sql);
        if (string.IsNullOrWhiteSpace(clean)) return false;
        return Regex.IsMatch(clean, @"^(DROP\s+DATABASE|CREATE\s+DATABASE|CREATE\s+SCHEMA|USE\s+)", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Valida que una sentencia DDL a ejecutar en la base temporal no amenace la seguridad
    /// ni afecte a la base de producción 'wahl_mirai_db'.
    /// </summary>
    public static void AssertSafeDdlStatement(string sql, string targetTestDbName)
    {
        ValidateTestDatabaseName(targetTestDbName);

        var clean = StripLeadingComments(sql);
        if (string.IsNullOrWhiteSpace(clean)) return;

        // 1. Prohibir cualquier DROP DATABASE que no sea explícitamente sobre la base temporal
        if (Regex.IsMatch(clean, @"\bDROP\s+DATABASE\b", RegexOptions.IgnoreCase))
        {
            if (!clean.Contains($"`{targetTestDbName}`", StringComparison.OrdinalIgnoreCase) &&
                !clean.Contains($" {targetTestDbName}", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Operación abortada por seguridad: se intentó ejecutar una sentencia DROP DATABASE que no apunta a la base temporal autorizada '{targetTestDbName}'. Sentencia: {sql}");
            }
        }

        // 2. Prohibir terminantemente cualquier referencia a 'wahl_mirai_db' en sentencias ejecutables
        if (Regex.IsMatch(clean, @"\b(wahl_mirai_db)\b", RegexOptions.IgnoreCase))
        {
            throw new InvalidOperationException(
                $"Operación abortada por seguridad: la sentencia DDL contiene referencia prohibida a la base de producción '{ProductionDatabaseName}'. Sentencia: {sql}");
        }
    }

    public async Task InitializeAsync()
    {
        if (!MySqlTestEnvironment.IsServerAvailable)
        {
            return;
        }

        ValidateTestDatabaseName(DatabaseName);

        var serverConnStr = MySqlTestEnvironment.ServerConnectionString
            ?? throw new InvalidOperationException("La cadena de conexión del servidor no está disponible.");

        ServerVersion = MySqlTestEnvironment.ServerVersion ?? "Unknown";

        // 1. Crear base de datos temporal en el servidor
        await using (var serverConn = new MySqlConnection(serverConnStr))
        {
            await serverConn.OpenAsync();
            await using var cmd = serverConn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            await cmd.ExecuteNonQueryAsync();
            _databaseCreated = true;
        }

        try
        {
            // 2. Configurar cadena de conexión apuntando a la base temporal
            var builder = new MySqlConnectionStringBuilder(serverConnStr)
            {
                Database = DatabaseName,
                AllowUserVariables = true
            };
            ConnectionString = builder.ConnectionString;

            // 3. Localizar y cargar docs/wahl_mirai_db_v3_2_completo.sql
            var ddlPath = ResolveDdlPath();
            var ddlContent = await File.ReadAllTextAsync(ddlPath, Encoding.UTF8);

            // 4. Ejecutar DDL sentencia por sentencia excluyendo comandos a nivel de base global
            var statements = SplitScriptStatements(ddlContent);

            await using (var dbConn = new MySqlConnection(ConnectionString))
            {
                await dbConn.OpenAsync();

                foreach (var stmt in statements)
                {
                    var trimmed = stmt.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;

                    // Filtrar DROP DATABASE, CREATE DATABASE y USE (incluso precedidos de comentarios)
                    if (IsDatabaseLevelCommand(trimmed))
                    {
                        continue;
                    }

                    // Sustituir cualquier referencia residual a wahl_mirai_db por la base temporal
                    var sanitized = trimmed.Replace(ProductionDatabaseName, DatabaseName, StringComparison.OrdinalIgnoreCase);

                    // Guardia de seguridad: validar que la sentencia sea 100% segura antes de ejecutar
                    AssertSafeDdlStatement(sanitized, DatabaseName);

                    try
                    {
                        await using var cmd = dbConn.CreateCommand();
                        cmd.CommandText = sanitized;
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (MySqlException ex)
                    {
                        FailedDdlStatements.Add(new DdlExecutionFailure
                        {
                            Statement = sanitized,
                            ErrorNumber = ex.Number,
                            ErrorMessage = ex.Message,
                            ServerVersion = ServerVersion
                        });
                    }
                }
            }
        }
        catch
        {
            // En caso de fallo durante la carga del DDL o conexión, asegurar la eliminación de la base
            await DropTestDatabaseAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await DropTestDatabaseAsync();
    }

    private async Task DropTestDatabaseAsync()
    {
        if (!MySqlTestEnvironment.IsServerAvailable || !_databaseCreated)
        {
            return;
        }

        try
        {
            ValidateTestDatabaseName(DatabaseName);

            var serverConnStr = MySqlTestEnvironment.ServerConnectionString;
            if (!string.IsNullOrEmpty(serverConnStr))
            {
                await using var serverConn = new MySqlConnection(serverConnStr);
                await serverConn.OpenAsync();
                await using var cmd = serverConn.CreateCommand();
                cmd.CommandText = $"DROP DATABASE IF EXISTS `{DatabaseName}`;";
                await cmd.ExecuteNonQueryAsync();
                _databaseCreated = false;
            }
        }
        catch
        {
            // Evitar que excepciones en la limpieza enmascaren resultados de pruebas
        }
    }

    /// <summary>
    /// Restablece los datos de prueba dejando intactos los catálogos y datos semilla del DDL.
    /// Permite aislamiento total entre pruebas de la misma colección.
    /// </summary>
    public async Task ResetDatabaseStateAsync()
    {
        if (string.IsNullOrEmpty(ConnectionString)) return;

        await using var conn = new MySqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SET FOREIGN_KEY_CHECKS = 0;
            DELETE FROM `votes`;
            DELETE FROM `event_participations`;
            DELETE FROM `candidacy_documents`;
            DELETE FROM `candidate_proposals`;
            DELETE FROM `candidates`;
            DELETE FROM `event_grades`;
            DELETE FROM `voting_events`;
            DELETE FROM `email_queue`;
            DELETE FROM `pqr_tickets`;
            DELETE FROM `audit_log`;
            DELETE FROM `position_requirements` WHERE `id` > 4;
            DELETE FROM `election_positions` WHERE `id` > 3;
            DELETE FROM `census_whitelist` WHERE `id` > 1;
            UPDATE `census_whitelist` SET `claimed_at` = NULL WHERE `id` = 1;
            DELETE FROM `users` WHERE `id` > 2;
            SET FOREIGN_KEY_CHECKS = 1;";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Crea un DbContext apuntando a la base de datos temporal con soporte relacional completo.
    /// </summary>
    public WahlMiraiDbContext CreateDbContext()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            throw new InvalidOperationException("No se puede crear el DbContext porque la base de prueba no está inicializada.");
        }

        var config = new ConfigurationBuilder().Build();
        var serverVersion = DatabaseServerVersionResolver.Resolve(config, ConnectionString);

        var optionsBuilder = new DbContextOptionsBuilder<WahlMiraiDbContext>();
        optionsBuilder.UseMySql(ConnectionString, serverVersion, mySqlOptions =>
        {
            mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });

        return new WahlMiraiDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Crea una conexión directa MySqlConnection apuntando a la base de prueba.
    /// </summary>
    public MySqlConnection CreateRawConnection()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            throw new InvalidOperationException("No se puede crear una conexión cruda porque la base de prueba no está inicializada.");
        }

        return new MySqlConnection(ConnectionString);
    }

    /// <summary>
    /// Localiza docs/wahl_mirai_db_v3_2_completo.sql subiendo desde el directorio del ensamblado.
    /// </summary>
    public static string ResolveDdlPath()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            var targetFile = Path.Combine(currentDir.FullName, "docs", "wahl_mirai_db_v3_2_completo.sql");
            if (File.Exists(targetFile))
            {
                return targetFile;
            }
            currentDir = currentDir.Parent;
        }

        throw new FileNotFoundException(
            "No se encontró 'docs/wahl_mirai_db_v3_2_completo.sql' subiendo desde el directorio del ensamblado de pruebas.");
    }

    /// <summary>
    /// Divide un script SQL en sentencias individuales respetando literales, comentarios y backticks.
    /// </summary>
    public static List<string> SplitScriptStatements(string script)
    {
        var statements = new List<string>();
        var current = new StringBuilder();
        bool inSingleLineComment = false;
        bool inMultiLineComment = false;
        bool inString = false;
        char stringDelimiter = '\0';
        bool inBacktick = false;

        for (int i = 0; i < script.Length; i++)
        {
            char c = script[i];
            char next = i + 1 < script.Length ? script[i + 1] : '\0';

            if (inSingleLineComment)
            {
                current.Append(c);
                if (c == '\n') inSingleLineComment = false;
            }
            else if (inMultiLineComment)
            {
                current.Append(c);
                if (c == '*' && next == '/')
                {
                    current.Append(next);
                    i++;
                    inMultiLineComment = false;
                }
            }
            else if (inString)
            {
                current.Append(c);
                if (c == '\\')
                {
                    if (i + 1 < script.Length)
                    {
                        current.Append(script[++i]);
                    }
                }
                else if (c == stringDelimiter)
                {
                    if (next == stringDelimiter)
                    {
                        current.Append(next);
                        i++;
                    }
                    else
                    {
                        inString = false;
                    }
                }
            }
            else if (inBacktick)
            {
                current.Append(c);
                if (c == '`')
                {
                    if (next == '`')
                    {
                        current.Append(next);
                        i++;
                    }
                    else
                    {
                        inBacktick = false;
                    }
                }
            }
            else
            {
                if ((c == '-' && next == '-') || c == '#')
                {
                    inSingleLineComment = true;
                    current.Append(c);
                    if (c == '-')
                    {
                        current.Append(next);
                        i++;
                    }
                }
                else if (c == '/' && next == '*')
                {
                    inMultiLineComment = true;
                    current.Append(c).Append(next);
                    i++;
                }
                else if (c == '\'' || c == '"')
                {
                    inString = true;
                    stringDelimiter = c;
                    current.Append(c);
                }
                else if (c == '`')
                {
                    inBacktick = true;
                    current.Append(c);
                }
                else if (c == ';')
                {
                    var stmt = current.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(stmt))
                    {
                        statements.Add(stmt);
                    }
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        var remaining = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(remaining))
        {
            statements.Add(remaining);
        }

        return statements;
    }
}

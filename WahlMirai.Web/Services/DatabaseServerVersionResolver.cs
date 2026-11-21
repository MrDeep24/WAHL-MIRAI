using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace WahlMirai.Web.Services;

/// <summary>
/// Resuelve la versión y tipo del servidor de base de datos (MySQL o MariaDB).
/// Soporta configuración explícita (ServerType, ServerVersion) sin abrir conexiones,
/// con fallback a ServerVersion.AutoDetect(connectionString) cuando ServerVersion está vacío.
/// </summary>
public static class DatabaseServerVersionResolver
{
    public const string DefaultServerType = "MySql";

    /// <summary>
    /// Evalúa de forma pura los valores configurados para tipo y versión de servidor.
    /// Devuelve una instancia concreta de ServerVersion (MySqlServerVersion o MariaDbServerVersion)
    /// si ServerVersion tiene valor, o null si ServerVersion está vacío (indicando fallback a AutoDetect).
    /// Lanza DatabaseConfigurationException si ServerType o ServerVersion son inválidos.
    /// No abre conexiones a la base de datos.
    /// </summary>
    public static ServerVersion? ResolveConfiguredVersion(string? serverType, string? serverVersion)
    {
        var trimmedType = serverType?.Trim();
        var trimmedVersion = serverVersion?.Trim();

        // Validar ServerType si está presente
        string resolvedType = DefaultServerType;
        if (!string.IsNullOrEmpty(trimmedType))
        {
            if (string.Equals(trimmedType, "MySql", StringComparison.OrdinalIgnoreCase))
            {
                resolvedType = "MySql";
            }
            else if (string.Equals(trimmedType, "MariaDb", StringComparison.OrdinalIgnoreCase))
            {
                resolvedType = "MariaDb";
            }
            else
            {
                throw new DatabaseConfigurationException(
                    $"El tipo de servidor de base de datos '{trimmedType}' no es válido. Los valores soportados son 'MySql' y 'MariaDb'.");
            }
        }

        // Si ServerVersion está vacío, no se usa versión fija; se delega a AutoDetect
        if (string.IsNullOrEmpty(trimmedVersion))
        {
            return null;
        }

        // Extraer la parte numérica si incluye sufijos (ej. '10.4.32-MariaDB' de SELECT VERSION())
        var versionCandidate = trimmedVersion;
        var dashIndex = versionCandidate.IndexOf('-');
        if (dashIndex > 0)
        {
            versionCandidate = versionCandidate.Substring(0, dashIndex);
        }

        // Validar formato de versión (x.y.z o x.y)
        if (!Version.TryParse(versionCandidate, out var parsedVersion) || parsedVersion.Major < 0 || parsedVersion.Minor < 0)
        {
            throw new DatabaseConfigurationException(
                $"La versión del servidor de base de datos '{trimmedVersion}' no tiene un formato válido (se esperaba un formato como '8.0.36' o '10.4.32').");
        }

        try
        {
            if (string.Equals(resolvedType, "MariaDb", StringComparison.OrdinalIgnoreCase))
            {
                return new MariaDbServerVersion(parsedVersion);
            }

            return new MySqlServerVersion(parsedVersion);
        }
        catch (Exception ex)
        {
            throw new DatabaseConfigurationException(
                $"Error al inicializar la versión de servidor '{resolvedType} {trimmedVersion}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Resuelve la versión de servidor leyendo la sección 'Database' de IConfiguration
    /// o ejecutando AutoDetect(connectionString) si ServerVersion está vacío.
    /// </summary>
    public static ServerVersion Resolve(IConfiguration configuration, string? connectionString)
    {
        var section = configuration.GetSection("Database");
        var serverType = section["ServerType"];
        var serverVersion = section["ServerVersion"];

        return Resolve(serverType, serverVersion, connectionString);
    }

    /// <summary>
    /// Resuelve la versión de servidor a partir de los valores directos o invoca AutoDetect.
    /// </summary>
    public static ServerVersion Resolve(string? serverType, string? serverVersion, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new DatabaseConfigurationException(
                "La cadena de conexión 'ConnectionStrings:WahlMiraiDb' no está configurada o está vacía.");
        }

        var configured = ResolveConfiguredVersion(serverType, serverVersion);
        if (configured != null)
        {
            return configured;
        }

        return ServerVersion.AutoDetect(connectionString);
    }
}

using System;
using System.Text.RegularExpressions;
using Serilog;

namespace WahlMirai.Web.Services;

/// <summary>
/// Diagnóstico de arranque para fallas de base de datos o configuración.
/// Proporciona explicaciones claras y sugerencias de acción en español sin filtrar credenciales ni stack traces.
/// </summary>
public static class StartupDiagnostics
{
    public const string DefaultSqlScript = "docs/wahl_mirai_db_v3_2_completo.sql";

    /// <summary>
    /// Clasifica una excepción de arranque y devuelve un motivo y una acción sugerida en español.
    /// Es un método puro, libre de efectos secundarios y completamente testeable.
    /// </summary>
    public static (string Reason, string Action) Describe(Exception ex)
    {
        if (ex == null)
        {
            return ("Error desconocido en el inicio del sistema.",
                    "Revisa los registros técnicos en la carpeta de logs para más información.");
        }

        // 1. Cadena de conexión ausente o vacía
        if (IsMissingConnectionString(ex))
        {
            return ("La cadena de conexión 'ConnectionStrings:WahlMiraiDb' no está configurada o está vacía.",
                    "Configura la cadena de conexión en 'appsettings.Development.json', user-secrets o la variable de entorno 'ConnectionStrings__WahlMiraiDb'.");
        }

        // 2. Configuración inválida de Database:ServerType o Database:ServerVersion
        if (IsInvalidDatabaseConfiguration(ex))
        {
            return ("La configuración de 'Database:ServerType' o 'Database:ServerVersion' no es válida.",
                    "Configura 'Database:ServerType' como 'MySql' o 'MariaDb', y 'Database:ServerVersion' en formato 'x.y.z' (ej. '8.0.36' para MySQL o '10.4.32' para MariaDB en XAMPP), o déjalos vacíos para detección automática.");
        }

        // 3. Inspección de códigos de error de base de datos (MySqlConnector / DbException)
        var errorCode = GetSqlErrorCode(ex);

        // 3a. Error 1045: Credenciales rechazadas
        if (errorCode == 1045 || ContainsSubstring(ex, "1045") || ContainsSubstring(ex, "Access denied for user") || ContainsSubstring(ex, "Acceso denegado"))
        {
            return ("Las credenciales de acceso a la base de datos fueron rechazadas (error 1045).",
                    "Revisa el usuario y la contraseña configurados en la cadena de conexión 'ConnectionStrings:WahlMiraiDb'.");
        }

        // 3b. Error 1049: Base de datos inexistente
        if (errorCode == 1049 || ContainsSubstring(ex, "1049") || ContainsSubstring(ex, "Unknown database") || ContainsSubstring(ex, "Base de datos desconocida"))
        {
            return ("La base de datos especificada no existe en el servidor (error 1049).",
                    $"Ejecuta el script '{DefaultSqlScript}' en tu servidor MySQL/MariaDB para crear la base de datos y sus tablas.");
        }

        // 3c. Error 1146: Tablas requeridas inexistentes
        if (errorCode == 1146 || ContainsSubstring(ex, "1146") || ContainsSubstring(ex, "doesn't exist") || ContainsSubstring(ex, "no existe"))
        {
            return ("Las tablas requeridas no existen en la base de datos (error 1146).",
                    $"Ejecuta el script '{DefaultSqlScript}' en tu servidor MySQL/MariaDB para crear la base de datos y sus tablas.");
        }

        // 3d. Error 2003 / 2002 o problemas de red/timeout: Servidor inalcanzable
        if (errorCode == 2003 || errorCode == 2002 || IsServerUnreachable(ex))
        {
            return ("No se pudo conectar con el servidor de base de datos.",
                    "Verifica que MySQL esté iniciado (en XAMPP, botón Start junto a MySQL) y que host y puerto de la cadena de conexión sean correctos.");
        }

        // 4. Caso general de base de datos
        var typeName = Sanitize(ex.GetType().Name);
        var safeMessage = Sanitize(ex.Message);
        return ($"Error al comunicarse con la base de datos ({typeName}): {safeMessage}",
                "Revisa el archivo de log en la carpeta 'logs' para obtener el diagnóstico técnico detallado.");
    }

    /// <summary>
    /// Determina si la excepción corresponde a un fallo de conexión o configuración de base de datos.
    /// Permite filtrar para no ocultar errores de lógica ajenos a la base de datos.
    /// </summary>
    public static bool IsDatabaseConnectionOrConfigException(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is DatabaseConfigurationException) return true;
            if (current is System.Net.Sockets.SocketException) return true;
            if (current is TimeoutException) return true;

            var type = current.GetType();
            var fullName = type.FullName ?? type.Name;
            if (fullName.Contains("MySql", StringComparison.OrdinalIgnoreCase)) return true;
            if (fullName.Contains("Pomelo", StringComparison.OrdinalIgnoreCase)) return true;

            if (GetSqlErrorCode(current).HasValue) return true;

            var msg = current.Message ?? string.Empty;
            if (msg.Contains("WahlMiraiDb", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("ServerVersion", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("cadena de conexión", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (current is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    if (IsDatabaseConnectionOrConfigException(inner)) return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Da formato al bloque de error amigable para consola.
    /// </summary>
    public static string FormatMessage(string reason, string action)
    {
        const string separator = "==================================================";
        return $"{separator}{Environment.NewLine}" +
               $"Wahl Mirai no pudo iniciar{Environment.NewLine}" +
               $"Motivo: {reason}{Environment.NewLine}" +
               $"Qué hacer: {action}{Environment.NewLine}" +
               $"{separator}";
    }

    /// <summary>
    /// Imprime el diagnóstico en Console.Error, registra un evento fatal en Serilog sin stack trace,
    /// vacía los buffers y finaliza el proceso con código de salida 1.
    /// </summary>
    public static void ReportAndExit(Exception ex, Action<int>? exitAction = null)
    {
        var (reason, action) = Describe(ex);
        var formatted = FormatMessage(reason, action);

        Console.Error.WriteLine(formatted);

        try
        {
            Log.Fatal("Wahl Mirai no pudo iniciar. Motivo: {Reason}. Qué hacer: {Action}", reason, action);
            Log.CloseAndFlush();
        }
        catch
        {
            // Ignorar errores al registrar en Serilog durante la salida de emergencia
        }

        if (exitAction != null)
        {
            exitAction(1);
        }
        else
        {
            Environment.Exit(1);
        }
    }

    private static bool IsMissingConnectionString(Exception ex)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            var msg = cur.Message ?? string.Empty;
            if (cur is DatabaseConfigurationException && msg.Contains("cadena de conexión", StringComparison.OrdinalIgnoreCase))
                return true;
            if (msg.Contains("ConnectionStrings:WahlMiraiDb", StringComparison.OrdinalIgnoreCase) &&
                (msg.Contains("vacía", StringComparison.OrdinalIgnoreCase) || msg.Contains("configurada", StringComparison.OrdinalIgnoreCase) || msg.Contains("missing", StringComparison.OrdinalIgnoreCase)))
                return true;
            if (msg.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) &&
                (msg.Contains("not been initialized", StringComparison.OrdinalIgnoreCase) || msg.Contains("empty", StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    private static bool IsInvalidDatabaseConfiguration(Exception ex)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            if (cur is DatabaseConfigurationException dbConfig)
            {
                var msg = dbConfig.Message ?? string.Empty;
                if (msg.Contains("ServerType", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("ServerVersion", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("tipo de servidor", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("versión del servidor", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool IsServerUnreachable(Exception ex)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            if (cur is System.Net.Sockets.SocketException) return true;
            if (cur is TimeoutException) return true;

            var msg = cur.Message ?? string.Empty;
            if (msg.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("actively refused", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("denegó", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("no se puede establecer una conexión", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Host is down", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Network is unreachable", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("tiempo de espera", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("No such host", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("connection attempt failed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (cur is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    if (IsServerUnreachable(inner)) return true;
                }
            }
        }
        return false;
    }

    private static bool ContainsSubstring(Exception ex, string substring)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            if ((cur.Message ?? string.Empty).Contains(substring, StringComparison.OrdinalIgnoreCase))
                return true;

            if (cur is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    if (ContainsSubstring(inner, substring)) return true;
                }
            }
        }
        return false;
    }

    private static int? GetSqlErrorCode(Exception ex)
    {
        for (var cur = ex; cur != null; cur = cur.InnerException)
        {
            var type = cur.GetType();

            var propNumber = type.GetProperty("Number");
            if (propNumber != null && propNumber.PropertyType == typeof(int))
            {
                var val = (int)propNumber.GetValue(cur)!;
                if (val > 0) return val;
            }

            var propErrorCode = type.GetProperty("ErrorCode");
            if (propErrorCode != null && propErrorCode.PropertyType == typeof(int))
            {
                var val = (int)propErrorCode.GetValue(cur)!;
                if (val > 0) return val;
            }

            if (cur is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    var code = GetSqlErrorCode(inner);
                    if (code.HasValue) return code;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Limpia mensajes para evitar fuga de credenciales, contraseñas o detalles sensibles de conexión.
    /// Trunca el resultado a una longitud máxima segura.
    /// </summary>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Eliminar valores asociados a credenciales en cadenas de conexión
        var cleaned = Regex.Replace(
            text,
            @"(?i)(password|pwd|user id|uid|user|username)\s*[:=]\s*[^;\r\n]+",
            "$1=***");

        cleaned = cleaned.Replace("\r", " ").Replace("\n", " ").Trim();
        if (cleaned.Length > 140)
        {
            cleaned = cleaned.Substring(0, 137) + "...";
        }
        return cleaned;
    }
}

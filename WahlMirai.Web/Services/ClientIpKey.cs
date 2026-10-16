// ============================================================
// Archivo:       ClientIpKey.cs
// Módulo:        Configuración transversal / Infraestructura HTTP
// Desarrollador: Kevin
// Descripción:   Extrae y normaliza claves de partición por IP para
//                rate limiting y contadores de bloqueo (IPv4 /32 e IPv6 /64).
// Requerimientos: RF-M01-00, RN-15.
// ============================================================
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;

namespace WahlMirai.Web.Services;

/// <summary>
/// Normaliza direcciones IP en claves de partición para rate limiting y contadores
/// de mitigación de fuerza bruta / DoS (RN-15, RF-M01-00).
/// Preserva IPv4 (/32) y agrupa IPv6 en prefijos /64.
/// </summary>
/// <remarks>
/// NOTA DE SEGURIDAD: Esta clave se emplea EXCLUSIVAMENTE para particionar contadores
/// de rate limiting y bloqueos de intentos (e.g. <c>register:{key}</c>). Las entradas
/// de auditoría forense (<c>audit_log</c>) deben preservar la IP cruda observada.
/// </remarks>
public static class ClientIpKey
{
    public const string Unknown = "unknown";

    /// <summary>
    /// Extrae la clave de partición a partir de la conexión del contexto HTTP actual.
    /// </summary>
    /// <param name="context">Contexto HTTP de la solicitud.</param>
    /// <returns>Clave normalizada o "unknown" si el contexto o la dirección remota es nula.</returns>
    public static string From(HttpContext? context)
    {
        return From(context?.Connection?.RemoteIpAddress);
    }

    /// <summary>
    /// Normaliza una dirección IP en una clave de partición.
    /// Reglas en orden de precedencia:
    /// a) null -> "unknown".
    /// b) IPv4-mapped IPv6 (::ffff:x.x.x.x) -> se mapea a IPv4 antes de verificar la familia.
    /// c) IPv4 nativa -> representación canónica ToString() (/32).
    /// d) IPv6 nativa -> prefijo /64 tomando los primeros 8 bytes, poniendo a cero los últimos 8,
    ///    descartando scope id y formateando como canonical ToString() + "/64".
    /// e) No lanza excepciones ante valores especiales como ::1 o 127.0.0.1.
    /// </summary>
    /// <param name="address">Dirección IP a normalizar.</param>
    /// <returns>Clave de partición formateada.</returns>
    public static string From(IPAddress? address)
    {
        if (address == null)
        {
            return Unknown;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return address.ToString();
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            for (var i = 8; i < 16; i++)
            {
                bytes[i] = 0;
            }

            var prefixAddress = new IPAddress(bytes);
            return $"{prefixAddress}/64";
        }

        return address.ToString();
    }
}

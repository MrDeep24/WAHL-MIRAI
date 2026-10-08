// ============================================================
// Archivo:       ForwardedHeadersConfiguration.cs
// Módulo:        Configuración transversal / Infraestructura HTTP
// Desarrollador: Kevin
// Descripción:   Configuración estricta, opt-in y fail-closed de
//                ForwardedHeadersOptions para despliegues tras reverse proxy.
// Requerimientos: RN-1, RN-15.
// ============================================================
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace WahlMirai.Web.Services;

/// <summary>
/// Provee la construcción, validación y registro estricto de opciones de cabeceras reenviadas.
/// </summary>
public static class ForwardedHeadersConfiguration
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>
    /// Construye y valida las opciones de cabeceras reenviadas a partir de la configuración.
    /// Retorna null si la sección no está habilitada (Enabled=false).
    /// Lanza <see cref="InvalidOperationException"/> si está habilitado con listas vacías o valores inválidos (fail-closed).
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>Instancia configurada de <see cref="ForwardedHeadersOptions"/> o null si está deshabilitado.</returns>
    /// <exception cref="InvalidOperationException">Si la configuración es inválida o viola el principio fail-closed.</exception>
    public static ForwardedHeadersOptions? BuildOptions(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(SectionName);
        var enabled = section.GetValue<bool>("Enabled", false);
        if (!enabled)
        {
            return null;
        }

        var rawProxies = ReadStringList(section.GetSection("KnownProxies"));
        var rawNetworks = ReadStringList(section.GetSection("KnownNetworks"));

        if (rawProxies.Count == 0 && rawNetworks.Count == 0)
        {
            throw new InvalidOperationException(
                "ForwardedHeaders está habilitado pero 'KnownProxies' y 'KnownNetworks' están vacías. " +
                "Configuración fail-closed: la aplicación no puede iniciar sin al menos un proxy o red confiable.");
        }

        var parsedProxies = new List<IPAddress>();
        foreach (var proxyEntry in rawProxies)
        {
            if (string.IsNullOrWhiteSpace(proxyEntry) || !IPAddress.TryParse(proxyEntry, out var ip))
            {
                throw new InvalidOperationException(
                    $"Entrada inválida en KnownProxies: '{proxyEntry}'. Debe ser una dirección IP válida.");
            }
            parsedProxies.Add(ip);
        }

        var parsedNetworks = new List<IPNetwork>();
        foreach (var networkEntry in rawNetworks)
        {
            if (string.IsNullOrWhiteSpace(networkEntry))
            {
                throw new InvalidOperationException(
                    "Entrada inválida en KnownNetworks: el valor no puede ser nulo o vacío.");
            }

            var parts = networkEntry.Split('/');
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            {
                throw new InvalidOperationException(
                    $"Entrada inválida en KnownNetworks: '{networkEntry}'. Formato CIDR inválido (se esperaba IP/prefijo).");
            }

            if (!IPAddress.TryParse(parts[0], out var prefixIp))
            {
                throw new InvalidOperationException(
                    $"Entrada inválida en KnownNetworks: '{networkEntry}'. La dirección IP '{parts[0]}' no es válida.");
            }

            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var prefixLength))
            {
                throw new InvalidOperationException(
                    $"Entrada inválida en KnownNetworks: '{networkEntry}'. La longitud de prefijo '{parts[1]}' no es un número entero válido.");
            }

            if (prefixIp.AddressFamily == AddressFamily.InterNetwork)
            {
                if (prefixLength < 0 || prefixLength > 32)
                {
                    throw new InvalidOperationException(
                        $"Entrada inválida en KnownNetworks: '{networkEntry}'. El prefijo IPv4 debe estar en el rango 0–32.");
                }
            }
            else if (prefixIp.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (prefixLength < 0 || prefixLength > 128)
                {
                    throw new InvalidOperationException(
                        $"Entrada inválida en KnownNetworks: '{networkEntry}'. El prefijo IPv6 debe estar en el rango 0–128.");
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Entrada inválida en KnownNetworks: '{networkEntry}'. Familia de direcciones IP no admitida.");
            }

            parsedNetworks.Add(new IPNetwork(prefixIp, prefixLength));
        }

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };

        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();

        foreach (var ip in parsedProxies)
        {
            options.KnownProxies.Add(ip);
        }

        foreach (var net in parsedNetworks)
        {
            options.KnownNetworks.Add(net);
        }

        return options;
    }

    /// <summary>
    /// Configura <see cref="ForwardedHeadersOptions"/> en el contenedor de servicios si la funcionalidad está habilitada.
    /// </summary>
    /// <param name="services">Colección de servicios de dependencias.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <returns>True si la configuración fue habilitada y registrada; false si está deshabilitada.</returns>
    public static bool ConfigureForwardedHeaders(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = BuildOptions(configuration);
        if (options == null)
        {
            return false;
        }

        services.Configure<ForwardedHeadersOptions>(opt =>
        {
            opt.ForwardedHeaders = options.ForwardedHeaders;
            opt.ForwardLimit = options.ForwardLimit;
            opt.KnownProxies.Clear();
            opt.KnownNetworks.Clear();

            foreach (var proxy in options.KnownProxies)
            {
                opt.KnownProxies.Add(proxy);
            }

            foreach (var network in options.KnownNetworks)
            {
                opt.KnownNetworks.Add(network);
            }
        });

        return true;
    }

    private static List<string> ReadStringList(IConfigurationSection section)
    {
        var array = section.Get<string[]>();
        if (array != null)
        {
            return array.ToList();
        }

        return section.GetChildren()
            .Select(c => c.Value)
            .Where(v => v != null)
            .Select(v => v!)
            .ToList();
    }
}

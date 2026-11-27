// ============================================================
// Archivo:       WahlMiraiWebFactory.cs
// Módulo:        Infraestructura de Pruebas HTTP (M01 / API Móvil)
// Desarrollador: Kevin
// Descripción:   Fábrica WebApplicationFactory<Program> aislada por instancia
//                para pruebas HTTP end-to-end con EF InMemory (o MySQL de prueba),
//                RecordingEmailSender, TimeProvider inyectable y directorios temporales.
// Requerimientos: RF-M01-00, RF-M01-02, RN-4, RN-4.1, RN-5, RN-14, RN-15, RN-16.
// ============================================================
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests.Http;

/// <summary>
/// Registro inmutable de un correo capturado por <see cref="RecordingEmailSender"/>.
/// </summary>
public sealed record SentEmailRecord(string To, string Subject, string HtmlBody, DateTimeOffset SentAt);

/// <summary>
/// Implementación falsa de <see cref="IEmailSender"/> que registra correos enviados en memoria
/// y permite simular fallos SMTP sin realizar envíos reales.
/// </summary>
public sealed class RecordingEmailSender : IEmailSender
{
    private readonly object _sync = new();
    private readonly List<SentEmailRecord> _sentEmails = new();

    /// <summary>Si es true, toda llamada a <see cref="SendAsync"/> lanza una excepción.</summary>
    public bool ShouldFail { get; set; }

    /// <summary>Predicado opcional (toEmail, subject) para simular fallos selectivos.</summary>
    public Func<string, string, bool>? FailurePredicate { get; set; }

    /// <summary>Excepción personalizada a lanzar cuando el envío falla.</summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>Instantánea de todos los correos registrados hasta el momento.</summary>
    public IReadOnlyList<SentEmailRecord> SentEmails
    {
        get
        {
            lock (_sync)
            {
                return _sentEmails.ToList();
            }
        }
    }

    /// <summary>Limpia los correos registrados.</summary>
    public void Clear()
    {
        lock (_sync)
        {
            _sentEmails.Clear();
        }
    }

    /// <inheritdoc />
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (ShouldFail || (FailurePredicate?.Invoke(toEmail, subject) ?? false))
        {
            throw ExceptionToThrow ?? new InvalidOperationException("Simulated SMTP failure.");
        }

        lock (_sync)
        {
            _sentEmails.Add(new SentEmailRecord(toEmail, subject, htmlBody, DateTimeOffset.UtcNow));
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Fábrica <see cref="WebApplicationFactory{TEntryPoint}"/> aislada para pruebas HTTP integrales
/// sobre el pipeline ASP.NET Core de Wahl Mirai (autenticación, cookies, rate limiting,
/// recuperación de acceso, resultados, autorización y API móvil).
/// </summary>
public sealed class WahlMiraiWebFactory : WebApplicationFactory<Program>
{
    private const string DummyInMemoryConnectionString =
        "Server=localhost;Database=wahl_mirai_inmemory_test;User=test;Password=test;";

    private readonly string _inMemoryDatabaseName = $"wahl_mirai_inmemory_{Guid.NewGuid():N}";
    private readonly string _tempRoot;
    private readonly TimeProvider? _timeProvider;
    private readonly string? _useMySqlConnectionString;
    private readonly Dictionary<string, string?> _settings;
    private readonly Action<IServiceCollection>? _configureServices;

    /// <summary>Ruta raíz temporal aislada para esta instancia de fábrica.</summary>
    public string TempRootPath => _tempRoot;

    /// <summary>Ruta aislada de llaves de Data Protection para esta instancia.</summary>
    public string DataProtectionKeysPath { get; }

    /// <summary>Ruta aislada de archivos subidos de candidaturas para esta instancia.</summary>
    public string CandidacyUploadsBasePath { get; }

    /// <summary>Instancia compartida del emisor de correos simulado.</summary>
    public RecordingEmailSender EmailSender { get; } = new();

    /// <summary>
    /// Crea una nueva fábrica aislada con base de datos InMemory única, directorios temporales
    /// y sustituciones opcionales de reloj, conexión MySQL de prueba o configuración.
    /// </summary>
    /// <param name="timeProvider">Reloj falso opcional para reemplazar <see cref="TimeProvider"/> en DI.</param>
    /// <param name="useMySqlConnectionString">Cadena de conexión MySQL de prueba opcional (nunca <c>wahl_mirai_db</c>).</param>
    /// <param name="configOverrides">Sobreescrituras opcionales de configuración antes del arranque.</param>
    /// <param name="configureServices">Delegado opcional para ajustes adicionales de servicios.</param>
    public WahlMiraiWebFactory(
        TimeProvider? timeProvider = null,
        string? useMySqlConnectionString = null,
        IDictionary<string, string?>? configOverrides = null,
        Action<IServiceCollection>? configureServices = null)
    {
        if (!string.IsNullOrWhiteSpace(useMySqlConnectionString))
        {
            var csb = new MySqlConnectionStringBuilder(useMySqlConnectionString);
            if (string.Equals(csb.Database, "wahl_mirai_db", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "WahlMiraiWebFactory jamás debe conectarse a la base de datos de desarrollo 'wahl_mirai_db'.");
            }
        }

        _timeProvider = timeProvider;
        _useMySqlConnectionString = useMySqlConnectionString;
        _configureServices = configureServices;

        EnsureContentRootEnvironmentFallback();

        _tempRoot = Path.Combine(AppContext.BaseDirectory, ".test-temp", Guid.NewGuid().ToString("N"));
        DataProtectionKeysPath = Path.Combine(_tempRoot, "dp-keys");
        CandidacyUploadsBasePath = Path.Combine(_tempRoot, "candidacy-uploads");
        Directory.CreateDirectory(DataProtectionKeysPath);
        Directory.CreateDirectory(Path.Combine(CandidacyUploadsBasePath, "candidacies"));

        _settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:WahlMiraiDb"] = string.IsNullOrWhiteSpace(useMySqlConnectionString)
                ? DummyInMemoryConnectionString
                : useMySqlConnectionString,
            ["Database:ServerType"] = "MySql",
            ["Database:ServerVersion"] = "10.4.32-MariaDB",
            ["EmailSettings:BaseUrl"] = "https://localhost",
            ["DataProtection:KeysPath"] = DataProtectionKeysPath,
            ["CandidacyUploads:BasePath"] = CandidacyUploadsBasePath
        };

        if (configOverrides != null)
        {
            foreach (var kvp in configOverrides)
            {
                _settings[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var kvp in _settings)
        {
            builder.UseSetting(kvp.Key, kvp.Value);
        }

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(_settings);
        });

        builder.ConfigureServices(services =>
        {
            // Permitir que pruebas HTTP simulen distintas IPs de cliente vía X-Test-Remote-Ip.
            services.AddTransient<IStartupFilter, TestRemoteIpStartupFilter>();

            // Eliminar EmailQueueBackgroundService para evitar polling de fondo durante pruebas.
            var hostedDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            (d.ImplementationType == typeof(EmailQueueBackgroundService) ||
                             d.ImplementationInstance is EmailQueueBackgroundService))
                .ToList();
            foreach (var descriptor in hostedDescriptors)
            {
                services.Remove(descriptor);
            }

            // Reemplazar IEmailSender por el emisor en memoria.
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);

            // Reemplazar TimeProvider si la prueba suministró uno determinista.
            if (_timeProvider != null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(_timeProvider);
            }

            // Reemplazar WahlMiraiDbContext y su configuración de proveedor MySQL.
            services.RemoveAll<DbContextOptions<WahlMiraiDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<WahlMiraiDbContext>();

            var efProviderDescriptors = services
                .Where(d =>
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition().FullName ==
                     "Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration`1" &&
                     d.ServiceType.GenericTypeArguments[0] == typeof(WahlMiraiDbContext)) ||
                    (d.ServiceType.Assembly.GetName().Name?.StartsWith("Pomelo.EntityFrameworkCore.MySql", StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.ImplementationType?.Assembly.GetName().Name?.StartsWith("Pomelo.EntityFrameworkCore.MySql", StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            foreach (var descriptor in efProviderDescriptors)
            {
                services.Remove(descriptor);
            }

            if (!string.IsNullOrWhiteSpace(_useMySqlConnectionString))
            {
                var serverVersion = ServerVersion.AutoDetect(_useMySqlConnectionString);
                services.AddDbContext<WahlMiraiDbContext>(options =>
                    options.UseMySql(
                        _useMySqlConnectionString,
                        serverVersion,
                        mySqlOptions => mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
            }
            else
            {
                services.AddDbContext<WahlMiraiDbContext>(options =>
                    options.UseInMemoryDatabase(_inMemoryDatabaseName)
                           .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            }

            _configureServices?.Invoke(services);
        });
    }

    /// <summary>
    /// Crea un <see cref="HttpClient"/> con redirección automática deshabilitada
    /// y cookies habilitadas para inspeccionar respuestas 302 y cabeceras Set-Cookie.
    /// </summary>
    public HttpClient CreateNoRedirectClient(bool useHttps = false, bool handleCookies = true)
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(useHttps ? "https://localhost" : "http://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = handleCookies
        });
    }

    /// <summary>
    /// Ejecuta una acción de sembrado sobre un alcance fresco de <see cref="WahlMiraiDbContext"/> y guarda los cambios.
    /// </summary>
    public async Task SeedAsync(Func<WahlMiraiDbContext, Task> seedAction)
    {
        ArgumentNullException.ThrowIfNull(seedAction);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WahlMiraiDbContext>();
        await seedAction(db);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Ejecuta una consulta asíncrona sobre un alcance fresco de <see cref="WahlMiraiDbContext"/>.
    /// </summary>
    public async Task<T> QueryDbAsync<T>(Func<WahlMiraiDbContext, Task<T>> queryAction)
    {
        ArgumentNullException.ThrowIfNull(queryAction);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WahlMiraiDbContext>();
        return await queryAction(db);
    }

    /// <summary>
    /// Ejecuta un delegado usando un <see cref="IServiceScope"/> creado desde el contenedor de la aplicación.
    /// </summary>
    public async Task ExecuteWithServicesAsync(Func<IServiceProvider, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            CleanupTempDirectory();
        }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        CleanupTempDirectory();
    }

    private static void EnsureContentRootEnvironmentFallback()
    {
        const string envVar = "ASPNETCORE_TEST_CONTENTROOT_WAHLMIRAI_WEB";
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)) ||
            File.Exists("MvcTestingAppManifest.json"))
        {
            return;
        }

        var manifestInBase = Path.Combine(AppContext.BaseDirectory, "MvcTestingAppManifest.json");
        if (File.Exists(manifestInBase))
        {
            var manifest = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(manifestInBase));
            var entry = manifest?.FirstOrDefault(kvp =>
                kvp.Key.StartsWith("WahlMirai.Web,", StringComparison.OrdinalIgnoreCase));
            if (entry is { Value: { Length: > 0 } contentRoot } && Directory.Exists(contentRoot))
            {
                Environment.SetEnvironmentVariable(envVar, contentRoot);
            }
        }
    }

    private void CleanupTempDirectory()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Ignorar errores transitorios de limpieza de archivos temporales al destruir el host.
        }
    }

    private sealed class TestRemoteIpStartupFilter : IStartupFilter
    {
        public const string HeaderName = "X-Test-Remote-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Headers.TryGetValue(HeaderName, out var rawIp) &&
                        IPAddress.TryParse(rawIp.ToString(), out var parsedIp))
                    {
                        context.Connection.RemoteIpAddress = parsedIp;
                    }

                    await nextMiddleware();
                });

                next(app);
            };
        }
    }
}

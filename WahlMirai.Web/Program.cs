using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;
using WahlMirai.Web.Middleware;
using WahlMirai.Web.Hubs;
using Serilog;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
var contentRoot = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "WahlMirai.Web", "Views"))
    ? Path.Combine(Directory.GetCurrentDirectory(), "WahlMirai.Web")
    : Directory.GetCurrentDirectory();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot
});

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

var connectionString = builder.Configuration.GetConnectionString("WahlMiraiDb");
builder.Services.AddDbContext<WahlMiraiDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions => mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// Add services to the container.
builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddSignalR();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddScoped<WahlMirai.Web.Services.IAuditService, WahlMirai.Web.Services.AuditService>();
builder.Services.AddScoped<WahlMirai.Web.Services.IAuthService, WahlMirai.Web.Services.AuthService>();
builder.Services.AddScoped<WahlMirai.Web.Services.ICensusService, WahlMirai.Web.Services.CensusService>();
builder.Services.AddScoped<WahlMirai.Web.Services.IVotingService, WahlMirai.Web.Services.VotingService>();
builder.Services.AddScoped<WahlMirai.Web.Services.IPromotionService, WahlMirai.Web.Services.PromotionService>();
builder.Services.AddScoped<WahlMirai.Web.Services.IEventService, WahlMirai.Web.Services.EventService>();
builder.Services.AddScoped<WahlMirai.Web.Services.IProfileService, WahlMirai.Web.Services.ProfileService>();
builder.Services.AddScoped<WahlMirai.Web.Services.ICandidateReviewService, WahlMirai.Web.Services.CandidateReviewService>();
builder.Services.AddScoped<WahlMirai.Web.Services.ICandidacyService, WahlMirai.Web.Services.CandidacyService>();
builder.Services.AddScoped<WahlMirai.Web.Services.IAdminAccountService, WahlMirai.Web.Services.AdminAccountService>();
// M01-00: self-registration against census_whitelist
builder.Services.AddScoped<WahlMirai.Web.Services.IWhitelistService, WahlMirai.Web.Services.WhitelistService>();

// ── Email & Access Recovery Services ─────────────────────────────────────────────
builder.Services.Configure<WahlMirai.Web.Models.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<WahlMirai.Web.Services.IEmailSender, WahlMirai.Web.Services.MailKitEmailSender>();
builder.Services.AddSingleton<WahlMirai.Web.Services.IPendingPasswordStore, WahlMirai.Web.Services.PendingPasswordStore>();
builder.Services.AddScoped<WahlMirai.Web.Services.ICredentialService, WahlMirai.Web.Services.CredentialService>();
builder.Services.AddHostedService<WahlMirai.Web.Services.EmailQueueBackgroundService>();
// ── Localization ─────────────────────────────────────────────────────────────────────
var supportedCultures = new[]
{
    new CultureInfo("es"), // Español
    new CultureInfo("en"), // Inglés
    new CultureInfo("de"), // Alemán
    new CultureInfo("fr"), // Francés
    new CultureInfo("ja"), // Japonés
    new CultureInfo("zh")  // Chino
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("es");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    var cookieProvider = new CookieRequestCultureProvider { CookieName = "CurrentCulture" };
    options.RequestCultureProviders.Insert(0, cookieProvider);
});

// ─────────────────────────────────────────────────────────────────────────────────
// ── Data Protection ──────────────────────────────────────────────────────────────
// Se usa PersistKeysToFileSystem con ruta configurable vía appsettings (DataProtection:KeysPath).
// En despliegues con contenedores o múltiples instancias, sobreescribir esa ruta con
// un volumen compartido o un proveedor de almacenamiento distribuido (Azure Blob, AWS S3, etc.).
// ADVERTENCIA: Si las llaves se pierden, los valores cifrados en encrypted_document quedan
// IRRECUPERABLES. Nunca borrar la carpeta de llaves en producción sin hacer un backup previo.
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "keys";
var keysDirectory = Path.IsPathRooted(keysPath)
    ? new DirectoryInfo(keysPath)
    : new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, keysPath));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keysDirectory)
    .SetApplicationName("WahlMirai");

// IDocumentEncryptionService se registra como Singleton porque IDataProtector (resultado de
// CreateProtector) es hilo-seguro y no tiene estado mutable tras su creación.
builder.Services.AddSingleton<WahlMirai.Web.Services.IDocumentEncryptionService,
    WahlMirai.Web.Services.DocumentEncryptionService>();
// ─────────────────────────────────────────────────────────────────────────────────

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ADMIN", policy => policy.RequireRole("ADMIN", "SUPER_ADMIN"));
        options.AddPolicy("ELECTOR", policy => policy.RequireRole("ELECTOR"));
    });

var app = builder.Build();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);


// ── Auto-migración de documentos al arranque ──────────────────────────────────────
// Al iniciar, cifra automáticamente cualquier encrypted_document que esté en texto plano.
// Es idempotente: los registros ya cifrados se detectan y se omiten sin costo.
// Esto elimina la necesidad de pasos manuales en cualquier PC o entorno de producción:
// basta con importar el SQL, arrancar la app y usar el sistema directamente.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db  = scope.ServiceProvider.GetRequiredService<WahlMiraiDbContext>();
        var enc = scope.ServiceProvider.GetRequiredService<WahlMirai.Web.Services.IDocumentEncryptionService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var voters = db.Voters.ToList();
        int migrated = 0;

        foreach (var voter in voters)
        {
            // Si Decrypt devuelve el mismo valor, el documento está en texto plano (fallback activo).
            // Si devuelve algo distinto, ya estaba cifrado: omitir.
            var decrypted = enc.Decrypt(voter.EncryptedDocument);
            if (decrypted == voter.EncryptedDocument)
            {
                voter.EncryptedDocument = enc.Encrypt(decrypted);
                migrated++;
            }
        }

        if (migrated > 0)
        {
            db.SaveChanges();
            logger.LogInformation("[Startup] Migración automática: {Count} documento(s) cifrado(s).", migrated);
        }
        else
        {
            logger.LogInformation("[Startup] Migración automática: todos los documentos ya estaban cifrados.");
        }
    }
    catch (Exception ex)
    {
        // No interrumpir el arranque si la BD no está disponible todavía
        var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
        startupLogger.LogWarning(ex, "[Startup] No se pudo ejecutar la auto-migración de documentos. " +
            "Si la BD está disponible, usa el botón 'Migrar documentos' en el censo.");
    }
}
// ─────────────────────────────────────────────────────────────────────────────────

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseExceptionHandler("/Error/500");
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseForcePasswordChange();

app.UseStaticFiles();

app.MapHub<ResultsHub>("/hubs/resultsHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapGet("/debug-env", (IWebHostEnvironment env) => new
{
    ContentRootPath = env.ContentRootPath,
    WebRootPath = env.WebRootPath,
    CurrentDir = Directory.GetCurrentDirectory(),
    ViewsExists = Directory.Exists(Path.Combine(env.ContentRootPath, "Views")),
    HomeIndexExists = File.Exists(Path.Combine(env.ContentRootPath, "Views", "Home", "Index.cshtml"))
});

app.Run();

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IOptions<MobileAppSettings> _mobileAppOptions;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IOptions<MobileAppSettings> mobileAppOptions,
        ILogger<HomeController> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _mobileAppOptions = mobileAppOptions;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("descargar-app")]
    public IActionResult DescargarApp()
    {
        var settings = _mobileAppOptions.Value;
        var localFallbackRel = !string.IsNullOrWhiteSpace(settings.LocalFallbackPath)
            ? settings.LocalFallbackPath
            : (_configuration["MobileApp:LocalFallbackPath"] ?? "downloads/wahl-mirai.apk");

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var localPath = Path.Combine(webRoot, localFallbackRel.Replace('/', Path.DirectorySeparatorChar));

        // Prioridad 1 (Intranet / Offline): archivo físico en el servidor local
        if (System.IO.File.Exists(localPath))
        {
            _logger.LogInformation("Sirviendo APK móvil local desde {LocalPath}", localPath);
            return PhysicalFile(localPath, "application/vnd.android.package-archive", "WahlMirai.apk");
        }

        // Respaldo por si el archivo local usa el nombre con guión del repositorio
        var alternativeLocalPath = Path.Combine(webRoot, "downloads", "Wahl-Mirai.apk");
        if (System.IO.File.Exists(alternativeLocalPath))
        {
            _logger.LogInformation("Sirviendo APK móvil local desde ruta alternativa {AlternativeLocalPath}", alternativeLocalPath);
            return PhysicalFile(alternativeLocalPath, "application/vnd.android.package-archive", "WahlMirai.apk");
        }

        // Prioridad 2 (Cloud / Internet): redirección directa a GitHub Releases
        var directUrl = !string.IsNullOrWhiteSpace(settings.ApkDirectUrl)
            ? settings.ApkDirectUrl
            : (_configuration["MobileApp:ApkDirectUrl"] ?? "https://github.com/MrDeep24/WAHL-MIRAI/releases/download/M%C3%B3vil/Wahl-Mirai.apk");

        _logger.LogInformation("Redirigiendo descarga de APK móvil a GitHub Releases: {DirectUrl}", directUrl);
        return Redirect(directUrl);
    }
}

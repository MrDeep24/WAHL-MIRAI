// ============================================================
// Archivo:       RecuperacionAccesoController.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Recibe solicitudes públicas de recuperación y
//                restablecimiento de contraseña mediante token efímero,
//                evitando revelar si un documento pertenece a una cuenta activa (anti-enumeración).
// Requerimientos: RF-M01-02, RN-2, RN-8, RN-9, RN-14, RN-15.
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

[AllowAnonymous]
/// <summary>
/// Expone el flujo público de recuperación de acceso por documento y restablecimiento de contraseña mediante token de un solo uso,
/// con respuesta anti-enumeración para cuentas existentes y no existentes (RF-M01-02, RN-2, RN-9, RN-14, RN-15).
/// </summary>
public class RecuperacionAccesoController : Controller
{
    private readonly WahlMiraiDbContext _dbContext;
    private readonly ICredentialService _credentialService;
    private readonly IMemoryCache _cache;
    private readonly IPasswordResetTokenStore _tokenStore;

    /// <summary>Inicializa el controlador con acceso a usuarios, servicio de credenciales, almacén de tokens y caché en memoria.</summary>
    /// <param name="dbContext">Contexto usado para buscar la cuenta por hash de documento.</param>
    /// <param name="credentialService">Servicio que emite y consume los tokens de restablecimiento.</param>
    /// <param name="cache">Caché en memoria para aplicar cooldown por documento.</param>
    /// <param name="tokenStore">Almacén de tokens efímeros para inspección no destructiva en GET (RN-15).</param>
    public RecuperacionAccesoController(
        WahlMiraiDbContext dbContext,
        ICredentialService credentialService,
        IMemoryCache cache,
        IPasswordResetTokenStore tokenStore)
    {
        _dbContext = dbContext;
        _credentialService = credentialService;
        _cache = cache;
        _tokenStore = tokenStore;
    }

    /// <summary>Muestra el formulario público para solicitar recuperación de acceso.</summary>
    [HttpGet]
    public IActionResult Recuperar()
    {
        return View();
    }

    /// <summary>
    /// Busca una cuenta activa por documento y solicita su enlace de restablecimiento con cooldown de 5 minutos por documento;
    /// siempre muestra el mismo resultado para evitar enumerar cuentas (RF-M01-02, RN-9).
    /// </summary>
    /// <param name="model">Documento enviado en la solicitud de recuperación.</param>
    /// <returns>El formulario con errores de validación o la vista de resultado genérico.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthRecovery")]
    public async Task<IActionResult> Recuperar(RecuperarAccesoViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var documentHash = ComputeSha256(model.Documento);

        var voter = await _dbContext.Voters
            .FirstOrDefaultAsync(v => v.DocumentHash == documentHash);

        if (voter != null && voter.Status == "ACTIVO")
        {
            var cacheKey = "rec:" + documentHash;
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                // NOTA: En memoria: el reinicio del proceso reinicia el cooldown de recuperación (mismo trade-off que IActiveSessionStore).
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(5));

                // NOTA: La entrega progresiva (RN-9) depende del consumidor de la cola, fuera del alcance de este archivo.
                // NOTA: Tras reemplazar BCrypt con un token aleatorio, la única diferencia residual de temporización frente a documentos inexistentes es la inserción en email_queue (~10-25 ms); se acepta porque el número de documento no es secreto en este contexto y la respuesta genérica, el cooldown y el rate limit se conservan; revisar si el modelo de amenazas cambia.
                await _credentialService.IssuePasswordResetTokenAsync((int)voter.Id, null);
            }
        }

        // Siempre retornamos la misma vista de éxito para evitar enumeración (anti-enumeración).
        // Si no se encuentra, simplemente aparentamos éxito.
        ViewBag.SuccessMessage = "Si el documento está registrado, recibirás un enlace de restablecimiento en el correo de contacto asociado en los próximos minutos.";
        return View("Exito");
    }

    /// <summary>
    /// Muestra el formulario para restablecer la contraseña si el token es válido (inspección no destructiva TryPeek, RN-15).
    /// Si el token es inválido o expiró, muestra el estado genérico de enlace inválido sin revelar el motivo.
    /// </summary>
    /// <param name="token">Token efímero de restablecimiento recibido por query string.</param>
    [HttpGet]
    [EnableRateLimiting("AuthRecovery")]
    public IActionResult Restablecer([FromQuery] string? token)
    {
        SetSecurityHeaders();

        if (string.IsNullOrWhiteSpace(token) || !_tokenStore.TryPeek(token, out _))
        {
            return View(new RestablecerViewModel { IsInvalidLink = true });
        }

        return View(new RestablecerViewModel { Token = token, IsInvalidLink = false });
    }

    /// <summary>
    /// Valida la complejidad de la nueva contraseña y completa el restablecimiento mediante el token (RF-M01-02, RN-2, RN-14, RN-15).
    /// Si la validación de modelo falla, NO consume el token. Si el token es inválido o expiró, muestra el estado genérico.
    /// </summary>
    /// <param name="model">Modelo con el token, la nueva contraseña y su confirmación.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthRecovery")]
    public async Task<IActionResult> Restablecer(RestablecerViewModel model)
    {
        SetSecurityHeaders();

        if (!ModelState.IsValid)
        {
            // NOTA: Si el modelo es inválido (complejidad o discrepancia), NO se consume el token (RN-15).
            return View(model);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, _) = await _credentialService.CompletePasswordResetAsync(
            model.Token,
            model.Password,
            ipAddress,
            HttpContext.RequestAborted);

        if (!success)
        {
            return View(new RestablecerViewModel { IsInvalidLink = true });
        }

        ViewBag.SuccessMessage = "Tu contraseña ha sido actualizada. Ya puedes iniciar sesión con tu nueva contraseña.";
        return View("Exito");
    }

    private void SetSecurityHeaders()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["Cache-Control"] = "no-cache, no-store";
        Response.Headers["Pragma"] = "no-cache";
    }

    private string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

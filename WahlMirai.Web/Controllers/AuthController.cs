// ============================================================
// Archivo:       AuthController.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Gestiona el inicio y cierre de sesión, el cambio
//                voluntario de contraseña y el auto-registro de electores.
// Requerimientos: RF-M01-00, RF-M01-01, RF-M07-01, RN-1, RN-1.1, RN-14, RN-15.
// ============================================================
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System.Security.Claims;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

/// <summary>
/// Gestiona la autenticación por documento, las sesiones y el auto-registro protegido
/// por lista blanca y contador por IP (RF-M01-00, RF-M01-01, RN-1, RN-1.1, RN-14, RN-15).
/// También expone la ruta existente de cambio voluntario de contraseña (RF-M07-01).
/// </summary>
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IWhitelistService _whitelistService;
    private readonly IActiveSessionStore _activeSessionStore;
    private readonly ILoginAttemptStore _loginAttemptStore;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthController> _logger;
    private readonly int _registrationFailureThreshold;

    // Generic anti-enumeration message for self-registration failures (RN-1).
    // Shows the same text regardless of whether the document was not found
    // in census_whitelist or was found but already claimed, so an external
    // observer cannot distinguish the two cases.
    private const string AntiEnumerationMessage =
        "No fue posible procesar el auto-registro con el documento ingresado. " +
        "Si consideras que esto es un error o requieres asistencia, " +
        "por favor contacta al Administrador institucional.";

    /// <summary>
    /// Crea el controlador con los servicios de autenticación, whitelist, sesión y mitigación RN-15.
    /// </summary>
    /// <param name="authService">Valida credenciales y proporciona funciones de hash.</param>
    /// <param name="whitelistService">Consulta y reclama entradas para el auto-registro.</param>
    /// <param name="activeSessionStore">Registra e invalida la sesión del usuario.</param>
    /// <param name="loginAttemptStore">Mantiene contadores aislados para login y auto-registro (RN-1, RN-15).</param>
    /// <param name="auditService">Registra la activación de bloqueos de auto-registro (RN-8, RN-15).</param>
    /// <param name="logger">Registra advertencias al activar el bloqueo por IP.</param>
    /// <param name="configuration">Proporciona el umbral configurable del bucket de auto-registro.</param>
    public AuthController(
        IAuthService authService,
        IWhitelistService whitelistService,
        IActiveSessionStore activeSessionStore,
        ILoginAttemptStore loginAttemptStore,
        IAuditService auditService,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _whitelistService = whitelistService;
        _activeSessionStore = activeSessionStore;
        _loginAttemptStore = loginAttemptStore;
        _auditService = auditService;
        _logger = logger;
        _registrationFailureThreshold = Math.Max(1, configuration.GetValue<int>("RateLimiting:RegisterFailureThreshold", 30));
    }

    /// <summary>
    /// Muestra el formulario de acceso o redirige al panel del usuario autenticado.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDefaultDashboard();
        }
        return View();
    }

    /// <summary>
    /// Valida las credenciales de una cuenta activa y emite la cookie con su sello de sesión
    /// (RF-M01-01, RN-14).
    /// </summary>
    /// <param name="model">Documento y contraseña enviados desde el formulario.</param>
    /// <returns>La vista con errores o la redirección al panel correspondiente al rol.</returns>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var voter = await _authService.ValidateLoginAsync(model.Document, model.Password, ip);

        if (voter == null)
        {
            ModelState.AddModelError("", "Documento o contraseña incorrectos, o cuenta inactiva.");
            return View(model);
        }

        var sessionStamp = Guid.NewGuid().ToString();
        _activeSessionStore.RegisterSession((uint)voter.Id, sessionStamp);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, voter.Id.ToString()),
            new Claim(ClaimTypes.Name, voter.FullName),
            new Claim(ClaimTypes.Role, voter.Role.Name),
            new Claim("SessionStamp", sessionStamp)
        };

        if (voter.Grade != null)
        {
            claims.Add(new Claim("Grade", voter.Grade.Name));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return RedirectToDefaultDashboard(voter.Role.Name);
    }

    /// <summary>
    /// Muestra el formulario de cambio voluntario de contraseña (RF-M07-01).
    /// </summary>
    [Authorize]
    [HttpGet]
    public IActionResult CambiarClave()
    {
        return View();
    }

    // NOTA: Este endpoint usa ChangePasswordViewModel, que permite desde 6 caracteres
    // y no aplica la regla de complejidad validada por ProfileController.ChangePassword (RF-M07-01).
    /// <summary>
    /// Actualiza la contraseña del usuario autenticado usando las validaciones del modelo
    /// de esta ruta y registra la operación mediante IAuthService (RF-M07-01).
    /// </summary>
    /// <param name="model">Nueva contraseña y su confirmación.</param>
    /// <returns>La vista con errores o la redirección al panel del usuario.</returns>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CambiarClave(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var success = await _authService.ChangePasswordAsync(userId, model.NewPassword, ip);

        if (success)
        {
            return RedirectToDefaultDashboard();
        }

        ModelState.AddModelError("", "No se pudo cambiar la contraseña.");
        return View(model);
    }

    /// <summary>
    /// Invalida el sello de la sesión actual, cierra la cookie de autenticación y redirige al acceso
    /// (RN-14).
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (uint.TryParse(userIdStr, out var userId))
        {
            _activeSessionStore.InvalidateSession(userId, SessionInvalidationReason.LoggedOut);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ────────────────────────────────────────────────────────────────────────────
    // M01-00 — Self-registration against census_whitelist (RF-M01-00, RN-1, RN-1.1)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Step 1 GET: render the document lookup form (RF-M01-00, RN-1).
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDefaultDashboard();

        return View(new RegistroStep1ViewModel());
    }

    /// <summary>
    /// Step 1 POST: validate the document against census_whitelist (RF-M01-00, RN-1, RN-15).
    /// On success, redirect to Completar (step 2) passing whitelist data via TempData.
    /// On failure, show the generic anti-enumeration message — does NOT reveal
    /// whether the document was absent or already claimed (RN-1).
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthRegister")]
    public async Task<IActionResult> Registro(RegistroStep1ViewModel model)
    {
        var ipAddress = GetClientIpAddress();
        // NOTA: La auditoría forense (audit_log) conserva la IP cruda observada (ipAddress),
        // mientras que la clave del bucket de auto-registro (bucketKey) usa ClientIpKey
        // para particionar IPv4 (/32) e IPv6 (/64), mitigando la evasión por rotación de prefijos.
        var bucketKey = GetRegistrationBucketKey();
        // NOTA: El límite por IP encarece la enumeración, pero rotar IPs lo evade y Completar revela nombre/grado. OTP al correo censal depende de otros módulos.
        if (_loginAttemptStore.IsLockedOut(bucketKey, out _))
            return RegistrationFailureView(model);

        if (!ModelState.IsValid)
            return View(model);

        var docHash = _authService.HashDocument(model.Document);
        var entry = await _whitelistService.GetUnclaimedEntryByDocumentHashAsync(docHash);

        if (entry == null)
        {
            // Anti-enumeration: same message for "not found" and "already claimed".
            await RecordRegistrationFailureAsync(bucketKey, ipAddress);
            return RegistrationFailureView(model);
        }

        // Pass whitelist display data to step 2 via TempData.
        TempData["Registro_WhitelistId"] = (int)entry.Id;
        TempData["Registro_FullName"]    = entry.FullName;
        TempData["Registro_GradeName"]   = entry.Grade.Name;
        TempData["Registro_Document"]    = model.Document;

        return RedirectToAction(nameof(Completar));
    }

    /// <summary>
    /// Step 2 GET: render the registration completion form, pre-filled with
    /// whitelist data (full_name, grade — read-only, not editable by student).
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [EnableRateLimiting("AuthRegister")]
    public IActionResult Completar()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDefaultDashboard();

        if (!TempData.ContainsKey("Registro_WhitelistId"))
            return RedirectToAction(nameof(Registro));

        var model = new RegistroStep2ViewModel
        {
            WhitelistId = (uint)(int)TempData["Registro_WhitelistId"]!,
            FullName    = TempData["Registro_FullName"]!.ToString()!,
            GradeName   = TempData["Registro_GradeName"]!.ToString()!,
            Document    = TempData["Registro_Document"]!.ToString()!,
        };

        // Re-expose TempData for the subsequent POST in case the page is re-rendered
        // after a validation failure.
        TempData.Keep();

        return View(model);
    }

    /// <summary>
    /// Step 2 POST: create the ELECTOR account, claim the whitelist entry,
    /// write the audit log, and auto-login the student (RF-M01-00, RN-1.1, RN-15).
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthRegister")]
    public async Task<IActionResult> Completar(RegistroStep2ViewModel model)
    {
        // Keep TempData alive in case we need to return the view on error.
        TempData.Keep();

        var ipAddress = GetClientIpAddress();
        // NOTA: La auditoría forense (audit_log) conserva la IP cruda observada (ipAddress),
        // mientras que la clave del bucket de auto-registro (bucketKey) usa ClientIpKey
        // para particionar IPv4 (/32) e IPv6 (/64), mitigando la evasión por rotación de prefijos.
        var bucketKey = GetRegistrationBucketKey();
        if (_loginAttemptStore.IsLockedOut(bucketKey, out _))
            return RegistrationFailureView(model);

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var voter = await _whitelistService.RegisterElectorAsync(
                model.Document,
                model.WhitelistId,
                model.ContactEmail,
                model.Password,
                ipAddress);

            // Auto-login via Cookie Authentication, exactly mirroring the Login action.
            var sessionStamp = Guid.NewGuid().ToString();
            _activeSessionStore.RegisterSession((uint)voter.Id, sessionStamp);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, voter.Id.ToString()),
                new Claim(ClaimTypes.Name,           voter.FullName),
                new Claim(ClaimTypes.Role,           voter.Role.Name),
                new Claim("SessionStamp",            sessionStamp)
            };

            if (voter.Grade != null)
                claims.Add(new Claim("Grade", voter.Grade.Name));

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            TempData["Success"] = $"¡Bienvenido/a, {voter.FullName}! Tu cuenta ha sido creada exitosamente.";
            return RedirectToAction("Dashboard", "Elector");
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("already claimed") ||
            ex.Message.Contains("does not match") ||
            ex.Message.Contains("not found") ||
            ex.Message.Contains("already exists"))
        {
            await RecordRegistrationFailureAsync(bucketKey, ipAddress);
            return RegistrationFailureView(model);
        }
        catch (DbUpdateException ex) when (IsMySqlDuplicateKeyViolation(ex))
        {
            // NOTA: WhitelistService revierte su transacción antes de relanzar; este request no vuelve a usar el DbContext y se dispone al finalizar.
            await RecordRegistrationFailureAsync(bucketKey, ipAddress);
            return RegistrationFailureView(model);
        }
        catch (DbUpdateException)
        {
            throw;
        }
    }

    /// <summary>Agrega el único mensaje anti-enumeración compartido por RF-M01-00 (RN-1).</summary>
    private IActionResult RegistrationFailureView<TModel>(TModel model)
    {
        ModelState.AddModelError(string.Empty, AntiEnumerationMessage);
        return View(model);
    }

    /// <summary>Incrementa el bucket IP de registro y audita sólo la activación del bloqueo (RN-1, RN-15).</summary>
    private async Task RecordRegistrationFailureAsync(string bucketKey, string ipAddress)
    {
        var result = _loginAttemptStore.RegisterFailure(bucketKey, _registrationFailureThreshold);
        if (!result.JustLockedOut)
            return;

        _logger.LogWarning(
            "Self-registration IP bucket {BucketKey} locked after {FailureCount} failures until {LockedUntilUtc}.",
            bucketKey,
            result.FailedCount,
            result.LockedUntilUtc);

        await _auditService.LogAsync(
            "REGISTRO_LOCKED_OUT",
            null,
            "census_whitelist",
            null,
            null,
            null,
            null,
            $"Registration IP bucket reached {_registrationFailureThreshold} failures; locked until {result.LockedUntilUtc:O}.",
            ipAddress);
    }

    /// <summary>Construye una clave IP aislada del bucket de auto-registro particionada con ClientIpKey (RN-1, RN-15, RF-M01-00).</summary>
    private string GetRegistrationBucketKey() =>
        $"register:{ClientIpKey.From(HttpContext.Connection.RemoteIpAddress)}";

    /// <summary>Obtiene el peer IP observado por ASP.NET Core para el bucket de registro (RN-15).</summary>
    private string GetClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

    /// <summary>Reconoce sólo MySQL DuplicateKeyEntry (1062), sin ocultar otros errores de persistencia (RN-1.1).</summary>
    private static bool IsMySqlDuplicateKeyViolation(DbUpdateException exception)
    {
        for (var innerException = exception.InnerException; innerException != null; innerException = innerException.InnerException)
        {
            if (innerException is MySqlException mySqlException && mySqlException.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
                return true;
        }

        return false;
    }

    private IActionResult RedirectToDefaultDashboard(string? role = null)
    {
        role ??= User.FindFirstValue(ClaimTypes.Role);

        if (role == "ADMIN" || role == "SUPER_ADMIN") return RedirectToAction("Index", "AdminEvents");
        return RedirectToAction("Dashboard", "Elector");
    }
}

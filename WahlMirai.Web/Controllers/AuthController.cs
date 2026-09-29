// ============================================================
// Archivo:       AuthController.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Gestiona el inicio y cierre de sesión, el cambio
//                voluntario de contraseña y el auto-registro de electores.
// Requerimientos: RF-M01-00, RF-M01-01, RF-M07-01, RN-1, RN-1.1, RN-14.
// ============================================================
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

/// <summary>
/// Gestiona la autenticación por documento, las sesiones de usuario y el flujo de
/// auto-registro contra la lista blanca (RF-M01-00, RF-M01-01, RN-1, RN-1.1, RN-14).
/// También expone la ruta existente de cambio voluntario de contraseña (RF-M07-01).
/// </summary>
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IWhitelistService _whitelistService;
    private readonly IActiveSessionStore _activeSessionStore;

    // Generic anti-enumeration message for self-registration failures (RN-1).
    // Shows the same text regardless of whether the document was not found
    // in census_whitelist or was found but already claimed, so an external
    // observer cannot distinguish the two cases.
    private const string AntiEnumerationMessage =
        "No fue posible procesar el auto-registro con el documento ingresado. " +
        "Si consideras que esto es un error o requieres asistencia, " +
        "por favor contacta al Administrador institucional.";

    /// <summary>
    /// Crea el controlador con los servicios de autenticación, lista blanca y sesión activa.
    /// </summary>
    /// <param name="authService">Valida credenciales y proporciona funciones de hash.</param>
    /// <param name="whitelistService">Consulta y reclama entradas para el auto-registro.</param>
    /// <param name="activeSessionStore">Registra e invalida la sesión del usuario.</param>
    public AuthController(
        IAuthService authService,
        IWhitelistService whitelistService,
        IActiveSessionStore activeSessionStore)
    {
        _authService = authService;
        _whitelistService = whitelistService;
        _activeSessionStore = activeSessionStore;
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
    /// Step 1 GET: render the document lookup form.
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
    /// Step 1 POST: validate the document against census_whitelist.
    /// On success, redirect to Completar (step 2) passing whitelist data via TempData.
    /// On failure, show the generic anti-enumeration message — does NOT reveal
    /// whether the document was absent or already claimed (RN-1).
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroStep1ViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var docHash = _authService.HashDocument(model.Document);
        var entry = await _whitelistService.GetUnclaimedEntryByDocumentHashAsync(docHash);

        if (entry == null)
        {
            // Anti-enumeration: same message for "not found" and "already claimed".
            ModelState.AddModelError(string.Empty, AntiEnumerationMessage);
            return View(model);
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
    /// write the audit log, and auto-login the student (RF-M01-00).
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Completar(RegistroStep2ViewModel model)
    {
        // Keep TempData alive in case we need to return the view on error.
        TempData.Keep();

        if (!ModelState.IsValid)
            return View(model);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        try
        {
            var voter = await _whitelistService.RegisterElectorAsync(
                model.Document,
                model.WhitelistId,
                model.ContactEmail,
                model.Password,
                ip);

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
            ex.Message.Contains("not found"))
        {
            // The whitelist entry was claimed between step 1 and step 2 (race condition),
            // or the document was tampered with. Show the same generic anti-enumeration message.
            ModelState.AddModelError(string.Empty, AntiEnumerationMessage);
            return View(model);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            // Document or email already registered (duplicate).
            ModelState.AddModelError(string.Empty, AntiEnumerationMessage);
            return View(model);
        }
    }

    private IActionResult RedirectToDefaultDashboard(string? role = null)
    {
        role ??= User.FindFirstValue(ClaimTypes.Role);

        if (role == "ADMIN" || role == "SUPER_ADMIN") return RedirectToAction("Index", "AdminEvents");
        return RedirectToAction("Dashboard", "Elector");
    }
}

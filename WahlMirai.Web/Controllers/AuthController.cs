using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IWhitelistService _whitelistService;

    // Generic anti-enumeration message for self-registration failures (RN-1).
    // Shows the same text regardless of whether the document was not found
    // in census_whitelist or was found but already claimed, so an external
    // observer cannot distinguish the two cases.
    private const string AntiEnumerationMessage =
        "No fue posible procesar el auto-registro con el documento ingresado. " +
        "Si consideras que esto es un error o requieres asistencia, " +
        "por favor contacta al Administrador institucional.";

    public AuthController(IAuthService authService, IWhitelistService whitelistService)
    {
        _authService = authService;
        _whitelistService = whitelistService;
    }

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

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var voter = await _authService.ValidateLoginAsync(model.Document, model.Password);

        if (voter == null)
        {
            ModelState.AddModelError("", "Documento o contraseña incorrectos, o cuenta inactiva.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, voter.Id.ToString()),
            new Claim(ClaimTypes.Name, voter.FullName),
            new Claim(ClaimTypes.Role, voter.Role.Name)
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

    [Authorize]
    [HttpGet]
    public IActionResult CambiarClave()
    {
        return View();
    }

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

    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
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
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, voter.Id.ToString()),
                new Claim(ClaimTypes.Name,           voter.FullName),
                new Claim(ClaimTypes.Role,           voter.Role.Name),
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

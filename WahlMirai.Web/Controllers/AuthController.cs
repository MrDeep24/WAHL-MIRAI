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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
            new Claim(ClaimTypes.Role, voter.Role.Name),
            new Claim("RequiereCambioClave", voter.RequiereCambioClave.ToString())
        };

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
            // Update claim so they don't get forced again
            var claims = User.Claims.ToList();
            var reqClaim = claims.FirstOrDefault(c => c.Type == "RequiereCambioClave");
            if (reqClaim != null) claims.Remove(reqClaim);
            claims.Add(new Claim("RequiereCambioClave", "False"));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return RedirectToDefaultDashboard();
        }

        ModelState.AddModelError("", "No se pudo cambiar la contraseña.");
        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private IActionResult RedirectToDefaultDashboard(string? role = null)
    {
        role ??= User.FindFirstValue(ClaimTypes.Role);
        
        if (role == "ADMIN") return RedirectToAction("Index", "AdminEvents");
        return RedirectToAction("Dashboard", "Elector");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly WahlMiraiDbContext _context;
    private readonly IProfileService _profileService;
    private readonly IDocumentEncryptionService _encryptionService;

    public ProfileController(
        WahlMiraiDbContext context,
        IProfileService profileService,
        IDocumentEncryptionService encryptionService)
    {
        _context = context;
        _profileService = profileService;
        _encryptionService = encryptionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var voter = await _context.Voters
            .Include(v => v.Role)
            .Include(v => v.Grade)
            .FirstOrDefaultAsync(v => v.Id == userId);

        if (voter == null) return NotFound();

        var model = new ProfileViewModel
        {
            FullName = voter.FullName,
            DocumentDisplay = _encryptionService.Decrypt(voter.EncryptedDocument),
            GradeName = voter.Grade?.Name,
            Role = voter.Role.Name,
            Status = voter.Status,
            ContactEmail = voter.ContactEmail
        };

        return View(model);
    }

    /// <summary>
    /// Verifica la contraseña actual del usuario (llamada AJAX).
    /// Retorna JSON: { ok: true } si es correcta, { ok: false, message: "..." } si no.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyCurrentPassword([FromBody] VerifyPasswordRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, message = "Sesión no válida." });

        if (string.IsNullOrEmpty(request?.Password))
            return Json(new { ok = false, message = "Debes ingresar tu contraseña actual." });

        var voter = await _context.Voters.FindAsync((uint)userId);
        if (voter == null)
            return Json(new { ok = false, message = "Usuario no encontrado." });

        var isValid = BCrypt.Net.BCrypt.Verify(request.Password, voter.PasswordHash);
        if (!isValid)
            return Json(new { ok = false, message = "La contraseña actual no es correcta." });

        return Json(new { ok = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        // Validación server-side adicional de complejidad de nueva contraseña si se proporciona
        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            if (!Regex.IsMatch(model.NewPassword, @"[A-Z]"))
                ModelState.AddModelError("NewPassword", "La nueva contraseña debe contener al menos una letra mayúscula.");
            if (!Regex.IsMatch(model.NewPassword, @"[!@#$%^&*()\-_=+\[\]{};':""\\|,.<>/?`~]"))
                ModelState.AddModelError("NewPassword", "La nueva contraseña debe contener al menos un símbolo especial.");
        }

        if (!ModelState.IsValid)
        {
            // Re-populate read-only fields since they are not posted back
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var voter = await _context.Voters
                    .Include(v => v.Role)
                    .Include(v => v.Grade)
                    .FirstOrDefaultAsync(v => v.Id == userId);

                if (voter != null)
                {
                    model.FullName = voter.FullName;
                    model.DocumentDisplay = _encryptionService.Decrypt(voter.EncryptedDocument);
                    model.GradeName = voter.Grade?.Name;
                    model.Role = voter.Role.Name;
                    model.Status = voter.Status;
                }
            }
            ViewBag.Error = "Por favor corrige los errores del formulario antes de continuar.";
            return View("Index", model);
        }

        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(currentUserIdStr, out int currentUserId)) return Unauthorized();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var (success, errorMessage) = await _profileService.UpdateProfileAsync(
            currentUserId,
            model.ContactEmail,
            model.CurrentPassword,
            model.NewPassword,
            ip
        );

        if (success)
        {
            TempData["Success"] = "Perfil actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        
        TempData["Error"] = errorMessage;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Cambia la contraseña del usuario autenticado (llamada desde el modal de 2 pasos).
    /// Espera JSON: { currentPassword, newPassword, confirmNewPassword }
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, message = "Sesión no válida." });

        // Validaciones básicas
        if (string.IsNullOrEmpty(request?.CurrentPassword))
            return Json(new { ok = false, message = "Debes ingresar tu contraseña actual." });

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
            return Json(new { ok = false, message = "La nueva contraseña debe tener al menos 8 caracteres." });

        if (!Regex.IsMatch(request.NewPassword, @"[A-Z]"))
            return Json(new { ok = false, message = "La nueva contraseña debe contener al menos una letra mayúscula." });

        if (!Regex.IsMatch(request.NewPassword, @"[!@#$%^&*()\-_=+\[\]{};':""\\|,.<>/?`~]"))
            return Json(new { ok = false, message = "La nueva contraseña debe contener al menos un símbolo especial (!@#$%...)." });

        if (request.NewPassword != request.ConfirmNewPassword)
            return Json(new { ok = false, message = "Las contraseñas nuevas no coinciden." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var (success, errorMessage) = await _profileService.UpdateProfileAsync(
            userId,
            newContactEmail: null, // null indica: no cambiar el email en este flujo
            currentPassword: request.CurrentPassword,
            newPassword: request.NewPassword,
            ipAddress: ip
        );

        if (success)
            return Json(new { ok = true, message = "¡Contraseña actualizada correctamente!" });

        return Json(new { ok = false, message = errorMessage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPasswordReset()
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(currentUserIdStr, out int currentUserId)) return Unauthorized();

        var (success, message) = await _profileService.RequestPasswordResetAsync(currentUserId);
        
        if (success)
        {
            TempData["Success"] = message;
        }
        else
        {
            TempData["Error"] = message;
        }

        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Request body para VerifyCurrentPassword.</summary>
public record VerifyPasswordRequest(string? Password);

/// <summary>Request body para ChangePassword.</summary>
public record ChangePasswordRequest(string? CurrentPassword, string? NewPassword, string? ConfirmNewPassword);

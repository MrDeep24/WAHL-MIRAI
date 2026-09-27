// ============================================================
// Archivo:       ProfileController.cs
// Módulo:        M07 — Perfil de Usuario y Autogestión de Credenciales
// Desarrollador: Kevin
// Descripción:   Expone las acciones autenticadas para consultar el perfil,
//                editar datos de contacto y contraseña, y solicitar su reinicio.
//                Adapta los datos de sesión y delega las operaciones de perfil.
// Requerimientos: RF-M07-01; soporte al flujo de recuperación RF-M01-02.
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

/// <summary>
/// Gestiona la consulta y autogestión del perfil del usuario autenticado.
/// </summary>
[Authorize]
public class ProfileController : Controller
{
    private readonly WahlMiraiDbContext _context;
    private readonly IProfileService _profileService;
    private readonly IDocumentEncryptionService _encryptionService;

    /// <summary>
    /// Inicializa el controlador con el contexto y los servicios de perfil y cifrado.
    /// </summary>
    /// <param name="context">Contexto para consultar los datos del usuario.</param>
    /// <param name="profileService">Servicio de actualización y reinicio de credenciales.</param>
    /// <param name="encryptionService">Servicio para descifrar el documento mostrado en el perfil.</param>
    public ProfileController(
        WahlMiraiDbContext context,
        IProfileService profileService,
        IDocumentEncryptionService encryptionService)
    {
        _context = context;
        _profileService = profileService;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Presenta los datos del perfil propio del usuario autenticado.
    /// </summary>
    /// <returns>La vista del perfil, o una respuesta de autorización o no encontrado.</returns>
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
            PositionTitle = voter.PositionTitle,
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

    /// <summary>
    /// Actualiza el correo de contacto o la contraseña del perfil propio.
    /// </summary>
    /// <param name="model">Datos editables y de validación del perfil.</param>
    /// <returns>La vista con errores de validación o una redirección con el resultado.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        // NOTA: Update usa un modelo de validación de contraseña más
        // débil que ChangePassword. Ver también la nota gemela en
        // AuthController.CambiarClave (M01). Pendiente de decidir cuál
        // política debe prevalecer (Kevin, 26-Sept-2026).
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
                    model.PositionTitle = voter.PositionTitle;
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
        // NOTA: Update usa un modelo de validación de contraseña más
        // débil que ChangePassword. Ver también la nota gemela en
        // AuthController.CambiarClave (M01). Pendiente de decidir cuál
        // política debe prevalecer (Kevin, 26-Sept-2026).
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

    /// <summary>
    /// Solicita el reinicio de contraseña para la cuenta autenticada.
    /// </summary>
    /// <returns>Una redirección al perfil con el resultado de la solicitud.</returns>
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

    /// <summary>
    /// Actualiza el correo de contacto del usuario autenticado (llamada AJAX desde el modal de correo).
    /// Espera JSON: { newEmail: string }
    /// Retorna JSON: { ok, emailSaved, notificationSent, newEmail, message }
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, message = "Sesión no válida." });

        if (string.IsNullOrWhiteSpace(request?.NewEmail))
            return Json(new { ok = false, message = "Debes ingresar un correo electrónico." });

        // Validación de formato server-side (no confiar solo en el cliente)
        var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailValidator.IsValid(request.NewEmail.Trim()))
            return Json(new { ok = false, message = "El formato del correo electrónico no es válido." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var (success, emailSaved, notificationSent, errorMessage) =
            await _profileService.UpdateContactEmailAsync(userId, request.NewEmail.Trim(), ip);

        if (!success)
            return Json(new { ok = false, message = errorMessage });

        if (!notificationSent)
        {
            return Json(new
            {
                ok = true,
                emailSaved = true,
                notificationSent = false,
                newEmail = request.NewEmail.Trim(),
                message = "Correo actualizado, pero no pudimos enviar la notificación de confirmación. " +
                          "El cambio quedó guardado correctamente."
            });
        }

        return Json(new
        {
            ok = true,
            emailSaved = true,
            notificationSent = true,
            newEmail = request.NewEmail.Trim(),
            message = "¡Correo de contacto actualizado correctamente!"
        });
    }
}

/// <summary>Request body para VerifyCurrentPassword.</summary>
public record VerifyPasswordRequest(string? Password);

/// <summary>Request body para ChangePassword.</summary>
public record ChangePasswordRequest(string? CurrentPassword, string? NewPassword, string? ConfirmNewPassword);

/// <summary>Request body para UpdateEmail.</summary>
public record UpdateEmailRequest(string? NewEmail);

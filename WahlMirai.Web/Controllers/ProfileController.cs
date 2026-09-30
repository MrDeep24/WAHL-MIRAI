// ============================================================
// Archivo:       ProfileController.cs
// Módulo:        M07 — Perfil de Usuario y Autogestión de Credenciales
// Desarrollador: Kevin
// Descripción:   Expone las acciones autenticadas para consultar el perfil,
//                editar datos de contacto y contraseña, y solicitar su reinicio.
//                Adapta los datos de sesión y delega las operaciones de perfil.
// Requerimientos: RF-M07-01; soporte al flujo de recuperación RF-M01-02; RN-15; RN-8.
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
    private readonly ILoginAttemptStore _loginAttemptStore;

    /// <summary>
    /// Inicializa el controlador con el contexto y los servicios de perfil, cifrado y throttling (RN-15).
    /// </summary>
    /// <param name="context">Contexto para consultar los datos del usuario.</param>
    /// <param name="profileService">Servicio de actualización y reinicio de credenciales.</param>
    /// <param name="encryptionService">Servicio para descifrar el documento mostrado en el perfil.</param>
    /// <param name="loginAttemptStore">Almacén de intentos para throttling del bucket de perfil (RN-15).</param>
    public ProfileController(
        WahlMiraiDbContext context,
        IProfileService profileService,
        IDocumentEncryptionService encryptionService,
        ILoginAttemptStore loginAttemptStore)
    {
        _context = context;
        _profileService = profileService;
        _encryptionService = encryptionService;
        _loginAttemptStore = loginAttemptStore;
    }

    /// <summary>Clave del bucket de intentos para operaciones de perfil (RN-15).</summary>
    private string ProfileBucket() =>
        $"profile:{User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown"}";

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
    /// Verifica la contraseña actual del usuario (llamada AJAX, Paso 1 del modal de contraseña).
    /// Aplica throttling por bucket de perfil (RN-15) y auditoría (RN-8) a través de <see cref="IProfileService.VerifyCurrentPasswordAsync"/>.
    /// Retorna JSON: { ok: true } si es correcta, { ok: false, message: "..." } si no.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyCurrentPassword([FromBody] VerifyPasswordRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, message = "Sesión no válida." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var (success, errorMessage) = await _profileService.VerifyCurrentPasswordAsync(userId, request?.Password, ip);
        if (!success)
        {
            return Json(new { ok = false, message = errorMessage });
        }

        return Json(new { ok = true });
    }

    /// <summary>
    /// Cambia la contraseña del usuario autenticado (llamada desde el modal de 2 pasos).
    /// Aplica throttling por bucket de perfil (RN-15).
    /// Espera JSON: { currentPassword, newPassword, confirmNewPassword }
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        // NOTA: La validación de complejidad se unificó según RF-M07-01 (mínimo 8 caracteres,
        // al menos una mayúscula y un símbolo especial). La verificación de contraseña actual
        // y el throttling residen en ProfileService.UpdateProfileAsync bajo el bucket profile:{id}.
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, message = "Sesión no válida." });

        // Throttling de perfil — bucket independiente del login (RN-15)
        var bucket = ProfileBucket();
        if (_loginAttemptStore.IsLockedOut(bucket, out var remaining))
        {
            var mins = (int)Math.Ceiling(remaining.TotalMinutes);
            return Json(new { ok = false, message = $"Demasiados intentos fallidos. Por seguridad, la operación está bloqueada temporalmente. Intenta de nuevo en {mins} minuto(s)." });
        }

        // Validaciones básicas
        if (string.IsNullOrEmpty(request?.CurrentPassword))
            return Json(new { ok = false, message = "Debes ingresar tu contraseña actual." });

        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 8)
            return Json(new { ok = false, message = "La nueva contraseña debe tener al menos 8 caracteres." });

        if (!Regex.IsMatch(request.NewPassword, @"[A-Z]"))
            return Json(new { ok = false, message = "La nueva contraseña debe contener al menos una letra mayúscula." });

        if (!Regex.IsMatch(request.NewPassword, @"[!@#$%^&*()\-_=+\[\]{};':""|,.<>/?`~]"))
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

        if (!success)
        {
            return Json(new { ok = false, message = errorMessage });
        }

        return Json(new { ok = true, message = "¡Contraseña actualizada correctamente!" });
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
    /// Requiere verificación de la contraseña actual (RF-M07-01). El throttling se gestiona en
    /// <see cref="IProfileService.UpdateContactEmailAsync"/> usando el bucket de perfil (RN-15).
    /// Espera JSON: { newEmail: string, currentPassword: string }
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

        // La contraseña actual es obligatoria para cambiar el correo (RF-M07-01)
        if (string.IsNullOrEmpty(request.CurrentPassword))
            return Json(new { ok = false, message = "Debes ingresar tu contraseña actual para modificar el correo de contacto." });

        // Validación de formato server-side (no confiar solo en el cliente)
        var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailValidator.IsValid(request.NewEmail.Trim()))
            return Json(new { ok = false, message = "El formato del correo electrónico no es válido." });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // La contraseña nunca se registra ni se refleja en la respuesta JSON
        var (success, emailSaved, notificationSent, errorMessage) =
            await _profileService.UpdateContactEmailAsync(userId, request.NewEmail.Trim(), request.CurrentPassword, ip);

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
                message = "Correo actualizado. El aviso de seguridad a tu correo anterior no pudo enviarse, pero el cambio quedó guardado."
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

/// <summary>Request body para UpdateEmail (RF-M07-01). Nunca se registra ni se refleja la contraseña.</summary>
public record UpdateEmailRequest(string? NewEmail, string? CurrentPassword);

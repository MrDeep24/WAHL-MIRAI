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
using WahlMirai.Web.Models;
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
    private readonly IRegistrationOtpStore _otpStore;
    private readonly IEmailSender? _emailSender;
    private readonly int _registrationFailureThreshold;

    public const string RegistrationTicketCookieName = "wm_reg_ticket";

    // Generic anti-enumeration message for self-registration failures (RN-1).
    // Shows the same text regardless of whether the document was not found
    // in census_whitelist or was found but already claimed, so an external
    // observer cannot distinguish the two cases.
    private const string AntiEnumerationMessage =
        "No fue posible procesar el auto-registro con el documento ingresado. " +
        "Si consideras que esto es un error o requieres asistencia, " +
        "por favor contacta al Administrador institucional.";

    /// <summary>
    /// Crea el controlador con los servicios de autenticación, whitelist, sesión, OTP y mitigación RN-15.
    /// </summary>
    public AuthController(
        IAuthService authService,
        IWhitelistService whitelistService,
        IActiveSessionStore activeSessionStore,
        ILoginAttemptStore loginAttemptStore,
        IAuditService auditService,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        IRegistrationOtpStore? otpStore = null,
        IEmailSender? emailSender = null)
    {
        _authService = authService;
        _whitelistService = whitelistService;
        _activeSessionStore = activeSessionStore;
        _loginAttemptStore = loginAttemptStore;
        _auditService = auditService;
        _logger = logger;
        _otpStore = otpStore ?? new RegistrationOtpStore(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        _emailSender = emailSender;
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
    /// On success or failure (missing, claimed, empty email), issues a ticket and redirects (302) to VerificarCodigo.
    /// Indistinguishable responses mitigate document enumeration at Step 1 (A2).
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
        if (_loginAttemptStore.IsLockedOut(bucketKey, out _))
            return RegistrationFailureView(model);

        if (!ModelState.IsValid)
            return View(model);

        var docHash = _authService.HashDocument(model.Document);
        var entry = await _whitelistService.GetUnclaimedEntryByDocumentHashAsync(docHash);

        // A2: Indistinguishable responses: when the document is missing, already claimed,
        // or has NULL/empty ContactEmail, issue a decoy registration ticket (never verifiable)
        // and redirect to the same verification screen with the same status code (302),
        // same copy and same cookie shape as the valid path. The failure still increments
        // the register:{clientIpKey} bucket exactly as today.
        if (entry == null || string.IsNullOrWhiteSpace(entry.ContactEmail))
        {
            await RecordRegistrationFailureAsync(bucketKey, ipAddress);

            var decoyResult = _otpStore.IssueRegistrationTicket(model.Document, null, null, docHash);
            SetRegistrationTicketCookie(decoyResult.Ticket);

            // NOTA: Las diferencias en los tiempos de entrega SMTP entre el flujo válido y el señuelo (decoy)
            // no se retrasan artificialmente para evitar riesgos de DoS y agotamiento de hilos del servidor.
            return RedirectToAction(nameof(VerificarCodigo));
        }

        // Flujo válido con correo preautorizado en census_whitelist
        var ticketResult = _otpStore.IssueRegistrationTicket(model.Document, entry.Id, entry.ContactEmail, docHash);
        SetRegistrationTicketCookie(ticketResult.Ticket);

        // A1: Cooldown de 60 s para emisión. Si está en cooldown, no despachar nuevo correo.
        if (!ticketResult.IsCooldownActive && !string.IsNullOrWhiteSpace(ticketResult.RawOtp) && _emailSender != null)
        {
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #1a73e8; margin-bottom: 8px;'>Wahl Mirai — Código de Verificación</h2>
                    <p>Hola <strong>{entry.FullName}</strong>,</p>
                    <p>Estás completando tu auto-registro en el sistema de votación estudiantil. Tu código de verificación de 6 dígitos es:</p>
                    <div style='background: #f1f5f9; padding: 16px; text-align: center; border-radius: 8px; margin: 20px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #0f172a;'>{ticketResult.RawOtp}</span>
                    </div>
                    <p style='color: #64748b; font-size: 14px;'>Este código es válido por 15 minutos. Si no solicitaste este código, puedes ignorar este mensaje.</p>
                    <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                    <p style='color: #94a3b8; font-size: 12px;'>Wahl Mirai — Sistema de Votaciones Digitales</p>
                </div>";

            try
            {
                await _emailSender.SendAsync(entry.ContactEmail, "Código de verificación para tu registro — Wahl Mirai", htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar código OTP de auto-registro a {Email}", entry.ContactEmail);
            }
        }

        // A3: No se envían datos sensibles de identidad en TempData (removidos Registro_FullName, Registro_GradeName, etc.)
        return RedirectToAction(nameof(VerificarCodigo));
    }

    /// <summary>
    /// Paso intermedio GET: interfaz para el ingreso del código OTP de 6 dígitos (RF-M01-00, D1).
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [EnableRateLimiting("AuthRegister")]
    public IActionResult VerificarCodigo()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDefaultDashboard();

        var ticket = GetRegistrationTicketFromCookie();
        if (string.IsNullOrWhiteSpace(ticket))
            return RedirectToAction(nameof(Registro));

        return View(new VerificarCodigoViewModel());
    }

    /// <summary>
    /// Paso intermedio POST: valida el código OTP de 6 dígitos (RF-M01-00, D1, D4).
    /// Las fallas computan en el bucket IP register:{clientIpKey} además de invalidar el código tras 5 fallos.
    /// Si el código es correcto, transiciona el ticket a estado verificado y redirige a Completar.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthRegister")]
    public async Task<IActionResult> VerificarCodigo(VerificarCodigoViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDefaultDashboard();

        var ticket = GetRegistrationTicketFromCookie();
        if (string.IsNullOrWhiteSpace(ticket))
            return RedirectToAction(nameof(Registro));

        var ipAddress = GetClientIpAddress();
        var bucketKey = GetRegistrationBucketKey();
        if (_loginAttemptStore.IsLockedOut(bucketKey, out _))
            return RegistrationFailureView(model);

        if (!ModelState.IsValid)
            return View(model);

        var isValid = _otpStore.TryValidateOtp(ticket, model.OtpCode, out var errorMessage, out _);
        if (!isValid)
        {
            // D4: OTP verification failures count in the existing IP bucket register:{clientIpKey}
            await RecordRegistrationFailureAsync(bucketKey, ipAddress);

            ModelState.AddModelError(nameof(model.OtpCode), errorMessage ?? "El código de verificación no es válido o ha expirado.");
            return View(model);
        }

        return RedirectToAction(nameof(Completar));
    }

    /// <summary>
    /// Reenvía el código OTP al correo censal respetando el cooldown de 60 segundos (A1, RF-M01-00).
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthRegister")]
    public async Task<IActionResult> ResendOtp()
    {
        var ticket = GetRegistrationTicketFromCookie();
        if (string.IsNullOrWhiteSpace(ticket))
            return RedirectToAction(nameof(Registro));

        var resendResult = _otpStore.ResendOtp(ticket);

        if (resendResult.Success && !string.IsNullOrWhiteSpace(resendResult.RawOtp) && !string.IsNullOrWhiteSpace(resendResult.ContactEmail) && _emailSender != null)
        {
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #1a73e8; margin-bottom: 8px;'>Wahl Mirai — Reenvío de Código de Verificación</h2>
                    <p>Has solicitado un reenvío de tu código de verificación para completar el auto-registro:</p>
                    <div style='background: #f1f5f9; padding: 16px; text-align: center; border-radius: 8px; margin: 20px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #0f172a;'>{resendResult.RawOtp}</span>
                    </div>
                    <p style='color: #64748b; font-size: 14px;'>Este código es válido por 15 minutos.</p>
                    <hr style='border: none; border-top: 1px solid #e2e8f0; margin-top: 24px;' />
                    <p style='color: #94a3b8; font-size: 12px;'>Wahl Mirai — Sistema de Votaciones Digitales</p>
                </div>";

            try
            {
                await _emailSender.SendAsync(resendResult.ContactEmail, "Reenvío de código de verificación — Wahl Mirai", htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reenviar código OTP de auto-registro a {Email}", resendResult.ContactEmail);
            }
        }

        TempData["InfoMessage"] = resendResult.Message ?? "Si la información corresponde a un registro pendiente, se ha enviado un nuevo código a tu correo.";
        return RedirectToAction(nameof(VerificarCodigo));
    }

    /// <summary>
    /// Step 2 GET: render the registration completion form.
    /// Reads elector name and grade strictly from the verified ticket (D1, A3), never from TempData.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [EnableRateLimiting("AuthRegister")]
    public async Task<IActionResult> Completar()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDefaultDashboard();

        var ticket = GetRegistrationTicketFromCookie();
        if (string.IsNullOrWhiteSpace(ticket))
            return RedirectToAction(nameof(Registro));

        if (!_otpStore.TryGetVerifiedTicket(ticket, out var whitelistId, out _))
        {
            // Unverified, decoy or expired ticket cannot reach completion (A2, D1)
            return RedirectToAction(nameof(Registro));
        }

        var entry = await _whitelistService.GetUnclaimedEntryByIdAsync(whitelistId);
        if (entry == null)
            return RedirectToAction(nameof(Registro));

        var model = new RegistroStep2ViewModel
        {
            FullName  = entry.FullName,
            GradeName = entry.Grade.Name
        };

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
        var ipAddress = GetClientIpAddress();
        var bucketKey = GetRegistrationBucketKey();
        if (_loginAttemptStore.IsLockedOut(bucketKey, out _))
            return RegistrationFailureView(model);

        var ticket = GetRegistrationTicketFromCookie();
        if (string.IsNullOrWhiteSpace(ticket))
            return RedirectToAction(nameof(Registro));

        if (!_otpStore.TryGetVerifiedTicket(ticket, out var whitelistId, out var document))
            return RedirectToAction(nameof(Registro));

        if (!model.AcceptTermsAndPrivacy)
        {
            ModelState.AddModelError(nameof(model.AcceptTermsAndPrivacy), "Debes aceptar los Términos de Uso y la Política de Privacidad para continuar.");
        }

        if (!ModelState.IsValid)
        {
            var entryRetry = await _whitelistService.GetUnclaimedEntryByIdAsync(whitelistId);
            if (entryRetry != null)
            {
                model.FullName = entryRetry.FullName;
                model.GradeName = entryRetry.Grade.Name;
            }
            return View(model);
        }

        // Consumir el ticket atómicamente de forma única
        if (!_otpStore.TryConsumeVerifiedTicket(ticket, out whitelistId, out document))
            return RedirectToAction(nameof(Registro));

        ClearRegistrationTicketCookie();

        try
        {
            // D2: ContactEmail se lee exclusivamente de census_whitelist.contact_email en WhitelistService
            var voter = await _whitelistService.RegisterElectorAsync(
                document,
                whitelistId,
                null!,
                model.Password,
                ipAddress);

            // Asentar consentimiento explícito e informado en el registro de auditoría (RN-16, Ley 1581 / Dec 1074)
            var consentDetails = System.Text.Json.JsonSerializer.Serialize(new
            {
                policy_version = LegalDocuments.Version,
                legal_basis = "Ley 1581 de 2012 / Dec. 1074 de 2015",
                accepted_at_utc = DateTime.UtcNow
            });
            await _auditService.LogAsync("CONSENT_ACCEPTED", (int)voter.Id, "users", (int)voter.Id, "terms_and_privacy_consent", null, "ACCEPTED", consentDetails, ipAddress);

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
            await RecordRegistrationFailureAsync(bucketKey, ipAddress);
            return RegistrationFailureView(model);
        }
        catch (DbUpdateException)
        {
            throw;
        }
    }

    private string? GetRegistrationTicketFromCookie() =>
        Request.Cookies[RegistrationTicketCookieName];

    private void SetRegistrationTicketCookie(string ticket)
    {
        Response.Cookies.Append(RegistrationTicketCookieName, ticket, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromMinutes(15)
        });
    }

    private void ClearRegistrationTicketCookie()
    {
        Response.Cookies.Delete(RegistrationTicketCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict
        });
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

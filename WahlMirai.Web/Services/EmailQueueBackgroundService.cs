// ============================================================
// Archivo:       EmailQueueBackgroundService.cs
// Módulo:        Servicio compartido de notificaciones
// Desarrollador: Kevin
// Descripción:   Procesa email_queue con control de tasa y reintentos.
// Requerimientos: RN-9
// ============================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

/// <summary>
/// Despacha notificaciones de email_queue respetando la tasa y los reintentos de RN-9.
/// </summary>
public class EmailQueueBackgroundService : BackgroundService
{
    private const byte MaxAttempts = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailQueueBackgroundService> _logger;

    public EmailQueueBackgroundService(IServiceScopeFactory scopeFactory, IOptions<EmailSettings> settings, ILogger<EmailQueueBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>Ejecuta el sondeo progresivo de la cola conforme a RN-9.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delayMs = GetDelayMs(_settings.RateLimitPerMinute);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing email queue.");
            }

            await Task.Delay(delayMs, stoppingToken);
        }
    }

    /// <summary>
    /// Convierte la tasa configurada en una pausa positiva, acotando RN-9 a 1–60 correos por minuto.
    /// </summary>
    private static int GetDelayMs(int rateLimitPerMinute)
    {
        var clampedRate = Math.Clamp(rateLimitPerMinute, 1, 60);
        return 60000 / clampedRate;
    }

    /// <summary>Procesa como máximo un correo pendiente y persiste su transición RN-9.</summary>
    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WahlMiraiDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        // NOTA: IPendingPasswordStore vive en memoria por proceso; el worker requiere una sola instancia.
        var passwordStore = scope.ServiceProvider.GetRequiredService<IPendingPasswordStore>();

        // NOTA: La fila no se reserva entre procesos; varias instancias pueden seleccionar y enviar el mismo correo.
        var pendingQueueItem = await dbContext.VwPendingEmailQueues
            .Where(e => e.Attempts < MaxAttempts)
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefaultAsync(stoppingToken);

        if (pendingQueueItem == null) return;

        var pendingEmail = await dbContext.EmailQueues
            .Include(e => e.Voter)
            .FirstOrDefaultAsync(e => e.Id == pendingQueueItem.Id, stoppingToken);

        if (pendingEmail == null) return;

        pendingEmail.Attempts++;
        bool isParsed = Enum.TryParse<EmailType>(pendingEmail.EmailType, out var emailType);
        bool requiresPassword = isParsed && EmailRetryPolicy.IsPasswordCarryingType(emailType);

        try
        {
            string subject = "";
            string htmlBody = "";
            bool shouldSend = false;

            if (requiresPassword)
            {
                if (passwordStore.TryGetPassword(pendingEmail.Id, out var secret))
                {
                    if (emailType == EmailType.RECUPERACION_ACCESO)
                    {
                        if (!IsValidBaseUrl(_settings.BaseUrl, out var validUri))
                        {
                            pendingEmail.Status = "FALLIDO";
                            pendingEmail.ErrorMessage = "EmailSettings:BaseUrl no está configurada o es inválida; no se envió el enlace.";
                            passwordStore.RemovePassword(pendingEmail.Id);
                            await dbContext.SaveChangesAsync(stoppingToken);
                            return;
                        }

                        var cleanBaseUrl = _settings.BaseUrl.Trim().TrimEnd('/');
                        var resetLink = $"{cleanBaseUrl}/RecuperacionAcceso/Restablecer?token={Uri.EscapeDataString(secret!)}";

                        subject = "Restablecer tu contraseña - Wahl Mirai";
                        var encodedName = System.Net.WebUtility.HtmlEncode(pendingEmail.Voter.FullName);

                        htmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #2e7d32;'>Hola {encodedName},</h2>
                            <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en Wahl Mirai.</p>
                            <p>Haz clic en el siguiente botón para definir una nueva contraseña:</p>
                            <div style='text-align: center; margin: 25px 0;'>
                                <a href='{resetLink}' style='background-color: #2e7d32; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>Restablecer Contraseña</a>
                            </div>
                            <p style='color: #666; font-size: 0.9em;'>Este enlace es de un solo uso y expirará en 30 minutos.</p>
                            <p style='color: #666; font-size: 0.9em;'>Si no solicitaste este cambio, ignora este correo. Tu contraseña actual permanecerá segura.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                            <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático del sistema Wahl Mirai, por favor no respondas a este correo.</p>
                        </div>
                        ";

                        shouldSend = true;
                    }
                    else
                    {
                        subject = "Credenciales de Acceso - Wahl Mirai";
                        var emailTypeFriendly = emailType switch
                        {
                            EmailType.REASIGNACION_ADMIN => "Reasignación por administrador",
                            _ => pendingEmail.EmailType
                        };

                        htmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #2e7d32;'>Hola {pendingEmail.Voter.FullName},</h2>
                            <p>Aquí tienes tu nueva contraseña para ingresar a Wahl Mirai:</p>
                            <div style='background:#f4f4f4; padding:15px; text-align:center; border-radius: 8px; margin: 20px 0;'>
                                <h3 style='margin: 0; font-family: monospace; letter-spacing: 2px;'>{secret}</h3>
                            </div>
                            <p style='color: #666; font-size: 0.9em;'>Motivo: {emailTypeFriendly}</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                            <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático del sistema Wahl Mirai, por favor no respondas a este correo.</p>
                        </div>
                        ";

                        shouldSend = true;
                    }
                }
                else
                {
                    pendingEmail.Status = "FALLIDO";
                    pendingEmail.ErrorMessage = "La contraseña en memoria se perdió (reinicio del servicio). El usuario deberá solicitarla nuevamente.";
                }
            }
            else
            {
                var (notificationSubject, statusMsg) = (isParsed ? (EmailType?)emailType : null) switch
                {
                    EmailType.CANDIDATURA_APROBADA => (
                        "Postulación Aprobada - Wahl Mirai",
                        "Tu postulación ha sido revisada y <strong>APROBADA</strong>. Ya estás visible en el tarjetón electoral."
                    ),
                    EmailType.CANDIDATURA_RECHAZADA => (
                        "Postulación Rechazada - Wahl Mirai",
                        "Tu postulación ha sido revisada y <strong>RECHAZADA</strong>. Puedes revisar los detalles en la plataforma."
                    ),
                    EmailType.RESPUESTA_PQR => (
                        "Respuesta a PQR - Wahl Mirai",
                        "Tu solicitud de PQR ha sido atendida y resuelta por la administración. Puedes revisar los detalles en la plataforma."
                    ),
                    EmailType.CAMBIO_PERFIL => (
                        "Actualización de Perfil - Wahl Mirai",
                        "Se ha registrado una actualización en la información de tu perfil de usuario en la plataforma."
                    ),
                    _ => (
                        "Notificación del Sistema - Wahl Mirai",
                        "Has recibido una nueva notificación en la plataforma Wahl Mirai."
                    )
                };

                subject = notificationSubject;
                htmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #2e7d32;'>Hola {pendingEmail.Voter.FullName},</h2>
                        <p>{statusMsg}</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                        <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático del sistema Wahl Mirai.</p>
                    </div>";

                shouldSend = true;
            }

            if (shouldSend)
            {
                // NOTA: La dirección se resuelve desde users.contact_email al entregar; no se guarda una instantánea (diseño M07).
                await emailSender.SendAsync(pendingEmail.Voter.ContactEmail, subject, htmlBody, stoppingToken);
                pendingEmail.Status = "ENVIADO";
                pendingEmail.ErrorMessage = null;
                // NOTA: sent_at conserva hora local por compatibilidad; otros flujos persisten UTC.
                pendingEmail.SentAt = DateTime.Now;

                if (requiresPassword)
                {
                    // Mantenerla hasta el éxito permite que un fallo SMTP transitorio use el mismo secreto en el siguiente intento.
                    passwordStore.RemovePassword(pendingEmail.Id);
                }
            }
        }
        catch (Exception ex)
        {
            if (pendingEmail.Attempts >= MaxAttempts)
            {
                pendingEmail.Status = "FALLIDO";
                if (requiresPassword)
                {
                    passwordStore.RemovePassword(pendingEmail.Id);
                }
            }
            pendingEmail.ErrorMessage = ex.Message.Length > 200 ? ex.Message.Substring(0, 200) : ex.Message;
            
            // Highly visible console output for the user
            Console.WriteLine("\n=======================================================");
            Console.WriteLine($"[ERROR DE CORREO] Falló el envío del email ID: {pendingEmail.Id}");
            Console.WriteLine($"MENSAJE DE ERROR: {ex.Message}");
            Console.WriteLine("=======================================================\n");

            _logger.LogError(ex, "Failed to send email ID {EmailId}", pendingEmail.Id);
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    /// <summary>
    /// Valida que la URL base sea absoluta, use esquema HTTPS o HTTP exclusivo para host loopback (RN-9).
    /// </summary>
    public static bool IsValidBaseUrl(string? baseUrl, out Uri? validUri)
    {
        validUri = null;
        if (string.IsNullOrWhiteSpace(baseUrl)) return false;
        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out validUri)) return false;

        if (validUri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        if (validUri.Scheme == Uri.UriSchemeHttp)
        {
            return validUri.IsLoopback
                || validUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || validUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || validUri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}

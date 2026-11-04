// ============================================================
// Archivo:       AdminEmailReportController.cs
// Módulo:        Panel Administrativo — Reporte de Notificaciones
// Desarrollador: Kevin
// Descripción:   Consulta, filtrado y paginación server-side de la cola
//                de correos progresiva (RN-9).
// Requerimientos: RN-9
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

/// <summary>
/// Modelo de vista para el reporte de entrega de correos con métricas y paginación server-side (RN-9).
/// </summary>
public class EmailReportViewModel
{
    public int TotalEmails { get; set; }
    public int SentEmails { get; set; }
    public int FailedEmails { get; set; }
    public int PendingEmails { get; set; }
    public double SuccessPercentage { get; set; }

    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? EmailTypeFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Contenedor paginado con metadatos de navegación y elementos de la página actual.
    /// </summary>
    public PagedResult<EmailQueueItemDto> PagedItems { get; set; } = new();

    /// <summary>
    /// Acceso directo a los elementos de la página actual para compatibilidad de vistas existentes.
    /// </summary>
    public List<EmailQueueItemDto> Items => PagedItems.Items;
}

/// <summary>
/// DTO con proyección plana de elementos en cola de correo para visualización administrativa.
/// </summary>
public class EmailQueueItemDto
{
    public ulong Id { get; set; }
    public uint VoterId { get; set; }
    public string VoterName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string EmailType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public byte Attempts { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}

[Authorize(Roles = "ADMIN,SUPER_ADMIN")]
public class AdminEmailReportController : Controller
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;

    public AdminEmailReportController(WahlMiraiDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Muestra el reporte de entrega con paginación server-side, orden determinista y filtros combinados (RN-9).
    /// </summary>
    public async Task<IActionResult> Index(
        string? status,
        string? emailType,
        DateTime? startDate,
        DateTime? endDate,
        string? search,
        int page = 1,
        int pageSize = 20)
    {
        // NOTA: Acotamiento de paginación para prevenir desbordes o peticiones con tamaños arbitrarios
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        else if (pageSize > 100) pageSize = 100;

        // NOTA: Si startDate > endDate, se intercambian las fechas para asegurar un rango válido sin introducir nuevas cadenas de error
        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        // Construir consulta base sin materialización de Voter completo
        var baseQuery = _context.EmailQueues
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(emailType))
        {
            baseQuery = baseQuery.Where(e => e.EmailType == emailType);
        }

        if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            baseQuery = baseQuery.Where(e => e.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            // endDate es inclusivo cubriendo hasta las 23:59:59.999 del día
            var endExclusive = endDate.Value.Date.AddDays(1);
            baseQuery = baseQuery.Where(e => e.CreatedAt < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            baseQuery = baseQuery.Where(e =>
                (e.Voter != null && e.Voter.FullName.ToLower().Contains(term)) ||
                (e.Voter != null && e.Voter.ContactEmail.ToLower().Contains(term)));
        }

        // Cómputo de contadores / KPIs mediante GroupBy excluyendo el filtro de estado específico
        var statusCounts = await baseQuery
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int sent = statusCounts.FirstOrDefault(x => x.Status == "ENVIADO")?.Count ?? 0;
        int failed = statusCounts.FirstOrDefault(x => x.Status == "FALLIDO")?.Count ?? 0;
        int pending = statusCounts.FirstOrDefault(x => x.Status == "PENDIENTE")?.Count ?? 0;
        int total = statusCounts.Sum(x => x.Count);

        double successPct = 0;
        int processedTotal = sent + failed;
        if (processedTotal > 0)
        {
            successPct = Math.Round((double)sent / processedTotal * 100, 1);
        }

        // Aplicar el filtro de estado exclusivamente a la consulta paginada
        var pagedQuery = baseQuery;
        if (!string.IsNullOrWhiteSpace(status))
        {
            pagedQuery = pagedQuery.Where(e => e.Status == status);
        }

        int totalFilteredCount = await pagedQuery.CountAsync();
        int totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalFilteredCount / pageSize) : 0;
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        // Proyección directa a DTO sin Include completo de Voter y con orden determinista (CreatedAt desc, Id desc)
        var items = await pagedQuery
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Select(e => new EmailQueueItemDto
            {
                Id = e.Id,
                VoterId = e.VoterId,
                VoterName = e.Voter != null ? e.Voter.FullName : "N/A",
                RecipientEmail = e.Voter != null ? e.Voter.ContactEmail : "N/A",
                EmailType = e.EmailType,
                Status = e.Status,
                Attempts = e.Attempts,
                ErrorMessage = e.ErrorMessage,
                CreatedAt = e.CreatedAt,
                SentAt = e.SentAt
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var pagedResult = new PagedResult<EmailQueueItemDto>
        {
            Items = items,
            TotalCount = totalFilteredCount,
            PageNumber = page,
            PageSize = pageSize
        };

        var model = new EmailReportViewModel
        {
            TotalEmails = total,
            SentEmails = sent,
            FailedEmails = failed,
            PendingEmails = pending,
            SuccessPercentage = successPct,
            Search = search,
            Status = status,
            EmailTypeFilter = emailType,
            StartDate = startDate,
            EndDate = endDate,
            PagedItems = pagedResult
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryEmail(
        ulong id,
        string? status,
        string? emailType,
        DateTime? startDate,
        DateTime? endDate,
        string? search,
        int page = 1)
    {
        var routeValues = BuildRouteValues(status, emailType, startDate, endDate, search, page);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var email = await _context.EmailQueues
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new { e.Status, e.EmailType })
            .FirstOrDefaultAsync();

        if (email == null || !EmailRetryPolicy.CanRetryManually(email.Status, email.EmailType))
        {
            if (email != null && email.Status == "FALLIDO" && EmailRetryPolicy.IsPasswordCarryingType(email.EmailType))
            {
                TempData["Error"] = email.EmailType switch
                {
                    nameof(EmailType.RECUPERACION_ACCESO) => "El usuario puede solicitar una nueva recuperación de acceso.",
                    nameof(EmailType.REASIGNACION_ADMIN) => "Para obtener una nueva contraseña, usa “Recuperar acceso”.",
                    _ => "El correo no existe o ya no se puede reintentar manualmente."
                };
            }
            else
            {
                TempData["Error"] = "El correo no existe o ya no se puede reintentar manualmente.";
            }

            return RedirectToAction(nameof(Index), routeValues);
        }

        // NOTA: El reintento manual solo admite FALLIDO; PENDIENTE sigue en la cola automática y ya no reinicia Attempts.
        var passwordCarryingTypes = EmailRetryPolicy.GetPasswordCarryingTypeNames();
        var updatedRows = await _context.EmailQueues
            .Where(e => e.Id == id && e.Status == "FALLIDO" && e.EmailType == email.EmailType && !passwordCarryingTypes.Contains(e.EmailType))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, "PENDIENTE")
                .SetProperty(e => e.Attempts, (byte)0)
                .SetProperty(e => e.ErrorMessage, (string?)null));

        if (updatedRows == 0)
        {
            TempData["Error"] = "El correo no existe o ya no se puede reintentar manualmente.";
            return RedirectToAction(nameof(Index), routeValues);
        }

        await _auditService.LogAsync("EMAIL_RETRY", null, "email_queue", (int)id, "status", "FALLIDO", "PENDIENTE",
            $"Queued manual retry for email ID {id}", ip);

        TempData["Success"] = "El correo se puso en cola para reintento automático.";
        return RedirectToAction(nameof(Index), routeValues);
    }

    private static RouteValueDictionary BuildRouteValues(
        string? status,
        string? emailType,
        DateTime? startDate,
        DateTime? endDate,
        string? search,
        int page)
    {
        var rv = new RouteValueDictionary();
        if (!string.IsNullOrWhiteSpace(status)) rv["status"] = status;
        if (!string.IsNullOrWhiteSpace(emailType)) rv["emailType"] = emailType;
        if (startDate.HasValue) rv["startDate"] = startDate.Value.ToString("yyyy-MM-dd");
        if (endDate.HasValue) rv["endDate"] = endDate.Value.ToString("yyyy-MM-dd");
        if (!string.IsNullOrWhiteSpace(search)) rv["search"] = search;
        if (page > 1) rv["page"] = page;
        return rv;
    }
}

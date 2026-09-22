using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

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

    public List<EmailQueueItemDto> Items { get; set; } = new();
}

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

    public async Task<IActionResult> Index(string? search, string? status, string? emailType, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.EmailQueues
            .Include(e => e.Voter)
            .AsNoTracking()
            .AsQueryable();

        // Aplicar Filtros
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(emailType))
        {
            query = query.Where(e => e.EmailType == emailType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(e => e.CreatedAt <= endOfDay);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e => e.Voter.FullName.ToLower().Contains(s) || e.Voter.ContactEmail.ToLower().Contains(s));
        }

        var allQueue = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();

        // Calcular Métricas / KPIs
        int total = allQueue.Count;
        int sent = allQueue.Count(e => e.Status == "ENVIADO");
        int failed = allQueue.Count(e => e.Status == "FALLIDO");
        int pending = allQueue.Count(e => e.Status == "PENDIENTE");

        double successPct = 0;
        int processedTotal = sent + failed;
        if (processedTotal > 0)
        {
            successPct = Math.Round((double)sent / processedTotal * 100, 1);
        }

        var items = allQueue.Select(e => new EmailQueueItemDto
        {
            Id = e.Id,
            VoterId = e.VoterId,
            VoterName = e.Voter?.FullName ?? "N/A",
            RecipientEmail = e.Voter?.ContactEmail ?? "N/A",
            EmailType = e.EmailType,
            Status = e.Status,
            Attempts = e.Attempts,
            ErrorMessage = e.ErrorMessage,
            CreatedAt = e.CreatedAt,
            SentAt = e.SentAt
        }).ToList();

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
            Items = items
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RetryEmail(ulong id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var email = await _context.EmailQueues.FindAsync(id);
        if (email == null)
        {
            TempData["Error"] = "Registro de correo no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        email.Status = "PENDIENTE";
        email.Attempts = 0;
        email.ErrorMessage = null;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("EMAIL_RETRY", null, "email_queue", (int)email.Id, "status", "FALLIDO", "PENDIENTE",
            $"Queued manual retry for email ID {id}", ip);

        TempData["Success"] = $"El correo #{id} ha sido reiniciado a estado PENDIENTE para su reintento automático por el servicio en segundo plano.";
        return RedirectToAction(nameof(Index));
    }
}

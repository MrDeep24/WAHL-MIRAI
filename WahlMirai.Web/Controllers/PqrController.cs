using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WahlMirai.Web.Models;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

[Authorize]
public class PqrController : Controller
{
    private readonly WahlMiraiDbContext _context;

    public PqrController(WahlMiraiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "ELECTOR")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    public IActionResult Manage()
    {
        return View();
    }

    [HttpGet("/Pqr/Mine")]
    [Authorize(Roles = "ELECTOR")]
    public async Task<IActionResult> Mine()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, message = "Sesión no válida." });

        var list = await _context.PqrTickets
            .Where(t => t.VoterId == (uint)userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                id = t.Id,
                subject = t.Subject,
                message = t.Message,
                status = t.Status,
                adminResponse = t.AdminResponse,
                respondedAt = t.RespondedAt,
                createdAt = t.CreatedAt
            })
            .ToListAsync();

        return Json(new { ok = true, tickets = list });
    }

    [HttpPost]
    [Authorize(Roles = "ELECTOR")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] PqrCreateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, message = "Sesión no válida." });

        if (!ModelState.IsValid)
        {
            var first = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Entrada inválida.";
            return Json(new { ok = false, message = first });
        }

        var ticket = new PqrTicket
        {
            VoterId = (uint)userId,
            Subject = dto.Subject.Trim(),
            Message = dto.Message.Trim(),
            Status = "ABIERTO",
            CreatedAt = DateTime.Now
        };

        _context.PqrTickets.Add(ticket);
        await _context.SaveChangesAsync();

        return Json(new { ok = true });
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    public async Task<IActionResult> List(string? status)
    {
        var q = _context.PqrTickets.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.ToUpperInvariant();
            if (s == "ABIERTO" || s == "RESUELTO") q = q.Where(t => t.Status == s);
        }

        var list = await q
            .Include(t => t.Voter)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new {
                id = t.Id,
                voterId = t.VoterId,
                voterName = t.Voter.FullName,
                subject = t.Subject,
                message = t.Message,
                status = t.Status,
                adminResponse = t.AdminResponse,
                respondedByVoterId = t.RespondedByVoterId,
                respondedAt = t.RespondedAt,
                createdAt = t.CreatedAt,
                updatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return Json(new { ok = true, tickets = list });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    [ValidateAntiForgeryToken]
    [Route("Pqr/Resolve/{id}")]
    public async Task<IActionResult> Resolve([FromRoute] ulong id, [FromBody] PqrResponseDto dto)
    {
        if (!ModelState.IsValid)
        {
            var first = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Entrada inválida.";
            return Json(new { ok = false, message = first });
        }

        var ticket = await _context.PqrTickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return Json(new { ok = false, message = "Ticket no encontrado." });
        if (ticket.Status == "RESUELTO") return Json(new { ok = false, message = "Ticket ya resuelto." });

        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(adminIdStr, out int adminId)) return Json(new { ok = false, message = "Sesión no válida." });

        ticket.Status = "RESUELTO";
        ticket.AdminResponse = dto.AdminResponse.Trim();
        ticket.RespondedByVoterId = (uint)adminId;
        ticket.RespondedAt = DateTime.Now;

        // Encolar notificación usando el patrón existente (no audit)
        var emailQueue = new EmailQueue
        {
            VoterId = ticket.VoterId,
            EmailType = "RESPUESTA_PQR",
            Status = "PENDIENTE",
            Attempts = 0,
            CreatedAt = DateTime.Now
        };

        _context.EmailQueues.Add(emailQueue);

        await _context.SaveChangesAsync();

        return Json(new { ok = true });
    }
}

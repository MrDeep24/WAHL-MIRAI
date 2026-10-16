// Archivo:       PqrController.cs
// Módulo:        M08 — Ayuda, Tutorial, PQR y Chatbot
// Desarrollador: Kevin
// Descripción:   Publica la vista pública de Ayuda y los endpoints para crear,
//                consultar y resolver solicitudes PQR según el rol del usuario.
//                Persiste tickets y encola la notificación de respuesta al elector.
// Requerimientos: RF-M08-00, RF-M08-01 y RF-M08-02.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

[Authorize]
/// <summary>
/// Controla la vista de Ayuda y las operaciones de creación, consulta y resolución de PQR.
/// El acceso se restringe por rol, excepto la vista pública de Ayuda.
/// </summary>
public class PqrController : Controller
{
    private readonly WahlMiraiDbContext _context;

    /// <summary>Inicializa el controlador con el contexto de persistencia de la aplicación.</summary>
    /// <param name="context">Contexto EF Core para tickets PQR y cola de correos.</param>
    public PqrController(WahlMiraiDbContext context)
    {
        _context = context;
    }

    [HttpGet("/Pqr")]
    [HttpGet("/Ayuda")]
    [AllowAnonymous]
    /// <summary>Muestra la página pública de Ayuda y tutorial (RF-M08-00).</summary>
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = Roles.ElectorName)]
    /// <summary>Muestra el formulario de creación de PQR para electores (RF-M08-01).</summary>
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    /// <summary>Muestra la consola administrativa de gestión de PQR (RF-M08-02).</summary>
    public IActionResult Manage()
    {
        return View();
    }

    [HttpGet("/Pqr/Mine")]
    [Authorize(Roles = "ELECTOR")]
    /// <summary>Devuelve al elector autenticado sus propios tickets PQR (RF-M08-01).</summary>
    public async Task<IActionResult> Mine()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, message = "Sesión no válida." });

        var list = await _context.PqrTickets
            .AsNoTracking()
            .Where(t => t.UserId == (uint)userId)
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
    [Authorize(Roles = Roles.ElectorName)]
    [ValidateAntiForgeryToken]
    /// <summary>Crea una solicitud PQR para el elector autenticado (RF-M08-01).</summary>
    /// <param name="dto">Asunto y descripción validados de la solicitud.</param>
    public async Task<IActionResult> Create([FromBody] PqrCreateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Json(new { ok = false, message = "SESSION_INVALID" });

        if (!ModelState.IsValid)
        {
            var first = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Entrada inválida.";
            return Json(new { ok = false, message = first });
        }

        var ticket = new PqrTicket
        {
            UserId = (uint)userId,
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
    /// <summary>
    /// Devuelve tickets PQR paginados para la gestión administrativa con soporte de filtros por estado y búsqueda (RF-M08-02).
    /// </summary>
    /// <param name="status">Estado opcional para filtrar: ABIERTO o RESUELTO.</param>
    /// <param name="search">Término opcional para búsqueda parcial insensible a mayúsculas por nombre de votante o asunto.</param>
    /// <param name="page">Número de página solicitada (base 1, normalizado).</param>
    /// <param name="pageSize">Cantidad de registros por página (acotado entre 1 y 100).</param>
    public async Task<IActionResult> List(string? status, string? search, int page = 1, int pageSize = 20)
    {
        var q = _context.PqrTickets.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.ToUpperInvariant();
            if (s == "ABIERTO" || s == "RESUELTO") q = q.Where(t => t.Status == s);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(t => t.Voter.FullName.ToLower().Contains(term) || t.Subject.ToLower().Contains(term));
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        else if (pageSize > 100) pageSize = 100;

        var totalCount = await q.CountAsync();
        var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        if (totalPages > 0 && page > totalPages)
        {
            page = totalPages;
        }

        var orderedQuery = q
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id);

        var projectedQuery = orderedQuery.Select(t => new
        {
            id = t.Id,
            userId = t.UserId,
            userName = t.Voter.FullName,
            subject = t.Subject,
            message = t.Message,
            status = t.Status,
            adminResponse = t.AdminResponse,
            respondedByVoterId = t.RespondedByVoterId,
            respondedAt = t.RespondedAt,
            createdAt = t.CreatedAt,
            updatedAt = t.UpdatedAt
        });

        var items = await projectedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync<object>();

        var pagedResult = new PagedResult<object>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };

        return Json(new
        {
            ok = true,
            items = pagedResult.Items,
            totalCount = pagedResult.TotalCount,
            pageNumber = pagedResult.PageNumber,
            pageSize = pagedResult.PageSize,
            totalPages = pagedResult.TotalPages,
            hasPreviousPage = pagedResult.HasPreviousPage,
            hasNextPage = pagedResult.HasNextPage
        });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    [ValidateAntiForgeryToken]
    [Route("Pqr/Resolve/{id}")]
    /// <summary>Registra la respuesta administrativa, resuelve el ticket y encola su notificación (RF-M08-02).</summary>
    /// <param name="id">Identificador del ticket que se resolverá.</param>
    /// <param name="dto">Respuesta redactada por el administrador.</param>
    public async Task<IActionResult> Resolve([FromRoute] ulong id, [FromBody] PqrResponseDto dto)
    {
        // NOTA: A diferencia de otras acciones administrativas del sistema,
        // la creación y resolución de PQR NO se registra en audit_log por
        // decisión de diseño explícita (RF-M08-02, condición especial). Su
        // trazabilidad se limita a los campos de la propia tabla
        // pqr_tickets (status, responded_by_user_id, responded_at).
        if (!ModelState.IsValid)
        {
            var first = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "INVALID_INPUT";
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
            VoterId = ticket.UserId,
            EmailType = EmailType.RESPUESTA_PQR.ToString(),
            Status = "PENDIENTE",
            Attempts = 0,
            CreatedAt = DateTime.Now
        };

        _context.EmailQueues.Add(emailQueue);

        await _context.SaveChangesAsync();

        return Json(new { ok = true });
    }
}

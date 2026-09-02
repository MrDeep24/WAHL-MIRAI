using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "ADMIN,SUPER_ADMIN")]
public class AdminCandidatesController : Controller
{
    private readonly ICandidateReviewService _candidateReviewService;
    private readonly IEventService _eventService;
    private readonly WahlMiraiDbContext _context;

    public AdminCandidatesController(
        ICandidateReviewService candidateReviewService,
        IEventService eventService,
        WahlMiraiDbContext context)
    {
        _candidateReviewService = candidateReviewService;
        _eventService = eventService;
        _context = context;
    }

    public async Task<IActionResult> Index(uint? eventId, string? status)
    {
        ViewBag.Events = await _eventService.GetEventsAsync();
        ViewBag.SelectedEventId = eventId;
        ViewBag.SelectedStatus = status;

        var candidates = await _candidateReviewService.GetCandidatesForReviewAsync(eventId, status);
        return View(candidates);
    }

    [HttpGet]
    public async Task<IActionResult> GetCandidateDetail(uint id)
    {
        var detail = await _candidateReviewService.GetCandidateReviewDetailAsync(id);
        if (detail == null) return NotFound();

        return Json(detail);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(uint candidateId, bool withExceptions, string? exceptionsDetail)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        uint adminId = await GetValidAdminVoterIdAsync();

        try
        {
            await _candidateReviewService.ApproveCandidateAsync(candidateId, withExceptions, exceptionsDetail, adminId, ip);
            TempData["Success"] = withExceptions
                ? "Candidatura aprobada con excepción correctamente."
                : "Candidatura aprobada correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.GetBaseException().Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(uint candidateId, string rejectionReason, bool allowCorrection = false)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        uint adminId = await GetValidAdminVoterIdAsync();

        try
        {
            await _candidateReviewService.RejectCandidateAsync(candidateId, rejectionReason, allowCorrection, adminId, ip);
            TempData["Success"] = allowCorrection 
                ? "Candidatura rechazada con opción a subsanar. Se ha notificado al postulante."
                : "Candidatura rechazada definitivamente. Se ha notificado al postulante.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.GetBaseException().Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Withdraw(uint candidateId, string reason)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        uint adminId = await GetValidAdminVoterIdAsync();

        try
        {
            await _candidateReviewService.WithdrawCandidateAsync(candidateId, reason, adminId, ip);
            TempData["Success"] = "Candidatura retirada exitosamente. El cambio es definitivo.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.GetBaseException().Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<uint> GetValidAdminVoterIdAsync()
    {
        if (uint.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out uint parsedId))
        {
            if (await _context.Voters.AnyAsync(v => v.Id == parsedId))
                return parsedId;
        }

        var anyVoter = await _context.Voters.FirstOrDefaultAsync(v => v.Status != "ELIMINADO");
        if (anyVoter != null)
            return anyVoter.Id;

        throw new InvalidOperationException("No existe una cuenta de usuario administrativa válida.");
    }
}

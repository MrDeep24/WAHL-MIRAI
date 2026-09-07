using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "ADMIN,SUPER_ADMIN")]
public class AdminEventsController : Controller
{
    private readonly IEventService _eventService;
    private readonly WahlMiraiDbContext _context;

    public AdminEventsController(IEventService eventService, WahlMiraiDbContext context)
    {
        _eventService = eventService;
        _context = context; 
    }

    public async Task<IActionResult> Index()
    {
        var events = await _eventService.GetEventsAsync();
        return View(events);
    }

    public IActionResult Create()
    {
        ViewBag.Grades = _context.Grades.ToList();
        ViewBag.Positions = _context.ElectionPositions.Where(p => p.Status == "ACTIVO").ToList();

        var firstPosId = _context.ElectionPositions.FirstOrDefault(p => p.Status == "ACTIVO")?.Id ?? 1;

        var today = DateTime.Now;
        return View("Form", new VotingEvent { 
            PositionId = firstPosId,
            RegistrationStartDate = DateOnly.FromDateTime(today),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),

            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(5)),
            ProposalsEndTime = new TimeOnly(17, 0),

            VotingStartDate = DateOnly.FromDateTime(today.AddDays(6)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(7)),
            VotingEndTime = new TimeOnly(16, 0)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(VotingEvent model, List<byte> gradeIds)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        try
        {
            model.CreatedByVoterId = await GetValidAdminVoterIdAsync();
            if (model.PositionId == 0)
            {
                model.PositionId = (await _context.ElectionPositions.FirstOrDefaultAsync(p => p.Status == "ACTIVO"))?.Id ?? 1;
            }

            var createdEvent = await _eventService.CreateEventAsync(model, gradeIds, ip);
            TempData["Success"] = "Proceso electoral creado correctamente. Ahora puedes añadir temas o candidatos.";
            return RedirectToAction(nameof(Edit), new { id = createdEvent.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = GetErrorMessage(ex);
            ViewBag.Grades = _context.Grades.ToList();
            ViewBag.Positions = _context.ElectionPositions.Where(p => p.Status == "ACTIVO").ToList();
            return View("Form", model);
        }
    }

    public async Task<IActionResult> Edit(uint id)
    {
        var ev = await _eventService.GetEventByIdAsync(id);
        if (ev == null) return NotFound();

        ViewBag.Grades = _context.Grades.ToList();
        ViewBag.Positions = _context.ElectionPositions.Where(p => p.Status == "ACTIVO").ToList();
        return View("Form", ev);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(VotingEvent model, List<byte> gradeIds)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        try
        {
            model.CreatedByVoterId = await GetValidAdminVoterIdAsync();
            if (model.PositionId == 0)
            {
                model.PositionId = (await _context.ElectionPositions.FirstOrDefaultAsync(p => p.Status == "ACTIVO"))?.Id ?? 1;
            }

            var updated = await _eventService.UpdateEventAsync(model, gradeIds, ip);
            if (updated == null) return NotFound();
            
            TempData["Success"] = "Proceso electoral actualizado correctamente.";
            return RedirectToAction("Edit", new { id = model.Id }); 
        }
        catch (Exception ex)
        {
            TempData["Error"] = GetErrorMessage(ex);
            ViewBag.Grades = _context.Grades.ToList();
            ViewBag.Positions = _context.ElectionPositions.Where(p => p.Status == "ACTIVO").ToList();
            return View("Form", model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(uint id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var success = await _eventService.SoftDeleteEventAsync(id, ip);
        if (success) TempData["Success"] = "Proceso electoral eliminado (lógico) correctamente.";
        else TempData["Error"] = "No se pudo eliminar el proceso electoral.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> AddCandidate(uint eventId, uint voterId, string? slogan, string? photoUrl)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        try
        {
            await _eventService.AddCandidateAsync(eventId, voterId, slogan, photoUrl, ip);
            TempData["Success"] = "Candidato añadido correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = GetErrorMessage(ex);
        }
        return RedirectToAction("Edit", new { id = eventId });
    }

    [HttpPost]
    public async Task<IActionResult> AddProposalOption(uint eventId, string name, string? slogan, string? photoUrl)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        try
        {
            await _eventService.AddProposalOptionAsync(eventId, name, slogan, photoUrl, ip);
            TempData["Success"] = "Opción temática añadida correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = GetErrorMessage(ex);
        }
        return RedirectToAction("Edit", new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> SearchVoter(string term)
    {
        var results = await _eventService.SearchVoterAsync(term);
        return Json(results);
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

        throw new InvalidOperationException("No existe ninguna cuenta de usuario en la base de datos para asociar como creador del evento.");
    }

    private static string GetErrorMessage(Exception ex)
    {
        var baseEx = ex.GetBaseException();
        return baseEx.Message;
    }
}

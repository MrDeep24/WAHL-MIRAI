using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "ADMIN")]
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
        return View("Form", new VotingEvent { 
            StartDate = DateOnly.FromDateTime(DateTime.Now), 
            EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(VotingEvent model, List<byte> gradeIds)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        
        uint adminId = 1;
        if (uint.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out uint parsedId))
            adminId = parsedId;
            
        model.CreatedByVoterId = adminId;

        try
        {
            var createdEvent = await _eventService.CreateEventAsync(model, gradeIds, ip);
            TempData["Success"] = "Proceso electoral creado correctamente. Ahora puedes añadir temas o candidatos.";
            return RedirectToAction(nameof(Edit), new { id = createdEvent.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            ViewBag.Grades = _context.Grades.ToList();
            return View("Form", model);
        }
    }

    public async Task<IActionResult> Edit(uint id)
    {
        var ev = await _eventService.GetEventByIdAsync(id);
        if (ev == null) return NotFound();

        ViewBag.Grades = _context.Grades.ToList();
        return View("Form", ev);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(VotingEvent model, List<byte> gradeIds)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        
        uint adminId = 1;
        if (uint.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out uint parsedId))
            adminId = parsedId;
            
        model.CreatedByVoterId = adminId;

        try
        {
            var updated = await _eventService.UpdateEventAsync(model, gradeIds, ip);
            if (updated == null) return NotFound();
            
            TempData["Success"] = "Proceso electoral actualizado correctamente.";
            return RedirectToAction("Edit", new { id = model.Id }); 
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            ViewBag.Grades = _context.Grades.ToList();
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
            TempData["Error"] = ex.Message;
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
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Edit", new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> SearchVoter(string term)
    {
        var results = await _eventService.SearchVoterAsync(term);
        return Json(results);
    }
}

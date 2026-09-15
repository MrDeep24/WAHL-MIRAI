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
    private readonly ICandidateReviewService _candidateReviewService;
    private readonly WahlMiraiDbContext _context;

    public AdminEventsController(
        IEventService eventService,
        ICandidateReviewService candidateReviewService,
        WahlMiraiDbContext context)
    {
        _eventService = eventService;
        _candidateReviewService = candidateReviewService;
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
        ViewBag.PositionRequirements = _context.PositionRequirements
            .Where(pr => pr.PositionId == firstPosId)
            .OrderBy(pr => pr.DisplayOrder)
            .ToList();

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
    public async Task<IActionResult> Create(VotingEvent model, List<byte> gradeIds, string? requirementsJson)
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
            
            // Guardar requisitos de candidatura configurados para este cargo
            await SavePositionRequirementsAsync(model.PositionId, requirementsJson);

            TempData["Success"] = "Proceso electoral creado correctamente. Ahora puedes añadir temas o candidatos.";
            return RedirectToAction(nameof(Edit), new { id = createdEvent.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = GetErrorMessage(ex);
            ViewBag.Grades = _context.Grades.ToList();
            ViewBag.Positions = _context.ElectionPositions.Where(p => p.Status == "ACTIVO").ToList();
            ViewBag.PositionRequirements = _context.PositionRequirements
                .Where(pr => pr.PositionId == model.PositionId)
                .OrderBy(pr => pr.DisplayOrder)
                .ToList();
            return View("Form", model);
        }
    }

    public async Task<IActionResult> Edit(uint id)
    {
        var ev = await _eventService.GetEventByIdAsync(id);
        if (ev == null) return NotFound();

        ViewBag.Grades = _context.Grades.ToList();
        ViewBag.Positions = _context.ElectionPositions.Where(p => p.Status == "ACTIVO").ToList();
        ViewBag.PositionRequirements = _context.PositionRequirements
            .Where(pr => pr.PositionId == ev.PositionId)
            .OrderBy(pr => pr.DisplayOrder)
            .ToList();

        if (ev.ElectionType == "PERSONAS")
        {
            ViewBag.CandidatesForReview = await _candidateReviewService.GetCandidatesForReviewAsync(id, null);
        }
        return View("Form", ev);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(VotingEvent model, List<byte> gradeIds, string? requirementsJson)
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

            // Guardar requisitos de candidatura configurados para este cargo
            await SavePositionRequirementsAsync(model.PositionId, requirementsJson);
            
            TempData["Success"] = "Proceso electoral actualizado correctamente.";
            return RedirectToAction("Edit", new { id = model.Id }); 
        }
        catch (Exception ex)
        {
            TempData["Error"] = GetErrorMessage(ex);
            ViewBag.Grades = _context.Grades.ToList();
            ViewBag.Positions = _context.ElectionPositions.Where(p => p.Status == "ACTIVO").ToList();
            ViewBag.PositionRequirements = _context.PositionRequirements
                .Where(pr => pr.PositionId == model.PositionId)
                .OrderBy(pr => pr.DisplayOrder)
                .ToList();
            if (model.ElectionType == "PERSONAS")
            {
                ViewBag.CandidatesForReview = await _candidateReviewService.GetCandidatesForReviewAsync(model.Id, null);
            }
            return View("Form", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPositionRequirements(uint positionId)
    {
        var reqs = await _context.PositionRequirements
            .Where(pr => pr.PositionId == positionId)
            .OrderBy(pr => pr.DisplayOrder)
            .Select(pr => new {
                id = pr.Id,
                description = pr.Description,
                isMandatory = pr.IsMandatory,
                displayOrder = pr.DisplayOrder
            })
            .ToListAsync();

        return Json(reqs);
    }

    private async Task SavePositionRequirementsAsync(uint positionId, string? requirementsJson)
    {
        if (string.IsNullOrWhiteSpace(requirementsJson) || positionId == 0) return;

        try
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<RequirementInputDto>>(requirementsJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (items == null) return;

            var existing = await _context.PositionRequirements
                .Include(r => r.CandidacyDocuments)
                .Where(r => r.PositionId == positionId)
                .ToListAsync();

            byte order = 1;
            var processedIds = new HashSet<uint>();

            foreach (var item in items)
            {
                var desc = item.Description?.Trim();
                if (string.IsNullOrWhiteSpace(desc)) continue;

                PositionRequirement? match = null;
                if (item.Id > 0)
                {
                    match = existing.FirstOrDefault(r => r.Id == item.Id);
                }
                if (match == null)
                {
                    match = existing.FirstOrDefault(r => !processedIds.Contains(r.Id) && r.Description.Equals(desc, StringComparison.OrdinalIgnoreCase));
                }

                if (match != null)
                {
                    match.Description = desc;
                    match.IsMandatory = item.IsMandatory;
                    match.DisplayOrder = order++;
                    processedIds.Add(match.Id);
                }
                else
                {
                    var newReq = new PositionRequirement
                    {
                        PositionId = positionId,
                        Description = desc,
                        IsMandatory = item.IsMandatory,
                        DisplayOrder = order++
                    };
                    _context.PositionRequirements.Add(newReq);
                }
            }

            // Eliminar requisitos que el admin quitó si no tienen documentos enlazados
            foreach (var exReq in existing)
            {
                if (!processedIds.Contains(exReq.Id))
                {
                    if (!exReq.CandidacyDocuments.Any())
                    {
                        _context.PositionRequirements.Remove(exReq);
                    }
                    else
                    {
                        exReq.IsMandatory = false;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            // Silencioso para no romper flujo principal en caso de parseo de requerimientos
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

public class RequirementInputDto
{
    public uint Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsMandatory { get; set; } = true;
}


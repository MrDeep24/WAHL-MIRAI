using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "ELECTOR")]
public class ElectorController : Controller
{
    private readonly IVotingService _votingService;
    private readonly ICandidacyService _candidacyService;

    public ElectorController(IVotingService votingService, ICandidacyService candidacyService)
    {
        _votingService = votingService;
        _candidacyService = candidacyService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        // Use the dashboard method that returns all relevant events for the elector's grade
        var events = await _votingService.GetEventsForVoterDashboardAsync(userId);
        var myPostulations = await _candidacyService.GetMyPostulationsAsync(userId);
        
        // Enhance with participation status and postulation info
        var eventsWithStatus = new List<dynamic>();
        foreach(var e in events)
        {
            bool hasVoted = await _votingService.HasVotedAsync(userId, (int)e.Id);
            var postulation = myPostulations.FirstOrDefault(p => p.EventId == e.Id);
            eventsWithStatus.Add(new { 
                Event = e, 
                HasVoted = hasVoted, 
                Status = e.Status,
                MyPostulation = postulation
            });
        }

        ViewBag.Events = eventsWithStatus;
        ViewBag.GradeName = await _votingService.GetVoterGradeNameAsync(userId);
        return View();
    }

    public async Task<IActionResult> Votar(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var events = await _votingService.GetEventsForVoterDashboardAsync(userId);
        var currentEvent = events.FirstOrDefault(e => e.Id == id);
        
        if (currentEvent == null)
        {
            TempData["Error"] = "El proceso electoral no está disponible para tu grado o no existe.";
            return RedirectToAction(nameof(Dashboard));
        }

        // Si la elección está en etapa de INSCRIPCION
        if (currentEvent.Status == "INSCRIPCION")
        {
            TempData["Info"] = "Este proceso electoral se encuentra actualmente en etapa de Inscripción de Candidatos.";
            var postulations = await _candidacyService.GetMyPostulationsAsync(userId);
            var myPost = postulations.FirstOrDefault(p => p.EventId == currentEvent.Id);
            if (myPost != null)
            {
                return RedirectToAction("Status", "Candidacy", new { id = myPost.CandidateId });
            }
            return RedirectToAction("Apply", "Candidacy", new { id = currentEvent.Id });
        }

        // Si la elección está en etapa de PROPUESTAS
        if (currentEvent.Status == "PROPUESTAS")
        {
            TempData["Info"] = "Este proceso electoral se encuentra actualmente en etapa de Consulta de Propuestas.";
            return RedirectToAction(nameof(Propuestas), new { id = currentEvent.Id });
        }

        // Si la elección ya finalizó
        if (currentEvent.Status == "FINALIZADA")
        {
            return RedirectToAction("Index", "Results", new { id = currentEvent.Id });
        }

        // Si la elección aún está programada y no ha iniciado inscripción
        if (currentEvent.Status == "PROGRAMADA")
        {
            TempData["Info"] = $"Este proceso electoral aún no ha iniciado. Las inscripciones inician el {currentEvent.RegistrationStartDate:dd/MM/yyyy} a las {currentEvent.RegistrationStartTime:HH:mm}.";
            return RedirectToAction(nameof(Dashboard));
        }

        // Si ya votó
        if (await _votingService.HasVotedAsync(userId, id))
        {
            TempData["Info"] = "Ya has participado en esta elección. Puedes consultar los resultados.";
            return RedirectToAction("Index", "Results", new { id = currentEvent.Id });
        }

        var candidates = await _votingService.GetCandidatesForEventAsync(id);
        if (candidates == null || !candidates.Any())
        {
            TempData["Error"] = "Aún no hay candidatos aprobados registrados para esta elección.";
            return RedirectToAction(nameof(Dashboard));
        }

        ViewBag.EventId = id;
        ViewBag.EventTitle = currentEvent.Title;
        ViewBag.Candidates = candidates;
        return View();
    }

    public async Task<IActionResult> Propuestas(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var events = await _votingService.GetEventsForVoterDashboardAsync(userId);
        var currentEvent = events.FirstOrDefault(e => e.Id == id);
        
        if (currentEvent == null)
        {
            TempData["Error"] = "El proceso electoral no está disponible para tu grado.";
            return RedirectToAction(nameof(Dashboard));
        }

        var candidates = await _votingService.GetCandidatesForEventAsync(id);
        ViewBag.Event = currentEvent;
        ViewBag.Candidates = candidates;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmarVoto(int eventId, int candidateId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var success = await _votingService.CastVoteAsync(userId, eventId, candidateId, ip);

        if (success)
        {
            TempData["Success"] = "¡Tu voto ha sido registrado exitosamente!";
            return RedirectToAction(nameof(Dashboard));
        }

        TempData["Error"] = "Hubo un problema al registrar tu voto. Es posible que ya hayas participado.";
        return RedirectToAction(nameof(Dashboard));
    }
}


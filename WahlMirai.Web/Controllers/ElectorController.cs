using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "ELECTOR")]
public class ElectorController : Controller
{
    private readonly IVotingService _votingService;

    public ElectorController(IVotingService votingService)
    {
        _votingService = votingService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var events = await _votingService.GetActiveEventsForVoterAsync(userId);
        
        // Enhance with participation status
        var eventsWithStatus = new List<dynamic>();
        foreach(var e in events)
        {
            bool hasVoted = await _votingService.HasVotedAsync(userId, (int)e.Id);
            eventsWithStatus.Add(new { Event = e, HasVoted = hasVoted });
        }

        ViewBag.Events = eventsWithStatus;
        return View();
    }

    public async Task<IActionResult> Votar(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        if (await _votingService.HasVotedAsync(userId, id))
        {
            TempData["Error"] = "Ya has participado en esta elección.";
            return RedirectToAction("Dashboard");
        }

        var candidates = await _votingService.GetCandidatesForEventAsync(id);
        var events = await _votingService.GetActiveEventsForVoterAsync(userId);
        var currentEvent = events.FirstOrDefault(e => e.Id == id);
        
        if (currentEvent == null) return NotFound();

        ViewBag.EventId = id;
        ViewBag.EventTitle = currentEvent.Title;
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
            return RedirectToAction("Dashboard");
        }

        TempData["Error"] = "Hubo un problema al registrar tu voto. Es posible que ya hayas participado.";
        return RedirectToAction("Dashboard");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize]
public class ResultsController : Controller
{
    private readonly WahlMiraiDbContext _context;
    private readonly IVotingService _votingService;

    public ResultsController(WahlMiraiDbContext context, IVotingService votingService)
    {
        _context = context;
        _votingService = votingService;
    }

    public async Task<IActionResult> Index(int id) // id is eventId
    {
        var votingEvent = await _context.VotingEvents.FindAsync((uint)id);
        if (votingEvent == null) return NotFound();

        var role = User.FindFirstValue(ClaimTypes.Role);

        // RN-5: ADMIN and SUPER_ADMIN have unrestricted access at any time — no further checks needed.
        if (role != "ADMIN" && role != "SUPER_ADMIN")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            if (votingEvent.Status == "ACTIVA")
            {
                // RN-4: While the election is ACTIVE, access requires
                // having already cast a vote in this election.
                bool hasVoted = await _votingService.HasVotedAsync(userId, id);
                if (!hasVoted)
                {
                    // If elector hasn't voted yet, deny access so they vote first (RN-4).
                    TempData["ErrorMessage"] = "Debes emitir tu voto antes de poder consultar los resultados en vivo.";
                    return RedirectToAction("Dashboard", "Elector");
                }
            }
            else if (votingEvent.Status == "FINALIZADA")
            {
                // RN-4.1: When FINALIZADA, results are open to ALL electors whose
                // grades are enabled for this election, regardless of whether they voted.
                var voter = await _context.Voters.FindAsync((uint)userId);
                if (voter == null) return Unauthorized();

                bool gradeIsEnabled = await _context.EventGrades
                    .AnyAsync(eg => eg.VotingEventId == (uint)id && eg.GradeId == voter.GradeId);

                if (!gradeIsEnabled)
                {
                    TempData["ErrorMessage"] = "Tu grado escolar no está habilitado para esta elección.";
                    return RedirectToAction("Dashboard", "Elector");
                }
            }
            else
            {
                // PROGRAMADA, INSCRIPCION, PROPUESTAS, ELIMINADO or any non-active/finalized stage
                TempData["ErrorMessage"] = "Los resultados no están disponibles para esta elección en la etapa actual.";
                return RedirectToAction("Dashboard", "Elector");
            }
        }

        var results = await _context.VwVoteCounts
            .Where(v => v.EventId == id)
            .OrderByDescending(v => v.TotalVotes)
            .ToListAsync();

        ViewBag.EventTitle = votingEvent.Title;
        ViewBag.EventStatus = votingEvent.Status;
        ViewBag.EventId = id;

        return View(results);
    }

    [HttpGet]
    public async Task<IActionResult> GetLiveData(int id)
    {
        var votingEvent = await _context.VotingEvents.FindAsync((uint)id);
        if (votingEvent == null) return NotFound();

        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != "ADMIN" && role != "SUPER_ADMIN")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            if (votingEvent.Status == "ACTIVA")
            {
                bool hasVoted = await _votingService.HasVotedAsync(userId, id);
                if (!hasVoted) return Forbid();
            }
            else if (votingEvent.Status == "FINALIZADA")
            {
                var voter = await _context.Voters.FindAsync((uint)userId);
                if (voter == null) return Unauthorized();
                bool gradeIsEnabled = await _context.EventGrades
                    .AnyAsync(eg => eg.VotingEventId == (uint)id && eg.GradeId == voter.GradeId);
                if (!gradeIsEnabled) return Forbid();
            }
            else
            {
                // PROGRAMADA, INSCRIPCION, PROPUESTAS, ELIMINADO
                return Forbid();
            }
        }

        var results = await _context.VwVoteCounts
            .Where(v => v.EventId == id)
            .OrderByDescending(v => v.TotalVotes)
            .Select(v => new {
                candidateId = v.CandidateId,
                candidateName = v.CandidateName,
                totalVotes = v.TotalVotes
            })
            .ToListAsync();

        long totalVotes = results.Sum(r => r.totalVotes);

        return Json(new { ok = true, eventId = id, totalVotes, candidates = results });
    }
}

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

        // RN-5: ADMIN has unrestricted access at any time — no further checks needed.
        if (role != "ADMIN")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            if (votingEvent.Status == "ELIMINADO")
            {
                // Electors can never access a soft-deleted election (RN-7.1).
                return Forbid();
            }
            else if (votingEvent.Status == "ACTIVA" || votingEvent.Status == "PROGRAMADA")
            {
                // RN-4: While the election is ACTIVE (or SCHEDULED), access requires
                // having already cast a vote in this election.
                bool hasVoted = await _votingService.HasVotedAsync(userId, id);
                if (!hasVoted)
                {
                    TempData["Error"] = "Debe votar para ver los resultados.";
                    return RedirectToAction("Dashboard", "Elector");
                }
            }
            else if (votingEvent.Status == "FINALIZADA")
            {
                // RN-4.1: Once the election is FINALIZED, results are open to all
                // electors whose grade_id belongs to the enabled grades (event_grades)
                // for this election — no prior participation required.
                var voter = await _context.Voters.FindAsync((uint)userId);
                if (voter == null) return Unauthorized();

                bool gradeIsEnabled = await _context.EventGrades
                    .AnyAsync(eg => eg.VotingEventId == (uint)id && eg.GradeId == voter.GradeId);

                if (!gradeIsEnabled)
                {
                    TempData["Error"] = "No pertenece a un grado habilitado para esta elección.";
                    return RedirectToAction("Dashboard", "Elector");
                }
            }
            else
            {
                // Unknown status — deny access to be safe.
                return Forbid();
            }
        }

        var results = await _context.VwVoteCounts
            .Where(v => v.EventId == id)
            .OrderByDescending(v => v.TotalVotes)
            .ToListAsync();

        ViewBag.EventTitle = votingEvent.Title;
        ViewBag.EventStatus = votingEvent.Status;

        return View(results);
    }
}

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
        var role = User.FindFirstValue(ClaimTypes.Role);
        
        if (role == "ELECTOR")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            bool hasVoted = await _votingService.HasVotedAsync(userId, id);
            if (!hasVoted)
            {
                // RN-4: Block if elector hasn't voted
                TempData["Error"] = "No puedes ver los resultados hasta que hayas emitido tu voto.";
                return RedirectToAction("Dashboard", "Elector");
            }
        }
        // RN-5: ADMIN bypasses this check

        var results = await _context.VwVoteCounts
            .Where(v => v.EventId == id)
            .OrderByDescending(v => v.TotalVotes)
            .ToListAsync();

        var votingEvent = await _context.VotingEvents.FindAsync((uint)id);
        if (votingEvent == null) return NotFound();

        ViewBag.EventTitle = votingEvent.Title;
        ViewBag.EventStatus = votingEvent.Status;
        
        return View(results);
    }
}

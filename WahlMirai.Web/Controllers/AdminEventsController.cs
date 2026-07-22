using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminEventsController : Controller
{
    private readonly WahlMiraiDbContext _context;

    public AdminEventsController(WahlMiraiDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _context.VotingEvents
            .Include(e => e.Candidates)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        return View(events);
    }
}

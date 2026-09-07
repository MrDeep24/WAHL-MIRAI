using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "ELECTOR")]
public class CandidacyController : Controller
{
    private readonly ICandidacyService _candidacyService;
    private readonly IWebHostEnvironment _env;

    public CandidacyController(ICandidacyService candidacyService, IWebHostEnvironment env)
    {
        _candidacyService = candidacyService;
        _env = env;
    }

    // ─── Dashboard "Mis Candidaturas" ─────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var voterId = GetVoterId();
        if (voterId == null) return Unauthorized();

        var myPostulations = await _candidacyService.GetMyPostulationsAsync(voterId.Value);
        var eligibleEvents = await _candidacyService.GetEligibleEventsForPostulationAsync(voterId.Value);

        ViewBag.MyPostulations = myPostulations;
        ViewBag.EligibleEvents = eligibleEvents;
        return View();
    }

    // ─── Formulario de Postulación ────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Apply(int id)
    {
        var voterId = GetVoterId();
        if (voterId == null) return Unauthorized();

        var formDetail = await _candidacyService.GetPostulationFormDetailAsync(id, voterId.Value);
        if (formDetail == null)
        {
            TempData["Error"] = "El proceso electoral no está disponible para inscripción o ya no se encuentra en período de inscripción.";
            return RedirectToAction(nameof(Index));
        }

        return View(formDetail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(
        int eventId,
        string? slogan,
        IFormFile? photo,
        IFormFile? governmentPlan,
        List<string>? proposals)
    {
        var voterId = GetVoterId();
        if (voterId == null) return Unauthorized();

        // Collect document uploads keyed by requirement_id
        var documents = new Dictionary<uint, IFormFile>();
        foreach (var key in Request.Form.Files.Select(f => f.Name))
        {
            if (key.StartsWith("doc_") && uint.TryParse(key.Replace("doc_", ""), out uint reqId))
            {
                var file = Request.Form.Files[key];
                if (file != null && file.Length > 0)
                    documents[reqId] = file;
            }
        }

        var dto = new PostulationSubmitDto
        {
            EventId = (uint)eventId,
            Slogan = slogan,
            Photo = photo,
            GovernmentPlan = governmentPlan,
            Proposals = proposals?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? [],
            Documents = documents
        };

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var result = await _candidacyService.SubmitPostulationAsync(dto, voterId.Value, ip, _env.WebRootPath);

        if (result.Success)
        {
            TempData["Success"] = "¡Tu postulación fue registrada exitosamente! El equipo administrativo la revisará pronto.";
            return RedirectToAction(nameof(Status), new { id = result.CandidateId });
        }

        TempData["Error"] = result.ErrorMessage;
        return RedirectToAction(nameof(Apply), new { id = eventId });
    }

    // ─── Estado de Postulación ────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
        var voterId = GetVoterId();
        if (voterId == null) return Unauthorized();

        // Load candidate filtered to this voter to prevent unauthorized access
        var myPostulations = await _candidacyService.GetMyPostulationsAsync(voterId.Value);
        var postulation = myPostulations.FirstOrDefault(p => p.CandidateId == (uint)id);

        if (postulation == null)
        {
            TempData["Error"] = "Candidatura no encontrada.";
            return RedirectToAction(nameof(Index));
        }

        return View(postulation);
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private int? GetVoterId()
    {
        var str = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(str, out var id) ? id : null;
    }
}

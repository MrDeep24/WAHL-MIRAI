using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly WahlMiraiDbContext _context;
    private readonly IProfileService _profileService;
    private readonly IDocumentEncryptionService _encryptionService;

    public ProfileController(
        WahlMiraiDbContext context,
        IProfileService profileService,
        IDocumentEncryptionService encryptionService)
    {
        _context = context;
        _profileService = profileService;
        _encryptionService = encryptionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var voter = await _context.Voters
            .Include(v => v.Role)
            .Include(v => v.Grade)
            .FirstOrDefaultAsync(v => v.Id == userId);

        if (voter == null) return NotFound();

        var model = new ProfileViewModel
        {
            FullName = voter.FullName,
            DocumentDisplay = _encryptionService.Decrypt(voter.EncryptedDocument),
            GradeName = voter.Grade?.Name,
            Role = voter.Role.Name,
            Status = voter.Status,
            ContactEmail = voter.ContactEmail
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (string.IsNullOrEmpty(model.CurrentPassword))
        {
            ModelState.AddModelError("CurrentPassword", "La contraseña actual es obligatoria para guardar los cambios.");
        }

        if (!ModelState.IsValid)
        {
            // We need to re-populate read-only fields since they are not posted back
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var voter = await _context.Voters
                    .Include(v => v.Role)
                    .Include(v => v.Grade)
                    .FirstOrDefaultAsync(v => v.Id == userId);

                if (voter != null)
                {
                    model.FullName = voter.FullName;
                    model.DocumentDisplay = _encryptionService.Decrypt(voter.EncryptedDocument);
                    model.GradeName = voter.Grade?.Name;
                    model.Role = voter.Role.Name;
                    model.Status = voter.Status;
                }
            }
            return View("Index", model);
        }

        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(currentUserIdStr, out int currentUserId)) return Unauthorized();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var (success, errorMessage) = await _profileService.UpdateProfileAsync(
            currentUserId,
            model.ContactEmail,
            model.CurrentPassword!,
            model.NewPassword,
            ip
        );

        if (success)
        {
            TempData["Success"] = "Perfil actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        
        TempData["Error"] = errorMessage;
        return RedirectToAction(nameof(Index));
    }
}

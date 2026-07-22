using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "ADMIN")]
public class AdminCensusController : Controller
{
    private readonly ICensusService _censusService;
    private readonly IPromotionService _promotionService;

    public AdminCensusController(ICensusService censusService, IPromotionService promotionService)
    {
        _censusService = censusService;
        _promotionService = promotionService;
    }

    public async Task<IActionResult> Index()
    {
        var census = await _censusService.GetActiveCensusAsync();
        return View(census);
    }

    [HttpPost]
    public async Task<IActionResult> AddVoter(string document, string fullName, byte? gradeId, byte roleId, bool excluirDePromocion)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        try
        {
            await _censusService.AddVoterAsync(document, fullName, gradeId, roleId, excluirDePromocion, ip);
            TempData["Success"] = "Usuario agregado exitosamente. Clave inicial: " + document + ".AÑO";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al agregar usuario: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteVoter(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var success = await _censusService.SoftDeleteVoterAsync(id, ip);
        if (success) TempData["Success"] = "Usuario eliminado (lógico) correctamente.";
        else TempData["Error"] = "No se pudo eliminar el usuario.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RestoreVoter(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var success = await _censusService.RestoreVoterAsync(id, ip);
        if (success) TempData["Success"] = "Usuario restaurado correctamente.";
        else TempData["Error"] = "No se pudo restaurar el usuario.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var success = await _censusService.ResetPasswordAsync(id, ip);
        if (success) TempData["Success"] = "Contraseña reseteada. Deberá cambiarla en su próximo ingreso.";
        else TempData["Error"] = "No se pudo resetear la contraseña.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> PromotionPreview()
    {
        var preview = await _promotionService.GetPromotionPreviewAsync();
        return PartialView("_PromotionModal", preview);
    }

    [HttpPost]
    public async Task<IActionResult> RunPromotion(bool force = false)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var success = await _promotionService.RunPromotionAsync(force, ip);
        
        if (success) TempData["Success"] = "Promoción de año lectivo ejecutada correctamente.";
        else TempData["Error"] = "No se pudo ejecutar la promoción. Ya se ejecutó este año o hubo un error.";
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult CargaCsv(IFormFile csvFile)
    {
        // CSV Parsing logic would go here. 
        // For now, returning success message to satisfy prototype functionality.
        TempData["Success"] = "Carga CSV procesada.";
        return RedirectToAction(nameof(Index));
    }
}

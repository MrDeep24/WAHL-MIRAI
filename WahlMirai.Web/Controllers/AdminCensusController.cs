using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "ADMIN,SUPER_ADMIN")]
public class AdminCensusController : Controller
{
    private readonly ICensusService _censusService;
    private readonly IPromotionService _promotionService;
    private readonly IDocumentEncryptionService _encryptionService;
    private readonly WahlMiraiDbContext _context;

    public AdminCensusController(
        ICensusService censusService,
        IPromotionService promotionService,
        IDocumentEncryptionService encryptionService,
        WahlMiraiDbContext context)
    {
        _censusService = censusService;
        _promotionService = promotionService;
        _encryptionService = encryptionService;
        _context = context;
    }

    private uint CurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return uint.TryParse(claim, out var id) ? id : 1;
    }

    // ── Censo Activo (Paginado) ───────────────────────────────────────────────────

    public async Task<IActionResult> Index(
        string? search = null,
        string? grade = null,
        string? status = null,
        byte? roleId = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var pagedVoters = await _censusService.GetVotersPagedAsync(search, grade, status, roleId, pageNumber, pageSize);
        var grades = await _context.Grades.OrderBy(g => g.SequenceOrder).ToListAsync();

        ViewBag.Search = search;
        ViewBag.Grade = grade;
        ViewBag.Status = status;
        ViewBag.RoleId = roleId;
        ViewBag.Grades = grades;

        return View(pagedVoters);
    }

    [HttpGet]
    public async Task<IActionResult> GetVoterDetails(int id)
    {
        var voter = await _censusService.GetVoterDetailsAsync(id);
        if (voter == null) return NotFound();
        return Json(voter);
    }

    [HttpPost]
    public async Task<IActionResult> EditVoter(int id, string fullName, string contactEmail, byte? gradeId, byte roleId, string status, bool excluirDePromocion)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        try
        {
            var success = await _censusService.UpdateVoterAsync(id, fullName, contactEmail, gradeId, roleId, status, excluirDePromocion, ip);
            if (success) TempData["Success"] = $"Información del usuario '{fullName}' actualizada correctamente.";
            else TempData["Error"] = "No se pudo actualizar el usuario especificado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al actualizar el usuario: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteVoter(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var success = await _censusService.SoftDeleteVoterAsync(id, ip);
        if (success) TempData["Success"] = "Usuario marcado como ELIMINADO (borrado lógico) en el censo.";
        else TempData["Error"] = "No se pudo eliminar el usuario.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RestoreVoter(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var success = await _censusService.RestoreVoterAsync(id, ip);
        if (success) TempData["Success"] = "Usuario restaurado a estado ACTIVO correctamente.";
        else TempData["Error"] = "No se pudo restaurar el usuario.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var success = await _censusService.ResetPasswordAsync(id, ip);
        if (success) TempData["Success"] = "Nueva contraseña aleatoria generada y encolada para envío al correo de contacto.";
        else TempData["Error"] = "No se pudo reasignar la contraseña. Verifique que el elector tenga un correo de contacto válido.";
        return RedirectToAction(nameof(Index));
    }

    // ── M02: Lista Blanca (Pendientes por reclamar) ───────────────────────────────

    [HttpGet]
    public async Task<IActionResult> PendingWhitelist(
        string? search = null,
        byte? gradeId = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var pagedWhitelist = await _censusService.GetPendingWhitelistPagedAsync(search, gradeId, pageNumber, pageSize);
        var stats = await _censusService.GetWhitelistStatsAsync();
        var grades = await _context.Grades.OrderBy(g => g.SequenceOrder).ToListAsync();

        ViewBag.Search = search;
        ViewBag.GradeId = gradeId;
        ViewBag.Stats = stats;
        ViewBag.Grades = grades;

        return View(pagedWhitelist);
    }

    [HttpGet]
    public async Task<IActionResult> GetWhitelistDetails(uint id)
    {
        var entry = await _censusService.GetWhitelistEntryAsync(id);
        if (entry == null) return NotFound();
        return Json(entry);
    }

    [HttpPost]
    public async Task<IActionResult> AddToWhitelist(string document, string fullName, byte gradeId, bool excluirDePromocion, string? returnUrl = null)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var adminUserId = CurrentUserId();
        try
        {
            await _censusService.AddToWhitelistAsync(document, fullName, gradeId, excluirDePromocion, adminUserId, ip);
            TempData["Success"] = $"Estudiante '{fullName}' registrado exitosamente en la lista blanca. Podrá realizar su auto-registro con el documento registrado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al registrar en lista blanca: " + ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(PendingWhitelist));
    }

    // Compatibilidad: redirige cualquier intento de AddVoter hacia la lista blanca
    [HttpPost]
    public async Task<IActionResult> AddVoter(string document, string fullName, byte gradeId, bool excluirDePromocion)
    {
        return await AddToWhitelist(document, fullName, gradeId, excluirDePromocion, Url.Action(nameof(Index)));
    }

    [HttpPost]
    public async Task<IActionResult> EditWhitelistEntry(uint id, string fullName, byte gradeId, bool excluirDePromocion)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var adminUserId = CurrentUserId();
        try
        {
            var success = await _censusService.UpdateWhitelistEntryAsync(id, fullName, gradeId, excluirDePromocion, adminUserId, ip);
            if (success) TempData["Success"] = $"Entrada de lista blanca para '{fullName}' actualizada correctamente.";
            else TempData["Error"] = "No se pudo actualizar la entrada especificada.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al actualizar entrada de lista blanca: " + ex.Message;
        }

        return RedirectToAction(nameof(PendingWhitelist));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteWhitelistEntry(uint id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var adminUserId = CurrentUserId();
        try
        {
            var success = await _censusService.DeleteWhitelistEntryAsync(id, adminUserId, ip);
            if (success) TempData["Success"] = "Entrada eliminada de la lista blanca correctamente.";
            else TempData["Error"] = "No se pudo eliminar la entrada especificada.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al eliminar entrada de lista blanca: " + ex.Message;
        }

        return RedirectToAction(nameof(PendingWhitelist));
    }

    // ── Carga Masiva CSV ─────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> CargaCsv(IFormFile csvFile, string? returnUrl = null)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var adminUserId = CurrentUserId();

        if (csvFile == null || csvFile.Length == 0)
        {
            TempData["Error"] = "Por favor seleccione un archivo CSV válido para cargar.";
            return RedirectToAction(nameof(PendingWhitelist));
        }

        if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Formato de archivo no permitido. Debe seleccionar un archivo con extensión .csv";
            return RedirectToAction(nameof(PendingWhitelist));
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            var importResult = await _censusService.ImportCsvAsync(stream, adminUserId, ip);

            var summary = $"Procesados: {importResult.ProcessedCount} | Insertados en Lista Blanca: {importResult.InsertedCount} | Duplicados: {importResult.DuplicateCount} | Errores: {importResult.ErrorCount}";

            if (importResult.ErrorCount == 0 && importResult.DuplicateCount == 0 && importResult.InsertedCount > 0)
            {
                TempData["Success"] = $"Importación de CSV completada con éxito. {summary}";
            }
            else
            {
                var errorMsgs = string.Join("<br/>", importResult.Errors.Take(10).Select(e => $"Fila {e.RowNumber} [{e.Identifier}]: {e.Reason}"));
                if (importResult.Errors.Count > 10)
                {
                    errorMsgs += $"<br/>... y {importResult.Errors.Count - 10} errores adicionales.";
                }

                TempData["Warning"] = $"Resumen de Carga CSV: {summary}.<br/><br/><strong>Detalles de observaciones:</strong><br/>{errorMsgs}";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error inesperado al procesar la carga masiva CSV: " + ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(PendingWhitelist));
    }

    [HttpGet]
    public IActionResult DescargarPlantillaCsv()
    {
        var csvBytes = _censusService.GenerateCsvTemplate();
        return File(csvBytes, "text/csv", "plantilla_censo_whitelist.csv");
    }

    // ── Promoción de Grado ───────────────────────────────────────────────────────

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
        var result = await _promotionService.RunPromotionAsync(force, ip);

        if (result.Success)
        {
            TempData["Success"] = $"{result.Message} Promovidos: {result.PromotedCount} (Activos: {result.ActivePromotedCount}, Pendientes: {result.WhitelistPromotedCount}) | Egresados: {result.GraduatedCount} | Repitentes mantenidos: {result.RetainedCount}";
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // ── Migración de Datos (Cifrado) ─────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> MigrateDocuments()
    {
        var voters = await _context.Voters.ToListAsync();
        int migrated = 0;
        int skipped  = 0;
        int failed   = 0;
        var failedIds = new List<uint>();

        foreach (var voter in voters)
        {
            try
            {
                var decrypted = _encryptionService.Decrypt(voter.EncryptedDocument);
                if (decrypted != voter.EncryptedDocument)
                {
                    skipped++;
                    continue;
                }

                voter.EncryptedDocument = _encryptionService.Encrypt(decrypted);
                migrated++;
            }
            catch (CryptographicException)
            {
                failed++;
                failedIds.Add(voter.Id);
            }
            catch (Exception)
            {
                failed++;
                failedIds.Add(voter.Id);
            }
        }

        if (migrated > 0)
            await _context.SaveChangesAsync();

        var summary = $"Migración completada. Migrados: {migrated} | Ya cifrados (omitidos): {skipped} | Fallidos: {failed}";
        if (failedIds.Count > 0)
            summary += $" | IDs fallidos: {string.Join(", ", failedIds)}";

        if (failed == 0)
            TempData["Success"] = summary;
        else
            TempData["Error"] = summary;

        return RedirectToAction(nameof(Index));
    }
}

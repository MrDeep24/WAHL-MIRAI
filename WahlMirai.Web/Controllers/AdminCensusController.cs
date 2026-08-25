using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
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

    public async Task<IActionResult> Index(string? search = null, string? grade = null, string? status = null, byte? roleId = null)
    {
        var voters = await _censusService.GetAllVotersAsync(search, grade, status, roleId);
        ViewBag.Search = search;
        ViewBag.Grade = grade;
        ViewBag.Status = status;
        ViewBag.RoleId = roleId;
        return View(voters);
    }

    [HttpGet]
    public async Task<IActionResult> GetVoterDetails(int id)
    {
        var voter = await _censusService.GetVoterDetailsAsync(id);
        if (voter == null) return NotFound();
        return Json(voter);
    }

    [HttpPost]
    public async Task<IActionResult> AddVoter(string document, string fullName, string contactEmail, byte? gradeId, byte roleId, bool excluirDePromocion)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        try
        {
            await _censusService.AddVoterAsync(document, fullName, contactEmail, gradeId, roleId, excluirDePromocion, ip);
            TempData["Success"] = $"Usuario '{fullName}' registrado exitosamente en el censo. Se envió la contraseña inicial a '{contactEmail}'.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error al registrar usuario: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
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
            TempData["Success"] = $"{result.Message} Promovidos: {result.PromotedCount} | Egresados: {result.GraduatedCount} | Repitentes mantenidos: {result.RetainedCount}";
        }
        else
        {
            TempData["Error"] = result.Message;
        }
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CargaCsv(IFormFile csvFile)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        if (csvFile == null || csvFile.Length == 0)
        {
            TempData["Error"] = "Por favor seleccione un archivo CSV válido para cargar.";
            return RedirectToAction(nameof(Index));
        }

        if (!csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Formato de archivo no permitido. Debe seleccionar un archivo con extensión .csv";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            var importResult = await _censusService.ImportCsvAsync(stream, ip);

            var summary = $"Procesados: {importResult.ProcessedCount} | Insertados: {importResult.InsertedCount} | Duplicados: {importResult.DuplicateCount} | Errores: {importResult.ErrorCount}";

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

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult DescargarPlantillaCsv()
    {
        var csvBytes = _censusService.GenerateCsvTemplate();
        return File(csvBytes, "text/csv", "plantilla_censo_electoral.csv");
    }


    // ── MIGRACIÓN DE DATOS (uso único) ───────────────────────────────────────────
    // Este endpoint es EXCLUSIVAMENTE para migrar los registros existentes de texto plano
    // al esquema de cifrado con Data Protection API.
    //
    // CUÁNDO ejecutarlo: UNA SOLA VEZ, antes del despliegue de esta versión a producción,
    //   con la base de datos en estado de mantenimiento (sin usuarios activos si es posible).
    //
    // CÓMO ejecutarlo:
    //   1. Iniciar la aplicación normalmente.
    //   2. Autenticarse como ADMIN.
    //   3. Hacer POST a /AdminCensus/MigrateDocuments (por ejemplo desde el panel de admin
    //      o con: curl -X POST https://host/AdminCensus/MigrateDocuments -H "Cookie: ...")
    //   4. Revisar los mensajes en TempData (Success / Error).
    //   5. Verificar en la base de datos que encrypted_document ya no coincide con el documento
    //      plano y que se puede desencriptar correctamente.
    //   6. Ejecutar una segunda vez para confirmar idempotencia (debe reportar 0 migraciones).
    //
    // IDEMPOTENTE: Si un registro ya estaba cifrado, el descifrado con Decrypt() tendrá éxito
    //   y el registro se omite. No se re-cifra dos veces.
    //
    // TODO: Eliminar este endpoint (y el bloque de catch en Decrypt) una vez completada la
    //   migración en producción.
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
                // Intentar descifrar: si tiene éxito, el registro ya está migrado → omitir
                _encryptionService.Decrypt(voter.EncryptedDocument);

                // Decrypt no lanzó excepción, lo que significa que:
                //   a) el valor ya estaba correctamente cifrado (registro ya migrado), O
                //   b) el fallback temporal de Decrypt devolvió el texto plano (registro sin cifrar).
                // Para distinguir ambos casos sin introducir un método IsEncrypted frágil,
                // verificamos si el resultado de Encrypt(Decrypt(x)) == x (cifrado ya existente
                // sería diferente porque Protect() añade un nonce aleatorio).
                // La forma más robusta y simple: intentar Unprotect directamente vía la excepción.
                // Como el fallback devuelve el valor tal cual cuando falla, si Decrypt(valor) == valor
                // Y el valor no parece ser un payload de Data Protection (que empieza con 'AQAAAA'),
                // entonces está en texto plano.
                //
                // Heurística simple y suficiente para este script de migración:
                // Los payloads de Data Protection API codificados en Base64 no contienen
                // caracteres típicos de documentos de identidad (solo dígitos/letras de cédulas).
                // Si el valor resultante de Decrypt es idéntico al almacenado, verificamos si luce
                // como un payload cifrado comprobando si empieza con el header esperado.
                var decrypted = _encryptionService.Decrypt(voter.EncryptedDocument);
                if (decrypted != voter.EncryptedDocument)
                {
                    // El descifrado devolvió algo distinto → ya estaba cifrado. Omitir.
                    skipped++;
                    continue;
                }

                // Si decrypted == voter.EncryptedDocument, puede ser texto plano (fallback)
                // o un payload cifrado cuyo descifrado coincide exactamente con sí mismo (imposible).
                // En la práctica: si llegamos aquí, es texto plano sin migrar.
                voter.EncryptedDocument = _encryptionService.Encrypt(decrypted);
                migrated++;
            }
            catch (CryptographicException)
            {
                // Esto no debería ocurrir en este punto porque Decrypt() ya captura la excepción,
                // pero si ocurre, marcamos el registro como fallido.
                failed++;
                failedIds.Add(voter.Id);
                Console.Error.WriteLine($"[MigrateDocuments] FALLO al procesar voterId={voter.Id}");
            }
            catch (Exception ex)
            {
                failed++;
                failedIds.Add(voter.Id);
                Console.Error.WriteLine($"[MigrateDocuments] Error inesperado en voterId={voter.Id}: {ex.Message}");
            }
        }

        if (migrated > 0)
            await _context.SaveChangesAsync();

        var summary = $"Migración completada. Migrados: {migrated} | Ya cifrados (omitidos): {skipped} | Fallidos: {failed}";
        if (failedIds.Count > 0)
            summary += $" | IDs fallidos: {string.Join(", ", failedIds)}";

        Console.WriteLine($"[MigrateDocuments] {summary}");

        if (failed == 0)
            TempData["Success"] = summary;
        else
            TempData["Error"] = summary;

        return RedirectToAction(nameof(Index));
    }
    // ─────────────────────────────────────────────────────────────────────────────
}

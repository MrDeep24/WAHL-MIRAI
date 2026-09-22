using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[Authorize]
public class MediaController : Controller
{
    private readonly IDocumentEncryptionService _docEncryptor;
    private readonly IConfiguration _config;

    public MediaController(IDocumentEncryptionService docEncryptor, IConfiguration config)
    {
        _docEncryptor = docEncryptor;
        _config = config;
    }

    [HttpGet]
    [Route("Media/Serve")]
    [Route("Media/ServeDocument")]
    public IActionResult Serve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest("Ruta de documento no especificada.");

        var basePath = _config["CandidacyUploads:BasePath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "candidacy-uploads");

        var fileName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Archivo no válido.");

        // Validar permisos: Electores solo pueden consultar fotos y planes de gobierno públicos
        if (!User.IsInRole("ADMIN") && !User.IsInRole("SUPER_ADMIN"))
        {
            if (!fileName.StartsWith("photo_", StringComparison.OrdinalIgnoreCase) &&
                !fileName.StartsWith("plan_", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }
        }

        var fullPath = Path.Combine(basePath, "candidacies", fileName);
        if (!System.IO.File.Exists(fullPath))
            return NotFound("El documento no está disponible.");

        string cipher;
        try
        {
            cipher = System.IO.File.ReadAllText(fullPath);
        }
        catch
        {
            return StatusCode(500, "Error al leer el documento.");
        }

        byte[] fileBytes;
        try
        {
            var plain = _docEncryptor.Decrypt(cipher);
            fileBytes = Convert.FromBase64String(plain);
        }
        catch
        {
            return StatusCode(500, "Error al descifrar el documento.");
        }

        var originalName = fileName[..^4];
        var ext = Path.GetExtension(originalName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf"  => "application/pdf",
            ".png"  => "image/png",
            ".jpg"  => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _       => "application/octet-stream"
        };

        return File(fileBytes, contentType);
    }
}

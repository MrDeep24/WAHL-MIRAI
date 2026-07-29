using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

[AllowAnonymous]
public class RecuperacionAccesoController : Controller
{
    private readonly WahlMiraiDbContext _dbContext;
    private readonly ICredentialService _credentialService;

    public RecuperacionAccesoController(WahlMiraiDbContext dbContext, ICredentialService credentialService)
    {
        _dbContext = dbContext;
        _credentialService = credentialService;
    }

    [HttpGet]
    public IActionResult Recuperar()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Recuperar(RecuperarAccesoViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var documentHash = ComputeSha256(model.Documento);

        var voter = await _dbContext.Voters
            .FirstOrDefaultAsync(v => v.DocumentHash == documentHash);

        if (voter != null && voter.Status == "ACTIVO")
        {
            await _credentialService.IssueNewPasswordAsync((int)voter.Id, EmailType.RECUPERACION_ACCESO, null);
        }

        // Siempre retornamos la misma vista de éxito para evitar enumeración (anti-enumeración).
        // Si no se encuentra, simplemente aparentamos éxito.
        ViewBag.SuccessMessage = "Si el documento está registrado, recibirás una nueva contraseña en el correo de contacto asociado.";
        return View("Exito");
    }

    private string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

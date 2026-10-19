// ============================================================
// Archivo:       LegalController.cs
// Módulo:        Cumplimiento Normativo / Legal (Ley 1581 / Dec 1074)
// Desarrollador: Kevin
// Descripción:   Publica las páginas legales de acceso anónimo: Aviso Legal,
//                Privacidad, Cookies, Términos de Uso y Contacto.
// Requerimientos: RN-16, Ley 1581 de 2012, Dec 1074 de 2015 Cap. 25.
// ============================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WahlMirai.Web.Models;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

/// <summary>
/// Controlador público para consultar la documentación jurídica, políticas de privacidad y canales de Habeas Data.
/// </summary>
[AllowAnonymous]
public class LegalController : Controller
{
    private readonly InstitutionSettings _institutionSettings;

    public LegalController(IOptions<InstitutionSettings> institutionOptions)
    {
        _institutionSettings = institutionOptions.Value;
    }

    private LegalViewModel BuildModel() => new()
    {
        Settings = _institutionSettings
    };

    /// <summary>Hub o índice del marco legal institucional.</summary>
    [HttpGet("/Legal")]
    [HttpGet("/Legal/Index")]
    public IActionResult Index()
    {
        return View(BuildModel());
    }

    /// <summary>Política de tratamiento de datos personales y privacidad (Habeas Data).</summary>
    [HttpGet("/Legal/Privacidad")]
    public IActionResult Privacidad()
    {
        return View(BuildModel());
    }

    /// <summary>Política técnica de cookies y tecnologías de almacenamiento local.</summary>
    [HttpGet("/Legal/Cookies")]
    public IActionResult Cookies()
    {
        return View(BuildModel());
    }

    /// <summary>Términos y condiciones de uso del sistema de votaciones.</summary>
    [HttpGet("/Legal/Terminos")]
    public IActionResult Terminos()
    {
        return View(BuildModel());
    }

    /// <summary>Canales oficiales de contacto institucional y atención de datos personales.</summary>
    [HttpGet("/Legal/Contacto")]
    public IActionResult Contacto()
    {
        return View(BuildModel());
    }
}

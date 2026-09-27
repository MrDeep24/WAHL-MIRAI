// ============================================================
// Archivo:       RecuperacionAccesoController.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Recibe solicitudes públicas de recuperación y
//                evita revelar si un documento pertenece a una cuenta activa.
// Requerimientos: RF-M01-02; apoyo para RN-8 y RN-9.
// ============================================================
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
/// <summary>
/// Expone el flujo público de recuperación de acceso por documento, con respuesta anti-enumeración
/// para cuentas existentes y no existentes (RF-M01-02).
/// </summary>
public class RecuperacionAccesoController : Controller
{
    private readonly WahlMiraiDbContext _dbContext;
    private readonly ICredentialService _credentialService;

    /// <summary>Inicializa el controlador con acceso a usuarios y al servicio de credenciales.</summary>
    /// <param name="dbContext">Contexto usado para buscar la cuenta por hash de documento.</param>
    /// <param name="credentialService">Servicio de apoyo que emite la nueva contraseña y crea el aviso.</param>
    public RecuperacionAccesoController(WahlMiraiDbContext dbContext, ICredentialService credentialService)
    {
        _dbContext = dbContext;
        _credentialService = credentialService;
    }

    /// <summary>Muestra el formulario público para solicitar recuperación de acceso.</summary>
    [HttpGet]
    public IActionResult Recuperar()
    {
        return View();
    }

    /// <summary>
    /// Busca una cuenta activa por documento y solicita su nueva contraseña; siempre muestra el mismo
    /// resultado para evitar enumerar cuentas (RF-M01-02).
    /// </summary>
    /// <param name="model">Documento enviado en la solicitud de recuperación.</param>
    /// <returns>El formulario con errores de validación o la vista de resultado genérico.</returns>
    [HttpPost]
    public async Task<IActionResult> Recuperar(RecuperarAccesoViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var documentHash = ComputeSha256(model.Documento);

        var voter = await _dbContext.Voters
            .FirstOrDefaultAsync(v => v.DocumentHash == documentHash);

        if (voter != null && voter.Status == "ACTIVO")
        {
            // NOTA: La entrega progresiva (RN-9) depende del consumidor de la cola, fuera del alcance de este archivo.
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

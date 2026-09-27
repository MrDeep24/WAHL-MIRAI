// ============================================================
// Archivo:       AdminAccountsController.cs
// Módulo:        M09 — Gestión de Cuentas Administrativas
//                (Exclusivo Súper Administrador)
// Desarrollador: Kevin
// Descripción:   Expone las acciones para consultar, crear, editar,
//                eliminar lógicamente y restaurar cuentas administrativas.
// Requerimientos: RF-M09-01; revocación de sesión conforme a RN-14.
// ============================================================
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

/// <summary>
/// Permite al Súper Administrador gestionar cuentas administrativas (RF-M09-01).
/// La eliminación lógica y los cambios de estado se delegan al servicio, que
/// invalida la sesión activa cuando corresponde (RN-14).
/// </summary>
[Authorize(Roles = "SUPER_ADMIN")]
public class AdminAccountsController : Controller
{
    private readonly IAdminAccountService _accountService;
    private readonly ILogger<AdminAccountsController> _logger;

    /// <summary>
    /// Inicializa el controlador con el servicio de cuentas y el registrador de errores.
    /// </summary>
    /// <param name="accountService">Servicio de operaciones sobre cuentas administrativas.</param>
    /// <param name="logger">Registrador para errores inesperados durante la creación.</param>
    public AdminAccountsController(IAdminAccountService accountService, ILogger<AdminAccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    /// <summary>
    /// Muestra las cuentas administrativas, incluidas las eliminadas lógicamente (RF-M09-01).
    /// </summary>
    /// <param name="ct">Token para cancelar la consulta.</param>
    /// <returns>La vista de gestión con las cuentas administrativas.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var accounts = await _accountService.GetAccountsAsync(true, ct);
        return View(accounts);
    }

    /// <summary>
    /// Crea una cuenta administrativa y solicita la emisión de su contraseña inicial (RF-M09-01).
    /// </summary>
    /// <param name="model">Datos validados de la nueva cuenta.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Redirección a la lista con el resultado de la operación.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminAccountFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise los datos de la cuenta administrativa.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _accountService.CreateAsync(model.Document, model.FullName, model.ContactEmail,
                model.RoleName, model.PositionTitle, CurrentUserId(), ClientIp(), ct);
            TempData["Success"] = $"La cuenta de {model.FullName} fue creada. La contraseña inicial fue encolada para envío.";
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear una cuenta administrativa.");
            TempData["Error"] = "No fue posible crear la cuenta administrativa. Verifique la configuración de la base de datos y vuelva a intentarlo.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Actualiza los datos, el rol y el cargo institucional de una cuenta (RF-M09-01).
    /// </summary>
    /// <param name="model">Datos validados de la cuenta que se editará.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Redirección a la lista con el resultado de la operación.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminAccountEditViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise los datos de la cuenta administrativa.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var updated = await _accountService.UpdateAsync(model.Id, model.Document, model.FullName,
                model.ContactEmail, model.RoleName, model.PositionTitle, CurrentUserId(), ClientIp(), ct);
            TempData[updated ? "Success" : "Error"] = updated
                ? "Cuenta administrativa actualizada correctamente."
                : "No se encontró la cuenta administrativa.";
        }
        catch (ArgumentException ex) { TempData["Error"] = ex.Message; }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Elimina lógicamente una cuenta y comunica el resultado (RF-M09-01, RN-14).
    /// </summary>
    /// <param name="id">Identificador de la cuenta administrativa.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Redirección a la lista con el resultado de la operación.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var deleted = await _accountService.SoftDeleteAsync(id, CurrentUserId(), ClientIp(), ct);
            TempData[deleted ? "Success" : "Error"] = deleted
                ? "Cuenta administrativa eliminada lógicamente."
                : "No se encontró una cuenta activa para eliminar.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Restaura una cuenta administrativa eliminada lógicamente (RF-M09-01).
    /// </summary>
    /// <param name="id">Identificador de la cuenta que se restaurará.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Redirección a la lista con el resultado de la operación.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        var restored = await _accountService.RestoreAsync(id, CurrentUserId(), ClientIp(), ct);
        TempData[restored ? "Success" : "Error"] = restored
            ? "Cuenta administrativa restaurada."
            : "No se encontró una cuenta eliminada para restaurar.";
        return RedirectToAction(nameof(Index));
    }

    private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new InvalidOperationException("No se pudo identificar al usuario autenticado.");

    private string ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
}
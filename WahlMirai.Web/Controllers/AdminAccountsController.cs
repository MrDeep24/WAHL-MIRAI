using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

[Authorize(Roles = "SUPER_ADMIN")]
public class AdminAccountsController : Controller
{
    private readonly IAdminAccountService _accountService;

    public AdminAccountsController(IAdminAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var accounts = await _accountService.GetAccountsAsync(true, ct);
        return View(accounts);
    }

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
        return RedirectToAction(nameof(Index));
    }

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
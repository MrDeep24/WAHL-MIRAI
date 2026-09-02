using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Controllers;

public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [Route("Error/{code:int?}")]
    public IActionResult Index(int? code)
    {
        var statusCode = code ?? 500;
        var path = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath ?? HttpContext.Request.Path.Value;

        var model = new ErrorViewModel
        {
            StatusCode = statusCode,
            ShowHomeButton = true,
            ShowHelpButton = false
        };

        switch (statusCode)
        {
            case 400:
                model.Title = "Solicitud incorrecta";
                model.Message = "El servidor no pudo procesar la solicitud debido a una sintaxis no válida.";
                break;
            case 401:
                model.Title = "No autorizado";
                model.Message = "Debe iniciar sesión para acceder a este recurso.";
                _logger.LogWarning("Status {StatusCode} at {Path} at {Timestamp}", statusCode, path, DateTimeOffset.UtcNow);
                break;
            case 403:
                model.Title = "Prohibido";
                model.Message = "No tiene los permisos necesarios para acceder a este recurso.";
                _logger.LogWarning("Status {StatusCode} at {Path} at {Timestamp}", statusCode, path, DateTimeOffset.UtcNow);
                break;
            case 404:
                model.Title = "No encontrado";
                model.Message = "El recurso solicitado no existe o ha sido movido.";
                _logger.LogWarning("Status {StatusCode} at {Path} at {Timestamp}", statusCode, path, DateTimeOffset.UtcNow);
                break;
            default:
                model.Title = "Error interno del servidor";
                model.Message = "Ha ocurrido un error inesperado. Por favor, inténtelo de nuevo más tarde.";
                break;
        }

        return View("Index", model);
    }

    [Route("Error/500")]
    public IActionResult Error500()
    {
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var path = exceptionHandlerPathFeature?.Path ?? HttpContext.Request.Path.Value;
        var error = exceptionHandlerPathFeature?.Error;

        _logger.LogError(error, "Unhandled exception at {Path} at {Timestamp}", path, DateTimeOffset.UtcNow);

        var model = new ErrorViewModel
        {
            StatusCode = 500,
            Title = "Error interno del servidor",
            Message = "Ha ocurrido un error inesperado. Nuestro equipo técnico ha sido notificado.",
            ShowHomeButton = true,
            ShowHelpButton = true
        };

        return View("Index", model);
    }
}

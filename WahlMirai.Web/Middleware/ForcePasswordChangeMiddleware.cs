// ============================================================
// Archivo:       ForcePasswordChangeMiddleware.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Conserva el punto de extensión legado para el cambio
//                de contraseña; actualmente deja pasar cada solicitud.
// Requerimientos: No fuerza cambios; el cambio vigente es voluntario (RF-M07-01, RN-2).
// ============================================================
namespace WahlMirai.Web.Middleware;

/// <summary>
/// Middleware de cambio de contraseña forzado.
/// NOTA v2.3: El cambio forzado de contraseña fue ELIMINADO en la ERS v2.3 (RN-2 actualizado).
/// El elector ya no necesita cambiar su clave al primer inicio de sesión; el cambio es
/// completamente voluntario desde el módulo de Perfil de Usuario (RF-M07).
/// Este middleware se conserva como stub para facilitar el futuro si el requisito regresa.
/// </summary>
public class ForcePasswordChangeMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Inicializa el middleware con el siguiente delegado del pipeline.</summary>
    /// <param name="next">Siguiente componente que procesará la solicitud.</param>
    public ForcePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Continúa el pipeline sin interceptar la solicitud; el cambio forzado fue eliminado en v2.3.
    /// </summary>
    /// <param name="context">Contexto HTTP de la solicitud actual.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // RN-2 v2.3: No se intercepta ni fuerza cambio de contraseña.
        // El cambio de clave es voluntario (RF-M07-01).
        await _next(context);
    }
}

/// <summary>Proporciona el método de registro del middleware legado en el pipeline HTTP.</summary>
public static class ForcePasswordChangeMiddlewareExtensions
{
    /// <summary>Agrega el middleware de cambio forzado, que actualmente opera como stub.</summary>
    /// <param name="builder">Constructor del pipeline de solicitudes.</param>
    /// <returns>El mismo constructor para permitir encadenar configuración.</returns>
    public static IApplicationBuilder UseForcePasswordChange(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ForcePasswordChangeMiddleware>();
    }
}

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

    public ForcePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // RN-2 v2.3: No se intercepta ni fuerza cambio de contraseña.
        // El cambio de clave es voluntario (RF-M07-01).
        await _next(context);
    }
}

public static class ForcePasswordChangeMiddlewareExtensions
{
    public static IApplicationBuilder UseForcePasswordChange(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ForcePasswordChangeMiddleware>();
    }
}

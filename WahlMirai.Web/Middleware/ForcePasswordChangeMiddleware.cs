using System.Security.Claims;

namespace WahlMirai.Web.Middleware;

public class ForcePasswordChangeMiddleware
{
    private readonly RequestDelegate _next;

    public ForcePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var reqChange = context.User.FindFirst("RequiereCambioClave")?.Value;
            if (reqChange == "True")
            {
                var path = context.Request.Path.Value?.ToLower();
                if (path != "/auth/cambiarclave" && path != "/auth/logout" && !path!.StartsWith("/css") && !path.StartsWith("/js") && !path.StartsWith("/images"))
                {
                    context.Response.Redirect("/Auth/CambiarClave");
                    return;
                }
            }
        }

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

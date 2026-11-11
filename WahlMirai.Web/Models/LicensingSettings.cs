namespace WahlMirai.Web.Models;

/// <summary>
/// Configuración de licenciamiento de software y acceso al código fuente (AGPL-3.0 §13).
/// Mapea la sección "Licensing" de appsettings.json.
/// </summary>
public class LicensingSettings
{
    /// <summary>
    /// URL del repositorio o servidor donde se aloja el código fuente correspondiente (AGPL §13).
    /// </summary>
    public string? SourceCodeUrl { get; set; } = "https://github.com/MrDeep24/WAHL-MIRAI";

    /// <summary>
    /// Retorna la URL del código fuente únicamente si constituye una URI absoluta con esquema HTTP o HTTPS;
    /// de lo contrario retorna null para mitigar inyecciones de esquemas no seguros (ej. javascript:, data:).
    /// </summary>
    public string? SafeSourceCodeUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SourceCodeUrl))
                return null;

            if (Uri.TryCreate(SourceCodeUrl.Trim(), UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri.ToString();
            }

            return null;
        }
    }
}

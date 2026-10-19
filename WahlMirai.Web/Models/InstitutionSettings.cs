namespace WahlMirai.Web.Models;

/// <summary>
/// Configuración de identidad institucional y cumplimiento normativo (Ley 1581 de 2012 / Dec. 1074 de 2015).
/// Mapea la sección "Institution" de appsettings.json.
/// </summary>
public class InstitutionSettings
{
    public string Name { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string DataProtectionEmail { get; set; } = string.Empty;
    public string RetentionNote { get; set; } = string.Empty;
    public string HostingProvider { get; set; } = string.Empty;
    public string HostingRegion { get; set; } = string.Empty;
    public bool RequireLegalData { get; set; } = false;

    /// <summary>
    /// Retorna el valor configurado o un marcador visible "[PENDIENTE: <campo>]" si el valor está vacío.
    /// </summary>
    public string FormatOrPending(string? value, string fieldName)
    {
        return string.IsNullOrWhiteSpace(value) ? $"[PENDIENTE: {fieldName}]" : value.Trim();
    }
}

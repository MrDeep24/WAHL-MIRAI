using WahlMirai.Web.Models;

namespace WahlMirai.Web.ViewModels;

/// <summary>
/// Modelo de vista para las páginas informativas del marco legal institucional.
/// </summary>
public class LegalViewModel
{
    public InstitutionSettings Settings { get; set; } = new();
    public string Version => LegalDocuments.Version;
    public string LastUpdated => LegalDocuments.LastUpdated;

    public string FormatOrPending(string? value, string fieldName) =>
        Settings.FormatOrPending(value, fieldName);
}

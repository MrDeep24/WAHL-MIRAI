namespace WahlMirai.Web.ViewModels;

public class ErrorViewModel
{
    public int StatusCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool ShowHomeButton { get; set; } = true;
    public bool ShowHelpButton { get; set; } = false;
    public string ExceptionMessage { get; set; } = string.Empty;
}

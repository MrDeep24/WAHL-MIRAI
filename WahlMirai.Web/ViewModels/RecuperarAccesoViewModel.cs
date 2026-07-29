using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

public class RecuperarAccesoViewModel
{
    [Required(ErrorMessage = "El documento es obligatorio.")]
    public string Documento { get; set; } = string.Empty;
}

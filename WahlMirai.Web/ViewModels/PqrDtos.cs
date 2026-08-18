using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

public class PqrCreateDto
{
    [Required(ErrorMessage = "El asunto es obligatorio.")]
    [StringLength(200, ErrorMessage = "El asunto debe tener máximo 200 caracteres.")]
    public string Subject { get; set; } = null!;

    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    public string Message { get; set; } = null!;
}

public class PqrResponseDto
{
    [Required(ErrorMessage = "La respuesta del administrador es obligatoria.")]
    public string AdminResponse { get; set; } = null!;
}

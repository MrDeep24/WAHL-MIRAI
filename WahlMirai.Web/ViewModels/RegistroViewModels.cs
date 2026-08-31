using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

public class RegistroStep1ViewModel
{
    [Required(ErrorMessage = "El número de documento es obligatorio.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "El documento debe contener solo dígitos, sin espacios ni guiones.")]
    [Display(Name = "Número de documento")]
    public string Document { get; set; } = string.Empty;
}

public class RegistroStep2ViewModel
{
    [Required]
    public string Document { get; set; } = string.Empty;

    public uint WhitelistId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string GradeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo de contacto es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede exceder los 150 caracteres.")]
    [Display(Name = "Correo de contacto")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?`~]).*$",
        ErrorMessage = "La contraseña debe contener al menos una letra mayúscula y un símbolo especial.")]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

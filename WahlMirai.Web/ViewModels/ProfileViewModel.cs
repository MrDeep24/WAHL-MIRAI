using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

public class ProfileViewModel
{
    // Read-only fields
    public string FullName { get; set; } = null!;
    
    public string DocumentDisplay { get; set; } = null!;
    
    public string? GradeName { get; set; }
    
    public string Role { get; set; } = null!;
    
    public string Status { get; set; } = null!;

    // Editable fields
    [Required(ErrorMessage = "El correo de contacto es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    [Display(Name = "Correo de Contacto")]
    public string ContactEmail { get; set; } = null!;

    // Password change fields
    [Display(Name = "Contraseña Actual")]
    public string? CurrentPassword { get; set; }

    [Display(Name = "Nueva Contraseña")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string? NewPassword { get; set; }

    [Display(Name = "Confirmar Nueva Contraseña")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas nuevas no coinciden.")]
    public string? ConfirmNewPassword { get; set; }
}

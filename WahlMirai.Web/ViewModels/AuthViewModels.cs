// ============================================================
// Archivo:       AuthViewModels.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define los datos de formulario y validaciones
//                de inicio de sesión y cambio de contraseña.
// Requerimientos: RF-M01-01 y apoyo de formulario para RF-M07-01.
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

/// <summary>Datos y validaciones de entrada para iniciar sesión por documento (RF-M01-01).</summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "El documento es obligatorio")]
    [RegularExpression(@"^\d+$", ErrorMessage = "El número de documento debe contener solo dígitos sin espacios ni guiones")]
    public string Document { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

// NOTA: La validación de AuthController.CambiarClave ya cumple la regla de complejidad de RF-M07-01.
/// <summary>
/// Datos de entrada para el cambio de contraseña de AuthController; exige longitud mínima de ocho
/// caracteres, una mayúscula, un símbolo especial y confirmación (RF-M07-01).
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?`~]).*$",
        ErrorMessage = "La contraseña debe contener al menos una letra mayúscula y un símbolo especial.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

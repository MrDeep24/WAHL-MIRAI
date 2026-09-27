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

// NOTA: Esta validación exige 6 caracteres y confirmación, pero no alcanza la regla de complejidad de RF-M07-01.
/// <summary>
/// Datos de entrada para el cambio de contraseña de AuthController; exige longitud mínima de seis
/// caracteres y confirmación, sin validar mayúscula ni símbolo (RF-M07-01).
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

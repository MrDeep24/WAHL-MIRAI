// ============================================================
// Archivo:       RecuperarAccesoViewModel.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define el documento obligatorio enviado en
//                la solicitud de recuperación de acceso y el modelo
//                de restablecimiento de contraseña mediante token.
// Requerimientos: RF-M01-02, RN-2 (complejidad), RN-9, RN-14, RN-15.
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

/// <summary>Datos mínimos de entrada para solicitar recuperación de acceso (RF-M01-02).</summary>
public class RecuperarAccesoViewModel
{
    [Required(ErrorMessage = "El documento es obligatorio.")]
    public string Documento { get; set; } = string.Empty;
}

/// <summary>
/// Datos de entrada para restablecer la contraseña mediante un token efímero de un solo uso (RF-M01-02, RN-2).
/// Reutiliza la misma regla de complejidad de contraseña (RN-2) que auto-registro (RegistroViewModels.cs).
/// </summary>
public class RestablecerViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?`~]).*$",
        ErrorMessage = "La contraseña debe contener al menos una letra mayúscula y un símbolo especial.")]
    [Display(Name = "Nueva contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>Indica si el enlace es inválido o ha expirado para renderizar el estado genérico de enlace inválido.</summary>
    public bool IsInvalidLink { get; set; }
}

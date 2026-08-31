using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

// ── Step 1: document lookup ───────────────────────────────────────────────────

/// <summary>
/// ViewModel for the first step of the self-registration flow (RF-M01-00).
/// The student enters their document number; the system checks census_whitelist.
/// </summary>
public class RegistroStep1ViewModel
{
    [Required(ErrorMessage = "El número de documento es obligatorio.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "El documento debe contener solo dígitos, sin espacios ni guiones.")]
    [Display(Name = "Número de documento")]
    public string Document { get; set; } = string.Empty;
}

// ── Step 2: complete registration ────────────────────────────────────────────

/// <summary>
/// ViewModel for the second step of the self-registration flow (RF-M01-00).
/// full_name and grade are pre-filled from the whitelist entry (read-only).
/// The student sets contact_email, password and confirmation.
/// </summary>
public class RegistroStep2ViewModel
{
    // ── Hidden / pre-filled — passed between steps ────────────────────────────

    /// <summary>Plain-text document number from step 1. Stored as hidden field.</summary>
    [Required]
    public string Document { get; set; } = string.Empty;

    /// <summary>census_whitelist.id confirmed in step 1.</summary>
    public uint WhitelistId { get; set; }

    /// <summary>Pre-filled from whitelist. Displayed read-only; not editable by student.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Grade name pre-filled from whitelist. Displayed read-only.</summary>
    public string GradeName { get; set; } = string.Empty;

    // ── Student-editable fields ───────────────────────────────────────────────

    [Required(ErrorMessage = "El correo de contacto es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede exceder los 150 caracteres.")]
    [Display(Name = "Correo de contacto")]
    public string ContactEmail { get; set; } = string.Empty;

    // Password complexity rule — same regex used in ProfileViewModel (M07)
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?`~]).*$",
        ErrorMessage = "La contraseña debe contener al menos una letra mayúscula y un símbolo especial.")]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

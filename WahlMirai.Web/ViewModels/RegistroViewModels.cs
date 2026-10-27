// ============================================================
// Archivo:       RegistroViewModels.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Modelos de vista y validaciones para los pasos de auto-registro
//                (documento, verificación OTP y creación de cuenta con clave).
// Requerimientos: RF-M01-00, RN-1, RN-1.1, RN-2, RN-2.1, RN-15.
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

// ── Step 1: consulta y validación de documento ────────────────────────────────

/// <summary>
/// Motivo específico de fallo durante la validación del documento en auto-registro (RF-M01-00, RN-1, RN-15).
/// </summary>
public enum RegistroFailureReason
{
    None = 0,
    NotFound,
    AlreadyClaimed,
    NoContactEmail,
    Cooldown,
    SendFailed,
    Locked
}

/// <summary>
/// Modelo para el primer paso del auto-registro (RF-M01-00, RN-1, RN-15).
/// </summary>
public class RegistroStep1ViewModel
{
    [Required(ErrorMessage = "El número de documento es obligatorio.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "El documento debe contener solo dígitos, sin espacios ni guiones.")]
    [Display(Name = "Número de documento")]
    public string Document { get; set; } = string.Empty;

    /// <summary>Motivo estructurado de fallo para guiar al usuario en la vista.</summary>
    public RegistroFailureReason FailureReason { get; set; } = RegistroFailureReason.None;

    /// <summary>Segundos restantes cuando el fallo se debe a cooldown de emisión activo.</summary>
    public int CooldownRemainingSeconds { get; set; }
}

// ── Step 2: verificación de código OTP ────────────────────────────────────────

/// <summary>
/// Modelo para el paso de verificación del código OTP de 6 dígitos (RF-M01-00, D1, RN-15).
/// Presenta el nombre y correo parcialmente enmascarados desde el ticket en memoria.
/// </summary>
public class VerificarCodigoViewModel
{
    [Required(ErrorMessage = "El código de verificación es obligatorio.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código de verificación debe contener exactamente 6 dígitos numéricos.")]
    [Display(Name = "Código de verificación")]
    public string OtpCode { get; set; } = string.Empty;

    /// <summary>Nombre completo parcialmente enmascarado del estudiante (ej. "Ke*** Ló***").</summary>
    public string? MaskedFullName { get; set; }

    /// <summary>Correo electrónico de contacto preautorizado enmascarado (ej. "k***@c***.co").</summary>
    public string? MaskedEmail { get; set; }
}

// ── Step 3: configuración final de cuenta y contraseña ────────────────────────

/// <summary>
/// Modelo para el paso final del auto-registro (RF-M01-00, D1, D2).
/// Solo accesible tras validar exitosamente el OTP. El nombre y grado provienen
/// exclusivamente de la lista blanca (solo lectura). La contraseña se valida con complejidad.
/// </summary>
public class RegistroStep2ViewModel
{
    /// <summary>Nombre completo obtenido de la lista blanca para visualización (solo lectura).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Grado escolar obtenido de la lista blanca para visualización (solo lectura).</summary>
    public string GradeName { get; set; } = string.Empty;

    /// <summary>Contraseña definida por el estudiante cumpliendo requisitos de complejidad.</summary>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?`~]).*$",
        ErrorMessage = "La contraseña debe contener al menos una letra mayúscula y un símbolo especial.")]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Confirmación de contraseña.</summary>
    [Required(ErrorMessage = "Confirme la contraseña.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>Consentimiento obligatorio y aceptación de Términos de Uso y Política de Privacidad (Ley 1581 / Dec 1074).</summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "Debes aceptar los Términos de Uso y la Política de Privacidad.")]
    [Display(Name = "Acepto los Términos de Uso y la Política de Privacidad")]
    public bool AcceptTermsAndPrivacy { get; set; }

    // ── Propiedades obsoletas/forjables ignoradas por el servidor (D2, Anti-tampering) ──

    /// <summary>Obsoleto: Ignorado por el servidor; la identidad se valida por el ticket en cookie.</summary>
    public string? Document { get; set; }

    /// <summary>Obsoleto: Ignorado por el servidor; la entrada se resuelve por el ticket en cookie.</summary>
    public uint WhitelistId { get; set; }

    /// <summary>Obsoleto: Ignorado por el servidor; el correo se toma exclusivamente de census_whitelist (D2).</summary>
    public string? ContactEmail { get; set; }
}

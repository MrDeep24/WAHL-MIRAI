// ============================================================
// Archivo:       AdminAccountViewModels.cs
// Módulo:        M09 — Gestión de Cuentas Administrativas
//                (Exclusivo Súper Administrador)
// Desarrollador: Kevin
// Descripción:   Define los modelos de formulario para crear y editar
//                cuentas administrativas, con validaciones de entrada.
// Requerimientos: RF-M09-01.
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

/// <summary>
/// Datos y validaciones del formulario de creación de una cuenta administrativa (RF-M09-01).
/// </summary>
public class AdminAccountFormViewModel
{
    [Required, StringLength(50)]
    public string Document { get; set; } = string.Empty;

    [Required, MinLength(3), StringLength(150)]
    [RegularExpression(@"^[\p{L}]+(?:[ '\-][\p{L}]+)*$", ErrorMessage = "El nombre solo puede contener letras, espacios, guiones o apóstrofes.")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required, RegularExpression("^(ADMIN|SUPER_ADMIN)$")]
    public string RoleName { get; set; } = "ADMIN";

    [StringLength(100)]
    // NOTA: la ERS (RF-M09-01) no deja claro si PositionTitle debe
    // ser obligatorio; este modelo no lo exige. Aclarar en la ERS
    // o decidir si se agrega validación.
    public string? PositionTitle { get; set; }
}

/// <summary>
/// Datos de edición de una cuenta administrativa, incluidos los campos del formulario base (RF-M09-01).
/// </summary>
public class AdminAccountEditViewModel : AdminAccountFormViewModel
{
    [Required]
    public int Id { get; set; }
}
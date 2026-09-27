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
/// Catálogo cerrado de cargos institucionales descriptivos (RF-M09-01, RN-13).
/// </summary>
public static class AdminAccountPositionTitles
{
    /// <summary>Rector(a), cargo descriptivo admitido por RF-M09-01 y RN-13.</summary>
    public const string Rector = "Rector(a)";

    /// <summary>Coordinador(a) Académico(a), cargo descriptivo admitido por RF-M09-01 y RN-13.</summary>
    public const string AcademicCoordinator = "Coordinador(a) Académico(a)";

    /// <summary>Coordinador(a) de Convivencia, cargo descriptivo admitido por RF-M09-01 y RN-13.</summary>
    public const string ConvivenceCoordinator = "Coordinador(a) de Convivencia";

    /// <summary>Orientador(a) Escolar, cargo descriptivo admitido por RF-M09-01 y RN-13.</summary>
    public const string SchoolCounselor = "Orientador(a) Escolar";

    /// <summary>Secretario(a) Académico(a), cargo descriptivo admitido por RF-M09-01 y RN-13.</summary>
    public const string AcademicSecretary = "Secretario(a) Académico(a)";

    /// <summary>Psicólogo(a) Escolar, cargo descriptivo admitido por RF-M09-01 y RN-13.</summary>
    public const string SchoolPsychologist = "Psicólogo(a) Escolar";

    /// <summary>Docente Líder de Gobierno Escolar, cargo descriptivo admitido por RF-M09-01 y RN-13.</summary>
    public const string StudentGovernmentTeacher = "Docente Líder de Gobierno Escolar";

    /// <summary>Auxiliar Administrativo(a), cargo descriptivo admitido por RF-M09-01 y RN-13.</summary>
    public const string AdministrativeAssistant = "Auxiliar Administrativo(a)";

    /// <summary>Valores disponibles para seleccionar un cargo administrativo (RF-M09-01, RN-13).</summary>
    public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[]
    {
        Rector,
        AcademicCoordinator,
        ConvivenceCoordinator,
        SchoolCounselor,
        AcademicSecretary,
        SchoolPsychologist,
        StudentGovernmentTeacher,
        AdministrativeAssistant
    });
}

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
    [AllowedValues(
        AdminAccountPositionTitles.Rector,
        AdminAccountPositionTitles.AcademicCoordinator,
        AdminAccountPositionTitles.ConvivenceCoordinator,
        AdminAccountPositionTitles.SchoolCounselor,
        AdminAccountPositionTitles.AcademicSecretary,
        AdminAccountPositionTitles.SchoolPsychologist,
        AdminAccountPositionTitles.StudentGovernmentTeacher,
        AdminAccountPositionTitles.AdministrativeAssistant,
        ErrorMessage = "Seleccione un cargo institucional válido de la lista.")]
    /// <summary>Cargo institucional opcional, restringido al catálogo cerrado sin texto libre (RF-M09-01, RN-13).</summary>
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
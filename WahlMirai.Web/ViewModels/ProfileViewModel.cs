// ============================================================
// Archivo:       ProfileViewModel.cs
// Módulo:        M07 — Perfil de Usuario y Autogestión de Credenciales
// Desarrollador: Kevin
// Descripción:   Transporta los datos de consulta del perfil propio para la vista GET Index.
// Requerimientos: RF-M07-01.
// ============================================================

using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

/// <summary>
/// Modelo de vista para presentar los datos del perfil propio en la vista GET Index.
/// </summary>
public class ProfileViewModel
{
    // Read-only fields
    public string FullName { get; set; } = null!;
    
    public string DocumentDisplay { get; set; } = null!;
    
    public string? GradeName { get; set; }
    
    public string Role { get; set; } = null!;
    
    public string? PositionTitle { get; set; }
    
    public string Status { get; set; } = null!;

    [Display(Name = "Correo de Contacto")]
    public string ContactEmail { get; set; } = null!;
}

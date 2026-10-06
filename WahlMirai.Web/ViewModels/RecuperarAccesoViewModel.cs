// ============================================================
// Archivo:       RecuperarAccesoViewModel.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define el documento obligatorio enviado en
//                la solicitud de recuperación de acceso.
// Requerimientos: RF-M01-02 (modelo de entrada).
// ============================================================
using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

/// <summary>Datos mínimos de entrada para solicitar recuperación de acceso (RF-M01-02).</summary>
public class RecuperarAccesoViewModel
{
    [Required(ErrorMessage = "El documento es obligatorio.")]
    public string Documento { get; set; } = string.Empty;
}

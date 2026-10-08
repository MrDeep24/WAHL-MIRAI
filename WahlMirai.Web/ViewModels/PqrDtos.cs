// Archivo:       PqrDtos.cs
// Módulo:        M08 — Ayuda, Tutorial, PQR y Chatbot
// Desarrollador: Kevin
// Descripción:   Define los datos de entrada para radicar solicitudes PQR y
//                responderlas desde la gestión administrativa, con validaciones
//                de campos aplicadas durante el model binding.
// Requerimientos: RF-M08-01 y RF-M08-02.

using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

/// <summary>Datos y reglas de validación para crear una solicitud PQR (RF-M08-01).</summary>
public class PqrCreateDto
{
    [Required(ErrorMessage = "El asunto es obligatorio.")]
    [StringLength(200, ErrorMessage = "El asunto debe tener máximo 200 caracteres.")]
    public string Subject { get; set; } = null!;

    [Required(ErrorMessage = "El mensaje es obligatorio.")]
    public string Message { get; set; } = null!;
}

/// <summary>Datos y reglas de validación para responder una solicitud PQR (RF-M08-02).</summary>
public class PqrResponseDto
{
    [Required(ErrorMessage = "La respuesta del administrador es obligatoria.")]
    public string AdminResponse { get; set; } = null!;
}

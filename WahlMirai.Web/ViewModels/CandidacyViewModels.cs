using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WahlMirai.Web.ViewModels;

/// <summary>
/// ViewModel para el formulario de autopostulación de candidaturas (RF-M04-01).
/// </summary>
public class CandidacyApplyViewModel
{
    [Required]
    public int EventId { get; set; }

    [MaxLength(200)]
    public string? Slogan { get; set; }

    public IFormFile? Photo { get; set; }

    public IFormFile? GovernmentPlan { get; set; }

    public List<string>? Proposals { get; set; } = new();

    /// <summary>
    /// Consentimiento obligatorio para la exhibición pública de fotografía, nombre, grado y propuestas (RF-M04-01).
    /// </summary>
    [Required(ErrorMessage = "Debe autorizar la exhibición pública de sus datos para postularse como candidato.")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Debe autorizar la exhibición pública de sus datos para postularse como candidato.")]
    [Display(Name = "Consentimiento de publicación")]
    public bool AcceptPublicationConsent { get; set; }
}

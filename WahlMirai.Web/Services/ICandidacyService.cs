using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

// ─── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>Datos de una elección abierta en etapa INSCRIPCION disponible para postulación</summary>
public class PostulableEventDto
{
    public uint Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string PositionName { get; set; } = "";
    public DateTime RegistrationEndDateTime { get; set; }
}

/// <summary>Detalle del formulario de postulación: cargo, requisitos, info del evento</summary>
public class PostulationFormDto
{
    public uint EventId { get; set; }
    public string EventTitle { get; set; } = "";
    public string PositionName { get; set; } = "";
    public string? PositionDescription { get; set; }
    public DateTime RegistrationEndDateTime { get; set; }
    public List<PositionRequirement> Requirements { get; set; } = new();
}

/// <summary>Datos enviados por el elector al autopostularse</summary>
public class PostulationSubmitDto
{
    public uint EventId { get; set; }
    public string? Slogan { get; set; }
    public IFormFile? Photo { get; set; }
    public IFormFile? GovernmentPlan { get; set; }
    /// <summary>Propuestas de campaña ordenadas (Index = display_order)</summary>
    public List<string> Proposals { get; set; } = new();
    /// <summary>Documentos subidos; key = requirement_id</summary>
    public Dictionary<uint, IFormFile> Documents { get; set; } = new();
}

/// <summary>Resumen de una candidatura propia visible desde "Mis Candidaturas"</summary>
public class MyPostulationDto
{
    public uint CandidateId { get; set; }
    public uint EventId { get; set; }
    public string EventTitle { get; set; } = "";
    public string PositionName { get; set; } = "";
    public string Status { get; set; } = "";
    public bool ApprovedWithExceptions { get; set; }
    public string? ExceptionsDetail { get; set; }
    public string? RejectionReason { get; set; }
    public bool AllowCorrection { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>Resultado de la operación SubmitPostulation</summary>
public class PostulationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public uint? CandidateId { get; set; }
}

// ─── Interface ─────────────────────────────────────────────────────────────────

public interface ICandidacyService
{
    /// <summary>Elecciones de tipo PERSONAS en etapa INSCRIPCION para el grado del elector, en las que aún no se ha postulado</summary>
    Task<List<PostulableEventDto>> GetEligibleEventsForPostulationAsync(int voterId);

    /// <summary>Detalle del formulario de postulación con el cargo y sus requisitos documentales</summary>
    Task<PostulationFormDto?> GetPostulationFormDetailAsync(int eventId, int voterId);

    /// <summary>Procesa y persiste la autopostulación del elector</summary>
    Task<PostulationResult> SubmitPostulationAsync(PostulationSubmitDto dto, int voterId, string clientIp, string webRootPath);

    /// <summary>Candidaturas previas del elector (historial)</summary>
    Task<List<MyPostulationDto>> GetMyPostulationsAsync(int voterId);
}

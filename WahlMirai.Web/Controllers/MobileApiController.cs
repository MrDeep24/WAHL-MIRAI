using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Web.Controllers;

[ApiController]
[Route("api/mobile")]
[AllowAnonymous]
public class MobileApiController : ControllerBase
{
    private const string TokenPurpose = "WahlMirai.MobileAuth.v1";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(2);

    private readonly WahlMiraiDbContext _context;
    private readonly IAuthService _authService;
    private readonly IVotingService _votingService;
    private readonly IActiveSessionStore _activeSessionStore;
    private readonly IDocumentEncryptionService _docEncryptor;
    private readonly IDataProtector _tokenProtector;
    private readonly ILogger<MobileApiController> _logger;

    public MobileApiController(
        WahlMiraiDbContext context,
        IAuthService authService,
        IVotingService votingService,
        IActiveSessionStore activeSessionStore,
        IDocumentEncryptionService docEncryptor,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<MobileApiController> logger)
    {
        _context = context;
        _authService = authService;
        _votingService = votingService;
        _activeSessionStore = activeSessionStore;
        _docEncryptor = docEncryptor;
        _tokenProtector = dataProtectionProvider.CreateProtector(TokenPurpose);
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            service = "WAHL-MIRAI Mobile API",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Document) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new MobileErrorResponse("Por favor ingrese documento y contraseña.", "INVALID_REQUEST"));
        }

        var document = request.Document.Trim();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var voter = await _authService.ValidateLoginAsync(document, request.Password, ip);
        if (voter == null)
        {
            return Unauthorized(new MobileErrorResponse(
                "Documento o contraseña incorrectos, o cuenta inactiva.",
                "INVALID_CREDENTIALS"));
        }

        var sessionStamp = Guid.NewGuid().ToString();
        _activeSessionStore.RegisterSession(voter.Id, sessionStamp);

        var expiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime);
        var payload = new MobileTokenPayload(voter.Id, sessionStamp, expiresAt.ToUnixTimeSeconds());
        var token = _tokenProtector.Protect(JsonSerializer.Serialize(payload));

        return Ok(new MobileLoginResponse(
            Token: token,
            ExpiresAt: expiresAt.ToString("O"),
            User: MapUserDto(voter, document)
        ));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authResult = await AuthenticateBearerAsync();
        if (authResult.Voter != null)
        {
            _activeSessionStore.InvalidateSession(authResult.Voter.Id, SessionInvalidationReason.LoggedOut);
        }

        return Ok(new { success = true });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var auth = await AuthenticateBearerAsync();
        if (auth.ErrorResult != null) return auth.ErrorResult;

        var voter = auth.Voter!;
        var plainDocument = _docEncryptor.Decrypt(voter.EncryptedDocument);

        return Ok(MapUserDto(voter, plainDocument));
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents()
    {
        var auth = await AuthenticateBearerAsync();
        if (auth.ErrorResult != null) return auth.ErrorResult;

        var voter = auth.Voter!;
        var isAdmin = IsAdminRole(voter.Role?.Name);

        List<VotingEvent> events;
        if (isAdmin)
        {
            events = await _context.VotingEvents
                .Where(ve => ve.Status != "ELIMINADO")
                .OrderByDescending(ve => ve.CreatedAt)
                .ToListAsync();
        }
        else
        {
            events = await _votingService.GetEventsForVoterDashboardAsync((int)voter.Id);
        }

        var result = new List<MobileVotingEventDto>(events.Count);
        foreach (var ve in events)
        {
            result.Add(await BuildVotingEventDtoAsync(ve, (int)voter.Id, isAdmin));
        }

        return Ok(result);
    }

    [HttpGet("events/{id:int}")]
    public async Task<IActionResult> GetEventById(int id)
    {
        var auth = await AuthenticateBearerAsync();
        if (auth.ErrorResult != null) return auth.ErrorResult;

        var voter = auth.Voter!;
        var isAdmin = IsAdminRole(voter.Role?.Name);

        VotingEvent? targetEvent;
        if (isAdmin)
        {
            targetEvent = await _context.VotingEvents
                .FirstOrDefaultAsync(ve => ve.Id == (uint)id && ve.Status != "ELIMINADO");
        }
        else
        {
            var events = await _votingService.GetEventsForVoterDashboardAsync((int)voter.Id);
            targetEvent = events.FirstOrDefault(e => e.Id == (uint)id);
        }

        if (targetEvent == null)
        {
            return NotFound(new MobileErrorResponse(
                "El proceso electoral no está disponible para tu grado o no existe.",
                "EVENT_NOT_FOUND"));
        }

        var dto = await BuildVotingEventDtoAsync(targetEvent, (int)voter.Id, isAdmin);
        return Ok(dto);
    }

    [HttpPost("vote")]
    public async Task<IActionResult> CastVote([FromBody] MobileVoteRequest? request)
    {
        var auth = await AuthenticateBearerAsync();
        if (auth.ErrorResult != null) return auth.ErrorResult;

        var voter = auth.Voter!;
        if (request == null || request.EventId <= 0 || request.CandidateId <= 0)
        {
            return BadRequest(new MobileErrorResponse("Datos de votación inválidos.", "INVALID_REQUEST"));
        }

        if (IsAdminRole(voter.Role?.Name))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new MobileErrorResponse(
                "Las cuentas administrativas no están habilitadas para emitir votos.",
                "ROLE_NOT_ALLOWED"));
        }

        var events = await _votingService.GetEventsForVoterDashboardAsync((int)voter.Id);
        var currentEvent = events.FirstOrDefault(e => e.Id == (uint)request.EventId);
        if (currentEvent == null)
        {
            return NotFound(new MobileErrorResponse(
                "El proceso electoral no está disponible para tu grado o no existe.",
                "EVENT_NOT_FOUND"));
        }

        if (!string.Equals(currentEvent.Status, "ACTIVA", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new MobileErrorResponse(
                "La votación no se encuentra en etapa ACTIVA.",
                "EVENT_NOT_ACTIVE"));
        }

        var candidateExists = await _context.Candidates.AnyAsync(c =>
            c.Id == (uint)request.CandidateId &&
            c.VotingEventId == (uint)request.EventId &&
            c.Status == "APROBADO");

        if (!candidateExists)
        {
            return BadRequest(new MobileErrorResponse(
                "El candidato seleccionado no es válido para esta elección.",
                "INVALID_CANDIDATE"));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var success = await _votingService.CastVoteAsync((int)voter.Id, request.EventId, request.CandidateId, ip);

        if (!success)
        {
            return Conflict(new MobileErrorResponse(
                "Ya has participado en esta elección o no es posible registrar el voto.",
                "ALREADY_VOTED"));
        }

        return Ok(new MobileVoteResponse(
            Success: true,
            Message: "¡Tu voto ha sido registrado exitosamente!"
        ));
    }

    private async Task<MobileVotingEventDto> BuildVotingEventDtoAsync(VotingEvent ve, int voterId, bool isAdmin)
    {
        bool hasVoted = await _votingService.HasVotedAsync(voterId, (int)ve.Id);
        var candidates = await _votingService.GetCandidatesForEventAsync((int)ve.Id);

        // RN-4 / RN-4.1: Mostrar resultados solo si es ADMIN, la elección FINALIZADA, o el elector ya votó en ACTIVA
        bool canSeeCounts = isAdmin
            || string.Equals(ve.Status, "FINALIZADA", StringComparison.OrdinalIgnoreCase)
            || (string.Equals(ve.Status, "ACTIVA", StringComparison.OrdinalIgnoreCase) && hasVoted);

        Dictionary<uint, (long Votes, float Percentage)> voteCountsByCandidate = new();
        if (canSeeCounts)
        {
            var counts = await _context.VwVoteCounts
                .Where(v => v.EventId == ve.Id)
                .ToListAsync();

            long totalEventVotes = counts.Sum(c => c.TotalVotes);
            foreach (var c in counts)
            {
                float pct = totalEventVotes > 0
                    ? (float)c.TotalVotes / totalEventVotes
                    : (float)((c.VotePercentage ?? 0m) / 100m);
                voteCountsByCandidate[c.CandidateId] = (c.TotalVotes, pct);
            }
        }

        var orderedCandidates = candidates
            .OrderBy(c => c.IsBlankVote ? 1 : 0)
            .ThenBy(c => c.Id)
            .ToList();

        int tarjetonCounter = 1;
        var candidateDtos = new List<MobileCandidateDto>(orderedCandidates.Count);
        foreach (var c in orderedCandidates)
        {
            string number = c.IsBlankVote ? "00" : (tarjetonCounter++).ToString("D2");
            string grade = c.IsBlankVote ? "N/A" : (c.Voter?.Grade?.Name ?? "General");

            var proposals = c.CandidateProposals
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Id)
                .Select(p => new MobileProposalDto(p.Id.ToString(), p.Content))
                .ToList();

            voteCountsByCandidate.TryGetValue(c.Id, out var counts);

            candidateDtos.Add(new MobileCandidateDto(
                Id: c.Id.ToString(),
                Name: c.Name,
                Number: number,
                Grade: grade,
                Slogan: c.Slogan ?? (c.IsBlankVote ? "Opción de desacuerdo o abstención consciente" : ""),
                Summary: c.IsBlankVote
                    ? "Voto institucional de neutralidad."
                    : (proposals.FirstOrNullContent() ?? c.Slogan ?? "Candidato aprobado."),
                Proposals: proposals,
                PhotoUrl: c.PhotoUrl,
                PdfUrl: c.GovernmentPlanUrl,
                IsWhiteVote: c.IsBlankVote,
                Status: c.Status,
                Votes: canSeeCounts ? (int)counts.Votes : 0,
                Percentage: canSeeCounts ? counts.Percentage : 0f
            ));
        }

        return new MobileVotingEventDto(
            Id: ve.Id.ToString(),
            Title: ve.Title,
            Description: ve.Description,
            ElectionType: ve.ElectionType,
            Status: ve.Status,
            RegistrationStartDate: ToEpochMillis(ve.RegistrationStartDate, ve.RegistrationStartTime),
            RegistrationEndDate: ToEpochMillis(ve.RegistrationEndDate, ve.RegistrationEndTime),
            ProposalsStartDate: ToEpochMillis(ve.ProposalsStartDate, ve.ProposalsStartTime),
            ProposalsEndDate: ToEpochMillis(ve.ProposalsEndDate, ve.ProposalsEndTime),
            VotingStartDate: ToEpochMillis(ve.VotingStartDate, ve.VotingStartTime),
            VotingEndDate: ToEpochMillis(ve.VotingEndDate, ve.VotingEndTime),
            HasVoted: hasVoted,
            Candidates: candidateDtos
        );
    }

    private async Task<(Voter? Voter, IActionResult? ErrorResult)> AuthenticateBearerAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return (null, Unauthorized(new MobileErrorResponse("Token de autenticación requerido.", "TOKEN_MISSING")));
        }

        var token = authHeader["Bearer ".Length..].Trim();
        MobileTokenPayload? payload;
        try
        {
            var json = _tokenProtector.Unprotect(token);
            payload = JsonSerializer.Deserialize<MobileTokenPayload>(json);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token móvil inválido o manipulado.");
            return (null, Unauthorized(new MobileErrorResponse("Su sesión ha expirado o es inválida.", "TOKEN_INVALID")));
        }

        if (payload == null || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.ExpiresAtUnix)
        {
            return (null, Unauthorized(new MobileErrorResponse("Su sesión ha expirado. Inicie sesión nuevamente.", "SESSION_EXPIRED")));
        }

        var (isValid, reason) = _activeSessionStore.IsSessionValid(payload.VoterId, payload.SessionStamp);
        if (!isValid)
        {
            var message = reason == SessionInvalidationReason.ReplacedByNewDevice
                ? "Tu sesión fue iniciada en otro dispositivo."
                : "Tu sesión ya no es válida. Inicia sesión nuevamente.";
            return (null, Unauthorized(new MobileErrorResponse(message, "SESSION_EXPIRED")));
        }

        var voter = await _context.Voters
            .Include(v => v.Role)
            .Include(v => v.Grade)
            .FirstOrDefaultAsync(v => v.Id == payload.VoterId);

        if (voter == null || voter.Status != "ACTIVO" || voter.DeletedAt != null)
        {
            return (null, Unauthorized(new MobileErrorResponse("La cuenta se encuentra inactiva o eliminada.", "ACCOUNT_DISABLED")));
        }

        return (voter, null);
    }

    private static MobileUserDto MapUserDto(Voter voter, string plainDocument)
    {
        var roleDisplay = voter.Role?.Name switch
        {
            "SUPER_ADMIN" => "Súper Administrador",
            "ADMIN" => "Administrador",
            "ELECTOR" => "Elector",
            _ => voter.Role?.Name ?? "Elector"
        };

        return new MobileUserDto(
            Id: voter.Id,
            Documento: plainDocument,
            Nombre: voter.FullName,
            Grado: voter.Grade?.Name ?? "N/A",
            Rol: roleDisplay,
            Status: voter.Status
        );
    }

    private static bool IsAdminRole(string? roleName) =>
        string.Equals(roleName, "ADMIN", StringComparison.OrdinalIgnoreCase)
        || string.Equals(roleName, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase);

    private static long ToEpochMillis(DateOnly date, TimeOnly time)
    {
        var localDateTime = date.ToDateTime(time, DateTimeKind.Local);
        return new DateTimeOffset(localDateTime).ToUnixTimeMilliseconds();
    }
}

file static class ProposalFormattingExtensions
{
    public static string? FirstOrNullContent(this IEnumerable<MobileProposalDto> proposals) =>
        proposals.FirstOrDefault()?.Text;
}

public record MobileTokenPayload(uint VoterId, string SessionStamp, long ExpiresAtUnix);

public record MobileLoginRequest(string Document, string Password);

public record MobileLoginResponse(string Token, string ExpiresAt, MobileUserDto User);

public record MobileUserDto(
    uint Id,
    string Documento,
    string Nombre,
    string Grado,
    string Rol,
    string Status);

public record MobileProposalDto(string Id, string Text);

public record MobileCandidateDto(
    string Id,
    string Name,
    string Number,
    string Grade,
    string Slogan,
    string Summary,
    List<MobileProposalDto> Proposals,
    string? PhotoUrl,
    string? PdfUrl,
    bool IsWhiteVote,
    string Status,
    int Votes,
    float Percentage);

public record MobileVotingEventDto(
    string Id,
    string Title,
    string? Description,
    string ElectionType,
    string Status,
    long RegistrationStartDate,
    long RegistrationEndDate,
    long ProposalsStartDate,
    long ProposalsEndDate,
    long VotingStartDate,
    long VotingEndDate,
    bool HasVoted,
    List<MobileCandidateDto> Candidates);

public record MobileVoteRequest(int EventId, int CandidateId);

public record MobileVoteResponse(bool Success, string Message);

public record MobileErrorResponse(string Message, string Code);

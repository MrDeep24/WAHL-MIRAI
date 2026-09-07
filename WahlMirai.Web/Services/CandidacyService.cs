using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class CandidacyService : ICandidacyService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;

    // Allowed file extensions and max size (10 MB)
    private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
    private const long MaxFileSize = 10 * 1024 * 1024;

    public CandidacyService(WahlMiraiDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // ─── Eligible Events ──────────────────────────────────────────────────────

    public async Task<List<PostulableEventDto>> GetEligibleEventsForPostulationAsync(int voterId)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.GradeId == null) return [];

        // Fetch all non-deleted events for the voter's grade
        var events = await _context.VotingEvents
            .Include(ve => ve.EventGrades)
            .Include(ve => ve.Position)
            .Where(ve =>
                ve.Status != "ELIMINADO" &&
                ve.ElectionType == "PERSONAS" &&
                ve.EventGrades.Any(eg => eg.GradeId == voter.GradeId))
            .ToListAsync();

        var now = DateTime.Now;
        var result = new List<PostulableEventDto>();

        foreach (var ve in events)
        {
            // Dynamically check if currently in INSCRIPCION window
            var regStart = ve.RegistrationStartDate.ToDateTime(ve.RegistrationStartTime);
            var regEnd = ve.RegistrationEndDate.ToDateTime(ve.RegistrationEndTime);
            if (now < regStart || now >= regEnd) continue;

            // Skip if already enrolled AND not correctable
            var existingCandidacy = await _context.Candidates
                .FirstOrDefaultAsync(c => c.VotingEventId == ve.Id && c.VoterId == (uint)voterId);
            if (existingCandidacy != null)
            {
                // Only allow re-display if previously rejected and correction is allowed
                if (existingCandidacy.Status != "RECHAZADO" || !existingCandidacy.AllowCorrection)
                    continue;
            }

            result.Add(new PostulableEventDto
            {
                Id = ve.Id,
                Title = ve.Title,
                Description = ve.Description,
                PositionName = ve.Position.Name,
                RegistrationEndDateTime = regEnd
            });
        }

        return result;
    }

    // ─── Form Detail ──────────────────────────────────────────────────────────

    public async Task<PostulationFormDto?> GetPostulationFormDetailAsync(int eventId, int voterId)
    {
        var ve = await _context.VotingEvents
            .Include(v => v.Position)
                .ThenInclude(p => p.PositionRequirements.OrderBy(r => r.DisplayOrder))
            .FirstOrDefaultAsync(v => v.Id == (uint)eventId && v.Status != "ELIMINADO");

        if (ve == null) return null;

        // Validate still in INSCRIPCION window
        var regStart = ve.RegistrationStartDate.ToDateTime(ve.RegistrationStartTime);
        var regEnd = ve.RegistrationEndDate.ToDateTime(ve.RegistrationEndTime);
        var now = DateTime.Now;
        if (now < regStart || now >= regEnd) return null;

        return new PostulationFormDto
        {
            EventId = ve.Id,
            EventTitle = ve.Title,
            PositionName = ve.Position.Name,
            PositionDescription = ve.Position.Description,
            RegistrationEndDateTime = regEnd,
            Requirements = ve.Position.PositionRequirements.ToList()
        };
    }

    // ─── Submit Postulation ───────────────────────────────────────────────────

    public async Task<PostulationResult> SubmitPostulationAsync(
        PostulationSubmitDto dto, int voterId, string clientIp, string webRootPath)
    {
        // 1. Load event and validate INSCRIPCION window
        var ve = await _context.VotingEvents
            .Include(v => v.Position)
                .ThenInclude(p => p.PositionRequirements)
            .FirstOrDefaultAsync(v => v.Id == dto.EventId && v.Status != "ELIMINADO");

        if (ve == null)
            return Fail("El proceso electoral no existe.");

        var now = DateTime.Now;
        var regStart = ve.RegistrationStartDate.ToDateTime(ve.RegistrationStartTime);
        var regEnd = ve.RegistrationEndDate.ToDateTime(ve.RegistrationEndTime);
        if (now < regStart || now >= regEnd)
            return Fail("El período de inscripción de candidatos no está activo.");

        // 2. Check for existing enrolment — allow correction path
        var existingCandidate = await _context.Candidates
            .Include(c => c.CandidateProposals)
            .Include(c => c.CandidacyDocuments)
            .FirstOrDefaultAsync(c => c.VotingEventId == dto.EventId && c.VoterId == (uint)voterId);

        bool isCorrection = false;
        if (existingCandidate != null)
        {
            if (existingCandidate.Status == "RECHAZADO" && existingCandidate.AllowCorrection)
                isCorrection = true;
            else
                return Fail("Ya tienes una postulación registrada en esta elección.");
        }

        // 3. Validate mandatory documents are present
        var mandatoryReqs = ve.Position.PositionRequirements.Where(r => r.IsMandatory).ToList();
        foreach (var req in mandatoryReqs)
        {
            if (!dto.Documents.ContainsKey(req.Id) || dto.Documents[req.Id] == null)
                return Fail($"El documento obligatorio «{req.Description}» es requerido.");
        }

        // 4. Validate file types and sizes
        var allFiles = new List<IFormFile>();
        if (dto.Photo != null) allFiles.Add(dto.Photo);
        if (dto.GovernmentPlan != null) allFiles.Add(dto.GovernmentPlan);
        allFiles.AddRange(dto.Documents.Values.Where(f => f != null));

        foreach (var file in allFiles)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return Fail($"El archivo «{file.FileName}» tiene un formato no permitido. Use PDF, PNG, JPG o JPEG.");
            if (file.Length > MaxFileSize)
                return Fail($"El archivo «{file.FileName}» supera el límite de 10 MB.");
        }

        // 5. Persist files and build candidate
        var uploadBase = Path.Combine(webRootPath, "uploads", "candidacies");
        Directory.CreateDirectory(uploadBase);

        string? photoUrl = null;
        if (dto.Photo != null)
            photoUrl = await SaveFileAsync(dto.Photo, uploadBase, "photo");

        string? govPlanUrl = null;
        if (dto.GovernmentPlan != null)
            govPlanUrl = await SaveFileAsync(dto.GovernmentPlan, uploadBase, "plan");

        // 6. Get voter full name for the candidate Name field
        var voter = await _context.Voters.FindAsync((uint)voterId);
        var candidateName = voter != null
            ? voter.FullName.Trim()
            : $"Candidato #{voterId}";

        // 7. Create or update candidate
        Candidate candidate;
        if (isCorrection && existingCandidate != null)
        {
            // Reset the existing record
            candidate = existingCandidate;
            candidate.Slogan = dto.Slogan?.Trim();
            if (photoUrl != null) candidate.PhotoUrl = photoUrl;
            if (govPlanUrl != null) candidate.GovernmentPlanUrl = govPlanUrl;
            candidate.Status = "PENDIENTE";
            candidate.RejectionReason = null;
            candidate.AllowCorrection = false;
            candidate.ApprovedWithExceptions = false;
            candidate.ExceptionsDetail = null;
            candidate.ReviewedByUserId = null;
            candidate.ReviewedAt = null;
            candidate.EnrolledAt = now;

            // Remove old proposals and documents to replace them
            _context.CandidateProposals.RemoveRange(candidate.CandidateProposals);
            _context.CandidacyDocuments.RemoveRange(candidate.CandidacyDocuments);
        }
        else
        {
            candidate = new Candidate
            {
                VotingEventId = dto.EventId,
                VoterId = (uint)voterId,
                Name = candidateName,
                Slogan = dto.Slogan?.Trim(),
                PhotoUrl = photoUrl,
                GovernmentPlanUrl = govPlanUrl,
                IsBlankVote = false,
                Status = "PENDIENTE",
                EnrolledAt = now
            };
            _context.Candidates.Add(candidate);
        }
        await _context.SaveChangesAsync();

        // 8. Save proposals preserving order
        byte order = 1;
        foreach (var content in dto.Proposals.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            _context.CandidateProposals.Add(new CandidateProposal
            {
                CandidateId = candidate.Id,
                Content = content.Trim(),
                DisplayOrder = order++
            });
        }

        // 9. Save candidacy documents
        foreach (var (reqId, file) in dto.Documents)
        {
            if (file == null) continue;
            var fileUrl = await SaveFileAsync(file, uploadBase, $"doc_{reqId}");
            _context.CandidacyDocuments.Add(new CandidacyDocument
            {
                CandidateId = candidate.Id,
                RequirementId = reqId,
                FileUrl = fileUrl,
                UploadedAt = now
            });
        }

        await _context.SaveChangesAsync();

        // 10. Audit
        await _auditService.LogAsync(
            "CANDIDACY_SUBMITTED", voterId, "candidates", (int)candidate.Id,
            null, null, "PENDIENTE",
            $"Evento: {ve.Title} | Cargo: {ve.Position.Name}", clientIp);

        return new PostulationResult { Success = true, CandidateId = candidate.Id };
    }

    // ─── My Postulations ──────────────────────────────────────────────────────

    public async Task<List<MyPostulationDto>> GetMyPostulationsAsync(int voterId)
    {
        return await _context.Candidates
            .Include(c => c.VotingEvent).ThenInclude(ve => ve.Position)
            .Where(c => c.VoterId == (uint)voterId && !c.IsBlankVote)
            .OrderByDescending(c => c.EnrolledAt)
            .Select(c => new MyPostulationDto
            {
                CandidateId = c.Id,
                EventId = c.VotingEventId,
                EventTitle = c.VotingEvent.Title,
                PositionName = c.VotingEvent.Position.Name,
                Status = c.Status,
                ApprovedWithExceptions = c.ApprovedWithExceptions,
                ExceptionsDetail = c.ExceptionsDetail,
                RejectionReason = c.RejectionReason,
                AllowCorrection = c.AllowCorrection,
                EnrolledAt = c.EnrolledAt,
                ReviewedAt = c.ReviewedAt
            })
            .ToListAsync();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static async Task<string> SaveFileAsync(IFormFile file, string uploadBase, string prefix)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{prefix}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadBase, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/candidacies/{fileName}";
    }

    private static PostulationResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

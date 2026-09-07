using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class CandidateReviewService : ICandidateReviewService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;

    public CandidateReviewService(WahlMiraiDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<CandidateReviewDetailDto>> GetCandidatesForReviewAsync(uint? eventId, string? status)
    {
        var query = _context.Candidates
            .Include(c => c.VotingEvent)
            .Include(c => c.Voter).ThenInclude(v => v!.Grade)
            .Include(c => c.CandidacyDocuments)
            .Include(c => c.ReviewedByVoter)
            .Where(c => !c.IsBlankVote) // Excluir voto en blanco de revisión
            .AsQueryable();

        if (eventId.HasValue && eventId.Value > 0)
        {
            query = query.Where(c => c.VotingEventId == eventId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        var candidates = await query.OrderByDescending(c => c.EnrolledAt).ToListAsync();

        // Obtener todos los requisitos por evento (usando PositionId o id de evento)
        var positionIds = candidates.Select(c => c.VotingEvent.PositionId).Distinct().ToList();
        var requirements = await _context.PositionRequirements
            .Where(r => positionIds.Contains(r.PositionId))
            .ToListAsync();

        var result = new List<CandidateReviewDetailDto>();
        foreach (var c in candidates)
        {
            result.Add(BuildCandidateDto(c, requirements.Where(r => r.PositionId == c.VotingEvent.PositionId).ToList()));
        }

        return result;
    }

    public async Task<CandidateReviewDetailDto?> GetCandidateReviewDetailAsync(uint candidateId)
    {
        var c = await _context.Candidates
            .Include(c => c.VotingEvent)
            .Include(c => c.Voter).ThenInclude(v => v!.Grade)
            .Include(c => c.CandidacyDocuments)
            .Include(c => c.ReviewedByVoter)
            .FirstOrDefaultAsync(cand => cand.Id == candidateId && !cand.IsBlankVote);

        if (c == null) return null;

        var requirements = await _context.PositionRequirements
            .Where(r => r.PositionId == c.VotingEvent.PositionId)
            .ToListAsync();

        return BuildCandidateDto(c, requirements);
    }

    public async Task ApproveCandidateAsync(uint candidateId, bool withExceptions, string? exceptionsDetail, uint adminUserId, string clientIp)
    {
        var candidate = await _context.Candidates
            .Include(c => c.Voter)
            .Include(c => c.VotingEvent)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate == null)
            throw new InvalidOperationException("Candidato no encontrado.");

        if (withExceptions && string.IsNullOrWhiteSpace(exceptionsDetail))
            throw new ArgumentException("Al aprobar con excepción, se debe ingresar el detalle justificado.");

        var oldStatus = candidate.Status;

        candidate.Status = "APROBADO";
        candidate.ApprovedWithExceptions = withExceptions;
        candidate.ExceptionsDetail = withExceptions ? exceptionsDetail : null;
        candidate.RejectionReason = null;
        candidate.ReviewedByUserId = adminUserId;
        candidate.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Registrar auditoría
        await _auditService.LogAsync(
            "CANDIDACY_APPROVED",
            (int)adminUserId,
            "candidates",
            (int)candidate.Id,
            "status",
            oldStatus,
            "APROBADO",
            withExceptions ? $"Aprobado con excepción: {exceptionsDetail}" : "Aprobación regular sin excepciones",
            clientIp
        );

        // Encolar correo si el candidato está asociado a un elector con correo
        if (candidate.VoterId.HasValue && candidate.Voter != null && !string.IsNullOrEmpty(candidate.Voter.ContactEmail))
        {
            var emailLog = new EmailQueue
            {
                VoterId = candidate.VoterId.Value,
                EmailType = "CANDIDATURA_APROBADA",
                Status = "PENDIENTE",
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };
            _context.EmailQueues.Add(emailLog);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RejectCandidateAsync(uint candidateId, string rejectionReason, bool allowCorrection, uint adminUserId, string clientIp)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Debe proporcionar un motivo obligatorio para el rechazo de la candidatura (RN-10).");

        var candidate = await _context.Candidates
            .Include(c => c.Voter)
            .Include(c => c.VotingEvent)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate == null)
            throw new InvalidOperationException("Candidato no encontrado.");

        var oldStatus = candidate.Status;

        candidate.Status = "RECHAZADO";
        candidate.ApprovedWithExceptions = false;
        candidate.ExceptionsDetail = null;
        candidate.RejectionReason = rejectionReason;
        candidate.AllowCorrection = allowCorrection;
        candidate.ReviewedByUserId = adminUserId;
        candidate.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Registrar auditoría
        await _auditService.LogAsync(
            "CANDIDACY_REJECTED",
            (int)adminUserId,
            "candidates",
            (int)candidate.Id,
            "status",
            oldStatus,
            "RECHAZADO",
            $"Motivo rechazo: {rejectionReason}",
            clientIp
        );

        // Encolar correo si el candidato tiene elector asociado
        if (candidate.VoterId.HasValue && candidate.Voter != null && !string.IsNullOrEmpty(candidate.Voter.ContactEmail))
        {
            var emailLog = new EmailQueue
            {
                VoterId = candidate.VoterId.Value,
                EmailType = "CANDIDATURA_RECHAZADA",
                Status = "PENDIENTE",
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };
            _context.EmailQueues.Add(emailLog);
            await _context.SaveChangesAsync();
        }
    }

    public async Task WithdrawCandidateAsync(uint candidateId, string reason, uint adminUserId, string clientIp)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Debe proporcionar un motivo para el retiro de la candidatura.");

        var candidate = await _context.Candidates
            .Include(c => c.Voter)
            .Include(c => c.VotingEvent)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate == null)
            throw new InvalidOperationException("Candidato no encontrado.");
            
        if (candidate.Status != "APROBADO")
            throw new InvalidOperationException("Solo se pueden retirar candidaturas previamente aprobadas.");

        var oldStatus = candidate.Status;

        candidate.Status = "RETIRADO";
        candidate.ApprovedWithExceptions = false;
        candidate.ExceptionsDetail = null;
        candidate.RejectionReason = reason;
        candidate.AllowCorrection = false; // El retiro es definitivo
        candidate.ReviewedByUserId = adminUserId;
        candidate.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Registrar auditoría
        await _auditService.LogAsync(
            "CANDIDACY_WITHDRAWN",
            (int)adminUserId,
            "candidates",
            (int)candidate.Id,
            "status",
            oldStatus,
            "RETIRADO",
            $"Motivo retiro: {reason}",
            clientIp
        );
    }

    private static CandidateReviewDetailDto BuildCandidateDto(Candidate c, List<PositionRequirement> requirements)
    {
        var reqChecks = new List<RequirementCheckDto>();
        int mandatoryCount = 0;
        int mandatoryUploaded = 0;

        foreach (var req in requirements.OrderBy(r => r.DisplayOrder))
        {
            var doc = c.CandidacyDocuments.FirstOrDefault(cd => cd.RequirementId == req.Id);
            bool isUploaded = doc != null;

            if (req.IsMandatory)
            {
                mandatoryCount++;
                if (isUploaded) mandatoryUploaded++;
            }

            reqChecks.Add(new RequirementCheckDto
            {
                RequirementId = req.Id,
                Description = req.Description,
                IsMandatory = req.IsMandatory,
                IsUploaded = isUploaded,
                FileUrl = doc?.FileUrl,
                UploadedAt = doc?.UploadedAt
            });
        }

        bool isEligible = mandatoryCount == 0 || mandatoryUploaded >= mandatoryCount;

        return new CandidateReviewDetailDto
        {
            CandidateId = c.Id,
            EventId = c.VotingEventId,
            EventTitle = c.VotingEvent.Title,
            CandidateName = c.Name,
            CandidateEmail = c.Voter?.ContactEmail,
            GradeName = c.Voter?.Grade?.Name ?? "N/A",
            Slogan = c.Slogan,
            PhotoUrl = c.PhotoUrl,
            GovernmentPlanUrl = c.GovernmentPlanUrl,
            Status = c.Status,
            IsBlankVote = c.IsBlankVote,
            ApprovedWithExceptions = c.ApprovedWithExceptions,
            ExceptionsDetail = c.ExceptionsDetail,
            RejectionReason = c.RejectionReason,
            AllowCorrection = c.AllowCorrection,
            EnrolledAt = c.EnrolledAt,
            ReviewedByName = c.ReviewedByVoter?.FullName,
            ReviewedAt = c.ReviewedAt,
            IsEligible = isEligible,
            MandatoryCount = mandatoryCount,
            MandatoryUploadedCount = mandatoryUploaded,
            Requirements = reqChecks
        };
    }
}

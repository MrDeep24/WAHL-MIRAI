using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class RequirementCheckDto
{
    public uint RequirementId { get; set; }
    public string Description { get; set; } = null!;
    public bool IsMandatory { get; set; }
    public bool IsUploaded { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? UploadedAt { get; set; }
}

public class CandidateReviewDetailDto
{
    public uint CandidateId { get; set; }
    public uint EventId { get; set; }
    public string EventTitle { get; set; } = null!;
    public string CandidateName { get; set; } = null!;
    public string? CandidateEmail { get; set; }
    public string GradeName { get; set; } = null!;
    public string? Slogan { get; set; }
    public string? PhotoUrl { get; set; }
    public string? GovernmentPlanUrl { get; set; }
    public string Status { get; set; } = null!;
    public bool IsBlankVote { get; set; }
    public bool ApprovedWithExceptions { get; set; }
    public string? ExceptionsDetail { get; set; }
    public string? RejectionReason { get; set; }
    public bool AllowCorrection { get; set; }
    public DateTime EnrolledAt { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool IsEligible { get; set; }
    public int MandatoryCount { get; set; }
    public int MandatoryUploadedCount { get; set; }
    public List<RequirementCheckDto> Requirements { get; set; } = new();
}

public interface ICandidateReviewService
{
    Task<List<CandidateReviewDetailDto>> GetCandidatesForReviewAsync(uint? eventId, string? status);
    Task<CandidateReviewDetailDto?> GetCandidateReviewDetailAsync(uint candidateId);
    Task ApproveCandidateAsync(uint candidateId, bool withExceptions, string? exceptionsDetail, uint adminUserId, string clientIp);
    Task RejectCandidateAsync(uint candidateId, string rejectionReason, bool allowCorrection, uint adminUserId, string clientIp);
    Task WithdrawCandidateAsync(uint candidateId, string reason, uint adminUserId, string clientIp);
}

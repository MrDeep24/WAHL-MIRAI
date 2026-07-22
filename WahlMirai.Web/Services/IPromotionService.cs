using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public interface IPromotionService
{
    Task<PromotionPreview> GetPromotionPreviewAsync();
    Task<bool> RunPromotionAsync(bool force, string adminIp);
}

public class PromotionPreview
{
    public bool HasRunThisYear { get; set; }
    public int EligibleCount { get; set; }
    public int ExcludedCount { get; set; }
    public int ToGraduateCount { get; set; }
}

public class PromotionService : IPromotionService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;

    public PromotionService(WahlMiraiDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PromotionPreview> GetPromotionPreviewAsync()
    {
        var currentYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsCurrent);

        var voters = await _context.Voters
            .Include(v => v.Grade)
            .Where(v => v.Status == "ACTIVO" && v.GradeId != null)
            .ToListAsync();

        var eligible  = voters.Where(v => !v.ExcluirDePromocion).ToList();
        var excluded  = voters.Where(v => v.ExcluirDePromocion).ToList();
        var toGraduate = eligible.Where(v => v.Grade!.IsLastGrade).ToList();

        return new PromotionPreview
        {
            HasRunThisYear = currentYear?.PromotionExecutedAt != null,
            EligibleCount  = eligible.Count,
            ExcludedCount  = excluded.Count,
            ToGraduateCount = toGraduate.Count
        };
    }

    public async Task<bool> RunPromotionAsync(bool force, string adminIp)
    {
        var currentYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsCurrent);
        if (currentYear == null) return false;

        if (currentYear.PromotionExecutedAt != null && !force)
            return false; // Already run, require force

        var allGrades = await _context.Grades.OrderBy(g => g.SequenceOrder).ToListAsync();

        var eligibleVoters = await _context.Voters
            .Include(v => v.Grade)
            .Where(v => v.Status == "ACTIVO" && v.GradeId != null && !v.ExcluirDePromocion)
            .ToListAsync();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            int promotedCount = 0;
            int graduatedCount = 0;

            foreach (var voter in eligibleVoters)
            {
                if (voter.Grade!.IsLastGrade)
                {
                    voter.Status = "EGRESADO";
                    graduatedCount++;
                }
                else
                {
                    var nextGrade = allGrades.FirstOrDefault(g => g.SequenceOrder > voter.Grade.SequenceOrder);
                    if (nextGrade != null)
                    {
                        voter.GradeId = nextGrade.Id;
                        promotedCount++;
                    }
                }
                voter.ExcluirDePromocion = false;
            }

            // Reset exclude flag for excluded voters too
            var excludedVoters = await _context.Voters
                .Where(v => v.Status == "ACTIVO" && v.GradeId != null && v.ExcluirDePromocion)
                .ToListAsync();
            foreach (var excluded in excludedVoters)
                excluded.ExcluirDePromocion = false;

            currentYear.PromotionExecutedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogAsync("PROMOTION_RUN", null, "academic_years", (int)currentYear.Id, null, null, null,
                $"Promoted: {promotedCount}, Graduated: {graduatedCount}, Force: {force}", adminIp);

            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }
}

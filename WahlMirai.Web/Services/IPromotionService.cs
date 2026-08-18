using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class VoterPromotionDetail
{
    public uint VoterId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CurrentGrade { get; set; } = string.Empty;
    public string TargetGrade { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty; // PROMOVIDO, REPITENTE, EGRESADO
}

public class PromotionPreview
{
    public ushort CurrentYear { get; set; }
    public bool HasRunThisYear { get; set; }
    public DateTime? PromotionExecutedAt { get; set; }
    public int EligibleCount { get; set; }
    public int ExcludedCount { get; set; }
    public int ToGraduateCount { get; set; }
    public List<VoterPromotionDetail> PreviewList { get; set; } = new();
}

public class PromotionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PromotedCount { get; set; }
    public int GraduatedCount { get; set; }
    public int RetainedCount { get; set; }
    public int ErrorCount { get; set; }
}

public interface IPromotionService
{
    Task<PromotionPreview> GetPromotionPreviewAsync();
    Task<PromotionResult> RunPromotionAsync(bool force, string adminIp);
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
        var currentYearObj = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsCurrent);
        ushort yearNum = currentYearObj?.Year ?? (ushort)DateTime.UtcNow.Year;

        var allGrades = await _context.Grades.OrderBy(g => g.SequenceOrder).ToListAsync();

        var voters = await _context.Voters
            .Include(v => v.Grade)
            .Where(v => v.Status == "ACTIVO" && v.GradeId != null)
            .ToListAsync();

        var eligible   = voters.Where(v => !v.ExcluirDePromocion).ToList();
        var excluded   = voters.Where(v => v.ExcluirDePromocion).ToList();
        var toGraduate = eligible.Where(v => v.Grade!.IsLastGrade).ToList();

        var previewList = new List<VoterPromotionDetail>();

        foreach (var v in voters)
        {
            var detail = new VoterPromotionDetail
            {
                VoterId = v.Id,
                FullName = v.FullName,
                CurrentGrade = v.Grade?.Name ?? "N/A"
            };

            if (v.ExcluirDePromocion)
            {
                detail.TargetGrade = v.Grade?.Name ?? "N/A";
                detail.Outcome = "REPITENTE (Permanece en el grado)";
            }
            else if (v.Grade != null && v.Grade.IsLastGrade)
            {
                detail.TargetGrade = "EGRESADO";
                detail.Outcome = "EGRESADO (Graduación de 11°)";
            }
            else if (v.Grade != null)
            {
                var nextGrade = allGrades.FirstOrDefault(g => g.SequenceOrder > v.Grade.SequenceOrder);
                detail.TargetGrade = nextGrade?.Name ?? "N/A";
                detail.Outcome = $"PROMOVIDO a {detail.TargetGrade}";
            }

            previewList.Add(detail);
        }

        return new PromotionPreview
        {
            CurrentYear = yearNum,
            HasRunThisYear = currentYearObj?.PromotionExecutedAt != null,
            PromotionExecutedAt = currentYearObj?.PromotionExecutedAt,
            EligibleCount  = eligible.Count,
            ExcludedCount  = excluded.Count,
            ToGraduateCount = toGraduate.Count,
            PreviewList = previewList
        };
    }

    public async Task<PromotionResult> RunPromotionAsync(bool force, string adminIp)
    {
        var currentYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsCurrent);
        if (currentYear == null)
        {
            return new PromotionResult
            {
                Success = false,
                Message = "No existe un año lectivo activo configurado en el sistema."
            };
        }

        if (currentYear.PromotionExecutedAt != null && !force)
        {
            return new PromotionResult
            {
                Success = false,
                Message = $"La promoción del año lectivo {currentYear.Year} ya fue ejecutada el {currentYear.PromotionExecutedAt.Value.ToString("dd/MM/yyyy HH:mm")}. Debe forzar la operación si desea ejecutarla nuevamente."
            };
        }

        var allGrades = await _context.Grades.OrderBy(g => g.SequenceOrder).ToListAsync();

        var activeVoters = await _context.Voters
            .Include(v => v.Grade)
            .Where(v => v.Status == "ACTIVO" && v.GradeId != null)
            .ToListAsync();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            int promotedCount = 0;
            int graduatedCount = 0;
            int retainedCount = 0;

            foreach (var voter in activeVoters)
            {
                if (voter.ExcluirDePromocion)
                {
                    retainedCount++;
                    voter.ExcluirDePromocion = false; // Se reinicia bandera para el nuevo ciclo
                    continue;
                }

                if (voter.Grade!.IsLastGrade)
                {
                    voter.Status = "EGRESADO";
                    voter.GradeId = null;
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
                voter.UpdatedAt = DateTime.UtcNow;
            }

            currentYear.PromotionExecutedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var auditDetails = JsonSerializer.Serialize(new {
                Year = currentYear.Year,
                Promoted = promotedCount,
                Graduated = graduatedCount,
                Retained = retainedCount,
                Forced = force
            });

            await _auditService.LogAsync("PROMOTION_RUN", null, "academic_years", (int)currentYear.Id, null, null, null,
                auditDetails, adminIp);

            return new PromotionResult
            {
                Success = true,
                Message = $"Promoción ejecutada con éxito para el año lectivo {currentYear.Year}.",
                PromotedCount = promotedCount,
                GraduatedCount = graduatedCount,
                RetainedCount = retainedCount,
                ErrorCount = 0
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new PromotionResult
            {
                Success = false,
                Message = $"Error al ejecutar el proceso de promoción: {ex.Message}",
                ErrorCount = 1
            };
        }
    }
}


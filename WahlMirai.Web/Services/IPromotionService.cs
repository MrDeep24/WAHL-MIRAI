using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class VoterPromotionDetail
{
    public uint Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CurrentGrade { get; set; } = string.Empty;
    public string TargetGrade { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty; // PROMOVIDO, REPITENTE, EGRESADO
    public string SourceType { get; set; } = "ACTIVO"; // ACTIVO, LISTA_BLANCA
}

public class PromotionPreview
{
    public ushort CurrentYear { get; set; }
    public bool HasRunThisYear { get; set; }
    public DateTime? PromotionExecutedAt { get; set; }
    public int EligibleCount { get; set; }
    public int ExcludedCount { get; set; }
    public int ToGraduateCount { get; set; }
    public int ActiveVotersCount { get; set; }
    public int PendingWhitelistCount { get; set; }
    public List<VoterPromotionDetail> PreviewList { get; set; } = new();
}

public class PromotionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PromotedCount { get; set; }
    public int GraduatedCount { get; set; }
    public int RetainedCount { get; set; }
    public int ActivePromotedCount { get; set; }
    public int WhitelistPromotedCount { get; set; }
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

        var activeVoters = await _context.Voters
            .Include(v => v.Grade)
            .Where(v => v.Status == "ACTIVO" && v.GradeId != null)
            .ToListAsync();

        var pendingWhitelist = await _context.CensusWhitelists
            .Include(w => w.Grade)
            .Where(w => w.ClaimedAt == null)
            .ToListAsync();

        var previewList = new List<VoterPromotionDetail>();

        int eligibleCount = 0;
        int excludedCount = 0;
        int toGraduateCount = 0;

        // 1. Procesar usuarios activos
        foreach (var v in activeVoters)
        {
            var detail = new VoterPromotionDetail
            {
                Id = v.Id,
                FullName = v.FullName,
                CurrentGrade = v.Grade?.Name ?? "N/A",
                SourceType = "ACTIVO"
            };

            if (v.ExcluirDePromocion)
            {
                detail.TargetGrade = v.Grade?.Name ?? "N/A";
                detail.Outcome = "REPITENTE (Permanece en el grado)";
                excludedCount++;
            }
            else if (v.Grade != null && v.Grade.IsLastGrade)
            {
                detail.TargetGrade = "EGRESADO";
                detail.Outcome = "EGRESADO (Graduación de 11°)";
                toGraduateCount++;
            }
            else if (v.Grade != null)
            {
                var nextGrade = allGrades.FirstOrDefault(g => g.SequenceOrder > v.Grade.SequenceOrder);
                detail.TargetGrade = nextGrade?.Name ?? "N/A";
                detail.Outcome = $"PROMOVIDO a {detail.TargetGrade}";
                eligibleCount++;
            }

            previewList.Add(detail);
        }

        // 2. Procesar entradas de lista blanca pendientes (no reclamadas)
        foreach (var w in pendingWhitelist)
        {
            var detail = new VoterPromotionDetail
            {
                Id = w.Id,
                FullName = $"{w.FullName} (Pendiente)",
                CurrentGrade = w.Grade?.Name ?? "N/A",
                SourceType = "LISTA_BLANCA"
            };

            if (w.ExcluirDePromocion)
            {
                detail.TargetGrade = w.Grade?.Name ?? "N/A";
                detail.Outcome = "REPITENTE (Permanece en el grado)";
                excludedCount++;
            }
            else if (w.Grade != null && w.Grade.IsLastGrade)
            {
                detail.TargetGrade = "EGRESADO (Excluido)";
                detail.Outcome = "EGRESADO (Último grado alcanzado)";
                toGraduateCount++;
            }
            else if (w.Grade != null)
            {
                var nextGrade = allGrades.FirstOrDefault(g => g.SequenceOrder > w.Grade.SequenceOrder);
                detail.TargetGrade = nextGrade?.Name ?? "N/A";
                detail.Outcome = $"PROMOVIDO a {detail.TargetGrade}";
                eligibleCount++;
            }

            previewList.Add(detail);
        }

        return new PromotionPreview
        {
            CurrentYear = yearNum,
            HasRunThisYear = currentYearObj?.PromotionExecutedAt != null,
            PromotionExecutedAt = currentYearObj?.PromotionExecutedAt,
            EligibleCount = eligibleCount,
            ExcludedCount = excludedCount,
            ToGraduateCount = toGraduateCount,
            ActiveVotersCount = activeVoters.Count,
            PendingWhitelistCount = pendingWhitelist.Count,
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

        var pendingWhitelist = await _context.CensusWhitelists
            .Include(w => w.Grade)
            .Where(w => w.ClaimedAt == null)
            .ToListAsync();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            int totalPromoted = 0;
            int totalGraduated = 0;
            int totalRetained = 0;

            int activePromoted = 0;
            int activeGraduated = 0;
            int activeRetained = 0;

            int whitelistPromoted = 0;
            int whitelistGraduated = 0;
            int whitelistRetained = 0;

            // 1. Promover electores activos en tabla users
            foreach (var voter in activeVoters)
            {
                if (voter.ExcluirDePromocion)
                {
                    totalRetained++;
                    activeRetained++;
                    voter.ExcluirDePromocion = false; // Se reinicia bandera para el nuevo ciclo
                }
                else if (voter.Grade!.IsLastGrade)
                {
                    voter.Status = "EGRESADO";
                    voter.GradeId = null;
                    totalGraduated++;
                    activeGraduated++;
                    voter.ExcluirDePromocion = false;
                }
                else
                {
                    var nextGrade = allGrades.FirstOrDefault(g => g.SequenceOrder > voter.Grade.SequenceOrder);
                    if (nextGrade != null)
                    {
                        voter.GradeId = nextGrade.Id;
                        totalPromoted++;
                        activePromoted++;
                    }
                    voter.ExcluirDePromocion = false;
                }

                voter.UpdatedAt = DateTime.UtcNow;
            }

            // 2. Promover entradas de lista blanca pendientes (claimed_at IS NULL)
            foreach (var entry in pendingWhitelist)
            {
                if (entry.ExcluirDePromocion)
                {
                    totalRetained++;
                    whitelistRetained++;
                    entry.ExcluirDePromocion = false;
                }
                else if (entry.Grade.IsLastGrade)
                {
                    // Al egresar de la lista blanca, se excluye de futuras promociones
                    entry.ExcluirDePromocion = true;
                    totalGraduated++;
                    whitelistGraduated++;
                }
                else
                {
                    var nextGrade = allGrades.FirstOrDefault(g => g.SequenceOrder > entry.Grade.SequenceOrder);
                    if (nextGrade != null)
                    {
                        entry.GradeId = nextGrade.Id;
                        totalPromoted++;
                        whitelistPromoted++;
                    }
                    entry.ExcluirDePromocion = false;
                }
            }

            currentYear.PromotionExecutedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var auditDetails = JsonSerializer.Serialize(new
            {
                Year = currentYear.Year,
                TotalPromoted = totalPromoted,
                TotalGraduated = totalGraduated,
                TotalRetained = totalRetained,
                ActivePromoted = activePromoted,
                ActiveGraduated = activeGraduated,
                ActiveRetained = activeRetained,
                WhitelistPromoted = whitelistPromoted,
                WhitelistGraduated = whitelistGraduated,
                WhitelistRetained = whitelistRetained,
                Forced = force
            });

            await _auditService.LogAsync("PROMOTION_RUN", null, "academic_years", (int)currentYear.Id, null, null, null,
                auditDetails, adminIp);

            return new PromotionResult
            {
                Success = true,
                Message = $"Promoción ejecutada con éxito para el año lectivo {currentYear.Year}.",
                PromotedCount = totalPromoted,
                GraduatedCount = totalGraduated,
                RetainedCount = totalRetained,
                ActivePromotedCount = activePromoted,
                WhitelistPromotedCount = whitelistPromoted,
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

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Services;

public class CsvRowError
{
    public int RowNumber { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class CsvImportResult
{
    public int ProcessedCount { get; set; }
    public int InsertedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }
    public List<CsvRowError> Errors { get; set; } = new();
}

public class VoterDetailDto
{
    public uint Id { get; set; }
    public string Document { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public byte? GradeId { get; set; }
    public string? GradeName { get; set; }
    public byte RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool ExcluirDePromocion { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class WhitelistDetailDto
{
    public uint Id { get; set; }
    public string Document { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public byte GradeId { get; set; }
    public string? GradeName { get; set; }
    public bool ExcluirDePromocion { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsClaimed { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public uint? ClaimedByUserId { get; set; }
}

public class WhitelistStatsDto
{
    public int TotalUploaded { get; set; }
    public int TotalClaimed { get; set; }
    public int TotalPending { get; set; }
}

public interface ICensusService
{
    Task<List<VwActiveCensu>> GetActiveCensusAsync();
    Task<PagedResult<VoterDetailDto>> GetVotersPagedAsync(string? search = null, string? grade = null, string? status = null, byte? roleId = null, int pageNumber = 1, int pageSize = 20);
    Task<List<VoterDetailDto>> GetAllVotersAsync(string? search = null, string? grade = null, string? status = null, byte? roleId = null);
    Task<VoterDetailDto?> GetVoterDetailsAsync(int voterId);
    Task<bool> UpdateVoterAsync(int voterId, string fullName, string contactEmail, byte? gradeId, byte roleId, string status, bool excluirDePromocion, string adminIp);
    Task<bool> SoftDeleteVoterAsync(int voterId, string adminIp);
    Task<bool> RestoreVoterAsync(int voterId, string adminIp);
    Task<bool> ResetPasswordAsync(int voterId, string adminIp);

    // ── M02: Lista Blanca (Whitelist) ─────────────────────────────────────────────
    Task<CensusWhitelist> AddToWhitelistAsync(string document, string fullName, byte gradeId, bool excluirDePromocion, uint adminUserId, string adminIp);
    Task<CsvImportResult> ImportCsvAsync(Stream csvStream, uint adminUserId, string adminIp);
    byte[] GenerateCsvTemplate();
    Task<PagedResult<WhitelistDetailDto>> GetPendingWhitelistPagedAsync(string? search = null, byte? gradeId = null, int pageNumber = 1, int pageSize = 20);
    Task<WhitelistStatsDto> GetWhitelistStatsAsync();
    Task<WhitelistDetailDto?> GetWhitelistEntryAsync(uint id);
    Task<bool> UpdateWhitelistEntryAsync(uint id, string fullName, byte gradeId, bool excluirDePromocion, uint adminUserId, string adminIp);
    Task<bool> DeleteWhitelistEntryAsync(uint id, uint adminUserId, string adminIp);
}

public class CensusService : ICensusService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;
    private readonly IDocumentEncryptionService _encryptionService;
    private readonly ICredentialService _credentialService;

    public CensusService(
        WahlMiraiDbContext context,
        IAuthService authService,
        IAuditService auditService,
        IDocumentEncryptionService encryptionService,
        ICredentialService credentialService)
    {
        _context = context;
        _authService = authService;
        _auditService = auditService;
        _encryptionService = encryptionService;
        _credentialService = credentialService;
    }

    public async Task<List<VwActiveCensu>> GetActiveCensusAsync()
    {
        return await _context.VwActiveCensus.ToListAsync();
    }

    public async Task<PagedResult<VoterDetailDto>> GetVotersPagedAsync(
        string? search = null,
        string? grade = null,
        string? status = null,
        byte? roleId = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _context.Voters
            .Include(v => v.Grade)
            .Include(v => v.Role)
            .Where(v => v.Role.Name == "ELECTOR")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(v => v.Status == status);
        }

        if (roleId.HasValue && roleId.Value > 0)
        {
            query = query.Where(v => v.RoleId == roleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(grade))
        {
            query = query.Where(v => v.Grade != null && v.Grade.Name == grade);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(v => v.FullName.ToLower().Contains(s) || v.ContactEmail.ToLower().Contains(s));
        }

        int totalCount = await query.CountAsync();

        var pagedVoters = await query
            .OrderBy(v => v.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = new List<VoterDetailDto>();
        foreach (var v in pagedVoters)
        {
            string decDoc;
            try
            {
                decDoc = _encryptionService.Decrypt(v.EncryptedDocument);
            }
            catch
            {
                decDoc = "N/A";
            }

            dtos.Add(new VoterDetailDto
            {
                Id = v.Id,
                Document = decDoc,
                FullName = v.FullName,
                ContactEmail = v.ContactEmail,
                GradeId = v.GradeId,
                GradeName = v.Grade?.Name,
                RoleId = v.RoleId,
                RoleName = v.Role?.Name ?? Roles.ElectorName,
                Status = v.Status,
                ExcluirDePromocion = v.ExcluirDePromocion,
                RegisteredAt = v.RegisteredAt,
                UpdatedAt = v.UpdatedAt,
                DeletedAt = v.DeletedAt
            });
        }

        return new PagedResult<VoterDetailDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<VoterDetailDto>> GetAllVotersAsync(string? search = null, string? grade = null, string? status = null, byte? roleId = null)
    {
        var paged = await GetVotersPagedAsync(search, grade, status, roleId, 1, 5000);
        return paged.Items;
    }

    public async Task<VoterDetailDto?> GetVoterDetailsAsync(int voterId)
    {
        var v = await _context.Voters
            .Include(v => v.Grade)
            .Include(v => v.Role)
            .FirstOrDefaultAsync(v => v.Id == (uint)voterId);

        if (v == null) return null;

        string decDoc;
        try
        {
            decDoc = _encryptionService.Decrypt(v.EncryptedDocument);
        }
        catch
        {
            decDoc = "N/A";
        }

        return new VoterDetailDto
        {
            Id = v.Id,
            Document = decDoc,
            FullName = v.FullName,
            ContactEmail = v.ContactEmail,
            GradeId = v.GradeId,
            GradeName = v.Grade?.Name,
            RoleId = v.RoleId,
            RoleName = v.Role?.Name ?? (v.RoleId == Roles.Admin ? Roles.AdminName : (v.RoleId == Roles.SuperAdmin ? Roles.SuperAdminName : Roles.ElectorName)),
            Status = v.Status,
            ExcluirDePromocion = v.ExcluirDePromocion,
            RegisteredAt = v.RegisteredAt,
            UpdatedAt = v.UpdatedAt,
            DeletedAt = v.DeletedAt
        };
    }

    public async Task<bool> UpdateVoterAsync(int voterId, string fullName, string contactEmail, byte? gradeId, byte roleId, string status, bool excluirDePromocion, string adminIp)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null) return false;

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre completo es obligatorio.");
        fullName = fullName.Trim();
        if (!Regex.IsMatch(fullName, @"^[\p{L}]+(?:[ '\-][\p{L}]+)*$"))
            throw new ArgumentException("El nombre solo puede contener letras, espacios, guiones o apóstrofes.");

        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new ArgumentException("El correo de contacto es obligatorio.");
        contactEmail = contactEmail.Trim();

        if (!Regex.IsMatch(contactEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new ArgumentException("El formato del correo de contacto no es válido.");

        if (roleId == Roles.Elector && (!gradeId.HasValue || gradeId.Value == 0))
            throw new ArgumentException("Debe seleccionar un grado escolar válido para electores.");

        if (roleId == Roles.Admin || roleId == Roles.SuperAdmin)
        {
            gradeId = null;
        }

        // Duplicado de correo en otro elector
        var existingEmail = await _context.Voters.FirstOrDefaultAsync(v => v.ContactEmail.ToLower() == contactEmail.ToLower() && v.Id != (uint)voterId);
        if (existingEmail != null)
            throw new InvalidOperationException($"El correo de contacto '{contactEmail}' pertenece a otro elector.");

        var oldValues = $"FullName: {voter.FullName}, Email: {voter.ContactEmail}, GradeId: {voter.GradeId}, RoleId: {voter.RoleId}, Status: {voter.Status}, ExcluirPromocion: {voter.ExcluirDePromocion}";

        voter.FullName = fullName;
        voter.ContactEmail = contactEmail;
        voter.GradeId = gradeId;
        voter.RoleId = roleId;
        voter.Status = status;
        voter.ExcluirDePromocion = excluirDePromocion;
        voter.UpdatedAt = DateTime.UtcNow;

        if (status == "ELIMINADO" && voter.DeletedAt == null)
        {
            voter.DeletedAt = DateTime.UtcNow;
        }
        else if (status != "ELIMINADO")
        {
            voter.DeletedAt = null;
        }

        await _context.SaveChangesAsync();

        var newValues = $"FullName: {voter.FullName}, Email: {voter.ContactEmail}, GradeId: {voter.GradeId}, RoleId: {voter.RoleId}, Status: {voter.Status}, ExcluirPromocion: {voter.ExcluirDePromocion}";

        await _auditService.LogAsync("VOTER_UPDATED", null, "voters", (int)voter.Id, null, oldValues, newValues,
            $"Updated voter ID {voterId}", adminIp);

        return true;
    }

    public async Task<bool> SoftDeleteVoterAsync(int voterId, string adminIp)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.Status == "ELIMINADO") return false;

        voter.Status    = "ELIMINADO";
        voter.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("VOTER_DELETED", null, "voters", (int)voter.Id, "status", "ACTIVO", "ELIMINADO",
            "Soft delete", adminIp);
        return true;
    }

    public async Task<bool> RestoreVoterAsync(int voterId, string adminIp)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.Status != "ELIMINADO") return false;

        voter.Status    = "ACTIVO";
        voter.DeletedAt = null;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("VOTER_RESTORED", null, "voters", (int)voter.Id, "status", "ELIMINADO", "ACTIVO",
            "Restore", adminIp);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int voterId, string adminIp)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || string.IsNullOrWhiteSpace(voter.ContactEmail)) return false;

        await _credentialService.IssueNewPasswordAsync(voterId, EmailType.REASIGNACION_ADMIN, null);
        
        await _auditService.LogAsync("PASSWORD_REASSIGNED", null, "voters", voterId, null, null, null,
            $"Password reset issued by admin", adminIp);

        return true;
    }

    // ── M02: Lista Blanca (Whitelist) ─────────────────────────────────────────────

    public async Task<CensusWhitelist> AddToWhitelistAsync(
        string document,
        string fullName,
        byte gradeId,
        bool excluirDePromocion,
        uint adminUserId,
        string adminIp)
    {
        if (string.IsNullOrWhiteSpace(document))
            throw new ArgumentException("El número de documento es obligatorio.");

        document = document.Trim();
        if (!Regex.IsMatch(document, @"^\d+$"))
            throw new ArgumentException("El número de documento sólo debe contener dígitos numéricos.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre completo es obligatorio.");

        fullName = fullName.Trim();
        if (!Regex.IsMatch(fullName, @"^[\p{L}]+(?:[ '\-][\p{L}]+)*$"))
            throw new ArgumentException("El nombre solo puede contener letras, espacios, guiones o apóstrofes.");

        var gradeExists = await _context.Grades.AnyAsync(g => g.Id == gradeId);
        if (!gradeExists)
            throw new ArgumentException("El grado escolar seleccionado no es válido.");

        var docHash = _authService.HashDocument(document);

        var existingWhitelist = await _context.CensusWhitelists.AnyAsync(w => w.DocumentHash == docHash);
        if (existingWhitelist)
            throw new InvalidOperationException($"El documento '{document}' ya se encuentra registrado en la lista blanca.");

        var existingUser = await _context.Voters.AnyAsync(v => v.DocumentHash == docHash);
        if (existingUser)
            throw new InvalidOperationException($"El documento '{document}' ya se encuentra registrado como usuario activo en el sistema.");

        var entry = new CensusWhitelist
        {
            DocumentHash = docHash,
            EncryptedDocument = _encryptionService.Encrypt(document),
            FullName = fullName,
            GradeId = gradeId,
            ExcluirDePromocion = excluirDePromocion,
            UploadedByUserId = adminUserId,
            CreatedAt = DateTime.UtcNow,
            ClaimedAt = null,
            ClaimedByUserId = null
        };

        _context.CensusWhitelists.Add(entry);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "WHITELIST_ENTRY_CREATED",
            (int)adminUserId,
            "census_whitelist",
            (int)entry.Id,
            null,
            null,
            null,
            $"Entrada de lista blanca creada: {fullName} (Doc: {document}, Grado ID: {gradeId})",
            adminIp);

        return entry;
    }

    public async Task<CsvImportResult> ImportCsvAsync(Stream csvStream, uint adminUserId, string adminIp)
    {
        var result = new CsvImportResult();
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(content))
        {
            result.Errors.Add(new CsvRowError { RowNumber = 0, Identifier = "Archivo", Reason = "El archivo CSV está completamente vacío." });
            result.ErrorCount = 1;
            return result;
        }

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            result.Errors.Add(new CsvRowError { RowNumber = 0, Identifier = "Archivo", Reason = "El archivo CSV no contiene filas de datos (solo cabecera o sin contenido)." });
            result.ErrorCount = 1;
            return result;
        }

        // Parse Header
        var header = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant().Replace("\"", "")).ToArray();
        int docIndex = Array.FindIndex(header, h => h.Contains("documento") || h.Contains("cedula") || h.Contains("doc"));
        int nameIndex = Array.FindIndex(header, h => h.Contains("nombre") || h.Contains("fullname") || h.Contains("estudiante"));
        int gradeIndex = Array.FindIndex(header, h => h.Contains("grado") || h.Contains("grade"));
        int excluirIndex = Array.FindIndex(header, h => h.Contains("excluir") || h.Contains("repitente"));

        if (docIndex == -1 || nameIndex == -1)
        {
            result.Errors.Add(new CsvRowError
            {
                RowNumber = 1,
                Identifier = "Cabecera",
                Reason = "Las columnas requeridas 'documento' y 'nombre' (o 'grado_id', 'excluir_promocion') no se encontraron en la primera línea del CSV."
            });
            result.ErrorCount = 1;
            return result;
        }

        var grades = await _context.Grades.ToListAsync();

        // Carga previa de hashes existentes para máxima eficiencia (soporta >2000 registros sin n+1 queries)
        var existingWhitelistHashes = (await _context.CensusWhitelists.Select(w => w.DocumentHash).ToListAsync()).ToHashSet();
        var existingUserHashes = (await _context.Voters.Select(v => v.DocumentHash).ToListAsync()).ToHashSet();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var processedDocHashes = new HashSet<string>();
            var entriesToAdd = new List<CensusWhitelist>();

            for (int i = 1; i < lines.Length; i++)
            {
                result.ProcessedCount++;
                var rowNum = i + 1;
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split(',').Select(c => c.Trim().Replace("\"", "")).ToArray();

                string doc = docIndex < columns.Length ? columns[docIndex] : "";
                string name = nameIndex < columns.Length ? columns[nameIndex] : "";
                string gradeStr = (gradeIndex >= 0 && gradeIndex < columns.Length) ? columns[gradeIndex] : "";
                string excluirStr = (excluirIndex >= 0 && excluirIndex < columns.Length) ? columns[excluirIndex] : "0";

                if (string.IsNullOrWhiteSpace(doc) || string.IsNullOrWhiteSpace(name))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = "Documento o nombre en blanco." });
                    continue;
                }

                if (!Regex.IsMatch(doc, @"^\d+$"))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = "El documento debe contener sólo números." });
                    continue;
                }

                if (!Regex.IsMatch(name, @"^[\p{L}]+(?:[ '\-][\p{L}]+)*$"))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = "El nombre contiene caracteres inválidos." });
                    continue;
                }

                // Mapear Grado
                byte? gradeId = null;
                if (!string.IsNullOrWhiteSpace(gradeStr))
                {
                    if (byte.TryParse(gradeStr, out byte parsedGId))
                    {
                        var matchingGrade = grades.FirstOrDefault(g => g.Id == parsedGId);
                        if (matchingGrade != null) gradeId = matchingGrade.Id;
                    }

                    if (!gradeId.HasValue)
                    {
                        var matchingGrade = grades.FirstOrDefault(g => g.Name.Equals(gradeStr, StringComparison.OrdinalIgnoreCase) || g.Name.Contains(gradeStr));
                        if (matchingGrade != null) gradeId = matchingGrade.Id;
                    }
                }

                if (!gradeId.HasValue && grades.Any())
                {
                    gradeId = grades.First().Id; // Fallback al primer grado registrado si no se especificó
                }

                if (!gradeId.HasValue)
                {
                    result.ErrorCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = "No se pudo asociar un grado escolar válido." });
                    continue;
                }

                bool excluir = excluirStr == "1" || excluirStr.Equals("true", StringComparison.OrdinalIgnoreCase) || excluirStr.Equals("si", StringComparison.OrdinalIgnoreCase);

                // Control de duplicados
                var docHash = _authService.HashDocument(doc);
                if (processedDocHashes.Contains(docHash))
                {
                    result.DuplicateCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = "Documento duplicado dentro del mismo archivo CSV." });
                    continue;
                }

                if (existingWhitelistHashes.Contains(docHash))
                {
                    result.DuplicateCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = "El documento ya existe en la lista blanca de la base de datos." });
                    continue;
                }

                if (existingUserHashes.Contains(docHash))
                {
                    result.DuplicateCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = "El documento ya pertenece a un usuario registrado en el sistema." });
                    continue;
                }

                processedDocHashes.Add(docHash);
                existingWhitelistHashes.Add(docHash); // Evitar colisiones posteriores en el mismo lote

                var whitelistEntry = new CensusWhitelist
                {
                    DocumentHash = docHash,
                    EncryptedDocument = _encryptionService.Encrypt(doc),
                    FullName = name,
                    GradeId = gradeId.Value,
                    ExcluirDePromocion = excluir,
                    UploadedByUserId = adminUserId,
                    CreatedAt = DateTime.UtcNow,
                    ClaimedAt = null,
                    ClaimedByUserId = null
                };

                entriesToAdd.Add(whitelistEntry);
                result.InsertedCount++;
            }

            if (entriesToAdd.Any())
            {
                _context.CensusWhitelists.AddRange(entriesToAdd);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            await _auditService.LogAsync(
                "CSV_IMPORT_WHITELIST",
                (int)adminUserId,
                "census_whitelist",
                null,
                null,
                null,
                null,
                $"Carga masiva CSV lista blanca: Procesados={result.ProcessedCount}, Insertados={result.InsertedCount}, Duplicados={result.DuplicateCount}, Errores={result.ErrorCount}",
                adminIp);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.ErrorCount++;
            result.Errors.Add(new CsvRowError { RowNumber = 0, Identifier = "Error Crítico", Reason = $"Error en la transacción de base de datos: {ex.Message}" });
        }

        return result;
    }

    public byte[] GenerateCsvTemplate()
    {
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("documento,nombre,grado_id,excluir_promocion");
        csvBuilder.AppendLine("1020304050,Juan Pérez,1,0");
        csvBuilder.AppendLine("1020304051,María Gómez,2,0");
        csvBuilder.AppendLine("1020304052,Carlos Rodríguez,3,1");
        return Encoding.UTF8.GetBytes(csvBuilder.ToString());
    }

    public async Task<PagedResult<WhitelistDetailDto>> GetPendingWhitelistPagedAsync(
        string? search = null,
        byte? gradeId = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _context.CensusWhitelists
            .Include(w => w.Grade)
            .Where(w => w.ClaimedAt == null)
            .AsQueryable();

        if (gradeId.HasValue && gradeId.Value > 0)
        {
            query = query.Where(w => w.GradeId == gradeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(w => w.FullName.ToLower().Contains(s));
        }

        int totalCount = await query.CountAsync();

        var pagedList = await query
            .OrderBy(w => w.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = new List<WhitelistDetailDto>();
        foreach (var w in pagedList)
        {
            string decDoc;
            try
            {
                decDoc = _encryptionService.Decrypt(w.EncryptedDocument);
            }
            catch
            {
                decDoc = "N/A";
            }

            dtos.Add(new WhitelistDetailDto
            {
                Id = w.Id,
                Document = decDoc,
                DocumentHash = w.DocumentHash,
                FullName = w.FullName,
                GradeId = w.GradeId,
                GradeName = w.Grade?.Name,
                ExcluirDePromocion = w.ExcluirDePromocion,
                CreatedAt = w.CreatedAt,
                IsClaimed = w.ClaimedAt != null,
                ClaimedAt = w.ClaimedAt,
                ClaimedByUserId = w.ClaimedByUserId
            });
        }

        return new PagedResult<WhitelistDetailDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<WhitelistStatsDto> GetWhitelistStatsAsync()
    {
        var totalUploaded = await _context.CensusWhitelists.CountAsync();
        var totalClaimed  = await _context.CensusWhitelists.CountAsync(w => w.ClaimedAt != null);
        var totalPending  = await _context.CensusWhitelists.CountAsync(w => w.ClaimedAt == null);

        return new WhitelistStatsDto
        {
            TotalUploaded = totalUploaded,
            TotalClaimed  = totalClaimed,
            TotalPending  = totalPending
        };
    }

    public async Task<WhitelistDetailDto?> GetWhitelistEntryAsync(uint id)
    {
        var entry = await _context.CensusWhitelists
            .Include(w => w.Grade)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (entry == null) return null;

        string decDoc;
        try
        {
            decDoc = _encryptionService.Decrypt(entry.EncryptedDocument);
        }
        catch
        {
            decDoc = "N/A";
        }

        return new WhitelistDetailDto
        {
            Id = entry.Id,
            Document = decDoc,
            DocumentHash = entry.DocumentHash,
            FullName = entry.FullName,
            GradeId = entry.GradeId,
            GradeName = entry.Grade?.Name,
            ExcluirDePromocion = entry.ExcluirDePromocion,
            CreatedAt = entry.CreatedAt,
            IsClaimed = entry.ClaimedAt != null,
            ClaimedAt = entry.ClaimedAt,
            ClaimedByUserId = entry.ClaimedByUserId
        };
    }

    public async Task<bool> UpdateWhitelistEntryAsync(
        uint id,
        string fullName,
        byte gradeId,
        bool excluirDePromocion,
        uint adminUserId,
        string adminIp)
    {
        var entry = await _context.CensusWhitelists.FindAsync(id);
        if (entry == null) return false;

        if (entry.ClaimedAt != null)
            throw new InvalidOperationException("No se puede modificar una entrada de lista blanca que ya ha sido reclamada por un estudiante.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("El nombre completo es obligatorio.");

        fullName = fullName.Trim();
        if (!Regex.IsMatch(fullName, @"^[\p{L}]+(?:[ '\-][\p{L}]+)*$"))
            throw new ArgumentException("El nombre solo puede contener letras, espacios, guiones o apóstrofes.");

        var gradeExists = await _context.Grades.AnyAsync(g => g.Id == gradeId);
        if (!gradeExists)
            throw new ArgumentException("El grado seleccionado no es válido.");

        var oldValues = $"FullName: {entry.FullName}, GradeId: {entry.GradeId}, ExcluirPromocion: {entry.ExcluirDePromocion}";

        entry.FullName = fullName;
        entry.GradeId = gradeId;
        entry.ExcluirDePromocion = excluirDePromocion;

        await _context.SaveChangesAsync();

        var newValues = $"FullName: {entry.FullName}, GradeId: {entry.GradeId}, ExcluirPromocion: {entry.ExcluirDePromocion}";

        await _auditService.LogAsync(
            "WHITELIST_ENTRY_UPDATED",
            (int)adminUserId,
            "census_whitelist",
            (int)entry.Id,
            null,
            oldValues,
            newValues,
            $"Entrada de lista blanca actualizada: ID {id}",
            adminIp);

        return true;
    }

    public async Task<bool> DeleteWhitelistEntryAsync(uint id, uint adminUserId, string adminIp)
    {
        var entry = await _context.CensusWhitelists.FindAsync(id);
        if (entry == null) return false;

        if (entry.ClaimedAt != null)
            throw new InvalidOperationException("No se puede eliminar una entrada de lista blanca que ya ha sido reclamada.");

        _context.CensusWhitelists.Remove(entry);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "WHITELIST_ENTRY_DELETED",
            (int)adminUserId,
            "census_whitelist",
            (int)id,
            null,
            null,
            null,
            $"Entrada de lista blanca eliminada: ID {id} ({entry.FullName})",
            adminIp);

        return true;
    }
}

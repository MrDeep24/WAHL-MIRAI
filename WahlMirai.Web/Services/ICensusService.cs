using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

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

public interface ICensusService
{
    Task<List<VwActiveCensu>> GetActiveCensusAsync();
    Task<List<VoterDetailDto>> GetAllVotersAsync(string? search = null, string? grade = null, string? status = null, byte? roleId = null);
    Task<VoterDetailDto?> GetVoterDetailsAsync(int voterId);
    Task<Voter> AddVoterAsync(string document, string fullName, string contactEmail, byte? gradeId, byte roleId, bool excluirDePromocion, string adminIp);
    Task<bool> UpdateVoterAsync(int voterId, string fullName, string contactEmail, byte? gradeId, byte roleId, string status, bool excluirDePromocion, string adminIp);
    Task<CsvImportResult> ImportCsvAsync(Stream csvStream, string adminIp);
    byte[] GenerateCsvTemplate();
    Task<bool> SoftDeleteVoterAsync(int voterId, string adminIp);
    Task<bool> RestoreVoterAsync(int voterId, string adminIp);
    Task<bool> ResetPasswordAsync(int voterId, string adminIp);
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

    public async Task<List<VoterDetailDto>> GetAllVotersAsync(string? search = null, string? grade = null, string? status = null, byte? roleId = null)
    {
        // El censo electoral solo representa estudiantes; las cuentas administrativas viven en M09.
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

        var list = await query.ToListAsync();

        var result = new List<VoterDetailDto>();
        foreach (var v in list)
        {
            string decDoc = string.Empty;
            try
            {
                decDoc = _encryptionService.Decrypt(v.EncryptedDocument);
            }
            catch
            {
                decDoc = "N/A";
            }

            result.Add(new VoterDetailDto
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
            });
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            result = result.Where(r => 
                r.FullName.ToLowerInvariant().Contains(s) || 
                r.ContactEmail.ToLowerInvariant().Contains(s) || 
                r.Document.ToLowerInvariant().Contains(s)
            ).ToList();
        }

        return result;
    }

    public async Task<VoterDetailDto?> GetVoterDetailsAsync(int voterId)
    {
        var v = await _context.Voters
            .Include(v => v.Grade)
            .Include(v => v.Role)
            .FirstOrDefaultAsync(v => v.Id == (uint)voterId);

        if (v == null) return null;

        string decDoc = string.Empty;
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

    public async Task<Voter> AddVoterAsync(string document, string fullName, string contactEmail, byte? gradeId, byte roleId, bool excluirDePromocion, string adminIp)
    {
        // 1. Validaciones de obligatoriedad y formato
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

        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new ArgumentException("El correo de contacto es obligatorio.");
        contactEmail = contactEmail.Trim();

        if (!Regex.IsMatch(contactEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new ArgumentException("El formato del correo de contacto no es válido.");

        if (roleId == Roles.Elector && (!gradeId.HasValue || gradeId.Value == 0))
            throw new ArgumentException("Debe seleccionar un grado escolar válido para usuarios con rol Elector.");

        if (roleId == Roles.Admin || roleId == Roles.SuperAdmin)
        {
            gradeId = null; // Admin no requiere grado
        }

        // 2. Control de duplicados
        var docHash = _authService.HashDocument(document);
        var existingVoterDoc = await _context.Voters.FirstOrDefaultAsync(v => v.DocumentHash == docHash);
        if (existingVoterDoc != null)
            throw new InvalidOperationException($"Ya existe un elector registrado con el número de documento '{document}'.");

        var existingEmail = await _context.Voters.FirstOrDefaultAsync(v => v.ContactEmail.ToLower() == contactEmail.ToLower());
        if (existingEmail != null)
            throw new InvalidOperationException($"El correo de contacto '{contactEmail}' ya se encuentra asignado a otro elector.");

        // 3. Creación y persistencia
        var voter = new Voter
        {
            DocumentHash      = docHash,
            EncryptedDocument = _encryptionService.Encrypt(document),
            FullName          = fullName,
            ContactEmail      = contactEmail,
            GradeId           = gradeId,
            RoleId            = roleId,
            PasswordHash      = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // temporal placeholder
            ExcluirDePromocion = excluirDePromocion,
            Status            = "ACTIVO",
            RegisteredAt      = DateTime.UtcNow
        };

        _context.Voters.Add(voter);
        await _context.SaveChangesAsync();

        // 4. Generación de credenciales y cola de envío de correo
        await _credentialService.IssueNewPasswordAsync((int)voter.Id, EmailType.CREDENCIAL_INICIAL, null);

        await _auditService.LogAsync("VOTER_CREATED", null, "voters", (int)voter.Id, null, null, null,
            $"Created voter: {fullName} (Doc: {document})", adminIp);

        return voter;
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

    public async Task<CsvImportResult> ImportCsvAsync(Stream csvStream, string adminIp)
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
        int emailIndex = Array.FindIndex(header, h => h.Contains("correo") || h.Contains("email") || h.Contains("contacto"));
        int gradeIndex = Array.FindIndex(header, h => h.Contains("grado") || h.Contains("grade"));
        int excluirIndex = Array.FindIndex(header, h => h.Contains("excluir") || h.Contains("repitente"));

        if (docIndex == -1 || nameIndex == -1)
        {
            result.Errors.Add(new CsvRowError { 
                RowNumber = 1, 
                Identifier = "Cabecera", 
                Reason = "Las columnas requeridas 'documento' y 'nombre' (o 'correo_contacto', 'grado_id') no se encontraron en la primera línea del CSV." 
            });
            result.ErrorCount = 1;
            return result;
        }

        var grades = await _context.Grades.ToListAsync();

        // Transaction for atomic import safety
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var processedDocHashes = new HashSet<string>();
            var processedEmails = new HashSet<string>();

            for (int i = 1; i < lines.Length; i++)
            {
                result.ProcessedCount++;
                var rowNum = i + 1;
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split(',').Select(c => c.Trim().Replace("\"", "")).ToArray();

                string doc = docIndex < columns.Length ? columns[docIndex] : "";
                string name = nameIndex < columns.Length ? columns[nameIndex] : "";
                string email = (emailIndex >= 0 && emailIndex < columns.Length) ? columns[emailIndex] : "";
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

                // Generar correo por defecto si no viene en el CSV
                if (string.IsNullOrWhiteSpace(email))
                {
                    email = $"elector{doc}@colegio.edu.co";
                }

                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    result.ErrorCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = $"El correo '{email}' no tiene un formato válido." });
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
                    gradeId = grades.First().Id; // Fallback al primer grado registrado
                }

                bool excluir = excluirStr == "1" || excluirStr.Equals("true", StringComparison.OrdinalIgnoreCase) || excluirStr.Equals("si", StringComparison.OrdinalIgnoreCase);

                // Control duplicados
                var docHash = _authService.HashDocument(doc);
                if (processedDocHashes.Contains(docHash))
                {
                    result.DuplicateCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = "Documento duplicado dentro del mismo archivo CSV." });
                    continue;
                }

                if (processedEmails.Contains(email.ToLower()))
                {
                    result.DuplicateCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = $"El correo '{email}' está duplicado dentro del mismo CSV." });
                    continue;
                }

                var dbExistingDoc = await _context.Voters.AnyAsync(v => v.DocumentHash == docHash);
                if (dbExistingDoc)
                {
                    result.DuplicateCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = "El documento ya existe registrado en la base de datos." });
                    continue;
                }

                var dbExistingEmail = await _context.Voters.AnyAsync(v => v.ContactEmail.ToLower() == email.ToLower());
                if (dbExistingEmail)
                {
                    result.DuplicateCount++;
                    result.Errors.Add(new CsvRowError { RowNumber = rowNum, Identifier = doc, Reason = $"El correo de contacto '{email}' ya existe registrado en la base de datos." });
                    continue;
                }

                processedDocHashes.Add(docHash);
                processedEmails.Add(email.ToLower());

                var voter = new Voter
                {
                    DocumentHash = docHash,
                    EncryptedDocument = _encryptionService.Encrypt(doc),
                    FullName = name,
                    ContactEmail = email,
                    GradeId = gradeId,
                    RoleId = Roles.Elector, // Role ELECTOR por defecto en cargas masivas
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    ExcluirDePromocion = excluir,
                    Status = "ACTIVO",
                    RegisteredAt = DateTime.UtcNow
                };

                _context.Voters.Add(voter);
                await _context.SaveChangesAsync();

                await _credentialService.IssueNewPasswordAsync((int)voter.Id, EmailType.CREDENCIAL_INICIAL, null);
                result.InsertedCount++;
            }

            await transaction.CommitAsync();

            await _auditService.LogAsync("CSV_IMPORT", null, "voters", null, null, null, null,
                $"CSV Import summary: Processed={result.ProcessedCount}, Inserted={result.InsertedCount}, Duplicates={result.DuplicateCount}, Errors={result.ErrorCount}", adminIp);
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
        csvBuilder.AppendLine("documento,nombre,correo_contacto,grado_id,excluir_promocion");
        csvBuilder.AppendLine("1020304050,Juan Pérez,juan.perez@colegio.edu.co,1,0");
        csvBuilder.AppendLine("1020304051,María Gómez,maria.gomez@colegio.edu.co,2,0");
        csvBuilder.AppendLine("1020304052,Carlos Rodríguez,carlos.rodriguez@colegio.edu.co,3,1");
        return Encoding.UTF8.GetBytes(csvBuilder.ToString());
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
}


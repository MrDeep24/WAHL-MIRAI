// ============================================================
// Archivo:       IAdminAccountService.cs
// Módulo:        M09 — Gestión de Cuentas Administrativas
//                (Exclusivo Súper Administrador)
// Desarrollador: Kevin
// Descripción:   Define el contrato y la implementación de las operaciones
//                de consulta paginada, alta, edición, baja lógica y restauración de cuentas.
// Requerimientos: RF-M09-01; invalidación de sesiones según RN-14.
// ============================================================
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Web.Services;

/// <summary>
/// Proyección de los datos de una cuenta administrativa para su presentación (RF-M09-01).
/// </summary>
public sealed class AdminAccountListItem
{
    public uint Id { get; set; }
    public string Document { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? PositionTitle { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Define las operaciones disponibles para gestionar cuentas administrativas (RF-M09-01).
/// </summary>
public interface IAdminAccountService
{
    /// <summary>
    /// Obtiene las cuentas administrativas ordenadas por nombre.
    /// </summary>
    /// <param name="includeDeleted">Indica si se incluyen las cuentas eliminadas lógicamente.</param>
    /// <param name="ct">Token para cancelar la consulta.</param>
    /// <returns>Lista de cuentas administrativas.</returns>
    Task<List<AdminAccountListItem>> GetAccountsAsync(bool includeDeleted = true, CancellationToken ct = default);

    /// <summary>
    /// Obtiene una página de cuentas administrativas con búsqueda y ordenamiento (RF-M09-01).
    /// </summary>
    /// <param name="pageNumber">Número de página, comenzando en uno.</param>
    /// <param name="pageSize">Cantidad de resultados por página, limitada a un máximo de cien.</param>
    /// <param name="search">Texto parcial para buscar por nombre, correo o cargo institucional.</param>
    /// <param name="sortBy">Nombre de propiedad admitido para ordenar; los valores desconocidos usan FullName.</param>
    /// <param name="ascending">Indica si el orden es ascendente para una propiedad admitida.</param>
    /// <param name="includeDeleted">Indica si se incluyen las cuentas eliminadas lógicamente.</param>
    /// <param name="ct">Token para cancelar la consulta.</param>
    /// <returns>Página de cuentas administrativas y metadatos de paginación.</returns>
    Task<PagedResult<AdminAccountListItem>> GetPagedAccountsAsync(
        int pageNumber, int pageSize, string? search, string? sortBy,
        bool ascending, bool includeDeleted = true, CancellationToken ct = default);

    /// <summary>
    /// Crea una cuenta administrativa e inicia la emisión de su contraseña (RF-M09-01).
    /// </summary>
    /// <param name="document">Documento de la cuenta.</param>
    /// <param name="fullName">Nombre completo de la persona.</param>
    /// <param name="contactEmail">Correo de contacto.</param>
    /// <param name="roleName">Rol ADMIN o SUPER_ADMIN.</param>
    /// <param name="positionTitle">Cargo institucional descriptivo, si se proporciona.</param>
    /// <param name="actorVoterId">Identificador del Súper Administrador que realiza la acción.</param>
    /// <param name="ipAddress">Dirección IP asociada al registro de auditoría.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task CreateAsync(string document, string fullName, string contactEmail, string roleName,
        string? positionTitle, int actorVoterId, string ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Actualiza los datos de una cuenta administrativa existente (RF-M09-01, RN-14).
    /// </summary>
    /// <param name="accountId">Identificador de la cuenta que se actualizará.</param>
    /// <param name="document">Nuevo documento.</param>
    /// <param name="fullName">Nuevo nombre completo.</param>
    /// <param name="contactEmail">Nuevo correo de contacto.</param>
    /// <param name="roleName">Nuevo rol ADMIN o SUPER_ADMIN.</param>
    /// <param name="positionTitle">Nuevo cargo institucional descriptivo, si se proporciona.</param>
    /// <param name="actorVoterId">Identificador del Súper Administrador que realiza la acción.</param>
    /// <param name="ipAddress">Dirección IP asociada al registro de auditoría.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns><see langword="true"/> si se actualizó una cuenta; de lo contrario, <see langword="false"/>.</returns>
    Task<bool> UpdateAsync(int accountId, string document, string fullName, string contactEmail,
        string roleName, string? positionTitle, int actorVoterId, string ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Elimina lógicamente una cuenta administrativa e invalida su sesión activa (RF-M09-01, RN-14).
    /// </summary>
    /// <param name="accountId">Identificador de la cuenta que se eliminará.</param>
    /// <param name="actorVoterId">Identificador del Súper Administrador que realiza la acción.</param>
    /// <param name="ipAddress">Dirección IP asociada al registro de auditoría.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns><see langword="true"/> si se eliminó la cuenta; de lo contrario, <see langword="false"/>.</returns>
    Task<bool> SoftDeleteAsync(int accountId, int actorVoterId, string ipAddress, CancellationToken ct = default);

    /// <summary>
    /// Restaura una cuenta administrativa eliminada lógicamente (RF-M09-01).
    /// </summary>
    /// <param name="accountId">Identificador de la cuenta que se restaurará.</param>
    /// <param name="actorVoterId">Identificador del Súper Administrador que realiza la acción.</param>
    /// <param name="ipAddress">Dirección IP asociada al registro de auditoría.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns><see langword="true"/> si se restauró la cuenta; de lo contrario, <see langword="false"/>.</returns>
    Task<bool> RestoreAsync(int accountId, int actorVoterId, string ipAddress, CancellationToken ct = default);
}

/// <summary>
/// Implementa la gestión persistente y auditada de cuentas administrativas, incluida la invalidación
/// de sesiones cuando una cuenta deja de estar activa (RF-M09-01, RN-14).
/// </summary>
public sealed class AdminAccountService : IAdminAccountService
{
    private static readonly string[] AllowedRoles = ["ADMIN", "SUPER_ADMIN"];
    private readonly WahlMiraiDbContext _context;
    private readonly IAuthService _authService;
    private readonly IDocumentEncryptionService _encryptionService;
    private readonly ICredentialService _credentialService;
    private readonly IAuditService _auditService;
    private readonly IActiveSessionStore _activeSessionStore;

    /// <summary>
    /// Inicializa el servicio con sus dependencias de persistencia, autenticación, credenciales,
    /// auditoría e invalidación de sesiones.
    /// </summary>
    /// <param name="context">Contexto de persistencia de Wahl Mirai.</param>
    /// <param name="authService">Servicio para calcular el hash del documento.</param>
    /// <param name="encryptionService">Servicio para cifrar y descifrar documentos.</param>
    /// <param name="credentialService">Servicio para emitir la contraseña inicial.</param>
    /// <param name="auditService">Servicio para registrar las operaciones administrativas.</param>
    /// <param name="activeSessionStore">Almacén compartido de sesiones activas, si se proporciona.</param>
    public AdminAccountService(
        WahlMiraiDbContext context,
        IAuthService authService,
        IDocumentEncryptionService encryptionService,
        ICredentialService credentialService,
        IAuditService auditService,
        IActiveSessionStore? activeSessionStore = null)
    {
        _context = context;
        _authService = authService;
        _encryptionService = encryptionService;
        _credentialService = credentialService;
        _auditService = auditService;
        // NOTA: El parámetro IActiveSessionStore es opcional con fallback a
        // new ActiveSessionStore() si no se inyecta. Verificado (26-Sept-2026):
        // en la app real esto NUNCA ocurre — Program.cs registra
        // IActiveSessionStore como AddSingleton y AdminAccountService se
        // resuelve por DI (AddScoped), sin ninguna instanciación manual en
        // WahlMirai.Web. El fallback solo se activa intencionalmente en
        // AdminAccountServiceTests.cs para aislar pruebas unitarias. RN-14
        // confirmado intacto para M09.
        _activeSessionStore = activeSessionStore ?? new ActiveSessionStore();
    }

    /// <summary>
    /// Obtiene las cuentas ADMIN y SUPER_ADMIN y proyecta sus datos para la vista.
    /// </summary>
    /// <param name="includeDeleted">Indica si se incluyen las cuentas eliminadas lógicamente.</param>
    /// <param name="ct">Token para cancelar la consulta.</param>
    /// <returns>Lista de cuentas administrativas ordenadas por nombre.</returns>
    public async Task<List<AdminAccountListItem>> GetAccountsAsync(bool includeDeleted = true, CancellationToken ct = default)
    {
        var query = _context.Voters.Include(v => v.Role)
            .Where(v => v.Role.Name == "ADMIN" || v.Role.Name == "SUPER_ADMIN");
        if (!includeDeleted)
            query = query.Where(v => v.Status != "ELIMINADO");

        var accounts = await query.OrderBy(v => v.FullName).ToListAsync(ct);
        return accounts.Select(v => new AdminAccountListItem
        {
            Id = v.Id,
            Document = DecryptDocument(v.EncryptedDocument),
            FullName = v.FullName,
            ContactEmail = v.ContactEmail,
            RoleName = v.Role.Name,
            PositionTitle = v.PositionTitle,
            Status = v.Status,
            RegisteredAt = v.RegisteredAt,
            DeletedAt = v.DeletedAt
        }).ToList();
    }

    /// <summary>
    /// Obtiene una página de cuentas administrativas filtrada, ordenada y proyectada (RF-M09-01).
    /// </summary>
    /// <param name="pageNumber">Número de página, comenzando en uno.</param>
    /// <param name="pageSize">Cantidad de resultados por página, limitada a un máximo de cien.</param>
    /// <param name="search">Texto parcial para buscar por nombre, correo o cargo institucional.</param>
    /// <param name="sortBy">Nombre de propiedad admitido para ordenar; los valores desconocidos usan FullName.</param>
    /// <param name="ascending">Indica si el orden es ascendente para una propiedad admitida.</param>
    /// <param name="includeDeleted">Indica si se incluyen las cuentas eliminadas lógicamente.</param>
    /// <param name="ct">Token para cancelar la consulta.</param>
    /// <returns>Página de cuentas administrativas y metadatos de paginación.</returns>
    public async Task<PagedResult<AdminAccountListItem>> GetPagedAccountsAsync(
        int pageNumber, int pageSize, string? search, string? sortBy,
        bool ascending, bool includeDeleted = true, CancellationToken ct = default)
    {
        const int minimumPageSize = 1;
        const int maximumPageSize = 100;
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, minimumPageSize, maximumPageSize);

        var query = _context.Voters.AsNoTracking().Include(v => v.Role)
            .Where(v => v.Role.Name == "ADMIN" || v.Role.Name == "SUPER_ADMIN");
        if (!includeDeleted)
            query = query.Where(v => v.Status != "ELIMINADO");

        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            normalizedSearch = normalizedSearch.ToLowerInvariant();
            query = query.Where(v => v.FullName.ToLower().Contains(normalizedSearch) ||
                v.ContactEmail.ToLower().Contains(normalizedSearch) ||
                (v.PositionTitle != null && v.PositionTitle.ToLower().Contains(normalizedSearch)));
        }

        var totalCount = await query.CountAsync(ct);
        var isKnownSort = sortBy is "Id" or "FullName" or "ContactEmail" or "RoleName" or
            "PositionTitle" or "Status" or "RegisteredAt";
        var effectiveSortBy = isKnownSort ? sortBy! : "FullName";
        var effectiveAscending = isKnownSort ? ascending : true;

        IOrderedQueryable<Voter> orderedQuery = (effectiveSortBy, effectiveAscending) switch
        {
            ("Id", true) => query.OrderBy(v => v.Id),
            ("Id", false) => query.OrderByDescending(v => v.Id),
            ("ContactEmail", true) => query.OrderBy(v => v.ContactEmail),
            ("ContactEmail", false) => query.OrderByDescending(v => v.ContactEmail),
            ("RoleName", true) => query.OrderBy(v => v.Role.Name),
            ("RoleName", false) => query.OrderByDescending(v => v.Role.Name),
            ("PositionTitle", true) => query.OrderBy(v => v.PositionTitle),
            ("PositionTitle", false) => query.OrderByDescending(v => v.PositionTitle),
            ("Status", true) => query.OrderBy(v => v.Status),
            ("Status", false) => query.OrderByDescending(v => v.Status),
            ("RegisteredAt", true) => query.OrderBy(v => v.RegisteredAt),
            ("RegisteredAt", false) => query.OrderByDescending(v => v.RegisteredAt),
            ("FullName", false) => query.OrderByDescending(v => v.FullName),
            _ => query.OrderBy(v => v.FullName)
        };

        var skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);
        var accounts = await orderedQuery.ThenBy(v => v.Id).Skip(skip).Take(pageSize).ToListAsync(ct);
        return new PagedResult<AdminAccountListItem>
        {
            Items = accounts.Select(v => new AdminAccountListItem
            {
                Id = v.Id,
                Document = DecryptDocument(v.EncryptedDocument),
                FullName = v.FullName,
                ContactEmail = v.ContactEmail,
                RoleName = v.Role.Name,
                PositionTitle = v.PositionTitle,
                Status = v.Status,
                RegisteredAt = v.RegisteredAt,
                DeletedAt = v.DeletedAt
            }).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Valida y crea una cuenta administrativa, emite su contraseña inicial y registra la acción (RF-M09-01).
    /// </summary>
    /// <param name="document">Documento de la cuenta.</param>
    /// <param name="fullName">Nombre completo de la persona.</param>
    /// <param name="contactEmail">Correo de contacto.</param>
    /// <param name="roleName">Rol ADMIN o SUPER_ADMIN.</param>
    /// <param name="positionTitle">Cargo institucional descriptivo, si se proporciona.</param>
    /// <param name="actorVoterId">Identificador del Súper Administrador que realiza la acción.</param>
    /// <param name="ipAddress">Dirección IP asociada al registro de auditoría.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task CreateAsync(string document, string fullName, string contactEmail, string roleName,
        string? positionTitle, int actorVoterId, string ipAddress, CancellationToken ct = default)
    {
        var normalizedDocument = NormalizeRequired(document, "documento");
        var normalizedName = NormalizeRequired(fullName, "nombre");
        var normalizedEmail = NormalizeEmail(contactEmail);
        var normalizedRole = NormalizeRole(roleName);
        var documentHash = _authService.HashDocument(normalizedDocument);

        if (await _context.Voters.AnyAsync(v => v.DocumentHash == documentHash, ct))
            throw new InvalidOperationException("Ya existe una cuenta con ese documento.");

        var account = new Voter
        {
            RoleId = await GetRoleIdAsync(normalizedRole, ct),
            GradeId = null,
            DocumentHash = documentHash,
            EncryptedDocument = _encryptionService.Encrypt(normalizedDocument),
            FullName = normalizedName,
            ContactEmail = normalizedEmail,
            PasswordHash = string.Empty,
            PositionTitle = NormalizeOptional(positionTitle),
            ExcluirDePromocion = false,
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        };

        _context.Voters.Add(account);
        await _context.SaveChangesAsync(ct);
        await _credentialService.IssueNewPasswordAsync((int)account.Id, EmailType.REASIGNACION_ADMIN, actorVoterId, ct);
        await _auditService.LogAsync("ADMIN_ACCOUNT_CREATED", actorVoterId, "users", (int)account.Id,
            null, null, normalizedRole, "{\"credential_email_type\":\"REASIGNACION_ADMIN\"}", ipAddress);
    }

            /// <summary>
            /// Actualiza una cuenta administrativa, impide retirar el último Súper Administrador activo
            /// y revoca la sesión si la cuenta no está activa (RF-M09-01, RN-14).
            /// </summary>
            /// <param name="accountId">Identificador de la cuenta que se actualizará.</param>
            /// <param name="document">Nuevo documento.</param>
            /// <param name="fullName">Nuevo nombre completo.</param>
            /// <param name="contactEmail">Nuevo correo de contacto.</param>
            /// <param name="roleName">Nuevo rol ADMIN o SUPER_ADMIN.</param>
            /// <param name="positionTitle">Nuevo cargo institucional descriptivo, si se proporciona.</param>
            /// <param name="actorVoterId">Identificador del Súper Administrador que realiza la acción.</param>
            /// <param name="ipAddress">Dirección IP asociada al registro de auditoría.</param>
            /// <param name="ct">Token para cancelar la operación.</param>
            /// <returns><see langword="true"/> si se actualizó una cuenta; de lo contrario, <see langword="false"/>.</returns>
    public async Task<bool> UpdateAsync(int accountId, string document, string fullName, string contactEmail,
        string roleName, string? positionTitle, int actorVoterId, string ipAddress, CancellationToken ct = default)
    {
        var account = await GetAdministrativeAccountAsync(accountId, ct);
        if (account == null) return false;

        var normalizedDocument = NormalizeRequired(document, "documento");
        var normalizedRole = NormalizeRole(roleName);
        var documentHash = _authService.HashDocument(normalizedDocument);
        if (await _context.Voters.AnyAsync(v => v.Id != account.Id && v.DocumentHash == documentHash, ct))
            throw new InvalidOperationException("Ya existe una cuenta con ese documento.");

        if (account.Role.Name == "SUPER_ADMIN" && normalizedRole != "SUPER_ADMIN" &&
            await CountActiveSuperAdminsAsync(ct) <= 1)
            throw new InvalidOperationException("No se puede retirar el último SUPER_ADMIN activo.");

        var oldRole = account.Role.Name;
        account.DocumentHash = documentHash;
        account.EncryptedDocument = _encryptionService.Encrypt(normalizedDocument);
        account.FullName = NormalizeRequired(fullName, "nombre");
        account.ContactEmail = NormalizeEmail(contactEmail);
        account.RoleId = await GetRoleIdAsync(normalizedRole, ct);
        account.PositionTitle = NormalizeOptional(positionTitle);
        await _context.SaveChangesAsync(ct);

        if (account.Status != "ACTIVO")
        {
            _activeSessionStore.InvalidateSession((uint)accountId, SessionInvalidationReason.AccountDisabledOrDeleted);
        }

        await _auditService.LogAsync("ADMIN_ACCOUNT_UPDATED", actorVoterId, "users", accountId,
            "role_id", oldRole, normalizedRole, null, ipAddress);
        return true;
    }

    /// <summary>
    /// Marca una cuenta como eliminada, impide la autoeliminación o dejar cero SUPER_ADMIN activos,
    /// e invalida la sesión de la cuenta (RF-M09-01, RN-14).
    /// </summary>
    /// <param name="accountId">Identificador de la cuenta que se eliminará.</param>
    /// <param name="actorVoterId">Identificador del Súper Administrador que realiza la acción.</param>
    /// <param name="ipAddress">Dirección IP asociada al registro de auditoría.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns><see langword="true"/> si se eliminó la cuenta; de lo contrario, <see langword="false"/>.</returns>
    public async Task<bool> SoftDeleteAsync(int accountId, int actorVoterId, string ipAddress, CancellationToken ct = default)
    {
        var account = await GetAdministrativeAccountAsync(accountId, ct);
        if (account == null || account.Status == "ELIMINADO") return false;
        if (account.Id == (uint)actorVoterId)
            throw new InvalidOperationException("No puede eliminar su propia cuenta.");
        if (account.Role.Name == "SUPER_ADMIN" && await CountActiveSuperAdminsAsync(ct) <= 1)
            throw new InvalidOperationException("No se puede eliminar el último SUPER_ADMIN activo.");

        account.Status = "ELIMINADO";
        account.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _activeSessionStore.InvalidateSession((uint)accountId, SessionInvalidationReason.AccountDisabledOrDeleted);

        await _auditService.LogAsync("ADMIN_ACCOUNT_DELETED", actorVoterId, "users", accountId,
            "status", "ACTIVO", "ELIMINADO", null, ipAddress);
        return true;
    }

    /// <summary>
    /// Reactiva una cuenta administrativa previamente eliminada y registra la restauración (RF-M09-01).
    /// </summary>
    /// <param name="accountId">Identificador de la cuenta que se restaurará.</param>
    /// <param name="actorVoterId">Identificador del Súper Administrador que realiza la acción.</param>
    /// <param name="ipAddress">Dirección IP asociada al registro de auditoría.</param>
    /// <param name="ct">Token para cancelar la operación.</param>
    /// <returns><see langword="true"/> si se restauró la cuenta; de lo contrario, <see langword="false"/>.</returns>
    public async Task<bool> RestoreAsync(int accountId, int actorVoterId, string ipAddress, CancellationToken ct = default)
    {
        var account = await GetAdministrativeAccountAsync(accountId, ct);
        if (account == null || account.Status != "ELIMINADO") return false;

        account.Status = "ACTIVO";
        account.DeletedAt = null;
        await _context.SaveChangesAsync(ct);
        await _auditService.LogAsync("ADMIN_ACCOUNT_RESTORED", actorVoterId, "users", accountId,
            "status", "ELIMINADO", "ACTIVO", null, ipAddress);
        return true;
    }

    private async Task<Voter?> GetAdministrativeAccountAsync(int accountId, CancellationToken ct) =>
        await _context.Voters.Include(v => v.Role).SingleOrDefaultAsync(v => v.Id == (uint)accountId &&
            (v.Role.Name == "ADMIN" || v.Role.Name == "SUPER_ADMIN"), ct);

    private async Task<byte> GetRoleIdAsync(string roleName, CancellationToken ct)
    {
        var role = await _context.Roles.SingleOrDefaultAsync(r => r.Name == roleName, ct)
            ?? throw new InvalidOperationException("El rol seleccionado no existe.");
        return role.Id;
    }

    private Task<int> CountActiveSuperAdminsAsync(CancellationToken ct) =>
        _context.Voters.Include(v => v.Role).CountAsync(v => v.Role.Name == "SUPER_ADMIN" && v.Status == "ACTIVO", ct);

    private string DecryptDocument(string value)
    {
        try { return _encryptionService.Decrypt(value); }
        catch { return "N/A"; }
    }

    private static string NormalizeRequired(string? value, string field)
    {
        var result = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(result)) throw new ArgumentException($"El {field} es obligatorio.");
        if (field == "nombre" && !System.Text.RegularExpressions.Regex.IsMatch(result, @"^[\p{L}]+(?:[ '\-][\p{L}]+)*$"))
            throw new ArgumentException("El nombre solo puede contener letras, espacios, guiones o apóstrofes.");
        if (field == "documento" && !result.All(char.IsDigit))
            throw new ArgumentException("El documento solo debe contener números.");
        return result;
    }

    private static string NormalizeEmail(string? value)
    {
        var result = NormalizeRequired(value, "correo");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(result))
            throw new ArgumentException("El correo no tiene un formato válido.");
        return result;
    }

    private static string NormalizeRole(string? value)
    {
        var result = NormalizeRequired(value, "rol").ToUpperInvariant();
        if (!AllowedRoles.Contains(result)) throw new ArgumentException("El rol debe ser ADMIN o SUPER_ADMIN.");
        return result;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
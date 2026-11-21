using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

/// <summary>
/// Resultado de la auto-migración y reparación de documentos semilla al inicio de la aplicación.
/// </summary>
public record SeedMigrationResult(int UsersMigrated, int WhitelistMigrated)
{
    public int TotalMigrated => UsersMigrated + WhitelistMigrated;
}

/// <summary>
/// Lógica de auto-migración y reparación de documentos semilla en texto plano o corruptos (con prefijo PENDIENTE_CIFRAR:).
/// </summary>
public static class SeedDocumentMigration
{
    public const string PendingEncryptionPrefix = "PENDIENTE_CIFRAR:";
    public const string PendingPrefix = PendingEncryptionPrefix;

    /// <summary>
    /// Ejecuta la migración e idempotencia de documentos semilla para las tablas 'users' (Voters) y 'census_whitelist' (CensusWhitelists).
    /// </summary>
    public static SeedMigrationResult MigrateSeedDocuments(
        WahlMiraiDbContext db,
        IDocumentEncryptionService enc,
        IAuthService? authService = null,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(enc);

        string ComputeHash(string doc) =>
            authService != null ? authService.HashDocument(doc) : ComputeSha256(doc);

        int usersMigrated = 0;
        int whitelistMigrated = 0;

        var voters = db.Voters.ToList();
        foreach (var voter in voters)
        {
            if (TryMigrateOrRepairDocument(
                voter.EncryptedDocument,
                voter.DocumentHash,
                "users",
                voter.Id,
                enc,
                ComputeHash,
                logger,
                out var newCipher))
            {
                voter.EncryptedDocument = newCipher;
                usersMigrated++;
            }
        }

        var whitelists = db.CensusWhitelists.ToList();
        foreach (var entry in whitelists)
        {
            if (TryMigrateOrRepairDocument(
                entry.EncryptedDocument,
                entry.DocumentHash,
                "census_whitelist",
                entry.Id,
                enc,
                ComputeHash,
                logger,
                out var newCipher))
            {
                entry.EncryptedDocument = newCipher;
                whitelistMigrated++;
            }
        }

        if (usersMigrated > 0 || whitelistMigrated > 0)
        {
            db.SaveChanges();
            logger?.LogInformation(
                "[Startup] Migración automática de documentos: {UsersCount} en users, {WhitelistCount} en census_whitelist migrado(s)/reparado(s).",
                usersMigrated,
                whitelistMigrated);
        }
        else
        {
            logger?.LogInformation(
                "[Startup] Migración automática de documentos: 0 en users, 0 en census_whitelist (todos los documentos ya estaban cifrados correctamente).");
        }

        return new SeedMigrationResult(usersMigrated, whitelistMigrated);
    }

    private static bool TryMigrateOrRepairDocument(
        string? stored,
        string? storedHash,
        string tableName,
        object rowId,
        IDocumentEncryptionService enc,
        Func<string, string> computeHash,
        ILogger? logger,
        out string newCipher)
    {
        newCipher = string.Empty;
        if (string.IsNullOrWhiteSpace(stored))
        {
            return false;
        }

        string decrypted = enc.Decrypt(stored);

        // Caso 1: Texto plano (no descifrable por Unprotect, Decrypt retorna el mismo valor)
        if (decrypted == stored)
        {
            if (stored.StartsWith(PendingEncryptionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var cleanDoc = stored.Substring(PendingEncryptionPrefix.Length);
                if (string.IsNullOrWhiteSpace(cleanDoc))
                {
                    logger?.LogWarning(
                        "[Startup] Fila en {Table} Id={Id} con prefijo sin documento. Omitiendo migración.",
                        tableName, rowId);
                    return false;
                }

                var calculatedHash = computeHash(cleanDoc);
                if (!string.Equals(calculatedHash, storedHash, StringComparison.OrdinalIgnoreCase))
                {
                    logger?.LogWarning(
                        "[Startup] Integridad fallida al migrar documento con prefijo en {Table} Id={Id}: el hash calculado no coincide con document_hash. Fila omitida.",
                        tableName, rowId);
                    return false;
                }

                newCipher = enc.Encrypt(cleanDoc);
                return true;
            }
            else
            {
                // Texto plano sin prefijo: comportamiento existente exacto (cifrar si estaba en claro)
                newCipher = enc.Encrypt(stored);
                return true;
            }
        }
        else
        {
            // Caso 2: Texto descifrable (decrypted != stored).
            // Si el texto descifrado inicia con el prefijo, la fila fue corrompida previamente.
            if (decrypted.StartsWith(PendingEncryptionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var cleanDoc = decrypted.Substring(PendingEncryptionPrefix.Length);
                if (string.IsNullOrWhiteSpace(cleanDoc))
                {
                    logger?.LogWarning(
                        "[Startup] Fila corrompida en {Table} Id={Id} con prefijo sin documento. Omitiendo reparación.",
                        tableName, rowId);
                    return false;
                }

                var calculatedHash = computeHash(cleanDoc);
                if (!string.Equals(calculatedHash, storedHash, StringComparison.OrdinalIgnoreCase))
                {
                    logger?.LogWarning(
                        "[Startup] Integridad fallida al reparar documento corrupto en {Table} Id={Id}: el hash calculado no coincide con document_hash. Fila omitida.",
                        tableName, rowId);
                    return false;
                }

                newCipher = enc.Encrypt(cleanDoc);
                return true;
            }

            // Caso 3: Ya estaba cifrado correctamente sin prefijo (idempotencia)
            return false;
        }
    }

    private static string ComputeSha256(string document)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(document));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}

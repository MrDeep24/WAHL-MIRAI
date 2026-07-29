using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace WahlMirai.Web.Services;

/// <summary>
/// Implementación de <see cref="IDocumentEncryptionService"/> usando la infraestructura
/// de <see cref="IDataProtectionProvider"/> de ASP.NET Core.
///
/// El purpose string "WahlMirai.DocumentEncryption.v1" está versionado de forma deliberada:
/// si en el futuro se necesita rotar el esquema de cifrado, se crea una v2 y se migran los datos.
/// NUNCA cambiar este string en una instancia en producción sin migrar los datos primero;
/// de lo contrario todos los documentos cifrados quedarán irrecuperables.
/// </summary>
public class DocumentEncryptionService : IDocumentEncryptionService
{
    private const string Purpose = "WahlMirai.DocumentEncryption.v1";

    private readonly IDataProtector _protector;
    private readonly ILogger<DocumentEncryptionService> _logger;

    public DocumentEncryptionService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DocumentEncryptionService> logger)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Encrypt(string plainDocument)
    {
        if (string.IsNullOrEmpty(plainDocument))
            throw new ArgumentNullException(nameof(plainDocument),
                "El documento en texto plano no puede ser nulo o vacío para cifrar.");

        return _protector.Protect(plainDocument);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch (CryptographicException ex)
        {
            // TODO (post-migración): Eliminar este bloque catch una vez que todos los registros
            // hayan sido cifrados con el script --migrate-documents. Este fallback es EXCLUSIVAMENTE
            // para el período de transición en que algunos registros aún tienen el documento en
            // texto plano. Cuando se elimine, los errores criptográficos se propagarán normalmente.
            _logger.LogWarning(ex,
                "No se pudo descifrar el valor de encrypted_document (longitud: {Len}). " +
                "Se retorna el valor tal cual — esto indica que el registro aún NO ha sido migrado al esquema de cifrado. " +
                "Ejecuta el comando --migrate-documents para completar la migración.",
                cipherText.Length);

            return cipherText;
        }
    }
}

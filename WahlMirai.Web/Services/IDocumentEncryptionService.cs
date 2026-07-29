namespace WahlMirai.Web.Services;

/// <summary>
/// Servicio de cifrado/descifrado del documento de identidad almacenado en <c>voters.encrypted_document</c>.
/// Usa ASP.NET Core Data Protection API internamente; no requiere gestión manual de claves.
/// </summary>
public interface IDocumentEncryptionService
{
    /// <summary>
    /// Cifra el documento de identidad en texto plano y devuelve el texto cifrado
    /// listo para persistir en <c>voters.encrypted_document</c>.
    /// </summary>
    /// <param name="plainDocument">Número de documento en texto plano.</param>
    /// <returns>Cadena cifrada (Base64-URL segura, producida por Data Protection API).</returns>
    string Encrypt(string plainDocument);

    /// <summary>
    /// Descifra el valor almacenado en <c>voters.encrypted_document</c> y devuelve el
    /// documento original en texto plano.
    /// </summary>
    /// <param name="cipherText">Valor cifrado tal como está en la base de datos.</param>
    /// <returns>
    /// Documento en texto plano. Durante la transición (migración de datos), si el valor
    /// almacenado todavía está en texto plano y el descifrado falla con una excepción
    /// criptográfica, se devuelve el valor tal cual como fallback temporal con un aviso de log.
    /// </returns>
    /// <remarks>
    /// IMPORTANTE: El fallback de retorno del valor original ante CryptographicException es una
    /// salvaguarda SOLO para el período de migración. Una vez que todos los registros hayan sido
    /// migrados con el script <c>--migrate-documents</c>, este fallback debe eliminarse para que
    /// los errores criptográficos sean propagados normalmente.
    /// TODO (post-migración): Eliminar el bloque catch y dejar que la excepción se propague.
    /// </remarks>
    string Decrypt(string cipherText);
}

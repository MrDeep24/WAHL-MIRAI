// ============================================================
// Archivo:       EmailRetryPolicy.cs
// Módulo:        Servicio compartido de notificaciones
// Desarrollador: Kevin
// Descripción:   Centraliza los tipos de correo con contraseña y
//                la política de reintento manual.
// Requerimientos: RN-9
// ============================================================
namespace WahlMirai.Web.Services;

/// <summary>Centraliza los tipos de correo que transportan contraseñas y su política de reintento manual.</summary>
public static class EmailRetryPolicy
{
    // NOTA: Esta lista es compartida por el worker al retirar secretos y por la política de reintento manual.
    private static readonly HashSet<EmailType> PasswordCarryingTypes = new()
    {
        EmailType.RECUPERACION_ACCESO,
        EmailType.REASIGNACION_ADMIN
    };

    /// <summary>Indica si el tipo de correo transporta una contraseña.</summary>
    /// <param name="emailType">Tipo de notificación.</param>
    /// <returns><see langword="true"/> si el tipo transporta una contraseña.</returns>
    public static bool IsPasswordCarryingType(EmailType emailType)
    {
        return PasswordCarryingTypes.Contains(emailType);
    }

    /// <summary>Indica si el tipo de correo transporta una contraseña según su valor persistido.</summary>
    /// <param name="emailType">Valor del tipo de notificación almacenado en la cola.</param>
    /// <returns><see langword="true"/> si el valor corresponde a un tipo que transporta una contraseña.</returns>
    public static bool IsPasswordCarryingType(string? emailType)
    {
        return Enum.TryParse<EmailType>(emailType, out var parsedType) && IsPasswordCarryingType(parsedType);
    }

    /// <summary>Devuelve los nombres de los tipos que transportan contraseñas para consultas traducibles por EF.</summary>
    /// <returns>Una copia de los nombres de tipo, segura para capturar en una consulta.</returns>
    public static string[] GetPasswordCarryingTypeNames()
    {
        return PasswordCarryingTypes.Select(emailType => emailType.ToString()).ToArray();
    }

    /// <summary>Determina si una fila admite reintento manual sin perder una contraseña efímera.</summary>
    /// <param name="status">Estado actual de la fila.</param>
    /// <param name="emailType">Tipo de notificación persistido.</param>
    /// <returns><see langword="true"/> únicamente para filas FALLIDO que no transportan contraseñas.</returns>
    public static bool CanRetryManually(string? status, string? emailType)
    {
        return string.Equals(status, "FALLIDO", StringComparison.Ordinal) && !IsPasswordCarryingType(emailType);
    }
}
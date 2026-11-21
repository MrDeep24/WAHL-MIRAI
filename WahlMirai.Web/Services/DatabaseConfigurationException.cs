using System;

namespace WahlMirai.Web.Services;

/// <summary>
/// Excepción lanzada cuando la configuración del servidor de base de datos es inválida o está ausente.
/// </summary>
public class DatabaseConfigurationException : Exception
{
    public DatabaseConfigurationException(string message) : base(message)
    {
    }

    public DatabaseConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

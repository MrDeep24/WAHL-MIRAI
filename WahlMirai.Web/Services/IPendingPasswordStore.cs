// ============================================================
// Archivo:       IPendingPasswordStore.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define e implementa el almacenamiento temporal en memoria
//                de claves en claro asociadas a notificaciones pendientes.
// Requerimientos: Contrato de apoyo para RF-M01-02, RF-M07-02 y RF-M09-01.
// ============================================================
using System.Collections.Concurrent;

namespace WahlMirai.Web.Services;

/// <summary>Contrato para conservar y retirar temporalmente claves ligadas a una notificación de correo.</summary>
public interface IPendingPasswordStore
{
    /// <summary>Asocia una clave temporal al identificador de la notificación pendiente.</summary>
    /// <param name="emailQueueId">Identificador de la fila de la cola de correo.</param>
    /// <param name="plainTextPassword">Contraseña en texto plano que usará el proceso de envío.</param>
    void StorePassword(ulong emailQueueId, string plainTextPassword);

    /// <summary>Intenta recuperar la clave temporal asociada a la notificación.</summary>
    /// <param name="emailQueueId">Identificador de la fila de la cola de correo.</param>
    /// <param name="plainTextPassword">Recibe la clave si se encontró; de lo contrario, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> si había una clave asociada.</returns>
    bool TryGetPassword(ulong emailQueueId, out string? plainTextPassword);

    /// <summary>Retira del almacén la clave temporal asociada a la notificación.</summary>
    /// <param name="emailQueueId">Identificador de la fila de la cola de correo.</param>
    void RemovePassword(ulong emailQueueId);
}

/// <summary>Implementa con un diccionario concurrente el almacén temporal de contraseñas pendientes.</summary>
public class PendingPasswordStore : IPendingPasswordStore
{
    private readonly ConcurrentDictionary<ulong, string> _store = new();

    /// <inheritdoc />
    public void StorePassword(ulong emailQueueId, string plainTextPassword)
    {
        _store[emailQueueId] = plainTextPassword;
    }

    /// <inheritdoc />
    public bool TryGetPassword(ulong emailQueueId, out string? plainTextPassword)
    {
        return _store.TryGetValue(emailQueueId, out plainTextPassword);
    }

    /// <inheritdoc />
    public void RemovePassword(ulong emailQueueId)
    {
        _store.TryRemove(emailQueueId, out _);
    }
}

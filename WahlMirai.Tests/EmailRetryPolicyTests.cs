// ============================================================
// Archivo:       EmailRetryPolicyTests.cs
// Módulo:        Pruebas de la política de reintento de correo
// Desarrollador: Kevin
// Descripción:   Verifica que los tipos con contraseña no admitan
//                reintento manual en ningún estado.
// Requerimientos: RN-9
// ============================================================
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

/// <summary>Pruebas unitarias de la política pura de reintento manual.</summary>
public class EmailRetryPolicyTests
{
    public static TheoryData<string, string, bool> StatusAndTypeCases => new()
    {
        { "FALLIDO", nameof(EmailType.RECUPERACION_ACCESO), false },
        { "FALLIDO", nameof(EmailType.REASIGNACION_ADMIN), false },
        { "FALLIDO", nameof(EmailType.CAMBIO_PERFIL), true },
        { "FALLIDO", nameof(EmailType.RESPUESTA_PQR), true },
        { "FALLIDO", nameof(EmailType.CANDIDATURA_APROBADA), true },
        { "FALLIDO", nameof(EmailType.CANDIDATURA_RECHAZADA), true },
        { "PENDIENTE", nameof(EmailType.RECUPERACION_ACCESO), false },
        { "PENDIENTE", nameof(EmailType.REASIGNACION_ADMIN), false },
        { "PENDIENTE", nameof(EmailType.CAMBIO_PERFIL), false },
        { "PENDIENTE", nameof(EmailType.RESPUESTA_PQR), false },
        { "PENDIENTE", nameof(EmailType.CANDIDATURA_APROBADA), false },
        { "PENDIENTE", nameof(EmailType.CANDIDATURA_RECHAZADA), false },
        { "ENVIADO", nameof(EmailType.RECUPERACION_ACCESO), false },
        { "ENVIADO", nameof(EmailType.REASIGNACION_ADMIN), false },
        { "ENVIADO", nameof(EmailType.CAMBIO_PERFIL), false },
        { "ENVIADO", nameof(EmailType.RESPUESTA_PQR), false },
        { "ENVIADO", nameof(EmailType.CANDIDATURA_APROBADA), false },
        { "ENVIADO", nameof(EmailType.CANDIDATURA_RECHAZADA), false }
    };

    /// <summary>Comprueba cada estado combinado con todos los tipos de correo definidos.</summary>
    [Theory]
    [MemberData(nameof(StatusAndTypeCases))]
    public void CanRetryManually_UsesStatusAndPasswordType(string status, string emailType, bool expected)
    {
        Assert.Equal(expected, EmailRetryPolicy.CanRetryManually(status, emailType));
    }
}
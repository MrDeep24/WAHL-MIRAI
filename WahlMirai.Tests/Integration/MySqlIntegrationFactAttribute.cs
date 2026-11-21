using System;
using Xunit;

namespace WahlMirai.Tests.Integration;

/// <summary>
/// Atributo Fact para pruebas de integración contra MySQL/MariaDB real.
/// Si el servidor no está disponible o no está definida WAHLMIRAI_TEST_DB,
/// el test se omite dinámicamente sin provocar fallo en la suite.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MySqlIntegrationFactAttribute : FactAttribute
{
    public MySqlIntegrationFactAttribute()
    {
        if (!MySqlTestEnvironment.IsServerAvailable)
        {
            Skip = MySqlTestEnvironment.SkipReason;
        }
    }
}

/// <summary>
/// Atributo Theory para pruebas de integración parametrizadas contra MySQL/MariaDB real.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MySqlIntegrationTheoryAttribute : TheoryAttribute
{
    public MySqlIntegrationTheoryAttribute()
    {
        if (!MySqlTestEnvironment.IsServerAvailable)
        {
            Skip = MySqlTestEnvironment.SkipReason;
        }
    }
}

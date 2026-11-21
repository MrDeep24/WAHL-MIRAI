using Xunit;

namespace WahlMirai.Tests.Integration;

/// <summary>
/// Definición de colección para pruebas de integración contra MySQL/MariaDB real.
/// Ejecuta las pruebas de forma secuencial compartiendo el esquema de base temporal.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class MySqlDatabaseCollection : ICollectionFixture<MySqlDatabaseFixture>
{
    public const string Name = "MySqlDatabaseCollection";
}

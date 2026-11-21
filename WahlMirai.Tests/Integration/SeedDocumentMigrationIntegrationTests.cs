using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests.Integration;

[Collection(MySqlDatabaseCollection.Name)]
public class SeedDocumentMigrationIntegrationTests : IAsyncLifetime
{
    private readonly MySqlDatabaseFixture _fixture;

    public SeedDocumentMigrationIntegrationTests(MySqlDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseStateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [MySqlIntegrationFact]
    public void MigrateSeedDocuments_RealDatabase_MigratesAllDdlSeedsAndIsIdempotent()
    {
        using var context = _fixture.CreateDbContext();
        var enc = new DocumentEncryptionService(
            new EphemeralDataProtectionProvider(),
            NullLogger<DocumentEncryptionService>.Instance);

        // La base de datos creada a partir del DDL contiene las semillas con prefijo PENDIENTE_CIFRAR:
        var firstRun = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(2, firstRun.UsersMigrated);
        Assert.Equal(1, firstRun.WhitelistMigrated);
        Assert.Equal(3, firstRun.TotalMigrated);

        // Verificamos que los registros en la base relacional real están ahora cifrados limpiamente
        var superAdmin = context.Voters.First(v => v.DocumentHash == "9e1f341dff9161b69d6afc54140ea30e44676ed3600dc98f507f0678fe64e320");
        Assert.False(superAdmin.EncryptedDocument.StartsWith(SeedDocumentMigration.PendingEncryptionPrefix));
        Assert.Equal("1020304050", enc.Decrypt(superAdmin.EncryptedDocument));

        var whitelist = context.CensusWhitelists.First(w => w.DocumentHash == "16ad24eeedca3411b3ab6e848d6789bdb0f6c864edb0b2b60e361a7277f69fbd");
        Assert.False(whitelist.EncryptedDocument.StartsWith(SeedDocumentMigration.PendingEncryptionPrefix));
        Assert.Equal("1015998877", enc.Decrypt(whitelist.EncryptedDocument));

        var elector = context.Voters.First(v => v.DocumentHash == "c7dc7b2f9562560342d1b7ba78febecc04199365383ea3452c44b9266a09f758");
        Assert.False(elector.EncryptedDocument.StartsWith(SeedDocumentMigration.PendingEncryptionPrefix));
        Assert.Equal("1001234567", enc.Decrypt(elector.EncryptedDocument));

        // Segunda corrida: idempotente, 0 migraciones
        var secondRun = SeedDocumentMigration.MigrateSeedDocuments(context, enc);
        Assert.Equal(0, secondRun.UsersMigrated);
        Assert.Equal(0, secondRun.WhitelistMigrated);
        Assert.Equal(0, secondRun.TotalMigrated);
    }
}

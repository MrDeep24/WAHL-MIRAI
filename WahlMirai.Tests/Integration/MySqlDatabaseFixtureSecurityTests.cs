using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace WahlMirai.Tests.Integration;

public class MySqlDatabaseFixtureSecurityTests
{
    [Theory]
    [InlineData("wahl_mirai_db")]
    [InlineData("WAHL_MIRAI_DB")]
    public void ValidateTestDatabaseName_ThrowsWhenPointingToProductionDatabase(string prodDbName)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            MySqlDatabaseFixture.ValidateTestDatabaseName(prodDbName));

        Assert.Contains("wahl_mirai_db", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("seguridad", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("production_db")]
    [InlineData("wahl_mirai")]
    [InlineData("test_db")]
    [InlineData("my_wahl_mirai_test_db")]
    public void ValidateTestDatabaseName_ThrowsWhenNameDoesNotStartWithMandatoryPrefix(string invalidName)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            MySqlDatabaseFixture.ValidateTestDatabaseName(invalidName));

        Assert.Contains("prefijo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateTestDatabaseName_ThrowsWhenNameIsNullOrEmpty(string? invalidName)
    {
        Assert.Throws<InvalidOperationException>(() =>
            MySqlDatabaseFixture.ValidateTestDatabaseName(invalidName!));
    }

    [Theory]
    [InlineData("wahl_mirai_test_12345")]
    [InlineData("wahl_mirai_test_d4c3f5e9")]
    [InlineData("WAHL_MIRAI_TEST_ABC")]
    public void ValidateTestDatabaseName_SucceedsForValidTemporaryPrefix(string validName)
    {
        var exception = Record.Exception(() => MySqlDatabaseFixture.ValidateTestDatabaseName(validName));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("wahl_mirai_test_abc; DROP DATABASE wahl_mirai_db;")]
    [InlineData("wahl_mirai_test_`db")]
    [InlineData("wahl_mirai_test_db with spaces")]
    [InlineData("wahl_mirai_test_db-with-hyphens")]
    public void ValidateTestDatabaseName_ThrowsOnIllegalCharacters(string invalidName)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            MySqlDatabaseFixture.ValidateTestDatabaseName(invalidName));

        Assert.Contains("caracteres no permitidos", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FixtureInstance_GeneratesNameWithMandatoryPrefix()
    {
        var fixture = new MySqlDatabaseFixture();

        Assert.StartsWith("wahl_mirai_test_", fixture.DatabaseName);
        Assert.NotEqual("wahl_mirai_db", fixture.DatabaseName);

        var exception = Record.Exception(() => MySqlDatabaseFixture.ValidateTestDatabaseName(fixture.DatabaseName));
        Assert.Null(exception);
    }

    [Fact]
    public void SplitScriptStatements_CorrectlyHandlesCommentsAndQuotes()
    {
        string sql = @"
            -- Comentario de línea
            CREATE TABLE `test1` (id INT);
            /* Comentario
               multilínea con ; adentro */
            INSERT INTO `test1` VALUES (1);
            SELECT 'hola; mundo' AS greeting;
        ";

        var statements = MySqlDatabaseFixture.SplitScriptStatements(sql);

        Assert.Equal(3, statements.Count);
        Assert.Contains("CREATE TABLE `test1`", statements[0]);
        Assert.Contains("INSERT INTO `test1`", statements[1]);
        Assert.Contains("SELECT 'hola; mundo'", statements[2]);
    }

    [Fact]
    public void StripLeadingComments_CorrectlyRemovesLeadingComments()
    {
        string sqlWithComments = @"
            -- Comentario 1
            # Comentario tipo hash
            /* Comentario multilínea
               con varias líneas */
            CREATE TABLE `test` (id INT);
        ";

        var clean = MySqlDatabaseFixture.StripLeadingComments(sqlWithComments);

        Assert.StartsWith("CREATE TABLE `test`", clean);
    }

    [Theory]
    [InlineData("DROP DATABASE IF EXISTS `wahl_mirai_db`;")]
    [InlineData("-- Comentario antes\nDROP DATABASE `wahl_mirai_db`;")]
    [InlineData("/* Bloque */ CREATE DATABASE `wahl_mirai_db`;")]
    [InlineData("# Hash comentario\nUSE `wahl_mirai_db`;")]
    [InlineData("CREATE SCHEMA `test_schema`;")]
    public void IsDatabaseLevelCommand_CorrectlyIdentifiesDropCreateAndUse_EvenWithLeadingComments(string sql)
    {
        Assert.True(MySqlDatabaseFixture.IsDatabaseLevelCommand(sql));
    }

    [Theory]
    [InlineData("CREATE TABLE `users` (id INT);")]
    [InlineData("-- Comentario\nINSERT INTO `roles` VALUES (1, 'ADMIN');")]
    [InlineData("SELECT * FROM `vw_vote_counts`;")]
    public void IsDatabaseLevelCommand_ReturnsFalseForNormalDdlAndDml(string sql)
    {
        Assert.False(MySqlDatabaseFixture.IsDatabaseLevelCommand(sql));
    }

    [Fact]
    public void AssertSafeDdlStatement_ThrowsWhenStatementContainsProductionDbOrUnsafeDrop()
    {
        string testDb = "wahl_mirai_test_12345678901234567890123456789012";

        // Caso 1: DROP DATABASE de otra base
        var ex1 = Assert.Throws<InvalidOperationException>(() =>
            MySqlDatabaseFixture.AssertSafeDdlStatement("DROP DATABASE IF EXISTS `other_db`;", testDb));
        Assert.Contains("DROP DATABASE", ex1.Message);

        // Caso 2: Referencia a wahl_mirai_db
        var ex2 = Assert.Throws<InvalidOperationException>(() =>
            MySqlDatabaseFixture.AssertSafeDdlStatement("SELECT * FROM `wahl_mirai_db`.`users`;", testDb));
        Assert.Contains("wahl_mirai_db", ex2.Message);
    }

    [Fact]
    public void AssertSafeDdlStatement_AllowsSafeStatementsInTestDb()
    {
        string testDb = "wahl_mirai_test_12345678901234567890123456789012";

        // DROP DATABASE de SU propia base temporal es seguro
        var exDrop = Record.Exception(() =>
            MySqlDatabaseFixture.AssertSafeDdlStatement($"DROP DATABASE IF EXISTS `{testDb}`;", testDb));
        Assert.Null(exDrop);

        // CREATE TABLE seguro
        var exCreate = Record.Exception(() =>
            MySqlDatabaseFixture.AssertSafeDdlStatement("CREATE TABLE `roles` (id INT PRIMARY KEY);", testDb));
        Assert.Null(exCreate);
    }

    [Fact]
    public void DdlScript_WhenSplitAndFiltered_ContainsZeroUnsafeStatementsAndNoProductionDb()
    {
        string testDb = "wahl_mirai_test_12345678901234567890123456789012";
        string ddlPath = MySqlDatabaseFixture.ResolveDdlPath();
        string ddlContent = File.ReadAllText(ddlPath);

        var statements = MySqlDatabaseFixture.SplitScriptStatements(ddlContent);
        var filteredStatements = new List<string>();

        foreach (var stmt in statements)
        {
            var trimmed = stmt.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            if (MySqlDatabaseFixture.IsDatabaseLevelCommand(trimmed))
            {
                continue;
            }

            var sanitized = trimmed.Replace(MySqlDatabaseFixture.ProductionDatabaseName, testDb, StringComparison.OrdinalIgnoreCase);
            MySqlDatabaseFixture.AssertSafeDdlStatement(sanitized, testDb);
            filteredStatements.Add(sanitized);
        }

        Assert.NotEmpty(filteredStatements);

        // Ninguna sentencia debe contener 'DROP DATABASE' ni 'wahl_mirai_db'
        foreach (var stmt in filteredStatements)
        {
            var clean = MySqlDatabaseFixture.StripLeadingComments(stmt);
            Assert.DoesNotContain("DROP DATABASE", clean, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CREATE DATABASE", clean, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("USE `", clean, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(MySqlDatabaseFixture.ProductionDatabaseName, clean, StringComparison.OrdinalIgnoreCase);
        }

        // Verificar que las tablas principales y las 5 vistas están presentes en el conjunto a ejecutar
        Assert.Contains(filteredStatements, s => s.Contains("CREATE TABLE `roles`"));
        Assert.Contains(filteredStatements, s => s.Contains("CREATE TABLE `users`"));
        Assert.Contains(filteredStatements, s => s.Contains("vw_vote_counts"));
        Assert.Contains(filteredStatements, s => s.Contains("vw_active_census"));
        Assert.Contains(filteredStatements, s => s.Contains("vw_pending_whitelist"));
        Assert.Contains(filteredStatements, s => s.Contains("vw_pending_email_queue"));
        Assert.Contains(filteredStatements, s => s.Contains("vw_pending_candidacies"));
    }
}

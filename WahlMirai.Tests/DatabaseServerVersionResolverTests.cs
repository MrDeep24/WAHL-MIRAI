using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class DatabaseServerVersionResolverTests
{
    [Fact]
    public void ResolveConfiguredVersion_MySql_ReturnsMySqlServerVersion()
    {
        // Act
        var result = DatabaseServerVersionResolver.ResolveConfiguredVersion("MySql", "8.0.36");

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MySqlServerVersion>(result);
        Assert.Equal(new Version(8, 0, 36), result.Version);
    }

    [Fact]
    public void ResolveConfiguredVersion_MariaDb_ReturnsMariaDbServerVersion()
    {
        // Act
        var result = DatabaseServerVersionResolver.ResolveConfiguredVersion("MariaDb", "10.4.32");

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MariaDbServerVersion>(result);
        Assert.Equal(new Version(10, 4, 32), result.Version);
    }

    [Theory]
    [InlineData("mysql", "8.0.36")]
    [InlineData("MYSQL", "8.0.36")]
    [InlineData("mariadb", "10.4.32")]
    [InlineData("MARIADB", "10.4.32")]
    [InlineData("  MariaDb  ", " 10.4.32 ")]
    public void ResolveConfiguredVersion_IsCaseAndWhitespaceInsensitive(string serverType, string serverVersion)
    {
        // Act
        var result = DatabaseServerVersionResolver.ResolveConfiguredVersion(serverType, serverVersion);

        // Assert
        Assert.NotNull(result);
        if (serverType.Trim().Equals("mariadb", StringComparison.OrdinalIgnoreCase))
        {
            Assert.IsType<MariaDbServerVersion>(result);
            Assert.Equal(new Version(10, 4, 32), result.Version);
        }
        else
        {
            Assert.IsType<MySqlServerVersion>(result);
            Assert.Equal(new Version(8, 0, 36), result.Version);
        }
    }

    [Theory]
    [InlineData(null, "8.0.36")]
    [InlineData("", "8.0.36")]
    [InlineData("   ", "8.0.36")]
    public void ResolveConfiguredVersion_EmptyType_AssumesMySql(string? serverType, string serverVersion)
    {
        // Act
        var result = DatabaseServerVersionResolver.ResolveConfiguredVersion(serverType, serverVersion);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MySqlServerVersion>(result);
        Assert.Equal(new Version(8, 0, 36), result.Version);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("MySql", "")]
    [InlineData("MariaDb", null)]
    [InlineData("MySql", "   ")]
    public void ResolveConfiguredVersion_EmptyVersion_ReturnsNullWithoutConnecting(string? serverType, string? serverVersion)
    {
        // Act: debe devolver null sin abrir conexión ni lanzar excepción
        var result = DatabaseServerVersionResolver.ResolveConfiguredVersion(serverType, serverVersion);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Postgres")]
    [InlineData("SqlServer")]
    [InlineData("Oracle")]
    [InlineData("SQLite")]
    public void ResolveConfiguredVersion_UnknownServerType_ThrowsDatabaseConfigurationException(string serverType)
    {
        // Act & Assert
        var ex = Assert.Throws<DatabaseConfigurationException>(() =>
            DatabaseServerVersionResolver.ResolveConfiguredVersion(serverType, "8.0.36"));

        Assert.Contains(serverType, ex.Message);
        Assert.Contains("MySql", ex.Message);
        Assert.Contains("MariaDb", ex.Message);
    }

    [Theory]
    [InlineData("invalid-version")]
    [InlineData("8.x")]
    [InlineData("version_10")]
    public void ResolveConfiguredVersion_UnparseableVersion_ThrowsDatabaseConfigurationException(string invalidVersion)
    {
        // Act & Assert
        var ex = Assert.Throws<DatabaseConfigurationException>(() =>
            DatabaseServerVersionResolver.ResolveConfiguredVersion("MySql", invalidVersion));

        Assert.Contains(invalidVersion, ex.Message);
        Assert.Contains("8.0.36", ex.Message);
    }

    [Fact]
    public void Resolve_FromIConfiguration_ResolvesMariaDbCorrectly()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ServerType"] = "MariaDb",
                ["Database:ServerVersion"] = "10.4.32"
            })
            .Build();

        // Act
        var result = DatabaseServerVersionResolver.Resolve(config, "dummy-conn-string");

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MariaDbServerVersion>(result);
        Assert.Equal(new Version(10, 4, 32), result.Version);
    }

    [Theory]
    [InlineData("10.4.32-MariaDB")]
    [InlineData("10.4.32-1:10.4.32+maria~ubu2004")]
    [InlineData("8.0.36-mysql")]
    public void ResolveConfiguredVersion_VersionWithSuffix_StripsSuffixAndParsesCorrectly(string versionWithSuffix)
    {
        // Act
        var result = DatabaseServerVersionResolver.ResolveConfiguredVersion("MariaDb", versionWithSuffix);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<MariaDbServerVersion>(result);
        if (versionWithSuffix.StartsWith("10.4.32"))
        {
            Assert.Equal(new Version(10, 4, 32), result.Version);
        }
        else
        {
            Assert.Equal(new Version(8, 0, 36), result.Version);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_ConfiguredVersionWithMissingConnectionString_ThrowsDatabaseConfigurationException(string? connStr)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ServerType"] = "MariaDb",
                ["Database:ServerVersion"] = "10.4.32"
            })
            .Build();

        // Act & Assert: Incluso si ServerVersion está configurada, una cadena ausente debe lanzar excepción clara
        var ex = Assert.Throws<DatabaseConfigurationException>(() =>
            DatabaseServerVersionResolver.Resolve(config, connStr));

        Assert.Contains("ConnectionStrings:WahlMiraiDb", ex.Message);
        Assert.Contains("vacía", ex.Message);
    }

    [Fact]
    public void Resolve_EmptyVersionAndMissingConnectionString_ThrowsDatabaseConfigurationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ServerType"] = "",
                ["Database:ServerVersion"] = ""
            })
            .Build();

        // Act & Assert
        var ex = Assert.Throws<DatabaseConfigurationException>(() =>
            DatabaseServerVersionResolver.Resolve(config, null));

        Assert.Contains("ConnectionStrings:WahlMiraiDb", ex.Message);
    }
}

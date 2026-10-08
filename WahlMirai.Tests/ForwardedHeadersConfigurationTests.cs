// ============================================================
// Archivo:       ForwardedHeadersConfigurationTests.cs
// Módulo:        Pruebas de Infraestructura HTTP / ForwardedHeaders
// Desarrollador: Kevin
// Descripción:   Pruebas de validación estricta, fail-closed y
//                comportamiento de ForwardedHeadersMiddleware.
// Requerimientos: RN-1, RN-15.
// ============================================================
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WahlMirai.Web.Services;
using Xunit;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace WahlMirai.Tests;

public sealed class ForwardedHeadersConfigurationTests
{
    [Fact]
    public void ConfigureForwardedHeaders_WhenDisabled_DoesNotRegisterOptionsAndBuildReturnsNull()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        var configured = ForwardedHeadersConfiguration.ConfigureForwardedHeaders(services, config);

        Assert.False(configured);
        Assert.Empty(services);
        Assert.Null(ForwardedHeadersConfiguration.BuildOptions(config));
    }

    [Fact]
    public void BuildOptions_WhenEnabledAndBothListsEmpty_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "true"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => ForwardedHeadersConfiguration.BuildOptions(config));
        Assert.Contains("fail-closed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("KnownProxies:0", "not-an-ip")]
    [InlineData("KnownProxies:0", "")]
    [InlineData("KnownNetworks:0", "not-a-cidr")]
    [InlineData("KnownNetworks:0", "192.168.1.1")]
    [InlineData("KnownNetworks:0", "192.168.1.1/33")]
    [InlineData("KnownNetworks:0", "::1/129")]
    [InlineData("KnownNetworks:0", "192.168.1.1/-1")]
    [InlineData("KnownNetworks:0", "999.999.999.999/24")]
    public void BuildOptions_WithInvalidProxyOrNetwork_ThrowsInvalidOperationException(string key, string value)
    {
        var dict = new Dictionary<string, string?>
        {
            ["ForwardedHeaders:Enabled"] = "true",
            [$"ForwardedHeaders:{key}"] = value
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        Assert.Throws<InvalidOperationException>(() => ForwardedHeadersConfiguration.BuildOptions(config));
    }

    [Fact]
    public void BuildOptions_WhenValid_SetsForwardLimit1AndOnlyXffAndXfp()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1",
                ["ForwardedHeaders:KnownNetworks:0"] = "192.168.1.0/24"
            })
            .Build();

        var options = ForwardedHeadersConfiguration.BuildOptions(config);

        Assert.NotNull(options);
        Assert.Equal(1, options.ForwardLimit);
        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
        Assert.False(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedHost));
        Assert.Single(options.KnownProxies);
        Assert.Equal(IPAddress.Parse("10.0.0.1"), options.KnownProxies[0]);
        Assert.Single(options.KnownNetworks);
        Assert.Equal(IPAddress.Parse("192.168.1.0"), options.KnownNetworks[0].Prefix);
        Assert.Equal(24, options.KnownNetworks[0].PrefixLength);
    }

    [Fact]
    public async Task Middleware_UntrustedProxy_DoesNotUpdateRemoteIpOrScheme()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1"
            })
            .Build();

        var options = ForwardedHeadersConfiguration.BuildOptions(config)!;
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.5");
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = "1.2.3.4";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await InvokeMiddlewareAsync(options, context);

        Assert.Equal(IPAddress.Parse("198.51.100.5"), context.Connection.RemoteIpAddress);
        Assert.Equal("http", context.Request.Scheme);
        Assert.False(context.Request.IsHttps);
    }

    [Fact]
    public async Task Middleware_TrustedProxyIp_UpdatesRemoteIpAndScheme()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1"
            })
            .Build();

        var options = ForwardedHeadersConfiguration.BuildOptions(config)!;
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = "1.2.3.4";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await InvokeMiddlewareAsync(options, context);

        Assert.Equal(IPAddress.Parse("1.2.3.4"), context.Connection.RemoteIpAddress);
        Assert.Equal("https", context.Request.Scheme);
        Assert.True(context.Request.IsHttps);
    }

    [Fact]
    public async Task Middleware_TrustedNetworkCidr_UpdatesRemoteIpAndScheme()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/24"
            })
            .Build();

        var options = ForwardedHeadersConfiguration.BuildOptions(config)!;
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.42");
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = "1.2.3.4";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await InvokeMiddlewareAsync(options, context);

        Assert.Equal(IPAddress.Parse("1.2.3.4"), context.Connection.RemoteIpAddress);
        Assert.Equal("https", context.Request.Scheme);
        Assert.True(context.Request.IsHttps);
    }

    [Fact]
    public async Task Middleware_TwoEntriesWithForwardLimit1_ConsumesOnlyRightmostEntry()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownProxies:0"] = "10.0.0.1"
            })
            .Build();

        var options = ForwardedHeadersConfiguration.BuildOptions(config)!;
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.195, 70.41.3.18";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await InvokeMiddlewareAsync(options, context);

        // ForwardLimit=1 consume únicamente la entrada del extremo derecho agregada por el proxy confiable
        Assert.Equal(IPAddress.Parse("70.41.3.18"), context.Connection.RemoteIpAddress);
        Assert.Equal("https", context.Request.Scheme);
        Assert.True(context.Request.IsHttps);
    }

    private static async Task InvokeMiddlewareAsync(ForwardedHeadersOptions options, HttpContext context)
    {
        var middleware = new ForwardedHeadersMiddleware(
            next: _ => Task.CompletedTask,
            loggerFactory: NullLoggerFactory.Instance,
            options: Options.Create(options));
        await middleware.Invoke(context);
    }
}

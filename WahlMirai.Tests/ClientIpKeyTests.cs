// ============================================================
// Archivo:       ClientIpKeyTests.cs
// Módulo:        Pruebas de Infraestructura HTTP / Particionamiento IP
// Desarrollador: Kevin
// Descripción:   Pruebas de normalización de claves IP (IPv4 /32,
//                IPv6 /64, IPv4-mapped, scope id y nulos).
// Requerimientos: RF-M01-00, RN-15.
// ============================================================
using System.Net;
using Microsoft.AspNetCore.Http;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public sealed class ClientIpKeyTests
{
    /// <summary>1. null -> "unknown" tanto para IPAddress como para HttpContext.</summary>
    [Fact]
    public void From_NullAddressAndContext_ReturnsUnknown()
    {
        Assert.Equal("unknown", ClientIpKey.From((IPAddress?)null));
        Assert.Equal(ClientIpKey.Unknown, ClientIpKey.From((IPAddress?)null));
        Assert.Equal("unknown", ClientIpKey.From((HttpContext?)null));
    }

    /// <summary>2. IPv4 se retorna inalterada (192.0.2.30).</summary>
    [Fact]
    public void From_IPv4Address_ReturnsUnchangedCanonicalString()
    {
        var ip = IPAddress.Parse("192.0.2.30");
        Assert.Equal("192.0.2.30", ClientIpKey.From(ip));
    }

    /// <summary>3. ::ffff:192.0.2.30 -> "192.0.2.30" (misma clave que IPv4 nativa).</summary>
    [Fact]
    public void From_IPv4MappedToIPv6_MapsToIPv4AndMatchesPlainIPv4()
    {
        var mappedIp = IPAddress.Parse("::ffff:192.0.2.30");
        var plainIp = IPAddress.Parse("192.0.2.30");

        Assert.True(mappedIp.IsIPv4MappedToIPv6);
        Assert.Equal("192.0.2.30", ClientIpKey.From(mappedIp));
        Assert.Equal(ClientIpKey.From(plainIp), ClientIpKey.From(mappedIp));
    }

    /// <summary>4. Dos direcciones IPv6 en el mismo prefijo /64 producen clave idéntica.</summary>
    [Fact]
    public void From_TwoIPv6InSamePrefix64_ProduceIdenticalKey()
    {
        var ip1 = IPAddress.Parse("2001:db8:abcd:0012:1111:2222:3333:4444");
        var ip2 = IPAddress.Parse("2001:db8:abcd:0012:ffff:eeee:dddd:cccc");

        var key1 = ClientIpKey.From(ip1);
        var key2 = ClientIpKey.From(ip2);

        Assert.Equal("2001:db8:abcd:12::/64", key1);
        Assert.Equal(key1, key2);
    }

    /// <summary>5. Dos direcciones IPv6 en diferentes prefijos /64 producen claves distintas.</summary>
    [Fact]
    public void From_TwoIPv6InDifferentPrefix64_ProduceDifferentKeys()
    {
        var ip1 = IPAddress.Parse("2001:db8:abcd:0012::1");
        var ip2 = IPAddress.Parse("2001:db8:abcd:0013::1");

        var key1 = ClientIpKey.From(ip1);
        var key2 = ClientIpKey.From(ip2);

        Assert.NotEqual(key1, key2);
        Assert.Equal("2001:db8:abcd:12::/64", key1);
        Assert.Equal("2001:db8:abcd:13::/64", key2);
    }

    /// <summary>6. Cadena exacta esperada para 2001:db8:1:2::abcd -> "2001:db8:1:2::/64".</summary>
    [Fact]
    public void From_SpecificIPv6Address_ReturnsExactExpectedPrefix()
    {
        var ip = IPAddress.Parse("2001:db8:1:2::abcd");
        Assert.Equal("2001:db8:1:2::/64", ClientIpKey.From(ip));
    }

    /// <summary>7. ::1 y 127.0.0.1 no lanzan excepciones y devuelven las claves documentadas.</summary>
    [Fact]
    public void From_LoopbackAddresses_DoNotThrowAndReturnDocumentedKeys()
    {
        var ipv4Loopback = IPAddress.Parse("127.0.0.1");
        var ipv6Loopback = IPAddress.Parse("::1");

        Assert.Equal("127.0.0.1", ClientIpKey.From(ipv4Loopback));
        Assert.Equal("::/64", ClientIpKey.From(ipv6Loopback));
    }

    /// <summary>8. IPv6 con identificador de ámbito (fe80::1%5) descarta scope id y coincide con versión sin scope.</summary>
    [Fact]
    public void From_IPv6WithScopeId_DropsScopeIdAndMatchesAddressWithoutScope()
    {
        var ipWithScope = IPAddress.Parse("fe80::1%5");
        var ipWithoutScope = IPAddress.Parse("fe80::1");

        var keyWithScope = ClientIpKey.From(ipWithScope);
        var keyWithoutScope = ClientIpKey.From(ipWithoutScope);

        Assert.Equal("fe80::/64", keyWithScope);
        Assert.Equal(keyWithoutScope, keyWithScope);
    }

    /// <summary>9. From(HttpContext) sobre DefaultHttpContext coincide con From(IPAddress).</summary>
    [Fact]
    public void From_HttpContext_MatchesFromIPAddressAndHandlesNullRemoteIp()
    {
        var httpContext = new DefaultHttpContext();
        Assert.Equal("unknown", ClientIpKey.From(httpContext));

        var testIp = IPAddress.Parse("192.0.2.30");
        httpContext.Connection.RemoteIpAddress = testIp;
        Assert.Equal(ClientIpKey.From(testIp), ClientIpKey.From(httpContext));

        var testIpv6 = IPAddress.Parse("2001:db8:1:2::abcd");
        httpContext.Connection.RemoteIpAddress = testIpv6;
        Assert.Equal("2001:db8:1:2::/64", ClientIpKey.From(httpContext));
    }
}

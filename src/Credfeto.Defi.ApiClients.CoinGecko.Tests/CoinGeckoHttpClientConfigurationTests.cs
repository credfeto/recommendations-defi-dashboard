using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.ApiClients.CoinGecko.Tests;

public sealed class CoinGeckoHttpClientConfigurationTests : TestBase
{
    [Fact]
    public static void Configure_WithApiKey_AddsDemoApiKeyHeader()
    {
        using HttpClient client = new();

        CoinGeckoHttpClientConfiguration.Configure(client: client, options: new() { ApiKey = "demo-key" });

        Assert.True(
            client.DefaultRequestHeaders.TryGetValues(
                name: CoinGeckoHttpClientConfiguration.DEMO_PLAN_HEADER,
                values: out IEnumerable<string>? values
            ),
            userMessage: "Demo API key header should be present"
        );
        Assert.Equal(expected: "demo-key", actual: Assert.Single(values));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public static void Configure_WithoutApiKey_OmitsDemoApiKeyHeader(string apiKey)
    {
        using HttpClient client = new();

        CoinGeckoHttpClientConfiguration.Configure(client: client, options: new() { ApiKey = apiKey });

        Assert.False(
            client.DefaultRequestHeaders.Contains(CoinGeckoHttpClientConfiguration.DEMO_PLAN_HEADER),
            userMessage: "Demo API key header should be absent"
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("demo-key")]
    public static void Configure_Always_SetsUserAgent(string apiKey)
    {
        using HttpClient client = new();

        CoinGeckoHttpClientConfiguration.Configure(client: client, options: new() { ApiKey = apiKey });

        Assert.Equal(
            expected: CoinGeckoHttpClientConfiguration.USER_AGENT,
            actual: string.Join(
                separator: ' ',
                values: client.DefaultRequestHeaders.UserAgent.Select(static p => p.ToString())
            )
        );
    }
}

using System.Collections.Generic;
using System.Net.Http;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.ApiClients.Webacy.Tests;

public sealed class WebacyHttpClientConfigurationTests : TestBase
{
    [Fact]
    public static void Configure_WithApiKey_AddsApiKeyHeader()
    {
        using HttpClient client = new();

        WebacyHttpClientConfiguration.Configure(client: client, options: new() { ApiKey = "test-key" });

        Assert.True(
            client.DefaultRequestHeaders.TryGetValues(
                name: WebacyHttpClientConfiguration.API_KEY_HEADER,
                values: out IEnumerable<string>? values
            ),
            userMessage: "API key header should be present"
        );
        Assert.Equal(expected: "test-key", actual: Assert.Single(values));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public static void Configure_WithoutApiKey_OmitsApiKeyHeader(string apiKey)
    {
        using HttpClient client = new();

        WebacyHttpClientConfiguration.Configure(client: client, options: new() { ApiKey = apiKey });

        Assert.False(
            client.DefaultRequestHeaders.Contains(WebacyHttpClientConfiguration.API_KEY_HEADER),
            userMessage: "API key header should be absent"
        );
    }
}

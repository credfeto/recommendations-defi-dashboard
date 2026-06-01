using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.ApiClients.DefiLlama;
using Credfeto.Defi.Server.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class DefiLlamaHacksClientTests : TestBase
{
    private static DefiLlamaHacksClient CreateClient(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<DefiLlamaHacksClient> logger = GetSubstitute<ILogger<DefiLlamaHacksClient>>();

        return new DefiLlamaHacksClient(httpClientFactory: factory, logger: logger);
    }

    [Fact]
    public async Task FetchHacksAsync_SuccessResponse_ReturnsParsedHacksAsync()
    {
        const string JSON =
            """[{"name":"Protocol X","date":1700000000,"amount":1000000,"classification":"Exploit","technique":"Flash Loan","source":"https://example.com"}]""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        DefiLlamaHacksClient client = CreateClient(httpClient);

        IReadOnlyList<RawHack> hacks = await client.FetchHacksAsync(this.CancellationToken());

        Assert.Single(hacks);
        Assert.Equal(expected: "Protocol X", actual: hacks[0].Name);
    }

    [Fact]
    public async Task FetchHacksAsync_NullResponse_ReturnsEmptyListAsync()
    {
        const string JSON = "null";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        DefiLlamaHacksClient client = CreateClient(httpClient);

        IReadOnlyList<RawHack> hacks = await client.FetchHacksAsync(this.CancellationToken());

        Assert.Empty(hacks);
    }

    [Fact]
    public async Task FetchHacksAsync_HttpError_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using HttpClient httpClient = new(handler);

        DefiLlamaHacksClient client = CreateClient(httpClient);

        IReadOnlyList<RawHack> hacks = await client.FetchHacksAsync(this.CancellationToken());

        Assert.Empty(hacks);
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpHandler(HttpResponseMessage response) => this._response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(this._response);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._response.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

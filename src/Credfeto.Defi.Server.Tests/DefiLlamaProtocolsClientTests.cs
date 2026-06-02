using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.DefiLlama;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class DefiLlamaProtocolsClientTests : TestBase
{
    private static DefiLlamaProtocolsClient CreateClient(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        ILogger<DefiLlamaProtocolsClient> logger = GetSubstitute<ILogger<DefiLlamaProtocolsClient>>();

        return new DefiLlamaProtocolsClient(httpClientFactory: factory, logger: logger);
    }

    [Fact]
    public async Task FetchProtocolsAsync_SuccessResponse_ReturnsParsedProtocolsAsync()
    {
        const string JSON = """[{"slug":"aave-v3","audits":"3","audit_links":["https://audit.example.com"]}]""";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        DefiLlamaProtocolsClient client = CreateClient(httpClient);

        IReadOnlyList<RawProtocol> protocols = await client.FetchProtocolsAsync(this.CancellationToken());

        Assert.Single(protocols);
        Assert.Equal(expected: "aave-v3", actual: protocols[0].Slug);
    }

    [Fact]
    public async Task FetchProtocolsAsync_NullResponse_ReturnsEmptyListAsync()
    {
        const string JSON = "null";
        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        DefiLlamaProtocolsClient client = CreateClient(httpClient);

        IReadOnlyList<RawProtocol> protocols = await client.FetchProtocolsAsync(this.CancellationToken());

        Assert.Empty(protocols);
    }

    [Fact]
    public async Task FetchProtocolsAsync_HttpError_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.BadGateway));
        using HttpClient httpClient = new(handler);

        DefiLlamaProtocolsClient client = CreateClient(httpClient);

        IReadOnlyList<RawProtocol> protocols = await client.FetchProtocolsAsync(this.CancellationToken());

        Assert.Empty(protocols);
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

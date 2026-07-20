using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.GoPlus;
using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Server.Tests.Common;
using Credfeto.Defi.Services;
using Credfeto.Defi.Storage;
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ContractSecurityServiceTests : TestBase
{
    private static readonly DateTimeOffset FixedNow = new(
        year: 2024,
        month: 6,
        day: 1,
        hour: 12,
        minute: 0,
        second: 0,
        offset: TimeSpan.Zero
    );

    private readonly FakeTimeProvider _timeProvider;
    private readonly FakeDatabase _database;
    private readonly ContractSecurityCacheService _cacheService;

    public ContractSecurityServiceTests()
    {
        this._timeProvider = new FakeTimeProvider(startDateTime: FixedNow);
        this._database = new FakeDatabase();
        this._cacheService = new ContractSecurityCacheService(
            database: this._database,
            timeProvider: this._timeProvider
        );
    }

    private static ContractSecurityRow BuildRow(
        string chain,
        string address,
        bool? isProxy,
        string? parentAddress = null
    )
    {
        return new ContractSecurityRow(
            Chain: chain,
            Address: address,
            ParentAddress: parentAddress,
            IsOpenSource: null,
            IsHoneypot: null,
            IsProxy: isProxy,
            BuyTax: null,
            SellTax: null,
            TransferTax: null,
            CannotBuy: null,
            HoneypotWithSameCreator: null,
            TokenName: null,
            TokenSymbol: null,
            CheckedAt: FixedNow - TimeSpan.FromHours(1)
        );
    }

    private static GoPlusClient CreateGoPlusClient(HttpClient httpClient)
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        return new GoPlusClient(httpClientFactory: factory, logger: GetSubstitute<ILogger<GoPlusClient>>());
    }

    private static GoPlusClient CreateMultiResponseGoPlusClient(string[] responses)
    {
        using MultiResponseHttpHandler handler = new(responses);
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));

        return new GoPlusClient(httpClientFactory: factory, logger: GetSubstitute<ILogger<GoPlusClient>>());
    }

    private static ProxyResolverService CreateNoOpProxyResolver()
    {
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();

        return new ProxyResolverService(
            rpcConfig: Options.Create(new RpcConfig()),
            httpClientFactory: factory,
            logger: GetSubstitute<ILogger<ProxyResolverService>>()
        );
    }

    private static ProxyResolverService CreateProxyResolverWithRpc(string rpcUrl, string[] rpcResponses)
    {
        using MultiResponseHttpHandler rpcHandler = new(rpcResponses);
        IHttpClientFactory factory = GetSubstitute<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(rpcHandler));

        return new ProxyResolverService(
            rpcConfig: Options.Create(new RpcConfig { Ethereum = rpcUrl }),
            httpClientFactory: factory,
            logger: GetSubstitute<ILogger<ProxyResolverService>>()
        );
    }

    private ContractSecurityService CreateService(GoPlusClient goPlusClient, ProxyResolverService proxyResolver)
    {
        return new ContractSecurityService(
            goPlusClient: goPlusClient,
            cache: this._cacheService,
            proxyResolver: proxyResolver
        );
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_EmptyAddresses_ReturnsEmptyListAsync()
    {
        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);

        ContractSecurityService service = this.CreateService(
            goPlusClient: CreateGoPlusClient(httpClient),
            proxyResolver: CreateNoOpProxyResolver()
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_CacheHit_DoesNotCallGoPlusAsync()
    {
        const string ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";

        this._database.WithReturn<ContractSecurityRow?>(BuildRow("Ethereum", ADDRESS, isProxy: false));

        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using HttpClient httpClient = new(handler);

        ContractSecurityService service = this.CreateService(
            goPlusClient: CreateGoPlusClient(httpClient),
            proxyResolver: CreateNoOpProxyResolver()
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Equal(expected: ADDRESS, actual: result[0].Address);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_CacheMiss_FetchesFromGoPlusAsync()
    {
        const string ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
        const string JSON =
            """{"code":1,"result":{"0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48":{"is_open_source":"1","is_honeypot":"0","is_proxy":"0"}}}""";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ContractSecurityService service = this.CreateService(
            goPlusClient: CreateGoPlusClient(httpClient),
            proxyResolver: CreateNoOpProxyResolver()
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Equal(expected: ADDRESS, actual: result[0].Address);
        Assert.True(result[0].IsOpenSource);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_CachedProxyContract_IncludesChildrenAsync()
    {
        const string PROXY_ADDRESS = "0xproxy00000000000000000000000000000000aa";
        const string IMPL_ADDRESS = "0ximpl000000000000000000000000000000000bb";

        this._database.WithReturn<ContractSecurityRow?>(BuildRow("Ethereum", PROXY_ADDRESS, isProxy: true));
        this._database.WithReturn<IReadOnlyList<ContractSecurityRow>>(
            [BuildRow("Ethereum", IMPL_ADDRESS, isProxy: false, parentAddress: PROXY_ADDRESS)]
        );

        using FakeHttpHandler handler = new(new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient httpClient = new(handler);

        ContractSecurityService service = this.CreateService(
            goPlusClient: CreateGoPlusClient(httpClient),
            proxyResolver: CreateNoOpProxyResolver()
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [PROXY_ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: 2, actual: result.Count);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_GoPlusAddressNotFound_SkipsItAsync()
    {
        const string ADDRESS = "0xdeadbeef0000000000000000000000000000dead";
        const string JSON = """{"code":1,"result":{}}""";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ContractSecurityService service = this.CreateService(
            goPlusClient: CreateGoPlusClient(httpClient),
            proxyResolver: CreateNoOpProxyResolver()
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_ProxyContract_NoRpcConfigured_ImplNotResolvedAsync()
    {
        const string PROXY_ADDRESS = "0xc0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
        string goPlusJson =
            "{\"code\":1,\"result\":{\""
            + PROXY_ADDRESS
            + "\":{\"is_open_source\":\"1\",\"is_honeypot\":\"0\",\"is_proxy\":\"1\"}}}";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(goPlusJson, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ContractSecurityService service = this.CreateService(
            goPlusClient: CreateGoPlusClient(httpClient),
            proxyResolver: CreateNoOpProxyResolver()
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [PROXY_ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Equal(expected: PROXY_ADDRESS, actual: result[0].Address);
        Assert.True(result[0].IsProxy);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_ProxyContract_GoPlusReturnsNoDataForImplAddress_OnlyProxyReturnedAsync()
    {
        const string PROXY_ADDRESS = "0xb0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
        string goPlusJson1 = BuildProxyGoPlusJson(PROXY_ADDRESS);
        const string IMPL_SLOT = "0x0000000000000000000000009999567890abcdef9999567890abcdef99995678";

        GoPlusClient goPlusClient = CreateMultiResponseGoPlusClient([goPlusJson1, """{"code":1,"result":{}}"""]);
        ProxyResolverService proxyResolver = CreateProxyResolverWithRpc(
            rpcUrl: "https://rpc.example.com",
            rpcResponses: [BuildRpcSlotJson(IMPL_SLOT), BuildRpcSlotJson(IMPL_SLOT), BuildRpcSlotJson(IMPL_SLOT)]
        );

        ContractSecurityService service = this.CreateService(goPlusClient: goPlusClient, proxyResolver: proxyResolver);

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [PROXY_ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Equal(expected: PROXY_ADDRESS, actual: result[0].Address);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_ProxyContract_ResolvesImplementationAsync()
    {
        const string PROXY_ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
        const string IMPL_ADDRESS = "0x1234567890abcdef1234567890abcdef12345678";
        const string IMPL_SLOT = "0x0000000000000000000000001234567890abcdef1234567890abcdef12345678";

        string goPlusJson2 =
            "{\"code\":1,\"result\":{\""
            + IMPL_ADDRESS
            + "\":{\"is_open_source\":\"1\",\"is_honeypot\":\"0\",\"is_proxy\":\"0\"}}}";

        GoPlusClient goPlusClient = CreateMultiResponseGoPlusClient([BuildProxyGoPlusJson(PROXY_ADDRESS), goPlusJson2]);
        ProxyResolverService proxyResolver = CreateProxyResolverWithRpc(
            rpcUrl: "https://rpc.example.com",
            rpcResponses: [BuildRpcSlotJson(IMPL_SLOT), BuildRpcSlotJson(IMPL_SLOT), BuildRpcSlotJson(IMPL_SLOT)]
        );

        ContractSecurityService service = this.CreateService(goPlusClient: goPlusClient, proxyResolver: proxyResolver);

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [PROXY_ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_NonNumericBoolField_ParsedAsNullAsync()
    {
        const string ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
        // "is_honeypot" has a non-numeric string value — ParseBool returns null for unparseable input
        const string JSON =
            "{\"code\":1,\"result\":{\""
            + ADDRESS
            + "\":{\"is_open_source\":\"1\",\"is_honeypot\":\"maybe\",\"is_proxy\":\"0\"}}}";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ContractSecurityService service = this.CreateService(
            goPlusClient: CreateGoPlusClient(httpClient),
            proxyResolver: CreateNoOpProxyResolver()
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Null(result[0].IsHoneypot);
    }

    [Fact]
    public async Task GetContractSecurityForAddressesAsync_NumericTaxFields_ParsedCorrectlyAsync()
    {
        const string ADDRESS = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
        // Tax fields with numeric string values — ParseNum returns the parsed double
        // Also tests ParseNum when value is non-parseable ("n/a" → null)
        const string JSON =
            "{\"code\":1,\"result\":{\""
            + ADDRESS
            + "\":{\"is_open_source\":\"1\",\"is_honeypot\":\"0\",\"is_proxy\":\"0\","
            + "\"buy_tax\":\"0.05\",\"sell_tax\":\"n/a\",\"transfer_tax\":\"0\"}}}";

        using FakeHttpHandler handler = new(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JSON, Encoding.UTF8, mediaType: "application/json"),
            }
        );
        using HttpClient httpClient = new(handler);

        ContractSecurityService service = this.CreateService(
            goPlusClient: CreateGoPlusClient(httpClient),
            proxyResolver: CreateNoOpProxyResolver()
        );

        IReadOnlyList<ContractSecurityInfo> result = await service.GetContractSecurityForAddressesAsync(
            chain: "Ethereum",
            addresses: [ADDRESS],
            cancellationToken: this.CancellationToken()
        );

        Assert.Single(result);
        Assert.Equal(expected: 0.05, actual: result[0].BuyTax);
        Assert.Null(result[0].SellTax);
        Assert.Equal(expected: 0.0, actual: result[0].TransferTax);
    }

    private static string BuildProxyGoPlusJson(string address)
    {
        return "{\"code\":1,\"result\":{\""
            + address
            + "\":{\"is_open_source\":\"1\",\"is_honeypot\":\"0\",\"is_proxy\":\"1\"}}}";
    }

    private static string BuildRpcSlotJson(string slot)
    {
        return "{\"jsonrpc\":\"2.0\",\"result\":\"" + slot + "\",\"id\":1}";
    }

    private sealed class MultiResponseHttpHandler : HttpMessageHandler
    {
        private readonly string[] _responses;
        private int _index;

        public MultiResponseHttpHandler(string[] responses)
        {
            this._responses = responses;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            string json =
                this._index < this._responses.Length ? this._responses[this._index++] : """{"code":1,"result":{}}""";

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, mediaType: "application/json"),
            };

            return Task.FromResult(response);
        }
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

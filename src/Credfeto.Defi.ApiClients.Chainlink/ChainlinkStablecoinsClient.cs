using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.Chainlink.Interfaces;
using Credfeto.Defi.ApiClients.Chainlink.LoggingExtensions;
using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credfeto.Defi.ApiClients.Chainlink;

/// <summary>
///     Fetches stablecoin USD prices from Chainlink on-chain AggregatorV3Interface price feeds.
/// </summary>
public sealed class ChainlinkStablecoinsClient : IChainlinkStablecoinsClient
{
    private const string LATEST_ROUND_DATA_SELECTOR = "0x50d25bcd";
    private const decimal USD_DECIMAL_DIVISOR = 100_000_000m; // 10^8

    private static readonly IReadOnlyDictionary<string, string> KnownFeeds = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["usdc"] = "0x8fFfFfd4AfB6115b954Bd326cbe7B4BA576818f6",
        ["usdt"] = "0x3E7d1eAB13ad0104d2750B8863b489D65364e32D",
        ["dai"] = "0xAed0c38402a5d19df6E4c03F4E2DceD6e29c1ee9",
        ["frax"] = "0xB9E1E3A9feFf48998E45Fa90847ed4D467E8BcfD",
        ["busd"] = "0x833D8Eb16D306ed1FbB5D7A2E019e22881BD76e2",
        ["lusd"] = "0x3D7aE7E594f2f2091Ad8798313450130d0Aba3a0",
        ["tusd"] = "0xec746eCF986E2927Abd291a2A1716c940100f8Ba",
        ["usdp"] = "0x09023c0DA49Aaf8fc3fA3ADF34C6A7016D38D5e3",
        ["gusd"] = "0xa89f5d2365ce98B3cD68012b6f503ab1416245Fc",
        ["crvusd"] = "0xEEf0C605546958c1f899b6fB336C20671f9cD49F",
        ["pyusd"] = "0x8f1dF6D7F2db73eECE86a18b4381F4707b918FB",
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChainlinkStablecoinsClient> _logger;
    private readonly RpcConfig _rpcConfig;

    /// <summary>
    ///     Initialises a new instance of <see cref="ChainlinkStablecoinsClient" />.
    /// </summary>
    public ChainlinkStablecoinsClient(
        IOptions<RpcConfig> rpcConfig,
        IHttpClientFactory httpClientFactory,
        ILogger<ChainlinkStablecoinsClient> logger
    )
    {
        this._rpcConfig = rpcConfig.Value;
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ChainlinkPriceFeed>> FetchStablecoinsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(this._rpcConfig.Ethereum))
        {
            return [];
        }

        List<ChainlinkPriceFeed> results = [];

        using HttpClient client = this._httpClientFactory.CreateClient(nameof(ChainlinkStablecoinsClient));

        foreach (KeyValuePair<string, string> feed in KnownFeeds)
        {
            ChainlinkPriceFeed? price = await this.FetchFeedAsync(
                client: client,
                symbol: feed.Key,
                feedAddress: feed.Value,
                cancellationToken: cancellationToken
            );

            if (price is not null)
            {
                results.Add(price);
            }
        }

        return results;
    }

    private async ValueTask<ChainlinkPriceFeed?> FetchFeedAsync(
        HttpClient client,
        string symbol,
        string feedAddress,
        CancellationToken cancellationToken
    )
    {
        try
        {
            string requestBody =
                "{\"jsonrpc\":\"2.0\",\"method\":\"eth_call\",\"params\":[{\"to\":\""
                + feedAddress
                + "\",\"data\":\""
                + LATEST_ROUND_DATA_SELECTOR
                + "\"},\"latest\"],\"id\":1}";

            using StringContent content = new(requestBody, Encoding.UTF8, mediaType: "application/json");
            using HttpResponseMessage response = await client.PostAsync(
                requestUri: new Uri(this._rpcConfig.Ethereum),
                content: content,
                cancellationToken: cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            RpcResponse? rpcResponse = await response.Content.ReadFromJsonAsync(
                jsonTypeInfo: AppJsonContext.Default.RpcResponse,
                cancellationToken: cancellationToken
            );

            if (rpcResponse?.Result is null)
            {
                return null;
            }

            decimal? price = ParseLatestRoundDataPrice(rpcResponse.Result);

            if (price is null)
            {
                return null;
            }

            return new ChainlinkPriceFeed { Symbol = symbol, CurrentPrice = price.Value };
        }
        catch (Exception ex)
        {
            this._logger.FetchFeedFailed(symbol: symbol, feedAddress: feedAddress, exception: ex);
            return null;
        }
    }

    private static decimal? ParseLatestRoundDataPrice(string hexResult)
    {
        // Response: 0x[32B roundId][32B answer][32B startedAt][32B updatedAt][32B answeredInRound]
        // We need answer (bytes 32-63) = hex chars at position 2+64=66, length 64
        if (hexResult.Length < 2 + 320)
        {
            return null;
        }

        ReadOnlySpan<char> hex = hexResult.AsSpan(2); // skip "0x"
        ReadOnlySpan<char> answerHex = hex.Slice(64, 64); // bytes 32-63

        // Prepend "0" to ensure BigInteger treats it as positive
        string answerStr = "0" + new string(answerHex);
        if (
            !BigInteger.TryParse(
                answerStr,
                NumberStyles.HexNumber,
                provider: CultureInfo.InvariantCulture,
                result: out BigInteger answer
            )
        )
        {
            return null;
        }

        if (answer <= BigInteger.Zero)
        {
            return null;
        }

        return (decimal)answer / USD_DECIMAL_DIVISOR;
    }
}

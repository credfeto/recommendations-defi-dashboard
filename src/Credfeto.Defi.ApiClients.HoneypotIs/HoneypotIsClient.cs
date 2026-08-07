using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.HoneypotIs.Interfaces;
using Credfeto.Defi.ApiClients.HoneypotIs.LoggingExtensions;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.HoneypotIs;

/// <summary>
///     Fetches contract security information from the Honeypot.is API.
/// </summary>
public sealed class HoneypotIsClient : IHoneypotIsClient
{
    private const string HONEYPOT_IS_BASE = "https://api.honeypot.is/v2/IsHoneypot";

    private static readonly IReadOnlyDictionary<string, int> ChainNameToId = new Dictionary<string, int>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["Ethereum"] = 1,
        ["BSC"] = 56,
        ["Base"] = 8453,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HoneypotIsClient> _logger;

    /// <summary>
    ///     Initialises a new instance of <see cref="HoneypotIsClient" />.
    /// </summary>
    public HoneypotIsClient(IHttpClientFactory httpClientFactory, ILogger<HoneypotIsClient> logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    /// <summary>
    ///     Fetches security information for one or more contract addresses on a given chain.
    ///     Returns a map of lowercased address to result.
    ///     Returns an empty map if the chain is unsupported or the request has no addresses.
    ///     Addresses that fail to simulate are omitted from the result rather than failing the whole call.
    /// </summary>
    public async ValueTask<IReadOnlyDictionary<string, HoneypotIsResult>> FetchTokenSecurityAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, HoneypotIsResult> result = new(StringComparer.OrdinalIgnoreCase);

        if (!ChainNameToId.TryGetValue(key: chain, out int chainId) || addresses.Count == 0)
        {
            return result;
        }

        using HttpClient client = this._httpClientFactory.CreateClient(nameof(HoneypotIsClient));

        foreach (string address in addresses)
        {
            string lowered = address.ToLowerInvariant();

            try
            {
                string url = string.Format(
                    provider: CultureInfo.InvariantCulture,
                    format: "{0}?address={1}&chainID={2}",
                    HONEYPOT_IS_BASE,
                    lowered,
                    chainId
                );

                HoneypotIsResponse? response = await client.GetFromJsonAsync(
                    requestUri: url,
                    jsonTypeInfo: AppJsonContext.Default.HoneypotIsResponse,
                    cancellationToken: cancellationToken
                );

                if (response is null)
                {
                    continue;
                }

                result[lowered] = new HoneypotIsResult
                {
                    IsHoneypot = response.HoneypotResult?.IsHoneypot,
                    BuyTax = response.SimulationResult?.BuyTax,
                    SellTax = response.SimulationResult?.SellTax,
                    SimulationSuccess = response.SimulationSuccess,
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                this._logger.FetchTokenSecurityFailed(chain: chain, address: lowered, exception: ex);
            }
        }

        return result;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.GoPlus.Interfaces;
using Credfeto.Defi.ApiClients.GoPlus.LoggingExtensions;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.GoPlus;

/// <summary>
///     Fetches contract security information from the GoPlus Labs API.
/// </summary>
public sealed class GoPlusClient : IGoPlusClient
{
    private const string GOPLUS_BASE = "https://api.gopluslabs.io/api/v1/token_security";

    private static readonly IReadOnlyDictionary<string, int> ChainNameToId = new Dictionary<string, int>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["Ethereum"] = 1,
        ["Arbitrum"] = 42161,
        ["Base"] = 8453,
        ["BSC"] = 56,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoPlusClient> _logger;

    /// <summary>
    ///     Initialises a new instance of <see cref="GoPlusClient" />.
    /// </summary>
    public GoPlusClient(IHttpClientFactory httpClientFactory, ILogger<GoPlusClient> logger)    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    /// <summary>
    ///     Fetches security information for one or more contract addresses on a given chain.
    ///     Returns a map of lowercased address to raw result.
    ///     Returns an empty map if the chain is unsupported or the request fails.
    /// </summary>
    public async ValueTask<IReadOnlyDictionary<string, GoPlusTokenResult>> FetchTokenSecurityAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    )
    {
        if (!ChainNameToId.TryGetValue(key: chain, out int chainId) || addresses.Count == 0)
        {
            return new Dictionary<string, GoPlusTokenResult>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            StringBuilder joined = new();

            for (int i = 0; i < addresses.Count; i++)
            {
                if (i > 0)
                {
                    _ = joined.Append(',');
                }

                _ = joined.Append(addresses[i].ToLowerInvariant());
            }

            string url = string.Format(
                provider: CultureInfo.InvariantCulture,
                format: "{0}/{1}?contract_addresses={2}",
                GOPLUS_BASE,
                chainId,
                joined
            );

            using HttpClient client = this._httpClientFactory.CreateClient(nameof(GoPlusClient));
            GoPlusResponse? response = await client.GetFromJsonAsync(
                requestUri: url,
                jsonTypeInfo: AppJsonContext.Default.GoPlusResponse,
                cancellationToken: cancellationToken
            );

            if (response?.Result is null)
            {
                return new Dictionary<string, GoPlusTokenResult>(StringComparer.OrdinalIgnoreCase);
            }

            Dictionary<string, GoPlusTokenResult> result = new(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, GoPlusTokenResult> kvp in response.Result)
            {
                result[kvp.Key.ToLowerInvariant()] = kvp.Value;
            }

            return result;
        }
        catch (Exception ex)
        {
            this._logger.FetchTokenSecurityFailed(chain: chain, exception: ex);

            return new Dictionary<string, GoPlusTokenResult>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

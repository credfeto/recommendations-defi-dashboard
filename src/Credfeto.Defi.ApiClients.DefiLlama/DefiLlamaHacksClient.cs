using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.DefiLlama.Interfaces;
using Credfeto.Defi.ApiClients.DefiLlama.LoggingExtensions;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Microsoft.Extensions.Logging;

namespace Credfeto.Defi.ApiClients.DefiLlama;

/// <summary>
///     Fetches recorded DeFi exploit data from the DefiLlama hacks API.
/// </summary>
public sealed class DefiLlamaHacksClient : IDefiLlamaHacksClient
{
    private const string HACKS_URL = "https://api.llama.fi/hacks";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DefiLlamaHacksClient> _logger;

    /// <summary>
    ///     Initialises a new instance of <see cref="DefiLlamaHacksClient" />.
    /// </summary>
    public DefiLlamaHacksClient(IHttpClientFactory httpClientFactory, ILogger<DefiLlamaHacksClient> logger)    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    /// <summary>
    ///     Fetches all recorded DeFi exploits from DefiLlama.
    /// </summary>
    public async ValueTask<IReadOnlyList<RawHack>> FetchHacksAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient client = this._httpClientFactory.CreateClient(nameof(DefiLlamaHacksClient));
            RawHack[]? hacks = await client.GetFromJsonAsync(
                requestUri: HACKS_URL,
                jsonTypeInfo: AppJsonContext.Default.RawHackArray,
                cancellationToken: cancellationToken
            );

            return hacks ?? [];
        }
        catch (Exception ex)
        {
            this._logger.FetchHacksFailed(ex);

            return [];
        }
    }
}

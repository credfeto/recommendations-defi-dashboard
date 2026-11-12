using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.Webacy.Interfaces;
using Credfeto.Defi.ApiClients.Webacy.LoggingExtensions;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credfeto.Defi.ApiClients.Webacy;

public sealed class WebacyRiskClient : IWebacyRiskClient
{
    private const string WEBACY_BASE = "https://api.webacy.com/addresses";

    private const double LOW_RISK_MAX = 23;

    private const double MEDIUM_RISK_MAX = 50;

    private static readonly IReadOnlyDictionary<string, string> ChainNameToCode = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["Ethereum"] = "eth",
        ["Arbitrum"] = "arb",
        ["Base"] = "base",
        ["BSC"] = "bsc",
        ["Polygon"] = "pol",
        ["Optimism"] = "opt",
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebacyRiskClient> _logger;
    private readonly WebacyOptions _options;

    public WebacyRiskClient(
        IHttpClientFactory httpClientFactory,
        IOptions<WebacyOptions> options,
        ILogger<WebacyRiskClient> logger
    )
    {
        this._httpClientFactory = httpClientFactory;
        this._options = options.Value;
        this._logger = logger;
    }

    public async ValueTask<IReadOnlyDictionary<string, WebacyResult>> FetchTokenRiskAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    )
    {
        Dictionary<string, WebacyResult> result = [with(StringComparer.OrdinalIgnoreCase)];

        // Webacy rejects every request without a key, so skip the calls entirely when none is configured.
        if (
            string.IsNullOrWhiteSpace(this._options.ApiKey)
            || !ChainNameToCode.TryGetValue(key: chain, out string? chainCode)
            || addresses.Count == 0
        )
        {
            return result;
        }

        using HttpClient client = this._httpClientFactory.CreateClient(nameof(WebacyRiskClient));

        // Sequential on purpose: the Demo plan allows one request per second.
        foreach (string address in addresses)
        {
            string lowered = address.ToLowerInvariant();

            try
            {
                WebacyResult? risk = await this.FetchOneAsync(
                    client: client,
                    chain: chain,
                    chainCode: chainCode,
                    address: lowered,
                    cancellationToken: cancellationToken
                );

                if (risk is not null)
                {
                    result[lowered] = risk;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Omit an address whose lookup fails rather than failing the whole call.
                this._logger.FetchTokenRiskFailed(chain: chain, address: lowered, exception: ex);
            }
        }

        return result;
    }

    private async ValueTask<WebacyResult?> FetchOneAsync(
        HttpClient client,
        string chain,
        string chainCode,
        string address,
        CancellationToken cancellationToken
    )
    {
        Uri url = new(
            uriString: string.Format(
                provider: CultureInfo.InvariantCulture,
                format: "{0}/{1}?chain={2}",
                WEBACY_BASE,
                Uri.EscapeDataString(address),
                chainCode
            )
        );

        using HttpResponseMessage response = await client.GetAsync(
            requestUri: url,
            cancellationToken: cancellationToken
        );

        // 202 Accepted means Webacy is still analysing the address; treat it like any other non-200 so
        // nothing is cached and the next lookup asks again.
        if (response.StatusCode != HttpStatusCode.OK)
        {
            this._logger.FetchTokenRiskUnavailable(chain: chain, address: address, statusCode: response.StatusCode);

            return null;
        }

        WebacyAddressResponse? body = await response.Content.ReadFromJsonAsync(
            jsonTypeInfo: AppJsonContext.Default.WebacyAddressResponse,
            cancellationToken: cancellationToken
        );

        if (body is null)
        {
            return null;
        }

        return new WebacyResult
        {
            RiskScore = body.OverallRisk,
            RiskLevel = ToRiskLevel(body.OverallRisk),
            IssueCount = body.Count,
            HighRiskIssueCount = body.High,
            MediumRiskIssueCount = body.Medium,
        };
    }

    private static string? ToRiskLevel(double? score)
    {
        return score switch
        {
            null => null,
            <= LOW_RISK_MAX => "Low",
            <= MEDIUM_RISK_MAX => "Medium",
            _ => "High",
        };
    }
}

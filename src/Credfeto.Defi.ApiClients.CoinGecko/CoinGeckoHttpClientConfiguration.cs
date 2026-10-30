using System.Net.Http;

namespace Credfeto.Defi.ApiClients.CoinGecko;

public static class CoinGeckoHttpClientConfiguration
{
    public const string DEMO_PLAN_HEADER = "x-cg-demo-api-key";

    // CoinGecko can answer 403 Forbidden to requests that carry no User-Agent (#432).
    public const string USER_AGENT =
        "Credfeto.Defi.Dashboard/1.0 (+https://github.com/credfeto/recommendations-defi-dashboard)";

    public static void Configure(HttpClient client, CoinGeckoOptions options)
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT);

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            client.DefaultRequestHeaders.Add(name: DEMO_PLAN_HEADER, value: options.ApiKey);
        }
    }
}

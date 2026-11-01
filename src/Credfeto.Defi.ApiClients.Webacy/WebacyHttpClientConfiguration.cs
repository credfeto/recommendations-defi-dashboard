using System.Net.Http;

namespace Credfeto.Defi.ApiClients.Webacy;

public static class WebacyHttpClientConfiguration
{
    public const string API_KEY_HEADER = "x-api-key";

    public static void Configure(HttpClient client, WebacyOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            client.DefaultRequestHeaders.Add(name: API_KEY_HEADER, value: options.ApiKey);
        }
    }
}

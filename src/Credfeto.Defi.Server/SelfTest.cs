using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Credfeto.Defi.Storage.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Credfeto.Defi.Server;

internal static class SelfTest
{
    public const int SUCCESS = 0;
    public const int FAILURE = 1;

    private const string SWITCH = "--self-test";

    public static bool IsSelfTest(IReadOnlyList<string> args)
    {
        return args.Contains(value: SWITCH, comparer: StringComparer.Ordinal);
    }

    public static Task<int> VerifyAsync(IServiceProvider services, TextWriter error)
    {
        return VerifyAsync(services: services, error: error, hasIcuCultureData: HasIcuCultureData);
    }

    public static async Task<int> VerifyAsync(IServiceProvider services, TextWriter error, Func<bool> hasIcuCultureData)
    {
        try
        {
            ResolveServices(services);
            HashWithOpenSsl();

            if (!hasIcuCultureData())
            {
                await error.WriteLineAsync("Self-test failed: ICU culture data is not available");

                return FAILURE;
            }

            return SUCCESS;
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync("Self-test failed:");
            await error.WriteLineAsync(exception.ToString());

            return FAILURE;
        }
    }

    private static void ResolveServices(IServiceProvider services)
    {
        // Constructing the hosted services builds their whole dependency graph without starting them,
        // so nothing listens on a port or connects to an external service.
        _ = services.GetServices<IHostedService>().ToArray();
        _ = services.GetServices<McpServerTool>().ToArray();
        _ = services.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;

        using HttpClient client = services.GetRequiredService<IHttpClientFactory>().CreateClient();
    }

    private static void HashWithOpenSsl()
    {
        _ = SHA256.HashData("self-test"u8);
    }

    private static bool HasIcuCultureData()
    {
        // Turkish dotted upper-case I only comes from real ICU data; invariant globalization returns 'I'.
        return CultureInfo.GetCultureInfo("tr-TR").TextInfo.ToUpper('i') == 'İ';
    }
}

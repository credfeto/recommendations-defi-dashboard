using System;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Docker.HealthCheck.Http.Client;

namespace Credfeto.Defi.Server;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return HealthCheckClient.IsHealthCheck(args: args, out string? checkUrl)
            ? await HealthCheckClient.ExecuteAsync(targetUrl: checkUrl, cancellationToken: CancellationToken.None)
            : await ServerApplication.RunAsync(
                args: args,
                createApplication: ServerApplication.Create,
                error: Console.Error
            );
    }
}

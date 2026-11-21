using System;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Docker.HealthCheck.Http.Client;

namespace Credfeto.Defi.Server;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // No token source here, because nothing would cancel it: the server's host installs its own
        // SIGINT/SIGTERM handling and drives a graceful shutdown, so a Main-level source would duplicate it,
        // while the health check and self-test are short-lived runs ended by the default signal handling
        // (and, for the health check, by the HEALTHCHECK timeout in the Dockerfile).
        return HealthCheckClient.IsHealthCheck(args: args, out string? checkUrl)
            ? await HealthCheckClient.ExecuteAsync(targetUrl: checkUrl, cancellationToken: CancellationToken.None)
            : await ServerApplication.RunAsync(
                args: args,
                createApplication: ServerApplication.Create,
                error: Console.Error,
                cancellationToken: CancellationToken.None
            );
    }
}

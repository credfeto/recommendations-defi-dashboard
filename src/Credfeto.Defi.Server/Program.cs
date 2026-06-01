using System;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Mcp;
using Credfeto.Defi.Server.Endpoints;
using Credfeto.Defi.Server.Helpers;
using Credfeto.Docker.HealthCheck.Http.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;

namespace Credfeto.Defi.Server;

/// <summary>
///     Application entry point.
/// </summary>
internal static class Program
{
    /// <summary>
    ///     Main entry point.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        return HealthCheckClient.IsHealthCheck(args: args, out string? checkUrl)
            ? await HealthCheckClient.ExecuteAsync(targetUrl: checkUrl, cancellationToken: CancellationToken.None)
            : await RunServerAsync(args);
    }

    private static async Task<int> RunServerAsync(string[] args)
    {
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

            _ = builder
                .Configuration.SetBasePath(builder.Environment.ContentRootPath)
                .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables();

            ConfigureLogging(builder);

            _ = builder.WebHost.ConfigureKestrel();

            _ = builder.AddDefiServices();

            await using WebApplication app = builder.Build();

            app.MapHealthCheck();
            app.MapPoolsEndpoints();
            app.MapMcpEndpoint();

            await app.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync("Fatal error during startup:");
            await Console.Error.WriteLineAsync(ex.Message);
            await Console.Error.WriteLineAsync(ex.StackTrace);

            return 1;
        }
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        Logger logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.Console()
            .CreateLogger();

        _ = builder.Logging.ClearProviders().AddSerilog(logger: logger, dispose: true);
    }
}

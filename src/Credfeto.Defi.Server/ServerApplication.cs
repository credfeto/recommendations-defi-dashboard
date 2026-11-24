using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Mcp;
using Credfeto.Defi.Server.Endpoints;
using Credfeto.Defi.Server.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;

namespace Credfeto.Defi.Server;

internal static class ServerApplication
{
    public static Task<int> RunAsync(
        string[] args,
        Func<string[], WebApplication> createApplication,
        TextWriter error,
        CancellationToken cancellationToken
    )
    {
        // The self-test builds the application without command-line arguments so the switch itself is never
        // interpreted as a configuration key.
        return SelfTest.IsSelfTest(args)
            ? RunApplicationAsync(
                createApplication: () => createApplication([]),
                run: app =>
                    SelfTest.VerifyAsync(services: app.Services, error: error, cancellationToken: cancellationToken),
                error: error
            )
            : RunApplicationAsync(
                createApplication: () => createApplication(args),
                run: app => RunServerAsync(app: app, cancellationToken: cancellationToken),
                error: error
            );
    }

    public static WebApplication Create(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);

        builder.Configuration.Sources.Clear();
        _ = builder
            .Configuration.SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile(path: "appsettings-local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        ConfigureLogging(builder);

        _ = builder.WebHost.ConfigureKestrel();

        _ = builder.AddDefiServices();

        WebApplication app = builder.Build();

        app.MapHealthCheck();
        app.MapPoolsEndpoints();
        app.MapMcpEndpoint();

        return app;
    }

    private static async Task<int> RunApplicationAsync(
        Func<WebApplication> createApplication,
        Func<WebApplication, Task<int>> run,
        TextWriter error
    )
    {
        try
        {
            await using WebApplication app = createApplication();

            return await run(app);
        }
        catch (Exception exception)
        {
            // The diagnostics must be written even when the caller's token is already cancelled, otherwise the
            // write throws out of the catch block, losing the error and the FAILURE exit code.
            await error.WriteLineAsync(value: "Fatal error during startup:", cancellationToken: CancellationToken.None);
            await error.WriteLineAsync(value: exception.Message, cancellationToken: CancellationToken.None);
            await error.WriteLineAsync(value: exception.StackTrace, cancellationToken: CancellationToken.None);

            return SelfTest.FAILURE;
        }
    }

    private static async Task<int> RunServerAsync(WebApplication app, CancellationToken cancellationToken)
    {
        try
        {
            await app.StartAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A cancelled token is a shutdown request, not a startup failure. The stop uses CancellationToken.None
            // because the run token is already cancelled, and hosted services that had started must still be
            // given the chance to stop; disposing the application does not stop them.
            await app.StopAsync(CancellationToken.None);

            return SelfTest.SUCCESS;
        }

        // Cancelling the token requests a graceful shutdown once the server has started.
        await app.WaitForShutdownAsync(cancellationToken);

        return SelfTest.SUCCESS;
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

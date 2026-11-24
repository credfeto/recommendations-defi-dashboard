using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ServerApplicationTests : TestBase
{
    [Fact]
    public async Task Create_ContentRootWithAppSettings_BuildsApplicationThatPassesSelfTest()
    {
        await using WebApplication app = ServerApplication.Create(["--contentRoot", AppContext.BaseDirectory]);
        await using StringWriter error = new();

        int result = await SelfTest.VerifyAsync(
            services: app.Services,
            error: error,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: SelfTest.SUCCESS, actual: result);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task RunAsync_SelfTestSwitch_BuildsWithoutArgumentsAndReturnsSuccess()
    {
        string[]? received = null;
        await using StringWriter error = new();

        int result = await ServerApplication.RunAsync(
            args: ["--self-test"],
            createApplication: args =>
            {
                received = args;

                return TestAppFactory.BuildApp();
            },
            error: error,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: SelfTest.SUCCESS, actual: result);
        Assert.Empty(received ?? ["not called"]);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task RunAsync_SelfTestSwitchWithBrokenApplication_ReturnsFailure()
    {
        await using StringWriter error = new();

        int result = await ServerApplication.RunAsync(
            args: ["--self-test"],
            createApplication: _ => WebApplication.CreateSlimBuilder().Build(),
            error: error,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: SelfTest.FAILURE, actual: result);
        Assert.NotEmpty(error.ToString());
    }

    [Fact]
    public async Task RunAsync_CreateApplicationThrows_ReturnsFailureAndReportsError()
    {
        await using StringWriter error = new();

        int result = await ServerApplication.RunAsync(
            args: [],
            createApplication: _ => throw new InvalidOperationException("Broken configuration"),
            error: error,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: SelfTest.FAILURE, actual: result);
        Assert.Contains(
            expectedSubstring: "Broken configuration",
            actualString: error.ToString(),
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task RunAsync_CreateApplicationThrowsWithCancelledToken_StillReturnsFailureAndReportsError()
    {
        await using StringWriter error = new();

        int result = await ServerApplication.RunAsync(
            args: [],
            createApplication: _ => throw new InvalidOperationException("Broken configuration"),
            error: error,
            cancellationToken: new CancellationToken(canceled: true)
        );

        Assert.Equal(expected: SelfTest.FAILURE, actual: result);
        Assert.Contains(
            expectedSubstring: "Broken configuration",
            actualString: error.ToString(),
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task RunAsync_NoSelfTestSwitch_RunsServerWithArgumentsUntilStopped()
    {
        string[] expectedArgs = ["--example", "value"];
        string[]? received = null;
        await using StringWriter error = new();

        int result = await ServerApplication.RunAsync(
            args: expectedArgs,
            createApplication: args =>
            {
                received = args;

                return BuildServerThatStopsOnceStarted();
            },
            error: error,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: SelfTest.SUCCESS, actual: result);
        Assert.Equal(expected: expectedArgs, actual: received);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task RunAsync_NoSelfTestSwitchAndTokenCancelledOnceStarted_StopsServerAndReturnsSuccess()
    {
        using CancellationTokenSource shutdown = CancellationTokenSource.CreateLinkedTokenSource(
            this.CancellationToken()
        );
        await using StringWriter error = new();
        IHostApplicationLifetime? lifetime = null;

        try
        {
            // Without the token reaching the server the run never ends, so bound it to fail rather than hang.
            int result = await ServerApplication
                .RunAsync(
                    args: [],
                    createApplication: _ =>
                    {
                        WebApplication server = BuildServerThatCancelsOnceStarted(shutdown);
                        lifetime = server.Lifetime;

                        return server;
                    },
                    error: error,
                    cancellationToken: shutdown.Token
                )
                .WaitAsync(timeout: TimeSpan.FromSeconds(30), cancellationToken: this.CancellationToken());

            Assert.Equal(expected: SelfTest.SUCCESS, actual: result);
            Assert.Empty(error.ToString());
        }
        finally
        {
            // A run that timed out is still serving, so stop it rather than leave Kestrel listening for the rest
            // of the test run. The lifetime is used because the token may be the very thing that failed to reach
            // the server; stopping an already stopped lifetime does nothing.
            lifetime?.StopApplication();
        }
    }

    [Fact]
    public async Task RunAsync_NoSelfTestSwitchAndTokenAlreadyCancelled_ReturnsSuccessWithoutReportingError()
    {
        await using StringWriter error = new();

        int result = await ServerApplication.RunAsync(
            args: [],
            createApplication: _ => BuildServer(),
            error: error,
            cancellationToken: new CancellationToken(canceled: true)
        );

        Assert.Equal(expected: SelfTest.SUCCESS, actual: result);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task RunAsync_NoSelfTestSwitchAndTokenCancelledDuringStartup_StopsStartedServicesAndReturnsSuccess()
    {
        using CancellationTokenSource shutdown = CancellationTokenSource.CreateLinkedTokenSource(
            this.CancellationToken()
        );
        await using StringWriter error = new();
        bool startedServiceStopped = false;

        int result = await ServerApplication.RunAsync(
            args: [],
            createApplication: _ =>
                BuildServerThatCancelsDuringStartup(shutdown: shutdown, onStartedServiceStopped: () =>
                    startedServiceStopped = true
                ),
            error: error,
            cancellationToken: shutdown.Token
        );

        Assert.Equal(expected: SelfTest.SUCCESS, actual: result);
        Assert.Empty(error.ToString());
        Assert.True(condition: startedServiceStopped, userMessage: "Started hosted service was not stopped");
    }

    [Fact]
    public async Task RunAsync_NoSelfTestSwitchAndHostedServiceFailsToStart_ReturnsFailureAndReportsError()
    {
        await using StringWriter error = new();

        int result = await ServerApplication.RunAsync(
            args: [],
            createApplication: _ => BuildServerWithFailingHostedService(),
            error: error,
            cancellationToken: this.CancellationToken()
        );

        Assert.Equal(expected: SelfTest.FAILURE, actual: result);
        Assert.Contains(
            expectedSubstring: "Hosted service broken",
            actualString: error.ToString(),
            comparisonType: StringComparison.Ordinal
        );
    }

    private static WebApplication BuildServerWithFailingHostedService()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");
        _ = builder.Services.AddSingleton<IHostedService>(
            new CallbackHostedService(
                onStart: static () => throw new InvalidOperationException("Hosted service broken"),
                onStop: static () => { }
            )
        );

        return builder.Build();
    }

    private static WebApplication BuildServer()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");

        return builder.Build();
    }

    private static WebApplication BuildServerThatCancelsDuringStartup(
        CancellationTokenSource shutdown,
        Action onStartedServiceStopped
    )
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");

        // Hosted services start in registration order, so the first has started when the second cancels.
        _ = builder.Services.AddSingleton<IHostedService>(
            new CallbackHostedService(onStart: static () => { }, onStop: onStartedServiceStopped)
        );
        _ = builder.Services.AddSingleton<IHostedService>(
            new CallbackHostedService(onStart: shutdown.Cancel, onStop: static () => { })
        );

        return builder.Build();
    }

    private static WebApplication BuildServerThatCancelsOnceStarted(CancellationTokenSource shutdown)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");

        WebApplication app = builder.Build();
        _ = app.Lifetime.ApplicationStarted.Register(shutdown.Cancel);

        return app;
    }

    private static WebApplication BuildServerThatStopsOnceStarted()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");

        WebApplication app = builder.Build();
        _ = app.Lifetime.ApplicationStarted.Register(app.Lifetime.StopApplication);

        return app;
    }

    private sealed class CallbackHostedService : IHostedService
    {
        private readonly Action _onStart;
        private readonly Action _onStop;

        public CallbackHostedService(Action onStart, Action onStop)
        {
            this._onStart = onStart;
            this._onStop = onStop;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._onStart();
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._onStop();

            return Task.CompletedTask;
        }
    }
}

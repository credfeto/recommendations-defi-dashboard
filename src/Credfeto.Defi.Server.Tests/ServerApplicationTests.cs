using System;
using System.IO;
using System.Threading.Tasks;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ServerApplicationTests : TestBase
{
    [Fact]
    public async Task Create_ContentRootWithAppSettings_BuildsApplicationThatPassesSelfTest()
    {
        await using WebApplication app = ServerApplication.Create(["--contentRoot", AppContext.BaseDirectory]);
        await using StringWriter error = new();

        int result = await SelfTest.VerifyAsync(services: app.Services, error: error);

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
            error: error
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
            error: error
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
            error: error
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
            error: error
        );

        Assert.Equal(expected: SelfTest.SUCCESS, actual: result);
        Assert.Equal(expected: expectedArgs, actual: received);
        Assert.Empty(error.ToString());
    }

    private static WebApplication BuildServerThatStopsOnceStarted()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");

        WebApplication app = builder.Build();
        _ = app.Lifetime.ApplicationStarted.Register(app.Lifetime.StopApplication);

        return app;
    }
}

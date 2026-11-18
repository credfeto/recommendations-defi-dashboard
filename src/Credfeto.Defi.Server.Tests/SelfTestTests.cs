using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class SelfTestTests : TestBase
{
    [Fact]
    public void IsSelfTest_SwitchPresent_ReturnsTrue()
    {
        Assert.True(
            SelfTest.IsSelfTest(["--urls", "http://localhost", "--self-test"]),
            userMessage: "The self-test switch should be detected"
        );
    }

    [Fact]
    public void IsSelfTest_NoArguments_ReturnsFalse()
    {
        Assert.False(SelfTest.IsSelfTest([]), userMessage: "No arguments should not request a self-test");
    }

    [Fact]
    public void IsSelfTest_OtherSwitch_ReturnsFalse()
    {
        Assert.False(
            SelfTest.IsSelfTest(["--health-check", "http://127.0.0.1:8080/ping"]),
            userMessage: "Another switch should not request a self-test"
        );
    }

    [Fact]
    public void IsSelfTest_DifferentCase_ReturnsFalse()
    {
        Assert.False(SelfTest.IsSelfTest(["--SELF-TEST"]), userMessage: "The switch should be case-sensitive");
    }

    [Fact]
    public async Task VerifyAsync_FullyRegisteredApplication_ReturnsSuccess()
    {
        await using WebApplication app = TestAppFactory.BuildApp();
        await using StringWriter error = new();

        int result = await SelfTest.VerifyAsync(services: app.Services, error: error);

        Assert.Equal(expected: SelfTest.SUCCESS, actual: result);
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task VerifyAsync_IcuCultureDataMissing_ReturnsFailureAndReportsError()
    {
        await using WebApplication app = TestAppFactory.BuildApp();
        await using StringWriter error = new();

        int result = await SelfTest.VerifyAsync(services: app.Services, error: error, hasIcuCultureData: () => false);

        Assert.Equal(expected: SelfTest.FAILURE, actual: result);
        Assert.Contains(
            expectedSubstring: "ICU",
            actualString: error.ToString(),
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task VerifyAsync_UnresolvableHostedService_ReturnsFailureAndReportsError()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        _ = builder.Services.AddSingleton<IHostedService>(services => new HostedServiceWithMissingDependency(
            services.GetRequiredService<IMissingDependency>()
        ));

        await using WebApplication app = builder.Build();
        await using StringWriter error = new();

        int result = await SelfTest.VerifyAsync(services: app.Services, error: error);

        Assert.Equal(expected: SelfTest.FAILURE, actual: result);
        Assert.Contains(
            expectedSubstring: nameof(IMissingDependency),
            actualString: error.ToString(),
            comparisonType: StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task VerifyAsync_ApplicationWithoutDefiServices_ReturnsFailure()
    {
        await using WebApplication app = WebApplication.CreateSlimBuilder().Build();
        await using StringWriter error = new();

        int result = await SelfTest.VerifyAsync(services: app.Services, error: error);

        Assert.Equal(expected: SelfTest.FAILURE, actual: result);
        Assert.NotEmpty(error.ToString());
    }

    private interface IMissingDependency;

    private sealed class HostedServiceWithMissingDependency : IHostedService
    {
        public HostedServiceWithMissingDependency(IMissingDependency dependency)
        {
            _ = dependency;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

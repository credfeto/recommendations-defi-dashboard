using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace Credfeto.Defi.Server.Composition.Tests;

public sealed class HealthEndpointHandlersTests : TestBase
{
    [Fact]
    public void Ping_ReturnsOkWithPong()
    {
        IResult result = HealthEndpointHandlers.Ping();

        Ok<string> ok = Assert.IsType<Ok<string>>(result);
        Assert.Equal(expected: "pong", actual: ok.Value);
    }
}

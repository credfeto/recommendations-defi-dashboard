using Microsoft.AspNetCore.Http;

namespace Credfeto.Defi.Server.Composition;

public static class HealthEndpointHandlers
{
    public static IResult Ping()
    {
        return Results.Ok("pong");
    }
}

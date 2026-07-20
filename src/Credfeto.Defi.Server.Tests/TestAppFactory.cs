using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Credfeto.Defi.Server.Tests;

internal static class TestAppFactory
{
    public static WebApplication BuildApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();

        builder.Configuration.Sources.Clear();
        _ = builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DatabaseConfiguration:Provider"] = "mssql",
                ["DatabaseConfiguration:ConnectionString"] =
                    "Server=(local);Database=defi_test;Integrated Security=true;Connection Timeout=1;",
                ["Rpc:Ethereum"] = string.Empty,
                ["Rpc:Arbitrum"] = string.Empty,
                ["Rpc:Base"] = string.Empty,
                ["Rpc:Bsc"] = string.Empty,
            }
        );

        _ = builder.AddDefiServices();

        return builder.Build();
    }
}

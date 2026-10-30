using System;
using System.Collections.Generic;
using Credfeto.Defi.Server.Tests.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Credfeto.Defi.Server.Tests;

internal static class TestAppFactory
{
    public const string TEST_COINGECKO_API_KEY = "test-coingecko-api-key";

    public static WebApplication BuildApp()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();

        builder.Configuration.Sources.Clear();
        _ = builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["DatabaseConfiguration:Provider"] = "mssql",
                ["DatabaseConfiguration:ConnectionString"] = TestConnectionStrings.FakeSqlServer,
                ["Rpc:Ethereum"] = string.Empty,
                ["Rpc:Arbitrum"] = string.Empty,
                ["Rpc:Base"] = string.Empty,
                ["Rpc:Bsc"] = string.Empty,
                ["CoinGecko:ApiKey"] = TEST_COINGECKO_API_KEY,
            }
        );

        _ = builder.AddDefiServices();

        return builder.Build();
    }
}

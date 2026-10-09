using System;
using System.Linq;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Services;

public static class PoolTypeService
{
    private static readonly PoolTypeMetadata[] PoolTypes =
    [
        new PoolTypeMetadata
        {
            Id = "ETH",
            Name = "Ethereum & Liquid Staking",
            Description = "Pools featuring ETH, ETH derivative tokens, and liquid staking tokens",
        },
        new PoolTypeMetadata
        {
            Id = "STABLES",
            Name = "Stablecoin Pools",
            Description = "Pools featuring stablecoin tokens",
        },
        new PoolTypeMetadata
        {
            Id = "HIGH_YIELD",
            Name = "High Yield (>5% APY)",
            Description = "Pools with APY greater than 5%",
        },
        new PoolTypeMetadata
        {
            Id = "LOW_TVL",
            Name = "Emerging Pools (<$10M TVL)",
            Description = "Newer pools with lower TVL",
        },
        new PoolTypeMetadata
        {
            Id = "BLUE_CHIP",
            Name = "Blue Chip (>$100M TVL)",
            Description = "Established pools with high TVL",
        },
    ];

    public static PoolTypeMetadata[] GetAllPoolTypes()
    {
        return PoolTypes;
    }

    public static bool IsValidPoolType(string poolTypeId)
    {
        return PoolTypes.Any(pt =>
            string.Equals(a: pt.Id, b: poolTypeId, comparisonType: StringComparison.OrdinalIgnoreCase)
        );
    }
}

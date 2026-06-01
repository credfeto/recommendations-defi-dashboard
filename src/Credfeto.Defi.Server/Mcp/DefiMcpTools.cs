using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services;
using Credfeto.Defi.Server.Utils;
using ModelContextProtocol.Server;

namespace Credfeto.Defi.Server.Mcp;

/// <summary>
///     MCP tool implementations for the DeFi Dashboard.
/// </summary>
[McpServerToolType]
public sealed class DefiMcpTools
{
    private readonly ContractSecurityService _contractSecurityService;
    private readonly PoolEnrichmentService _enrichmentService;

    /// <summary>
    ///     Initialises a new instance of <see cref="DefiMcpTools" />.
    /// </summary>
    public DefiMcpTools(PoolEnrichmentService enrichmentService, ContractSecurityService contractSecurityService)
    {
        this._enrichmentService = enrichmentService;
        this._contractSecurityService = contractSecurityService;
    }

    /// <summary>
    ///     Returns the available DeFi pool categories that can be queried.
    /// </summary>
    [McpServerTool(Name = "get_pool_types", Title = "Get Pool Types")]
    public PoolTypeMetadata[] GetPoolTypes()
    {
        return PoolTypeService.GetAllPoolTypes();
    }

    /// <summary>
    ///     Fetches enriched DeFi pool recommendations for a given category.
    ///     Returns pools with APY, TVL, hack history, depeg alerts, audit info, contract security,
    ///     contract addresses, KYC requirements, and liquidity/exit info.
    /// </summary>
    [McpServerTool(Name = "get_pools", Title = "Get Pools")]
    public async Task<IReadOnlyList<Pool>> GetPoolsAsync(
        string poolType,
        int limit = 10,
        CancellationToken cancellationToken = default
    )
    {
        if (!PoolTypeService.IsValidPoolType(poolType))
        {
            return [];
        }

        int clampedLimit = Math.Clamp(value: limit, min: 1, max: 50);

        IReadOnlyList<RawPool> allPools = await this._enrichmentService.GetAllPoolsAsync(cancellationToken);
        IReadOnlyList<RawPool> filtered = PoolFilterService.FilterPoolsByType(allPools: allPools, poolType: poolType);
        IReadOnlyList<RawPool> sliced = Slice(list: filtered, count: clampedLimit);

        return await this._enrichmentService.EnrichPoolsAsync(
            filteredPools: sliced,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    ///     Checks GoPlus contract security info for one or more token addresses on a given chain.
    ///     Returns honeypot status, tax info, proxy detection, and open-source status.
    /// </summary>
    [McpServerTool(Name = "check_contract_security", Title = "Check Contract Security")]
    public async Task<IReadOnlyList<ContractSecurityInfo>> CheckContractSecurityAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken = default
    )
    {
        // Reject any address that is not a valid 0x-prefixed 40-hex-char Ethereum address
        // to prevent malformed input reaching the GoPlus URL query string.
        IReadOnlyList<string> validated = [.. addresses.Where(ContractAddressUtils.IsContractAddress).Take(10)];

        return await this._contractSecurityService.GetContractSecurityForAddressesAsync(
            chain: chain,
            addresses: validated,
            cancellationToken: cancellationToken
        );
    }

    private static IReadOnlyList<T> Slice<T>(IReadOnlyList<T> list, int count)
    {
        if (list.Count <= count)
        {
            return list;
        }

        T[] sliced = new T[count];

        for (int i = 0; i < count; i++)
        {
            sliced[i] = list[i];
        }

        return sliced;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Services;
using Credfeto.Defi.Services.Utils;
using ModelContextProtocol.Server;

namespace Credfeto.Defi.Mcp;

[McpServerToolType]
public sealed class DefiMcpTools
{
    private readonly ContractSecurityService _contractSecurityService;
    private readonly PoolEnrichmentService _enrichmentService;

    public DefiMcpTools(PoolEnrichmentService enrichmentService, ContractSecurityService contractSecurityService)
    {
        this._enrichmentService = enrichmentService;
        this._contractSecurityService = contractSecurityService;
    }

    [McpServerTool(Name = "get_pool_types", Title = "Get Pool Types")]
    public static PoolTypeMetadata[] GetPoolTypes()
    {
        return PoolTypeService.GetAllPoolTypes();
    }

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

    [McpServerTool(Name = "check_contract_security", Title = "Check Contract Security")]
    public async Task<IReadOnlyList<ContractSecurityInfo>> CheckContractSecurityAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken = default
    )
    {
        // Reject any address that is not a valid 0x-prefixed 40-hex-char Ethereum address
        // to prevent malformed input reaching the upstream GoPlus/Honeypot.is API query strings.
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

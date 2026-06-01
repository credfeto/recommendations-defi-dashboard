using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.Config;
using Credfeto.Defi.Server.Json;
using Credfeto.Defi.Server.Models;
using Credfeto.Defi.Server.Services.LoggingExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credfeto.Defi.Server.Services;

/// <summary>
///     Resolves the implementation address of upgradeable proxy contracts via raw JSON-RPC calls.
/// </summary>
internal sealed class ProxyResolverService
{
    /// <summary>
    ///     EIP-1967 implementation slot (keccak256("eip1967.proxy.implementation") - 1).
    ///     Most modern upgradeable proxies (OpenZeppelin TransparentUpgradeableProxy, UUPS) store
    ///     the implementation address here.
    /// </summary>
    private const string SLOT_EIP1967 = "0x360894a13ba1a3210667c828492db98dca3e2076cc3735a920a3ca505d382bbc";

    /// <summary>
    ///     EIP-1967 beacon slot (keccak256("eip1967.proxy.beacon") - 1).
    ///     Beacon proxies store the beacon address here; the beacon holds the impl.
    /// </summary>
    private const string SLOT_EIP1967_BEACON = "0xa3f0ad74e5423aebfd80d3ef4346578335a9a72aeaee59ff6cb3582b35133d50";

    /// <summary>
    ///     OpenZeppelin legacy slot (keccak256("org.zeppelinos.proxy.implementation")).
    ///     Used by older OZ proxy contracts before EIP-1967.
    /// </summary>
    private const string SLOT_OZ_LEGACY = "0x7050c9e0f4ca769c69bd3a8ef740bc37934f8e2c036e5a723fd8ee048ed3f8c3";

    private const string ZERO_RESULT = "0x" + "0000000000000000000000000000000000000000000000000000000000000000";

    private static readonly string[] ProxySlots = [SLOT_EIP1967, SLOT_EIP1967_BEACON, SLOT_OZ_LEGACY];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly RpcConfig _rpcConfig;

    /// <summary>
    ///     Initialises a new instance of <see cref="ProxyResolverService" />.
    /// </summary>
    public ProxyResolverService(IOptions<RpcConfig> rpcConfig, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        this._rpcConfig = rpcConfig.Value;
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    /// <summary>
    ///     Attempts to resolve the implementation address of an upgradeable proxy.
    ///     Tries slots in order: EIP-1967 → EIP-1967 beacon → OZ legacy.
    ///     Returns the implementation address (lowercase) or null if no RPC is configured
    ///     for the chain, or no known proxy slot contains a non-zero address.
    /// </summary>
    public async ValueTask<string?> ResolveProxyImplementationAsync(
        string chain,
        string proxyAddress,
        CancellationToken cancellationToken
    )
    {
        string? rpcUrl = this.GetRpcUrl(chain);

        if (string.IsNullOrEmpty(rpcUrl))
        {
            return null;
        }

        foreach (string slot in ProxySlots)
        {
            string? value = await this.GetStorageAtAsync(
                rpcUrl: rpcUrl,
                address: proxyAddress,
                slot: slot,
                cancellationToken: cancellationToken
            );

            if (value is not null)
            {
                string? impl = ExtractAddress(value);

                if (impl is not null)
                {
                    return impl;
                }
            }
        }

        return null;
    }

    private string? GetRpcUrl(string chain)
    {
        if (string.Equals(a: chain, b: "Ethereum", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(this._rpcConfig.Ethereum) ? null : this._rpcConfig.Ethereum;
        }

        if (string.Equals(a: chain, b: "Arbitrum", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(this._rpcConfig.Arbitrum) ? null : this._rpcConfig.Arbitrum;
        }

        if (string.Equals(a: chain, b: "Base", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(this._rpcConfig.Base) ? null : this._rpcConfig.Base;
        }

        if (!string.Equals(a: chain, b: "BSC", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return string.IsNullOrEmpty(this._rpcConfig.Bsc) ? null : this._rpcConfig.Bsc;
    }

    private async ValueTask<string?> GetStorageAtAsync(
        string rpcUrl,
        string address,
        string slot,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using HttpClient client = this._httpClientFactory.CreateClient(nameof(ProxyResolverService));

            RpcRequest request = new()
            {
                Jsonrpc = "2.0",
                Method = "eth_getStorageAt",
                Params = [address, slot, "latest"],
                Id = 1,
            };

            using HttpResponseMessage response = await client.PostAsJsonAsync(
                requestUri: rpcUrl,
                value: request,
                jsonTypeInfo: AppJsonContext.Default.RpcRequest,
                cancellationToken: cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            RpcResponse? rpcResponse = await response.Content.ReadFromJsonAsync(
                jsonTypeInfo: AppJsonContext.Default.RpcResponse,
                cancellationToken: cancellationToken
            );

            return rpcResponse?.Result;
        }
        catch (Exception ex)
        {
            this._logger.GetStorageAtFailed(address: address, slot: slot, exception: ex);

            return null;
        }
    }

    private static string? ExtractAddress(string storageValue)
    {
        if (
            string.IsNullOrEmpty(storageValue)
            || string.Equals(a: storageValue, b: ZERO_RESULT, comparisonType: StringComparison.OrdinalIgnoreCase)
        )
        {
            return null;
        }

        string hex = storageValue.Replace(
            oldValue: "0x",
            newValue: string.Empty,
            comparisonType: StringComparison.OrdinalIgnoreCase
        );

        if (hex.Length != 64)
        {
            return null;
        }

        string addr = "0x" + hex[^40..];
        string zeroAddress = "0x" + new string(c: '0', count: 40);

        return string.Equals(a: addr, b: zeroAddress, comparisonType: StringComparison.OrdinalIgnoreCase)
            ? null
            : addr.ToLowerInvariant();
    }
}

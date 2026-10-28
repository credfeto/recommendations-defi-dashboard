using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Config;
using Credfeto.Defi.Data.Models.Json;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Services.LoggingExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credfeto.Defi.Services;

public sealed class ProxyResolverService
{
    // keccak256("eip1967.proxy.implementation") - 1.
    // OpenZeppelin TransparentUpgradeableProxy and UUPS proxies store the implementation here.
    private const string SLOT_EIP1967 = "0x360894a13ba1a3210667c828492db98dca3e2076cc3735a920a3ca505d382bbc";

    // keccak256("eip1967.proxy.beacon") - 1: beacon proxies store the beacon address here; the beacon holds the impl.
    private const string SLOT_EIP1967_BEACON = "0xa3f0ad74e5423aebfd80d3ef4346578335a9a72aeaee59ff6cb3582b35133d50";

    // keccak256("org.zeppelinos.proxy.implementation"): used by older OpenZeppelin proxies that predate EIP-1967.
    private const string SLOT_OZ_LEGACY = "0x7050c9e0f4ca769c69bd3a8ef740bc37934f8e2c036e5a723fd8ee048ed3f8c3";

    // bytes4(keccak256("implementation()")): the IBeacon getter for the current implementation.
    private const string SELECTOR_IMPLEMENTATION = "0x5c60da1b";

    private const string BLOCK_LATEST = "latest";

    private const string ZERO_RESULT = "0x" + "0000000000000000000000000000000000000000000000000000000000000000";

    // Probe order matters: the first slot holding a non-zero address wins,
    // so EIP-1967 then EIP-1967 beacon then OZ legacy.
    private static readonly string[] ProxySlots = [SLOT_EIP1967, SLOT_EIP1967_BEACON, SLOT_OZ_LEGACY];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyResolverService> _logger;
    private readonly RpcConfig _rpcConfig;

    public ProxyResolverService(
        IOptions<RpcConfig> rpcConfig,
        IHttpClientFactory httpClientFactory,
        ILogger<ProxyResolverService> logger
    )
    {
        this._rpcConfig = rpcConfig.Value;
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

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
            string? impl = await this.ResolveSlotAsync(
                rpcUrl: rpcUrl,
                proxyAddress: proxyAddress,
                slot: slot,
                cancellationToken: cancellationToken
            );

            if (impl is not null)
            {
                return impl;
            }
        }

        return null;
    }

    private async ValueTask<string?> ResolveSlotAsync(
        string rpcUrl,
        string proxyAddress,
        string slot,
        CancellationToken cancellationToken
    )
    {
        string? value = await this.GetStorageAtAsync(
            rpcUrl: rpcUrl,
            address: proxyAddress,
            slot: slot,
            cancellationToken: cancellationToken
        );

        string? address = value is null ? null : ExtractAddress(value);

        if (
            address is null
            || !string.Equals(a: slot, b: SLOT_EIP1967_BEACON, comparisonType: StringComparison.Ordinal)
        )
        {
            return address;
        }

        // The beacon slot holds the beacon contract rather than the implementation, so it must never be returned as-is.
        return await this.GetBeaconImplementationAsync(
            rpcUrl: rpcUrl,
            beaconAddress: address,
            cancellationToken: cancellationToken
        );
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
            RpcRequest request = new()
            {
                Jsonrpc = "2.0",
                Method = "eth_getStorageAt",
                Params = [address, slot, BLOCK_LATEST],
                Id = 1,
            };

            return await this.SendRpcAsync(
                rpcUrl: rpcUrl,
                request: request,
                requestTypeInfo: AppJsonContext.Default.RpcRequest,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            this._logger.GetStorageAtFailed(address: address, slot: slot, exception: ex);

            return null;
        }
    }

    private async ValueTask<string?> GetBeaconImplementationAsync(
        string rpcUrl,
        string beaconAddress,
        CancellationToken cancellationToken
    )
    {
        try
        {
            RpcEthCallRequest request = new()
            {
                Jsonrpc = "2.0",
                Method = "eth_call",
                Params = [new EthCallTransaction { To = beaconAddress, Data = SELECTOR_IMPLEMENTATION }, BLOCK_LATEST],
                Id = 1,
            };

            string? value = await this.SendRpcAsync(
                rpcUrl: rpcUrl,
                request: request,
                requestTypeInfo: AppJsonContext.Default.RpcEthCallRequest,
                cancellationToken: cancellationToken
            );

            return value is null ? null : ExtractAddress(value);
        }
        catch (Exception ex)
        {
            this._logger.BeaconImplementationCallFailed(beacon: beaconAddress, exception: ex);

            return null;
        }
    }

    private async ValueTask<string?> SendRpcAsync<TRequest>(
        string rpcUrl,
        TRequest request,
        JsonTypeInfo<TRequest> requestTypeInfo,
        CancellationToken cancellationToken
    )
    {
        using HttpClient client = this._httpClientFactory.CreateClient(nameof(ProxyResolverService));

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            requestUri: rpcUrl,
            value: request,
            jsonTypeInfo: requestTypeInfo,
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

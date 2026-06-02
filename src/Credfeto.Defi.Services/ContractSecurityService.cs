using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.GoPlus.Interfaces;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Database;

namespace Credfeto.Defi.Services;

/// <summary>
///     Fetches and caches GoPlus contract security information for pool token addresses.
/// </summary>
public sealed class ContractSecurityService
{
    private readonly ContractSecurityCacheService _cache;
    private readonly IGoPlusClient _goPlusClient;
    private readonly ProxyResolverService _proxyResolver;

    /// <summary>
    ///     Initialises a new instance of <see cref="ContractSecurityService" />.
    /// </summary>
    public ContractSecurityService(
        IGoPlusClient goPlusClient,
        ContractSecurityCacheService cache,
        ProxyResolverService proxyResolver
    )
    {
        this._goPlusClient = goPlusClient;
        this._cache = cache;
        this._proxyResolver = proxyResolver;
    }

    /// <summary>
    ///     Returns security info for each address, plus proxy implementation rows.
    ///
    ///     For each address:
    ///     1. Return DB row if checked within the last 24 h.
    ///     2. Otherwise fetch from GoPlus, persist result.
    ///     3. If the contract is an upgradeable proxy, resolve its implementation
    ///        address via RPC, fetch + persist that too (with ParentAddress set).
    ///
    ///     Returns a flat list of <see cref="ContractSecurityInfo" /> covering all addresses and
    ///     their implementations.
    /// </summary>
    public async ValueTask<IReadOnlyList<ContractSecurityInfo>> GetContractSecurityForAddressesAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    )
    {
        if (addresses.Count == 0)
        {
            return [];
        }

        (List<ContractSecurityInfo> results, List<string> staleAddresses) = await this.SeparateCachedAsync(
            chain: chain,
            addresses: addresses,
            cancellationToken: cancellationToken
        );

        if (staleAddresses.Count == 0)
        {
            return results;
        }

        await this.FetchAndCacheStaleAsync(
            chain: chain,
            staleAddresses: staleAddresses,
            results: results,
            cancellationToken: cancellationToken
        );

        return results;
    }

    private async ValueTask<(List<ContractSecurityInfo> Results, List<string> StaleAddresses)> SeparateCachedAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    )
    {
        List<ContractSecurityInfo> results = [];
        List<string> staleAddresses = [];

        foreach (string addr in addresses)
        {
            ContractSecurityInfo? cached = await this._cache.GetAsync(
                chain: chain,
                address: addr,
                cancellationToken: cancellationToken
            );

            if (cached is not null)
            {
                results.Add(cached);

                if (cached.IsProxy is >= 0.5)
                {
                    IReadOnlyList<ContractSecurityInfo> children = await this._cache.GetChildrenAsync(
                        chain: chain,
                        parentAddress: addr,
                        cancellationToken: cancellationToken
                    );
                    results.AddRange(children);
                }
            }
            else
            {
                staleAddresses.Add(addr);
            }
        }

        return (results, staleAddresses);
    }

    private async ValueTask FetchAndCacheStaleAsync(
        string chain,
        List<string> staleAddresses,
        List<ContractSecurityInfo> results,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyDictionary<string, GoPlusTokenResult> goplusMap = await this._goPlusClient.FetchTokenSecurityAsync(
            chain: chain,
            addresses: staleAddresses,
            cancellationToken: cancellationToken
        );

        foreach (string addr in staleAddresses)
        {
            if (!goplusMap.TryGetValue(key: addr.ToLowerInvariant(), out GoPlusTokenResult? raw))
            {
                continue;
            }

            ContractSecurityInfo info = RawToInfo(chain: chain, address: addr, parentAddress: null, raw: raw);
            await this._cache.SetAsync(info: info, cancellationToken: cancellationToken);
            results.Add(info);

            if (info.IsProxy is >= 0.5)
            {
                await this.ResolveAndCacheProxyImplAsync(
                    chain: chain,
                    proxyAddr: addr,
                    results: results,
                    cancellationToken: cancellationToken
                );
            }
        }
    }

    private async ValueTask ResolveAndCacheProxyImplAsync(
        string chain,
        string proxyAddr,
        List<ContractSecurityInfo> results,
        CancellationToken cancellationToken
    )
    {
        string? implAddr = await this._proxyResolver.ResolveProxyImplementationAsync(
            chain: chain,
            proxyAddress: proxyAddr,
            cancellationToken: cancellationToken
        );

        if (string.IsNullOrEmpty(implAddr))
        {
            return;
        }

        IReadOnlyDictionary<string, GoPlusTokenResult> implMap = await this._goPlusClient.FetchTokenSecurityAsync(
            chain: chain,
            addresses: [implAddr],
            cancellationToken: cancellationToken
        );

        if (implMap.TryGetValue(key: implAddr.ToLowerInvariant(), out GoPlusTokenResult? implRaw))
        {
            ContractSecurityInfo implInfo = RawToInfo(
                chain: chain,
                address: implAddr,
                parentAddress: proxyAddr.ToLowerInvariant(),
                raw: implRaw
            );
            await this._cache.SetAsync(info: implInfo, cancellationToken: cancellationToken);
            results.Add(implInfo);
        }
    }

    private static double? ParseNum(string? val)
    {
        if (string.IsNullOrEmpty(val))
        {
            return null;
        }

        return double.TryParse(
            s: val,
            style: NumberStyles.Any,
            provider: CultureInfo.InvariantCulture,
            result: out double n
        )
            ? n
            : null;
    }

    private static ContractSecurityInfo RawToInfo(
        string chain,
        string address,
        string? parentAddress,
        GoPlusTokenResult raw
    )
    {
        return new ContractSecurityInfo
        {
            Chain = chain,
            Address = address.ToLowerInvariant(),
            ParentAddress = parentAddress,
            IsOpenSource = ParseNum(raw.IsOpenSource),
            IsHoneypot = ParseNum(raw.IsHoneypot),
            IsProxy = ParseNum(raw.IsProxy),
            BuyTax = ParseNum(raw.BuyTax),
            SellTax = ParseNum(raw.SellTax),
            TransferTax = ParseNum(raw.TransferTax),
            CannotBuy = ParseNum(raw.CannotBuy),
            HoneypotWithSameCreator = ParseNum(raw.HoneypotWithSameCreator),
            TokenName = raw.TokenName,
            TokenSymbol = raw.TokenSymbol,
        };
    }
}

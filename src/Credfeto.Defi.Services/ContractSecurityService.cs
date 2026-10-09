using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.ApiClients.GoPlus.Interfaces;
using Credfeto.Defi.ApiClients.HoneypotIs.Interfaces;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage;

namespace Credfeto.Defi.Services;

public sealed class ContractSecurityService
{
    private readonly ContractSecurityCacheService _cache;
    private readonly IGoPlusClient _goPlusClient;
    private readonly IHoneypotIsClient _honeypotIsClient;
    private readonly ProxyResolverService _proxyResolver;

    public ContractSecurityService(
        IGoPlusClient goPlusClient,
        IHoneypotIsClient honeypotIsClient,
        ContractSecurityCacheService cache,
        ProxyResolverService proxyResolver
    )
    {
        this._goPlusClient = goPlusClient;
        this._honeypotIsClient = honeypotIsClient;
        this._cache = cache;
        this._proxyResolver = proxyResolver;
    }

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

        List<ContractSecurityInfo>[] combined = await Task.WhenAll(
            this.GetGoPlusResultsAsync(chain: chain, addresses: addresses, cancellationToken: cancellationToken),
            this.GetHoneypotIsResultsAsync(chain: chain, addresses: addresses, cancellationToken: cancellationToken)
        );

        // Both sources' rows are kept side by side: interpreting agreement or disagreement is left to callers.
        combined[0].AddRange(combined[1]);

        return combined[0];
    }

    private async Task<List<ContractSecurityInfo>> GetGoPlusResultsAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    )
    {
        (List<ContractSecurityInfo> results, List<string> staleAddresses) = await SplitCachedAsync(
            addresses: addresses,
            getCached: (addr, ct) => this._cache.GetAsync(chain: chain, address: addr, cancellationToken: ct),
            cancellationToken: cancellationToken
        );

        int cachedCount = results.Count;

        for (int i = 0; i < cachedCount; ++i)
        {
            ContractSecurityInfo cached = results[i];

            if (cached.IsProxy is true)
            {
                IReadOnlyList<ContractSecurityInfo> children = await this._cache.GetChildrenAsync(
                    chain: chain,
                    parentAddress: cached.Address,
                    cancellationToken: cancellationToken
                );
                results.AddRange(children);
            }
        }

        if (staleAddresses.Count != 0)
        {
            await this.FetchAndCacheStaleAsync(
                chain: chain,
                staleAddresses: staleAddresses,
                results: results,
                cancellationToken: cancellationToken
            );
        }

        return results;
    }

    private static async ValueTask<(List<ContractSecurityInfo> Results, List<string> StaleAddresses)> SplitCachedAsync(
        IReadOnlyList<string> addresses,
        Func<string, CancellationToken, ValueTask<ContractSecurityInfo?>> getCached,
        CancellationToken cancellationToken
    )
    {
        List<ContractSecurityInfo> results = [];
        List<string> staleAddresses = [];

        foreach (string addr in addresses)
        {
            ContractSecurityInfo? cached = await getCached(addr, cancellationToken);

            if (cached is not null)
            {
                results.Add(cached);
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
            string loweredAddr = addr.ToLowerInvariant();

            if (!goplusMap.TryGetValue(key: loweredAddr, out GoPlusTokenResult? raw))
            {
                continue;
            }

            ContractSecurityInfo info = RawToInfo(chain: chain, address: loweredAddr, parentAddress: null, raw: raw);
            await this._cache.SetAsync(info: info, cancellationToken: cancellationToken);
            results.Add(info);

            if (info.IsProxy is true)
            {
                await this.ResolveAndCacheProxyImplAsync(
                    chain: chain,
                    proxyAddr: loweredAddr,
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

        string loweredImplAddr = implAddr.ToLowerInvariant();

        if (implMap.TryGetValue(key: loweredImplAddr, out GoPlusTokenResult? implRaw))
        {
            ContractSecurityInfo implInfo = RawToInfo(
                chain: chain,
                address: loweredImplAddr,
                parentAddress: proxyAddr,
                raw: implRaw
            );
            await this._cache.SetAsync(info: implInfo, cancellationToken: cancellationToken);
            results.Add(implInfo);
        }
    }

    private async Task<List<ContractSecurityInfo>> GetHoneypotIsResultsAsync(
        string chain,
        IReadOnlyList<string> addresses,
        CancellationToken cancellationToken
    )
    {
        (List<ContractSecurityInfo> results, List<string> staleAddresses) = await SplitCachedAsync(
            addresses: addresses,
            getCached: (addr, ct) => this._cache.GetHoneypotIsAsync(chain: chain, address: addr, cancellationToken: ct),
            cancellationToken: cancellationToken
        );

        if (staleAddresses.Count == 0)
        {
            return results;
        }

        IReadOnlyDictionary<string, HoneypotIsResult> honeypotIsMap =
            await this._honeypotIsClient.FetchTokenSecurityAsync(
                chain: chain,
                addresses: staleAddresses,
                cancellationToken: cancellationToken
            );

        foreach (string addr in staleAddresses)
        {
            string loweredAddr = addr.ToLowerInvariant();

            if (!honeypotIsMap.TryGetValue(key: loweredAddr, out HoneypotIsResult? raw))
            {
                continue;
            }

            ContractSecurityInfo info = HoneypotRawToInfo(chain: chain, address: loweredAddr, raw: raw);
            await this._cache.SetHoneypotIsAsync(info: info, cancellationToken: cancellationToken);
            results.Add(info);
        }

        return results;
    }

    private static ContractSecurityInfo HoneypotRawToInfo(string chain, string address, HoneypotIsResult raw)
    {
        return new ContractSecurityInfo
        {
            Chain = chain,
            Address = address,
            Source = ContractSecuritySource.HoneypotIs,
            IsHoneypot = raw.IsHoneypot,
            BuyTax = PercentToFraction(raw.BuyTax),
            SellTax = PercentToFraction(raw.SellTax),
            SimulationSuccess = raw.SimulationSuccess,
        };
    }

    private static double? PercentToFraction(double? percent)
    {
        return percent / 100.0;
    }

    private static bool? ParseBool(string? val)
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
            ? n >= 0.5
            : null;
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
            Address = address,
            Source = ContractSecuritySource.GoPlus,
            ParentAddress = parentAddress,
            IsOpenSource = ParseBool(raw.IsOpenSource),
            IsHoneypot = ParseBool(raw.IsHoneypot),
            IsProxy = ParseBool(raw.IsProxy),
            BuyTax = ParseNum(raw.BuyTax),
            SellTax = ParseNum(raw.SellTax),
            TransferTax = ParseNum(raw.TransferTax),
            CannotBuy = ParseBool(raw.CannotBuy),
            HoneypotWithSameCreator = ParseBool(raw.HoneypotWithSameCreator),
            TokenName = raw.TokenName,
            TokenSymbol = raw.TokenSymbol,
        };
    }
}

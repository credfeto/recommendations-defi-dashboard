using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Services.Utils;

/// <summary>
///     Utilities for extracting on-chain contract addresses from pool data.
/// </summary>
public static partial class ContractAddressUtils
{
    [GeneratedRegex(
        pattern: "^0x[0-9a-fA-F]{40}$",
        options: RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 500
    )]
    private static partial Regex EthAddressRegex { get; }

    /// <summary>
    ///     Returns true if the given string looks like an Ethereum-style hex address.
    /// </summary>
    public static bool IsContractAddress(string value)
    {
        return EthAddressRegex.IsMatch(value);
    }

    /// <summary>
    ///     Builds the deduplicated list of on-chain contract addresses for a pool.
    ///
    ///     Sources (in priority order):
    ///     1. UnderlyingTokens -- the underlying asset contract addresses
    ///     2. RewardTokens -- reward token contract addresses
    ///     3. PoolId -- used directly when it is a 0x address (e.g. Pendle market contracts)
    /// </summary>
    public static string[] BuildContractAddresses(RawPool pool)
    {
        HashSet<string> addresses = new(StringComparer.OrdinalIgnoreCase);

        IEnumerable<string> underlying = pool.UnderlyingTokens?.Where(IsContractAddress) ?? [];

        foreach (string addr in underlying)
        {
            _ = addresses.Add(addr.ToLowerInvariant());
        }

        IEnumerable<string> rewards = pool.RewardTokens?.Where(IsContractAddress) ?? [];

        foreach (string addr in rewards)
        {
            _ = addresses.Add(addr.ToLowerInvariant());
        }

        if (IsContractAddress(pool.PoolId))
        {
            _ = addresses.Add(pool.PoolId.ToLowerInvariant());
        }

        string[] result = new string[addresses.Count];
        addresses.CopyTo(result);

        return result;
    }
}

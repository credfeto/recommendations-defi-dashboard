using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

/// <summary>
///     GoPlus contract security information for an on-chain token contract.
/// </summary>
[DebuggerDisplay("{Chain}/{Address} isProxy={IsProxy} isHoneypot={IsHoneypot}")]
public sealed record ContractSecurityInfo
{
    /// <summary>
    ///     Chain name (e.g. "Ethereum").
    /// </summary>
    public required string Chain { get; init; }

    /// <summary>
    ///     Lowercased contract address.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    ///     When this row is a proxy implementation, points to the proxy contract address.
    /// </summary>
    public string? ParentAddress { get; init; }

    /// <summary>
    ///     True = open source, false = not open source, null = unknown.
    /// </summary>
    public bool? IsOpenSource { get; init; }

    /// <summary>
    ///     True = honeypot, false = not honeypot, null = unknown.
    /// </summary>
    public bool? IsHoneypot { get; init; }

    /// <summary>
    ///     True = proxy contract, false = not proxy, null = unknown.
    /// </summary>
    public bool? IsProxy { get; init; }

    /// <summary>
    ///     Buy tax as a decimal fraction (e.g. 0.05 = 5%).
    /// </summary>
    public double? BuyTax { get; init; }

    /// <summary>
    ///     Sell tax as a decimal fraction (e.g. 0.05 = 5%).
    /// </summary>
    public double? SellTax { get; init; }

    /// <summary>
    ///     Transfer tax as a decimal fraction.
    /// </summary>
    public double? TransferTax { get; init; }

    /// <summary>
    ///     True = cannot buy, false = can buy, null = unknown.
    /// </summary>
    public bool? CannotBuy { get; init; }

    /// <summary>
    ///     True = honeypot with same creator exists, null = unknown.
    /// </summary>
    public bool? HoneypotWithSameCreator { get; init; }

    /// <summary>
    ///     Token name as reported by the contract.
    /// </summary>
    public string? TokenName { get; init; }

    /// <summary>
    ///     Token symbol as reported by the contract.
    /// </summary>
    public string? TokenSymbol { get; init; }
}

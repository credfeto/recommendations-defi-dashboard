using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("{Chain}/{Address} source={Source} isProxy={IsProxy} isHoneypot={IsHoneypot}")]
public sealed record ContractSecurityInfo
{
    public required string Chain { get; init; }

    public required string Address { get; init; }

    public required string Source { get; init; }

    public string? ParentAddress { get; init; }

    public bool? IsOpenSource { get; init; }

    public bool? IsHoneypot { get; init; }

    public bool? IsProxy { get; init; }

    public double? BuyTax { get; init; }

    public double? SellTax { get; init; }

    public double? TransferTax { get; init; }

    public bool? CannotBuy { get; init; }

    public bool? HoneypotWithSameCreator { get; init; }

    public string? TokenName { get; init; }

    public string? TokenSymbol { get; init; }

    public bool? SimulationSuccess { get; init; }
}

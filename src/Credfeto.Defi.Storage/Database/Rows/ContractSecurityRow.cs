using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Chain}/{Address} isProxy={IsProxy} isHoneypot={IsHoneypot}")]
public sealed record ContractSecurityRow(
    string Chain,
    string Address,
    string? ParentAddress,
    bool? IsOpenSource,
    bool? IsHoneypot,
    bool? IsProxy,
    double? BuyTax,
    double? SellTax,
    double? TransferTax,
    bool? CannotBuy,
    bool? HoneypotWithSameCreator,
    string? TokenName,
    string? TokenSymbol,
    DateTimeOffset CheckedAt
);

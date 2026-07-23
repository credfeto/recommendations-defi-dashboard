using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{Chain}/{Address} isProxy={IsProxy} isHoneypot={IsHoneypot}")]
[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Instantiated in ContractSecurityCacheService"
)]
internal sealed record GoPlusTokenSecuritySyncRow(
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
    string? TokenSymbol
);

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{Name}")]
[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Instantiated in DefiLlamaHackStorageService"
)]
internal sealed record DefiLlamaHackSyncRow(
    string Name,
    DateTimeOffset HackDate,
    string? Classification,
    string? Technique,
    decimal Amount,
    string Source,
    string? ParentProtocolId
);

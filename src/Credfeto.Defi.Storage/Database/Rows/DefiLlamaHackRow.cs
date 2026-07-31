using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Name}")]
public sealed record DefiLlamaHackRow(
    string Name,
    DateTimeOffset HackDate,
    string? Classification,
    string? Technique,
    decimal Amount,
    string Source,
    string? ParentProtocolId,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);

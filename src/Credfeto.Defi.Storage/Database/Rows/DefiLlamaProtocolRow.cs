using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Slug}")]
public sealed record DefiLlamaProtocolRow(
    string Slug,
    string? Audits,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);

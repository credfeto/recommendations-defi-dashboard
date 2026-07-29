using System;
using System.Diagnostics;

namespace Credfeto.Defi.Storage.Database.Rows;

[DebuggerDisplay("{Slug} -> {AuditLink}")]
public sealed record DefiLlamaProtocolAuditLinkRow(
    string Slug,
    string AuditLink,
    DateTimeOffset DateCreated,
    DateTimeOffset DateUpdated,
    DateTimeOffset? DataDate
);

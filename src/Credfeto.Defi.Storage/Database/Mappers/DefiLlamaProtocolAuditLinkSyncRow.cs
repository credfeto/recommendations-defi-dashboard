using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Database.Mappers;

[DebuggerDisplay("{Slug} -> {AuditLink}")]
[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1812:AvoidUninstantiatedInternalClasses",
    Justification = "Instantiated in DefiLlamaProtocolStorageService"
)]
internal sealed record DefiLlamaProtocolAuditLinkSyncRow(string Slug, string AuditLink);

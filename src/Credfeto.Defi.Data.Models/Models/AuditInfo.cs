using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("Audits={Audits}")]
public sealed record AuditInfo
{
    public required int Audits { get; init; }

    public required string[] AuditLinks { get; init; } = [];
}

using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("{Name} ({Date}) ${AmountUsd}")]
public sealed record HackInfo
{
    public required string Name { get; init; }

    public required long Date { get; init; }

    public required decimal AmountUsd { get; init; }

    public required string Classification { get; init; }

    public required string Technique { get; init; }

    public required string Source { get; init; }
}

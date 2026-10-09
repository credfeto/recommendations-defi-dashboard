using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("KycEntry={KycRequiredForEntry} Liquid={IsLiquid}")]
public sealed record PoolAccessInfo
{
    public bool? KycRequiredForEntry { get; init; }

    public bool? KycRequiredForExit { get; init; }

    public bool? CanSwapToExit { get; init; }

    public bool? IsLiquid { get; init; }

    public string? LockupDescription { get; init; }
}

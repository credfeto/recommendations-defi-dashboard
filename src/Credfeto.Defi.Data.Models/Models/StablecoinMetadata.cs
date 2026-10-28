using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Models;

[DebuggerDisplay("{DataSource} {PegType}")]
public readonly record struct StablecoinMetadata(string DataSource, string? PegType);

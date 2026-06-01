using System.Diagnostics;

namespace Credfeto.Defi.Server.Config;

/// <summary>
///     Configuration for the SQLite API cache.
/// </summary>
[DebuggerDisplay("DbDirectory={DbDirectory}")]
public sealed class CacheConfig
{
    /// <summary>
    ///     Directory where the SQLite database file will be stored.
    /// </summary>
    public string DbDirectory { get; set; } = "/app/data";
}

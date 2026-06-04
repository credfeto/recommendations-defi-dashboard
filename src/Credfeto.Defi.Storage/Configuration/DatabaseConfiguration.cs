using System.Diagnostics;

namespace Credfeto.Defi.Storage.Configuration;

[DebuggerDisplay("ConnectionString: {ConnectionString}")]
public sealed class DatabaseConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
}

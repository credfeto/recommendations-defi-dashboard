using System.Diagnostics;

namespace Credfeto.Defi.Data.Models.Config;

[DebuggerDisplay("Ethereum={Ethereum} Arbitrum={Arbitrum} Base={Base} Bsc={Bsc}")]
public sealed class RpcConfig
{
    public string Ethereum { get; set; } = string.Empty;

    public string Arbitrum { get; set; } = string.Empty;

    public string Base { get; set; } = string.Empty;

    public string Bsc { get; set; } = string.Empty;
}

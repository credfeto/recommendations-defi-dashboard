using System.Diagnostics;

namespace Credfeto.Defi.Server.Config;

/// <summary>
///     JSON-RPC endpoint URLs for supported chains.
///     Used for resolving proxy implementation addresses via eth_getStorageAt.
/// </summary>
[DebuggerDisplay("Ethereum={Ethereum} Arbitrum={Arbitrum} Base={Base} Bsc={Bsc}")]
public sealed class RpcConfig
{
    /// <summary>
    ///     RPC URL for Ethereum mainnet, or empty string if not configured.
    /// </summary>
    public string Ethereum { get; set; } = string.Empty;

    /// <summary>
    ///     RPC URL for Arbitrum One, or empty string if not configured.
    /// </summary>
    public string Arbitrum { get; set; } = string.Empty;

    /// <summary>
    ///     RPC URL for Base, or empty string if not configured.
    /// </summary>
    public string Base { get; set; } = string.Empty;

    /// <summary>
    ///     RPC URL for BNB Smart Chain, or empty string if not configured.
    /// </summary>
    public string Bsc { get; set; } = string.Empty;
}

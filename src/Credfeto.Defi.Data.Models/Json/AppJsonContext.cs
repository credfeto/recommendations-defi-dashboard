using System.Collections.Generic;
using System.Text.Json.Serialization;
using Credfeto.Defi.Data.Models.Models;

namespace Credfeto.Defi.Data.Models.Json;

/// <summary>
///     AOT-compatible JSON serialisation context.
///     Covers all models used in HTTP responses and external API response parsing.
/// </summary>
[JsonSerializable(typeof(Pool))]
[JsonSerializable(typeof(Pool[]))]
[JsonSerializable(typeof(IReadOnlyList<Pool>))]
[JsonSerializable(typeof(PoolTypeMetadata))]
[JsonSerializable(typeof(PoolTypeMetadata[]))]
[JsonSerializable(typeof(IReadOnlyList<PoolTypeMetadata>))]
[JsonSerializable(typeof(HackInfo))]
[JsonSerializable(typeof(HackInfo[]))]
[JsonSerializable(typeof(DepegAlert))]
[JsonSerializable(typeof(DepegAlert[]))]
[JsonSerializable(typeof(AuditInfo))]
[JsonSerializable(typeof(ContractSecurityInfo))]
[JsonSerializable(typeof(ContractSecurityInfo[]))]
[JsonSerializable(typeof(IReadOnlyList<ContractSecurityInfo>))]
[JsonSerializable(typeof(PoolAccessInfo))]
[JsonSerializable(typeof(Predictions))]
[JsonSerializable(typeof(RawPool))]
[JsonSerializable(typeof(RawPool[]))]
[JsonSerializable(typeof(IReadOnlyList<RawPool>))]
[JsonSerializable(typeof(RawPredictions))]
[JsonSerializable(typeof(DefiLlamaPoolsResponse))]
[JsonSerializable(typeof(RawHack))]
[JsonSerializable(typeof(RawHack[]))]
[JsonSerializable(typeof(IReadOnlyList<RawHack>))]
[JsonSerializable(typeof(RawProtocol))]
[JsonSerializable(typeof(RawProtocol[]))]
[JsonSerializable(typeof(IReadOnlyList<RawProtocol>))]
[JsonSerializable(typeof(CoinGeckoStablecoin))]
[JsonSerializable(typeof(CoinGeckoStablecoin[]))]
[JsonSerializable(typeof(IReadOnlyList<CoinGeckoStablecoin>))]
[JsonSerializable(typeof(CoinGeckoCoinPlatforms))]
[JsonSerializable(typeof(CoinGeckoCoinPlatforms[]))]
[JsonSerializable(typeof(IReadOnlyList<CoinGeckoCoinPlatforms>))]
[JsonSerializable(typeof(PendleMarketsResponse))]
[JsonSerializable(typeof(PendleMarket))]
[JsonSerializable(typeof(PendleMarket[]))]
[JsonSerializable(typeof(PendleLiquidity))]
[JsonSerializable(typeof(PendleTradingVolume))]
[JsonSerializable(typeof(GoPlusResponse))]
[JsonSerializable(typeof(GoPlusTokenResult))]
[JsonSerializable(typeof(RpcRequest))]
[JsonSerializable(typeof(RpcResponse))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
public sealed partial class AppJsonContext : JsonSerializerContext { }

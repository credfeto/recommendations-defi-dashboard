using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage;

public sealed class DefiLlamaPoolStorage : IDefiLlamaPoolStorage
{
    private readonly IDatabase _database;

    public DefiLlamaPoolStorage(IDatabase database)
    {
        this._database = database;
    }

    public async ValueTask StorePoolsAsync(
        IReadOnlyList<RawPool> pools,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<DefiLlamaPoolSyncRow> poolRows = BuildPoolRows(pools);
        IReadOnlyList<DefiLlamaPoolTokenSyncRow> rewardTokenRows = BuildRewardTokenRows(pools);
        IReadOnlyList<DefiLlamaPoolTokenSyncRow> underlyingTokenRows = BuildUnderlyingTokenRows(pools);

        await this._database.ExecuteAsync(action: SyncPoolsAsync, cancellationToken: cancellationToken);
        await this._database.ExecuteAsync(action: SyncRewardTokensAsync, cancellationToken: cancellationToken);
        await this._database.ExecuteAsync(action: SyncUnderlyingTokensAsync, cancellationToken: cancellationToken);

        ValueTask SyncPoolsAsync(DbConnection c, CancellationToken ct) =>
            DefiLlamaDatabase.Pool_SyncAsync(connection: c, rows: poolRows, dataDate: dataDate, cancellationToken: ct);

        ValueTask SyncRewardTokensAsync(DbConnection c, CancellationToken ct) =>
            DefiLlamaDatabase.PoolRewardToken_SyncAsync(
                connection: c,
                rows: rewardTokenRows,
                dataDate: dataDate,
                cancellationToken: ct
            );

        ValueTask SyncUnderlyingTokensAsync(DbConnection c, CancellationToken ct) =>
            DefiLlamaDatabase.PoolUnderlyingToken_SyncAsync(
                connection: c,
                rows: underlyingTokenRows,
                dataDate: dataDate,
                cancellationToken: ct
            );
    }

    public async ValueTask<IReadOnlyList<RawPool>> GetAllPoolsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DefiLlamaPoolRow> poolRows = await this._database.ExecuteAsync(
            action: DefiLlamaDatabase.Pool_GetAllAsync,
            cancellationToken: cancellationToken
        );

        IReadOnlyList<DefiLlamaPoolRewardTokenRow> rewardTokenRows = await this._database.ExecuteAsync(
            action: DefiLlamaDatabase.PoolRewardToken_GetAllAsync,
            cancellationToken: cancellationToken
        );

        IReadOnlyList<DefiLlamaPoolUnderlyingTokenRow> underlyingTokenRows = await this._database.ExecuteAsync(
            action: DefiLlamaDatabase.PoolUnderlyingToken_GetAllAsync,
            cancellationToken: cancellationToken
        );

        return MapToRawPools(
            poolRows: poolRows,
            rewardTokenRows: rewardTokenRows,
            underlyingTokenRows: underlyingTokenRows
        );
    }

    private static IReadOnlyList<DefiLlamaPoolSyncRow> BuildPoolRows(IReadOnlyList<RawPool> pools)
    {
        DefiLlamaPoolSyncRow[] rows = new DefiLlamaPoolSyncRow[pools.Count];

        for (int i = 0; i < pools.Count; i++)
        {
            RawPool p = pools[i];
            rows[i] = new DefiLlamaPoolSyncRow(
                PoolId: p.PoolId,
                Chain: p.Chain,
                Project: p.Project,
                Symbol: p.Symbol,
                TvlUsd: p.TvlUsd,
                ApyBase: p.ApyBase,
                ApyReward: p.ApyReward,
                Apy: p.Apy,
                ApyPct1D: p.ApyPct1D,
                ApyPct7D: p.ApyPct7D,
                ApyPct30D: p.ApyPct30D,
                Stablecoin: p.Stablecoin,
                IlRisk: p.IlRisk,
                Exposure: p.Exposure,
                PredictedClass: p.Predictions?.PredictedClass,
                PredictedProbability: p.Predictions?.PredictedProbability,
                BinnedConfidence: p.Predictions?.BinnedConfidence,
                PoolMeta: p.PoolMeta,
                Mu: p.Mu,
                Sigma: p.Sigma,
                Count: p.Count,
                Outlier: p.Outlier,
                Il7d: p.Il7d,
                ApyBase7d: p.ApyBase7d,
                ApyMean30d: p.ApyMean30d,
                VolumeUsd1d: p.VolumeUsd1d,
                VolumeUsd7d: p.VolumeUsd7d,
                ApyBaseInception: p.ApyBaseInception
            );
        }

        return rows;
    }

    private static IReadOnlyList<DefiLlamaPoolTokenSyncRow> BuildRewardTokenRows(IReadOnlyList<RawPool> pools)
    {
        List<DefiLlamaPoolTokenSyncRow> rows = [];

        foreach (RawPool pool in pools)
        {
            if (pool.RewardTokens is null)
            {
                continue;
            }

            for (int i = 0; i < pool.RewardTokens.Length; i++)
            {
                rows.Add(
                    new DefiLlamaPoolTokenSyncRow(PoolId: pool.PoolId, SortOrder: i, TokenAddress: pool.RewardTokens[i])
                );
            }
        }

        return rows;
    }

    private static IReadOnlyList<DefiLlamaPoolTokenSyncRow> BuildUnderlyingTokenRows(IReadOnlyList<RawPool> pools)
    {
        List<DefiLlamaPoolTokenSyncRow> rows = [];

        foreach (RawPool pool in pools)
        {
            if (pool.UnderlyingTokens is null)
            {
                continue;
            }

            for (int i = 0; i < pool.UnderlyingTokens.Length; i++)
            {
                rows.Add(
                    new DefiLlamaPoolTokenSyncRow(
                        PoolId: pool.PoolId,
                        SortOrder: i,
                        TokenAddress: pool.UnderlyingTokens[i]
                    )
                );
            }
        }

        return rows;
    }

    private static IReadOnlyList<RawPool> MapToRawPools(
        IReadOnlyList<DefiLlamaPoolRow> poolRows,
        IReadOnlyList<DefiLlamaPoolRewardTokenRow> rewardTokenRows,
        IReadOnlyList<DefiLlamaPoolUnderlyingTokenRow> underlyingTokenRows
    )
    {
        Dictionary<string, List<string>> rewardTokensByPool = GroupTokensByPool(rewardTokenRows);
        Dictionary<string, List<string>> underlyingTokensByPool = GroupUnderlyingTokensByPool(underlyingTokenRows);

        RawPool[] result = new RawPool[poolRows.Count];

        for (int i = 0; i < poolRows.Count; i++)
        {
            DefiLlamaPoolRow row = poolRows[i];

            _ = rewardTokensByPool.TryGetValue(key: row.PoolId, value: out List<string>? rewardTokens);
            _ = underlyingTokensByPool.TryGetValue(key: row.PoolId, value: out List<string>? underlyingTokens);

            result[i] = new RawPool
            {
                PoolId = row.PoolId,
                Chain = row.Chain,
                Project = row.Project,
                Symbol = row.Symbol,
                TvlUsd = row.TvlUsd,
                ApyBase = row.ApyBase,
                ApyReward = row.ApyReward,
                Apy = row.Apy,
                ApyPct1D = row.ApyPct1D,
                ApyPct7D = row.ApyPct7D,
                ApyPct30D = row.ApyPct30D,
                Stablecoin = row.Stablecoin,
                IlRisk = row.IlRisk,
                Exposure = row.Exposure,
                Predictions =
                    row.PredictedClass is not null
                    || row.PredictedProbability is not null
                    || row.BinnedConfidence is not null
                        ? new RawPredictions
                        {
                            PredictedClass = row.PredictedClass,
                            PredictedProbability = row.PredictedProbability,
                            BinnedConfidence = row.BinnedConfidence,
                        }
                        : null,
                PoolMeta = row.PoolMeta,
                Mu = row.Mu,
                Sigma = row.Sigma,
                Count = row.Count,
                Outlier = row.Outlier,
                Il7d = row.Il7d,
                ApyBase7d = row.ApyBase7d,
                ApyMean30d = row.ApyMean30d,
                VolumeUsd1d = row.VolumeUsd1d,
                VolumeUsd7d = row.VolumeUsd7d,
                ApyBaseInception = row.ApyBaseInception,
                RewardTokens = rewardTokens?.ToArray(),
                UnderlyingTokens = underlyingTokens?.ToArray(),
            };
        }

        return result;
    }

    private static Dictionary<string, List<string>> GroupTokensByPool(
        IReadOnlyList<DefiLlamaPoolRewardTokenRow> rewardTokenRows
    )
    {
        Dictionary<string, List<string>> result = [];

        foreach (DefiLlamaPoolRewardTokenRow row in rewardTokenRows)
        {
            if (!result.TryGetValue(key: row.PoolId, value: out List<string>? tokens))
            {
                tokens = [];
                result[row.PoolId] = tokens;
            }

            tokens.Add(row.TokenAddress);
        }

        return result;
    }

    private static Dictionary<string, List<string>> GroupUnderlyingTokensByPool(
        IReadOnlyList<DefiLlamaPoolUnderlyingTokenRow> underlyingTokenRows
    )
    {
        Dictionary<string, List<string>> result = [];

        foreach (DefiLlamaPoolUnderlyingTokenRow row in underlyingTokenRows)
        {
            if (!result.TryGetValue(key: row.PoolId, value: out List<string>? tokens))
            {
                tokens = [];
                result[row.PoolId] = tokens;
            }

            tokens.Add(row.TokenAddress);
        }

        return result;
    }
}

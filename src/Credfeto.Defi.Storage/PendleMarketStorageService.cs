using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage;

public sealed class PendleMarketStorageService : IPendleMarketStorageService
{
    private static readonly IReadOnlyDictionary<int, string> ChainIdToName = new Dictionary<int, string>
    {
        [1] = "Ethereum",
        [42161] = "Arbitrum",
        [8453] = "Base",
        [56] = "BSC",
    };

    private readonly IDatabase _database;

    public PendleMarketStorageService(IDatabase database)
    {
        this._database = database;
    }

    public async ValueTask StoreMarketsAsync(
        IReadOnlyList<PendleMarket> markets,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<PendleMarketSyncRow> marketRows = BuildMarketRows(markets);
        IReadOnlyList<PendleMarketCategorySyncRow> categoryRows = BuildCategoryRows(markets);

        await this._database.ExecuteAsync(action: SyncAsync, cancellationToken: cancellationToken);

        ValueTask SyncAsync(DbConnection c, CancellationToken ct) =>
            PendleDatabase.Market_SyncAsync(
                connection: c,
                markets: marketRows,
                categories: categoryRows,
                dataDate: dataDate,
                cancellationToken: ct
            );
    }

    public async ValueTask<IReadOnlyList<RawPool>> GetAllPoolsAsync(CancellationToken cancellationToken)
    {
        ValueTask<IReadOnlyList<PendleMarketRow>> marketRowsTask = this._database.ExecuteAsync(
            action: PendleDatabase.Market_GetAllAsync,
            cancellationToken: cancellationToken
        );

        ValueTask<IReadOnlyList<PendleMarketCategoryRow>> categoryRowsTask = this._database.ExecuteAsync(
            action: PendleDatabase.MarketCategory_GetAllAsync,
            cancellationToken: cancellationToken
        );

        IReadOnlyList<PendleMarketRow> marketRows = await marketRowsTask;
        IReadOnlyList<PendleMarketCategoryRow> categoryRows = await categoryRowsTask;

        return MapToRawPools(marketRows: marketRows, categoryRows: categoryRows);
    }

    private static IReadOnlyList<PendleMarketSyncRow> BuildMarketRows(IReadOnlyList<PendleMarket> markets)
    {
        return
        [
            .. markets.Select(m => new PendleMarketSyncRow(
                Address: m.Address,
                ChainId: m.ChainId,
                SimpleSymbol: m.SimpleSymbol,
                Expiry: m.Expiry,
                IsActive: m.IsActive,
                LiquidityUsd: m.Liquidity?.Usd,
                AggregatedApy: m.AggregatedApy,
                UnderlyingApy: m.UnderlyingApy,
                PendleApy: m.PendleApy,
                LpRewardApy: m.LpRewardApy,
                SwapFeeApy: m.SwapFeeApy,
                TradingVolumeUsd: m.TradingVolume?.Usd
            )),
        ];
    }

    private static IReadOnlyList<PendleMarketCategorySyncRow> BuildCategoryRows(IReadOnlyList<PendleMarket> markets)
    {
        List<PendleMarketCategorySyncRow> rows = [];

        foreach (PendleMarket market in markets)
        {
            if (market.CategoryIds is null)
            {
                continue;
            }

            foreach (string categoryId in market.CategoryIds)
            {
                rows.Add(
                    new PendleMarketCategorySyncRow(
                        Address: market.Address,
                        ChainId: market.ChainId,
                        CategoryId: categoryId
                    )
                );
            }
        }

        return rows;
    }

    private static IReadOnlyList<RawPool> MapToRawPools(
        IReadOnlyList<PendleMarketRow> marketRows,
        IReadOnlyList<PendleMarketCategoryRow> categoryRows
    )
    {
        ILookup<(string Address, int ChainId), string> categoriesByMarket = categoryRows.ToLookup(
            keySelector: row => (row.Address, row.ChainId),
            elementSelector: row => row.CategoryId
        );

        return
        [
            .. marketRows.Select(row =>
                NormaliseMarket(row: row, categoryIds: categoriesByMarket[(row.Address, row.ChainId)])
            ),
        ];
    }

    private static RawPool NormaliseMarket(PendleMarketRow row, IEnumerable<string> categoryIds)
    {
        string chain = ChainIdToName.TryGetValue(key: row.ChainId, out string? name)
            ? name
            : row.ChainId.ToString(CultureInfo.InvariantCulture);

        string? poolMeta = null;

        if (
            !string.IsNullOrEmpty(row.Expiry)
            && DateTimeOffset.TryParse(
                input: row.Expiry,
                formatProvider: CultureInfo.InvariantCulture,
                styles: DateTimeStyles.None,
                result: out DateTimeOffset expiry
            )
        )
        {
            poolMeta =
                $"Maturity {expiry.ToString(format: "dd MMM yyyy", formatProvider: CultureInfo.InvariantCulture)}";
        }

        bool isStable = categoryIds.Contains("stables", StringComparer.OrdinalIgnoreCase);

        return new RawPool
        {
            Chain = chain,
            Project = "pendle",
            Symbol = row.SimpleSymbol,
            TvlUsd = row.LiquidityUsd ?? 0,
            Apy = row.AggregatedApy * 100,
            ApyBase = row.UnderlyingApy * 100,
            ApyReward = (row.PendleApy + row.LpRewardApy + row.SwapFeeApy) * 100,
            IlRisk = "no",
            Stablecoin = isStable,
            PoolId = row.Address,
            PoolMeta = poolMeta,
            VolumeUsd1d = row.TradingVolumeUsd,
            Predictions = new RawPredictions(),
            Outlier = false,
        };
    }
}

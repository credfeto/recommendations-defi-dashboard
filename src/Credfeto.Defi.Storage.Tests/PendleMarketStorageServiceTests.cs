using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using FunFair.Test.Infrastructure.Mocks;
using Xunit;

namespace Credfeto.Defi.Storage.Tests;

public sealed class PendleMarketStorageServiceTests : TestBase
{
    private static readonly DateTimeOffset FixedNow = MockDateTimeSources.Past.GetUtcNow();

    private readonly FakeDatabase _database;
    private readonly PendleMarketStorageService _storage;

    public PendleMarketStorageServiceTests()
    {
        this._database = new FakeDatabase();
        this._storage = new PendleMarketStorageService(database: this._database);
    }

    [Theory]
    [InlineData(1, "Ethereum")]
    [InlineData(42161, "Arbitrum")]
    [InlineData(8453, "Base")]
    [InlineData(56, "BSC")]
    public async Task GetAllPoolsAsync_KnownChainId_MapsToChainNameAsync(int chainId, string expectedChain)
    {
        CancellationToken cancellationToken = this.CancellationToken();
        this._database.WithReturn<IReadOnlyList<PendleMarketRow>>([CreateRow(chainId)]);
        this._database.WithReturn<IReadOnlyList<PendleMarketCategoryRow>>([]);

        IReadOnlyList<RawPool> result = await this._storage.GetAllPoolsAsync(cancellationToken);

        RawPool pool = Assert.Single(result);
        Assert.Equal(expected: expectedChain, actual: pool.Chain);
        Assert.Equal(expected: "pendle", actual: pool.Project);
    }

    [Fact]
    public async Task GetAllPoolsAsync_UnknownChainId_MapsToNumericChainAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        this._database.WithReturn<IReadOnlyList<PendleMarketRow>>([CreateRow(999)]);
        this._database.WithReturn<IReadOnlyList<PendleMarketCategoryRow>>([]);

        IReadOnlyList<RawPool> result = await this._storage.GetAllPoolsAsync(cancellationToken);

        RawPool pool = Assert.Single(result);
        Assert.Equal(expected: "999", actual: pool.Chain);
    }

    private static PendleMarketRow CreateRow(int chainId)
    {
        return new PendleMarketRow(
            Address: "0xmarket",
            ChainId: chainId,
            SimpleSymbol: "PT-USDC",
            Expiry: null,
            IsActive: true,
            LiquidityUsd: 1_000_000,
            AggregatedApy: 0.05,
            UnderlyingApy: 0.03,
            PendleApy: 0.01,
            LpRewardApy: 0.005,
            SwapFeeApy: 0.005,
            TradingVolumeUsd: 10_000,
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );
    }
}

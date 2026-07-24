using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Storage.Tests;

public sealed class CoinGeckoCoinStorageServiceTests : TestBase
{
    private static readonly DateTimeOffset FixedNow = new(
        year: 2024,
        month: 6,
        day: 1,
        hour: 12,
        minute: 0,
        second: 0,
        offset: TimeSpan.Zero
    );

    private readonly FakeDatabase _database;
    private readonly CoinGeckoCoinStorageService _storage;

    public CoinGeckoCoinStorageServiceTests()
    {
        this._database = new FakeDatabase();
        this._storage = new CoinGeckoCoinStorageService(database: this._database);
    }

    [Fact]
    public async Task GetAllAsync_NoCoins_ReturnsEmptyListAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        this._database.WithReturn<IReadOnlyList<CoinGeckoCoinRow>>([]);
        this._database.WithReturn<IReadOnlyList<CoinGeckoCoinPlatformAddressRow>>([]);

        IReadOnlyList<CoinGeckoCoinPlatforms> result = await this._storage.GetAllAsync(cancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_CoinWithNoPlatforms_ReturnsCoinWithNullPlatformsAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        CoinGeckoCoinRow coinRow = new(
            Id: "bitcoin",
            Symbol: "btc",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        this._database.WithReturn<IReadOnlyList<CoinGeckoCoinRow>>([coinRow]);
        this._database.WithReturn<IReadOnlyList<CoinGeckoCoinPlatformAddressRow>>([]);

        IReadOnlyList<CoinGeckoCoinPlatforms> result = await this._storage.GetAllAsync(cancellationToken);

        Assert.Single(result);
        Assert.Equal(expected: "bitcoin", actual: result[0].Id);
        Assert.Equal(expected: "btc", actual: result[0].Symbol);
        Assert.Null(result[0].Platforms);
    }

    [Fact]
    public async Task GetAllAsync_CoinWithMultiplePlatforms_MapsAllAddressesAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        CoinGeckoCoinRow usdcRow = new(
            Id: "usd-coin",
            Symbol: "usdc",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        CoinGeckoCoinPlatformAddressRow usdcEthereum = new(
            CoinId: "usd-coin",
            Platform: "ethereum",
            ContractAddress: "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );
        CoinGeckoCoinPlatformAddressRow usdcPolygon = new(
            CoinId: "usd-coin",
            Platform: "polygon-pos",
            ContractAddress: "0x2791Bca1f2de4661ED88A30C99A7a9449Aa84174",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        this._database.WithReturn<IReadOnlyList<CoinGeckoCoinRow>>([usdcRow]);
        this._database.WithReturn<IReadOnlyList<CoinGeckoCoinPlatformAddressRow>>([usdcEthereum, usdcPolygon]);

        IReadOnlyList<CoinGeckoCoinPlatforms> result = await this._storage.GetAllAsync(cancellationToken);

        Assert.Single(result);
        IReadOnlyDictionary<string, string>? platforms = result[0].Platforms;
        Assert.NotNull(platforms);
        Assert.Equal(expected: 2, actual: platforms.Count);
        Assert.Equal(expected: usdcEthereum.ContractAddress, actual: platforms["ethereum"]);
        Assert.Equal(expected: usdcPolygon.ContractAddress, actual: platforms["polygon-pos"]);
    }

    [Fact]
    public async Task GetAllAsync_MultipleCoins_AddressesGroupedByCorrectCoinAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        CoinGeckoCoinRow usdcRow = new(
            Id: "usd-coin",
            Symbol: "usdc",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );
        CoinGeckoCoinRow daiRow = new(
            Id: "dai",
            Symbol: "dai",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        CoinGeckoCoinPlatformAddressRow usdcEthereum = new(
            CoinId: "usd-coin",
            Platform: "ethereum",
            ContractAddress: "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );
        CoinGeckoCoinPlatformAddressRow daiEthereum = new(
            CoinId: "dai",
            Platform: "ethereum",
            ContractAddress: "0x6B175474E89094C44Da98b954EedeAC495271d0F",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        this._database.WithReturn<IReadOnlyList<CoinGeckoCoinRow>>([usdcRow, daiRow]);
        this._database.WithReturn<IReadOnlyList<CoinGeckoCoinPlatformAddressRow>>([usdcEthereum, daiEthereum]);

        IReadOnlyList<CoinGeckoCoinPlatforms> result = await this._storage.GetAllAsync(cancellationToken);

        Assert.Equal(expected: 2, actual: result.Count);

        CoinGeckoCoinPlatforms usdc = result[0];
        CoinGeckoCoinPlatforms dai = result[1];

        Assert.NotNull(usdc.Platforms);
        Assert.Single(usdc.Platforms);
        Assert.Equal(expected: usdcEthereum.ContractAddress, actual: usdc.Platforms["ethereum"]);

        Assert.NotNull(dai.Platforms);
        Assert.Single(dai.Platforms);
        Assert.Equal(expected: daiEthereum.ContractAddress, actual: dai.Platforms["ethereum"]);
    }

    [Fact]
    public async Task StoreAsync_CoinsWithAndWithoutPlatforms_CompletesWithoutThrowingAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        IReadOnlyList<CoinGeckoCoinPlatforms> coins =
        [
            new CoinGeckoCoinPlatforms
            {
                Id = "usd-coin",
                Symbol = "usdc",
                Platforms = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["ethereum"] = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48",
                    ["polygon-pos"] = string.Empty,
                },
            },
            new CoinGeckoCoinPlatforms
            {
                Id = "bitcoin",
                Symbol = "btc",
                Platforms = null,
            },
        ];

        await this._storage.StoreAsync(coins: coins, dataDate: FixedNow, cancellationToken: cancellationToken);
    }
}

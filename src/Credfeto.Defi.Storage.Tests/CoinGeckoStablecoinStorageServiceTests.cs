using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Storage.Tests;

public sealed class CoinGeckoStablecoinStorageServiceTests : TestBase
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
    private readonly CoinGeckoStablecoinStorageService _storage;

    public CoinGeckoStablecoinStorageServiceTests()
    {
        this._database = new FakeDatabase();
        this._storage = new CoinGeckoStablecoinStorageService(database: this._database);
    }

    [Fact]
    public async Task GetAllAsync_NoStablecoins_ReturnsEmptyListAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        this._database.WithReturn<IReadOnlyList<CoinGeckoStablecoinRow>>([]);

        IReadOnlyList<CoinGeckoStablecoin> result = await this._storage.GetAllAsync(cancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_StablecoinWithPrice_MapsAllFieldsAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        CoinGeckoStablecoinRow row = new(
            Id: "usd-coin",
            Symbol: "usdc",
            Name: "USD Coin",
            CurrentPrice: 1.001m,
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: FixedNow
        );

        this._database.WithReturn<IReadOnlyList<CoinGeckoStablecoinRow>>([row]);

        IReadOnlyList<CoinGeckoStablecoin> result = await this._storage.GetAllAsync(cancellationToken);

        Assert.Single(result);
        Assert.Equal(expected: "usd-coin", actual: result[0].Id);
        Assert.Equal(expected: "usdc", actual: result[0].Symbol);
        Assert.Equal(expected: "USD Coin", actual: result[0].Name);
        Assert.Equal(expected: 1.001m, actual: result[0].CurrentPrice);
    }

    [Fact]
    public async Task GetAllAsync_StablecoinWithNullPrice_MapsNullPriceAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        CoinGeckoStablecoinRow row = new(
            Id: "tether",
            Symbol: "usdt",
            Name: "Tether",
            CurrentPrice: null,
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        this._database.WithReturn<IReadOnlyList<CoinGeckoStablecoinRow>>([row]);

        IReadOnlyList<CoinGeckoStablecoin> result = await this._storage.GetAllAsync(cancellationToken);

        Assert.Single(result);
        Assert.Null(result[0].CurrentPrice);
    }

    [Fact]
    public async Task StoreAsync_StablecoinsWithAndWithoutPrice_CompletesWithoutThrowingAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        IReadOnlyList<CoinGeckoStablecoin> stablecoins =
        [
            new CoinGeckoStablecoin
            {
                Id = "usd-coin",
                Symbol = "usdc",
                Name = "USD Coin",
                CurrentPrice = 1.001m,
            },
            new CoinGeckoStablecoin
            {
                Id = "tether",
                Symbol = "usdt",
                Name = "Tether",
                CurrentPrice = null,
            },
        ];

        await this._storage.StoreAsync(
            stablecoins: stablecoins,
            dataDate: FixedNow,
            cancellationToken: cancellationToken
        );
    }
}

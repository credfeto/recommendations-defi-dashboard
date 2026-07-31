using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using FunFair.Test.Common.Mocks;
using Xunit;

namespace Credfeto.Defi.Storage.Tests;

public sealed class DefiLlamaHackStorageServiceTests : TestBase
{
    private static readonly DateTimeOffset FixedNow = MockDateTimeSources.Past.GetUtcNow();

    private readonly FakeDatabase _database;
    private readonly DefiLlamaHackStorageService _storage;

    public DefiLlamaHackStorageServiceTests()
    {
        this._database = new FakeDatabase();
        this._storage = new DefiLlamaHackStorageService(database: this._database);
    }

    [Fact]
    public async Task GetAllHacksAsync_NoHacks_ReturnsEmptyListAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        this._database.WithReturn<IReadOnlyList<DefiLlamaHackRow>>([]);

        IReadOnlyList<RawHack> result = await this._storage.GetAllHacksAsync(cancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllHacksAsync_SingleHack_MapsAllFieldsAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        DateTimeOffset hackDate = DateTimeOffset.FromUnixTimeSeconds(1672531200);

        DefiLlamaHackRow row = new(
            Name: "Aave",
            HackDate: hackDate,
            Classification: "Protocol",
            Technique: "Flash Loan",
            Amount: 1_000_000m,
            Source: "defillama",
            ParentProtocolId: "parent-aave",
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        this._database.WithReturn<IReadOnlyList<DefiLlamaHackRow>>([row]);

        IReadOnlyList<RawHack> result = await this._storage.GetAllHacksAsync(cancellationToken);

        Assert.Single(result);
        Assert.Equal(expected: "Aave", actual: result[0].Name);
        Assert.Equal(expected: hackDate.ToUnixTimeSeconds(), actual: result[0].Date);
        Assert.Equal(expected: "Protocol", actual: result[0].Classification);
        Assert.Equal(expected: "Flash Loan", actual: result[0].Technique);
        Assert.Equal(expected: 1_000_000m, actual: result[0].Amount);
        Assert.Equal(expected: "defillama", actual: result[0].Source);
        Assert.Equal(expected: "parent-aave", actual: result[0].ParentProtocolId);
    }

    [Fact]
    public async Task GetAllHacksAsync_HackWithNoParentProtocolId_ReturnsNullParentProtocolIdAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        DefiLlamaHackRow row = new(
            Name: "Standalone Hack",
            HackDate: FixedNow,
            Classification: null,
            Technique: null,
            Amount: 500m,
            Source: "defillama",
            ParentProtocolId: null,
            DateCreated: FixedNow,
            DateUpdated: FixedNow,
            DataDate: null
        );

        this._database.WithReturn<IReadOnlyList<DefiLlamaHackRow>>([row]);

        IReadOnlyList<RawHack> result = await this._storage.GetAllHacksAsync(cancellationToken);

        Assert.Single(result);
        Assert.Null(result[0].Classification);
        Assert.Null(result[0].Technique);
        Assert.Null(result[0].ParentProtocolId);
    }

    [Fact]
    public async Task StoreHacksAsync_HacksWithAndWithoutParentProtocolId_CompletesWithoutThrowingAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        IReadOnlyList<RawHack> hacks =
        [
            new RawHack
            {
                Date = 1672531200,
                Name = "Aave",
                Classification = "Protocol",
                Technique = "Flash Loan",
                Amount = 1_000_000m,
                Source = "defillama",
                ParentProtocolId = "parent-aave",
            },
            new RawHack
            {
                Date = 1672617600,
                Name = "Standalone Hack",
                Classification = null,
                Technique = null,
                Amount = 500m,
                Source = "defillama",
                ParentProtocolId = null,
            },
        ];

        await this._storage.StoreHacksAsync(hacks: hacks, dataDate: FixedNow, cancellationToken: cancellationToken);
    }

    [Fact]
    public async Task StoreHacksAsync_DuplicateNameAndDate_DedupesBeforeSyncAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        IReadOnlyList<RawHack> hacks =
        [
            new RawHack
            {
                Date = 1672531200,
                Name = "Aave",
                Classification = "Protocol",
                Technique = "Flash Loan",
                Amount = 1_000_000m,
                Source = "defillama",
            },
            new RawHack
            {
                Date = 1672531200,
                Name = "Aave",
                Classification = "Protocol",
                Technique = "Flash Loan",
                Amount = 1_000_000m,
                Source = "defillama",
            },
        ];

        await this._storage.StoreHacksAsync(hacks: hacks, dataDate: null, cancellationToken: cancellationToken);
    }
}

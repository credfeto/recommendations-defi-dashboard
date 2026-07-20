using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.Tests.Common;
using Credfeto.Defi.Storage;
using Credfeto.Defi.Storage.Database.Rows;
using FunFair.Test.Common;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ApiCacheServiceTests : TestBase
{
    private static readonly DateTimeOffset FixedNow = new(year: 2024, month: 6, day: 1, hour: 12, minute: 0, second: 0, offset: TimeSpan.Zero);

    private readonly FakeDatabase _database;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ApiCacheService _cache;

    public ApiCacheServiceTests()
    {
        this._timeProvider = new FakeTimeProvider(startDateTime: FixedNow);
        this._database = new FakeDatabase();
        this._cache = new ApiCacheService(database: this._database, timeProvider: this._timeProvider);
    }

    [Fact]
    public async Task GetOrFetchAsync_CacheMiss_CallsFetcherAndReturnsFreshDataAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();
        int fetchCount = 0;

        string result = await this._cache.GetOrFetchAsync(
            key: "test-key",
            fetcher: _ =>
            {
                fetchCount++;

                return ValueTask.FromResult("fresh-value");
            },
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: "fresh-value", actual: result);
        Assert.Equal(expected: 1, actual: fetchCount);
    }

    [Fact]
    public async Task GetOrFetchAsync_FreshHit_ReturnsCachedDataWithoutCallingFetcherAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        string cachedJson = JsonSerializer.Serialize("cached-value", TestJsonContext.Default.String);
        ApiCacheRow freshRow = new("test-key", cachedJson, FixedNow - TimeSpan.FromMinutes(30));
        this._database.WithReturn<ApiCacheRow?>(freshRow);

        int fetchCount = 0;

        string result = await this._cache.GetOrFetchAsync(
            key: "test-key",
            fetcher: _ =>
            {
                fetchCount++;

                return ValueTask.FromResult("new-value");
            },
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: "cached-value", actual: result);
        Assert.Equal(expected: 0, actual: fetchCount);
    }

    [Fact]
    public async Task GetOrFetchAsync_StaleHit_FetcherThrows_ReturnsStaleCachedDataAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        string cachedJson = JsonSerializer.Serialize("stale-value", TestJsonContext.Default.String);
        ApiCacheRow staleRow = new("stale-key", cachedJson, FixedNow - TimeSpan.FromHours(1.5));
        this._database.WithReturn<ApiCacheRow?>(staleRow);

        string result = await this._cache.GetOrFetchAsync(
            key: "stale-key",
            fetcher: _ => throw new InvalidOperationException("Simulated fetch failure"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: "stale-value", actual: result);
    }

    [Fact]
    public Task GetOrFetchAsync_ExpiredCache_FetcherThrows_ExceptionPropagatesAsync()
    {
        string cachedJson = JsonSerializer.Serialize("old-value", TestJsonContext.Default.String);
        ApiCacheRow expiredRow = new("expired-key", cachedJson, FixedNow - TimeSpan.FromHours(3));
        this._database.WithReturn<ApiCacheRow?>(expiredRow);

        return Assert.ThrowsAsync<InvalidOperationException>(() =>
            this
                ._cache.GetOrFetchAsync(
                    key: "expired-key",
                    fetcher: _ => throw new InvalidOperationException("Simulated fetch failure"),
                    typeInfo: TestJsonContext.Default.String,
                    cancellationToken: this.CancellationToken()
                )
                .AsTask()
        );
    }

    [Fact]
    public async Task GetOrFetchAsync_ExpiredCache_FetcherSucceeds_ReturnsNewValueAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        string cachedJson = JsonSerializer.Serialize("old-value", TestJsonContext.Default.String);
        ApiCacheRow expiredRow = new("refresh-key", cachedJson, FixedNow - TimeSpan.FromHours(2));
        this._database.WithReturn<ApiCacheRow?>(expiredRow);

        string result = await this._cache.GetOrFetchAsync(
            key: "refresh-key",
            fetcher: _ => ValueTask.FromResult("new-value"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: "new-value", actual: result);
    }

    [Fact]
    public async Task IsFreshAsync_CacheMiss_ReturnsFalseAsync()
    {
        bool result = await this._cache.IsFreshAsync("nonexistent-key");
        Assert.False(result, userMessage: "A missing cache entry should not be considered fresh");
    }

    [Fact]
    public async Task IsFreshAsync_RecentEntry_ReturnsTrueAsync()
    {
        string cachedJson = JsonSerializer.Serialize("value", TestJsonContext.Default.String);
        ApiCacheRow recentRow = new("fresh-check-key", cachedJson, FixedNow - TimeSpan.FromMinutes(10));
        this._database.WithReturn<ApiCacheRow?>(recentRow);

        bool result = await this._cache.IsFreshAsync("fresh-check-key");
        Assert.True(result, userMessage: "A recently cached entry should be considered fresh");
    }

    [Fact]
    public async Task IsFreshAsync_OldEntry_ReturnsFalseAsync()
    {
        string cachedJson = JsonSerializer.Serialize("value", TestJsonContext.Default.String);
        ApiCacheRow oldRow = new("stale-check-key", cachedJson, FixedNow - TimeSpan.FromHours(2));
        this._database.WithReturn<ApiCacheRow?>(oldRow);

        bool result = await this._cache.IsFreshAsync("stale-check-key");
        Assert.False(result, userMessage: "An old cache entry should not be considered fresh");
    }
}

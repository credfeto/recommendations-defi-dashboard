using System;
using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.Cache;
using Credfeto.Defi.Server.Config;
using FunFair.Test.Common;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class ApiCacheServiceTests : TestBase, IDisposable
{
    private readonly string _tempDir;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ApiCacheService _cache;

    public ApiCacheServiceTests()
    {
        this._tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        this._timeProvider = new FakeTimeProvider();

        IOptions<CacheConfig> options = Options.Create(new CacheConfig { DbDirectory = this._tempDir });
        this._cache = new ApiCacheService(config: options, timeProvider: this._timeProvider);
    }

    public void Dispose()
    {
        this._cache.Dispose();

        if (Directory.Exists(this._tempDir))
        {
            Directory.Delete(path: this._tempDir, recursive: true);
        }
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

        // Prime the cache
        await this._cache.GetOrFetchAsync(
            key: "test-key",
            fetcher: _ => ValueTask.FromResult("cached-value"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        int fetchCount = 0;

        // Fetch again immediately - should be fresh
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

        // Prime the cache
        await this._cache.GetOrFetchAsync(
            key: "stale-key",
            fetcher: _ => ValueTask.FromResult("stale-value"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        // Advance time by 1.5 hours (past fresh TTL of 1h, but within stale TTL of 2h)
        this._timeProvider.Advance(TimeSpan.FromHours(1.5));

        string result = await this._cache.GetOrFetchAsync(
            key: "stale-key",
            fetcher: _ => throw new InvalidOperationException("Simulated fetch failure"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: "stale-value", actual: result);
    }

    [Fact]
    public async Task GetOrFetchAsync_ExpiredCache_FetcherThrows_ExceptionPropagatesAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        // Prime the cache
        await this._cache.GetOrFetchAsync(
            key: "expired-key",
            fetcher: _ => ValueTask.FromResult("old-value"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        // Advance time by 3 hours (past stale TTL of 2h)
        this._timeProvider.Advance(TimeSpan.FromHours(3));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            this
                ._cache.GetOrFetchAsync(
                    key: "expired-key",
                    fetcher: _ => throw new InvalidOperationException("Simulated fetch failure"),
                    typeInfo: TestJsonContext.Default.String,
                    cancellationToken: cancellationToken
                )
                .AsTask()
        );
    }

    [Fact]
    public async Task GetOrFetchAsync_ExpiredCache_FetcherSucceeds_ReturnsNewValueAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        // Prime the cache
        await this._cache.GetOrFetchAsync(
            key: "refresh-key",
            fetcher: _ => ValueTask.FromResult("old-value"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        // Advance time by 2 hours (stale)
        this._timeProvider.Advance(TimeSpan.FromHours(2));

        string result = await this._cache.GetOrFetchAsync(
            key: "refresh-key",
            fetcher: _ => ValueTask.FromResult("new-value"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        Assert.Equal(expected: "new-value", actual: result);
    }

    [Fact]
    public void IsFresh_CacheMiss_ReturnsFalse()
    {
        bool result = this._cache.IsFresh("nonexistent-key");
        Assert.False(result, userMessage: "A missing cache entry should not be considered fresh");
    }

    [Fact]
    public async Task IsFresh_RecentEntry_ReturnsTrueAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        await this._cache.GetOrFetchAsync(
            key: "fresh-check-key",
            fetcher: _ => ValueTask.FromResult("value"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        bool result = this._cache.IsFresh("fresh-check-key");
        Assert.True(result, userMessage: "A recently cached entry should be considered fresh");
    }

    [Fact]
    public async Task IsFresh_OldEntry_ReturnsFalseAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        await this._cache.GetOrFetchAsync(
            key: "stale-check-key",
            fetcher: _ => ValueTask.FromResult("value"),
            typeInfo: TestJsonContext.Default.String,
            cancellationToken: cancellationToken
        );

        this._timeProvider.Advance(TimeSpan.FromHours(2));

        bool result = this._cache.IsFresh("stale-check-key");
        Assert.False(result, userMessage: "An old cache entry should not be considered fresh");
    }

    [Fact]
    public void Constructor_CreatesDbDirectoryIfMissing()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "subdir");

        try
        {
            IOptions<CacheConfig> options = Options.Create(new CacheConfig { DbDirectory = tempDir });
            using ApiCacheService cache = new(config: options, timeProvider: this._timeProvider);

            Assert.True(Directory.Exists(tempDir), userMessage: "Constructor should create the DB directory");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(path: tempDir, recursive: true);
            }
        }
    }
}

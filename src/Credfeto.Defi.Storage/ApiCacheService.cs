using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;
using Credfeto.Defi.Storage.Database;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage;

public sealed class ApiCacheService
{
    private static readonly TimeSpan FreshTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan StaleTtl = TimeSpan.FromHours(2);

    private readonly IDatabase _database;
    private readonly TimeProvider _timeProvider;

    public ApiCacheService(IDatabase database, TimeProvider timeProvider)
    {
        this._database = database;
        this._timeProvider = timeProvider;
    }

    /// <summary>
    ///     Returns cached data if fresh (&lt;1 h).
    ///     Otherwise calls <paramref name="fetcher" /> to get fresh data and updates the cache.
    ///     If the fetch fails and stale data exists (&lt;2 h), the stale data is returned.
    ///     If the fetch fails and data is older than 2 h (or absent), the exception propagates.
    /// </summary>
    public async ValueTask<T> GetOrFetchAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> fetcher,
        JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken
    )
    {
        DateTimeOffset now = this._timeProvider.GetUtcNow();

        ApiCacheRow? row = await this._database.ExecuteAsync(
            action: (c, ct) => DefiDatabase.ApiCache_GetByKeyAsync(connection: c, key: key, cancellationToken: ct),
            cancellationToken: cancellationToken
        );

        if (row is not null && now - row.FetchedAt < FreshTtl)
        {
            T? deserialized = JsonSerializer.Deserialize(json: row.Data, jsonTypeInfo: typeInfo);

            if (deserialized is not null)
            {
                return deserialized;
            }
        }

        try
        {
            T freshData = await fetcher(cancellationToken);
            string json = JsonSerializer.Serialize(value: freshData, jsonTypeInfo: typeInfo);

            await this._database.ExecuteAsync(
                action: (c, ct) =>
                    DefiDatabase.ApiCache_UpsertAsync(
                        connection: c,
                        key: key,
                        data: json,
                        fetchedAt: now,
                        cancellationToken: ct
                    ),
                cancellationToken: cancellationToken
            );

            return freshData;
        }
        catch when (row is not null && now - row.FetchedAt < StaleTtl)
        {
            T? stale = JsonSerializer.Deserialize(json: row.Data, jsonTypeInfo: typeInfo);

            if (stale is not null)
            {
                return stale;
            }

            throw;
        }
    }

    /// <summary>
    ///     Returns whether the entry for <paramref name="key" /> is fresh (less than 1 hour old).
    /// </summary>
    public async ValueTask<bool> IsFreshAsync(string key)
    {
        DateTimeOffset now = this._timeProvider.GetUtcNow();

        ApiCacheRow? row = await this._database.ExecuteAsync(
            action: (c, ct) => DefiDatabase.ApiCache_GetByKeyAsync(connection: c, key: key, cancellationToken: ct),
            cancellationToken: CancellationToken.None
        );

        return row is not null && now - row.FetchedAt < FreshTtl;
    }
}

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.Config;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Credfeto.Defi.Server.Cache;

/// <summary>
///     SQLite-backed cache for external API responses.
///     Fresh TTL: &lt;1 hour; usable (stale) TTL: &lt;2 hours.
/// </summary>
public sealed class ApiCacheService : IDisposable
{
    private static readonly TimeSpan FreshTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan StaleTtl = TimeSpan.FromHours(2);

    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _lock = new(initialCount: 1, maxCount: 1);
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initialises a new instance of <see cref="ApiCacheService" />.
    /// </summary>
    public ApiCacheService(IOptions<CacheConfig> config, TimeProvider timeProvider)
    {
        this._timeProvider = timeProvider;

        string dbDirectory = config.Value.DbDirectory;

        if (!Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        string dbPath = Path.Combine(path1: dbDirectory, path2: "cache.db");
        this._connection = new SqliteConnection($"Data Source={dbPath}");
        this._connection.Open();

        EnsureSchema(this._connection);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this._lock.Dispose();
        this._connection.Dispose();
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS api_cache (
                key        TEXT    PRIMARY KEY,
                data       TEXT    NOT NULL,
                fetched_at INTEGER NOT NULL
            )";
        cmd.ExecuteNonQuery();
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
        await this._lock.WaitAsync(cancellationToken);

        try
        {
            long nowMs = this._timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            (string? cachedJson, long fetchedAt) = ReadCacheRow(connection: this._connection, key: key);

            if (cachedJson is not null)
            {
                long ageMs = nowMs - fetchedAt;

                if (ageMs < (long)FreshTtl.TotalMilliseconds)
                {
                    T? deserialized = JsonSerializer.Deserialize(json: cachedJson, jsonTypeInfo: typeInfo);

                    if (deserialized is not null)
                    {
                        return deserialized;
                    }
                }
            }

            try
            {
                T freshData = await fetcher(cancellationToken);
                string json = JsonSerializer.Serialize(value: freshData, jsonTypeInfo: typeInfo);
                WriteCache(connection: this._connection, key: key, json: json, fetchedAtMs: nowMs);

                return freshData;
            }
            catch when (cachedJson is not null)
            {
                long ageMs = nowMs - fetchedAt;

                if (ageMs < (long)StaleTtl.TotalMilliseconds)
                {
                    T? stale = JsonSerializer.Deserialize(json: cachedJson, jsonTypeInfo: typeInfo);

                    if (stale is not null)
                    {
                        return stale;
                    }
                }

                throw;
            }
        }
        finally
        {
            _ = this._lock.Release();
        }
    }

    /// <summary>
    ///     Returns whether the entry for <paramref name="key" /> is fresh (less than 1 hour old).
    ///     This method is synchronous and called from non-async contexts (e.g. LINQ predicates in the
    ///     cache warmer). <see cref="CancellationToken.None" /> is intentional: the check is
    ///     best-effort and never needs to be cancelled independently.
    /// </summary>
    public bool IsFresh(string key)
    {
        this._lock.Wait(CancellationToken.None);

        try
        {
            long nowMs = this._timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            (string? cachedJson, long fetchedAt) = ReadCacheRow(connection: this._connection, key: key);

            return cachedJson is not null && nowMs - fetchedAt < (long)FreshTtl.TotalMilliseconds;
        }
        finally
        {
            _ = this._lock.Release();
        }
    }

    private static (string? Json, long FetchedAt) ReadCacheRow(SqliteConnection connection, string key)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT data, fetched_at FROM api_cache WHERE key = @key";
        _ = cmd.Parameters.AddWithValue(parameterName: "@key", value: key);

        using SqliteDataReader reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            return (null, 0L);
        }

        string json = reader.GetString(0);
        long fetchedAt = reader.GetInt64(1);

        return (json, fetchedAt);
    }

    private static void WriteCache(SqliteConnection connection, string key, string json, long fetchedAtMs)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO api_cache (key, data, fetched_at) VALUES (@key, @data, @fetchedAt)";
        _ = cmd.Parameters.AddWithValue(parameterName: "@key", value: key);
        _ = cmd.Parameters.AddWithValue(parameterName: "@data", value: json);
        _ = cmd.Parameters.AddWithValue(parameterName: "@fetchedAt", value: fetchedAtMs);
        cmd.ExecuteNonQuery();
    }
}

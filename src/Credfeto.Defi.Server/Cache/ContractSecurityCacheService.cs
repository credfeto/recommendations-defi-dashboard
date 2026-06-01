using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Server.Config;
using Credfeto.Defi.Server.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Credfeto.Defi.Server.Cache;

/// <summary>
///     SQLite-backed cache for GoPlus contract security data.
///     TTL: 24 hours.
/// </summary>
internal sealed class ContractSecurityCacheService : IDisposable
{
    private static readonly TimeSpan SecurityTtl = TimeSpan.FromHours(24);

    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _lock = new(initialCount: 1, maxCount: 1);
    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Initialises a new instance of <see cref="ContractSecurityCacheService" />.
    /// </summary>
    public ContractSecurityCacheService(IOptions<CacheConfig> config, TimeProvider timeProvider)
    {
        this._timeProvider = timeProvider;

        string dbDirectory = config.Value.DbDirectory;

        if (!Directory.Exists(dbDirectory))
        {
            _ = Directory.CreateDirectory(dbDirectory);
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
            CREATE TABLE IF NOT EXISTS contract_security (
                chain                       TEXT    NOT NULL,
                address                     TEXT    NOT NULL,
                parent_address              TEXT,
                is_open_source              REAL,
                is_honeypot                 REAL,
                is_proxy                    REAL,
                buy_tax                     REAL,
                sell_tax                    REAL,
                transfer_tax                REAL,
                cannot_buy                  REAL,
                honeypot_with_same_creator  REAL,
                token_name                  TEXT,
                token_symbol                TEXT,
                checked_at                  INTEGER NOT NULL,
                PRIMARY KEY (chain, address)
            )";
        _ = cmd.ExecuteNonQuery();
    }

    /// <summary>
    ///     Returns a cached entry if it exists and is within the 24-hour TTL.
    ///     Returns null if not found or expired.
    /// </summary>
    public async ValueTask<ContractSecurityInfo?> GetAsync(
        string chain,
        string address,
        CancellationToken cancellationToken
    )
    {
        await this._lock.WaitAsync(cancellationToken);

        try
        {
            long nowMs = this._timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

            return ReadRow(
                connection: this._connection,
                chain: chain,
                address: address.ToLowerInvariant(),
                nowMs: nowMs,
                ttlMs: (long)SecurityTtl.TotalMilliseconds
            );
        }
        finally
        {
            _ = this._lock.Release();
        }
    }

    /// <summary>
    ///     Returns all cached child rows (proxy implementations) for the given parent proxy address.
    /// </summary>
    public async ValueTask<IReadOnlyList<ContractSecurityInfo>> GetChildrenAsync(
        string chain,
        string parentAddress,
        CancellationToken cancellationToken
    )
    {
        await this._lock.WaitAsync(cancellationToken);

        try
        {
            return ReadChildren(
                connection: this._connection,
                chain: chain,
                parentAddress: parentAddress.ToLowerInvariant()
            );
        }
        finally
        {
            _ = this._lock.Release();
        }
    }

    /// <summary>
    ///     Persists a <see cref="ContractSecurityInfo" /> entry to the cache.
    /// </summary>
    public async ValueTask SetAsync(ContractSecurityInfo info, CancellationToken cancellationToken)
    {
        await this._lock.WaitAsync(cancellationToken);

        try
        {
            long nowMs = this._timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
            WriteRow(connection: this._connection, info: info, checkedAtMs: nowMs);
        }
        finally
        {
            _ = this._lock.Release();
        }
    }

    private static ContractSecurityInfo? ReadRow(
        SqliteConnection connection,
        string chain,
        string address,
        long nowMs,
        long ttlMs
    )
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM contract_security WHERE chain = @chain AND address = @address";
        _ = cmd.Parameters.AddWithValue(parameterName: "@chain", value: chain);
        _ = cmd.Parameters.AddWithValue(parameterName: "@address", value: address);

        using SqliteDataReader reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        long checkedAt = reader.GetInt64(reader.GetOrdinal("checked_at"));

        return nowMs - checkedAt >= ttlMs ? null : MapRow(reader);
    }

    private static IReadOnlyList<ContractSecurityInfo> ReadChildren(
        SqliteConnection connection,
        string chain,
        string parentAddress
    )
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM contract_security WHERE chain = @chain AND parent_address = @parentAddress";
        _ = cmd.Parameters.AddWithValue(parameterName: "@chain", value: chain);
        _ = cmd.Parameters.AddWithValue(parameterName: "@parentAddress", value: parentAddress);

        using SqliteDataReader reader = cmd.ExecuteReader();
        List<ContractSecurityInfo> results = [];

        while (reader.Read())
        {
            results.Add(MapRow(reader));
        }

        return results;
    }

    private static ContractSecurityInfo MapRow(SqliteDataReader reader)
    {
        return new ContractSecurityInfo
        {
            Chain = reader.GetString(reader.GetOrdinal("chain")),
            Address = reader.GetString(reader.GetOrdinal("address")),
            ParentAddress = reader.IsDBNull(reader.GetOrdinal("parent_address"))
                ? null
                : reader.GetString(reader.GetOrdinal("parent_address")),
            IsOpenSource = reader.IsDBNull(reader.GetOrdinal("is_open_source"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("is_open_source")),
            IsHoneypot = reader.IsDBNull(reader.GetOrdinal("is_honeypot"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("is_honeypot")),
            IsProxy = reader.IsDBNull(reader.GetOrdinal("is_proxy"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("is_proxy")),
            BuyTax = reader.IsDBNull(reader.GetOrdinal("buy_tax"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("buy_tax")),
            SellTax = reader.IsDBNull(reader.GetOrdinal("sell_tax"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("sell_tax")),
            TransferTax = reader.IsDBNull(reader.GetOrdinal("transfer_tax"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("transfer_tax")),
            CannotBuy = reader.IsDBNull(reader.GetOrdinal("cannot_buy"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("cannot_buy")),
            HoneypotWithSameCreator = reader.IsDBNull(reader.GetOrdinal("honeypot_with_same_creator"))
                ? null
                : reader.GetDouble(reader.GetOrdinal("honeypot_with_same_creator")),
            TokenName = reader.IsDBNull(reader.GetOrdinal("token_name"))
                ? null
                : reader.GetString(reader.GetOrdinal("token_name")),
            TokenSymbol = reader.IsDBNull(reader.GetOrdinal("token_symbol"))
                ? null
                : reader.GetString(reader.GetOrdinal("token_symbol")),
        };
    }

    private static void WriteRow(SqliteConnection connection, ContractSecurityInfo info, long checkedAtMs)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText =
            @"
            INSERT OR REPLACE INTO contract_security
                (chain, address, parent_address, is_open_source, is_honeypot, is_proxy,
                 buy_tax, sell_tax, transfer_tax, cannot_buy, honeypot_with_same_creator,
                 token_name, token_symbol, checked_at)
            VALUES
                (@chain, @address, @parentAddress, @isOpenSource, @isHoneypot, @isProxy,
                 @buyTax, @sellTax, @transferTax, @cannotBuy, @honeypotWithSameCreator,
                 @tokenName, @tokenSymbol, @checkedAt)";

        object isOpenSourceValue = info.IsOpenSource.HasValue ? info.IsOpenSource.Value : DBNull.Value;
        object isHoneypotValue = info.IsHoneypot.HasValue ? info.IsHoneypot.Value : DBNull.Value;
        object isProxyValue = info.IsProxy.HasValue ? info.IsProxy.Value : DBNull.Value;
        object buyTaxValue = info.BuyTax.HasValue ? info.BuyTax.Value : DBNull.Value;
        object sellTaxValue = info.SellTax.HasValue ? info.SellTax.Value : DBNull.Value;
        object transferTaxValue = info.TransferTax.HasValue ? info.TransferTax.Value : DBNull.Value;
        object cannotBuyValue = info.CannotBuy.HasValue ? info.CannotBuy.Value : DBNull.Value;
        object honeypotWithSameCreatorValue = info.HoneypotWithSameCreator.HasValue
            ? info.HoneypotWithSameCreator.Value
            : DBNull.Value;

        _ = cmd.Parameters.AddWithValue(parameterName: "@chain", value: info.Chain);
        _ = cmd.Parameters.AddWithValue(parameterName: "@address", value: info.Address.ToLowerInvariant());
        _ = cmd.Parameters.AddWithValue(
            parameterName: "@parentAddress",
            value: (object?)info.ParentAddress ?? DBNull.Value
        );
        _ = cmd.Parameters.AddWithValue(parameterName: "@isOpenSource", value: isOpenSourceValue);
        _ = cmd.Parameters.AddWithValue(parameterName: "@isHoneypot", value: isHoneypotValue);
        _ = cmd.Parameters.AddWithValue(parameterName: "@isProxy", value: isProxyValue);
        _ = cmd.Parameters.AddWithValue(parameterName: "@buyTax", value: buyTaxValue);
        _ = cmd.Parameters.AddWithValue(parameterName: "@sellTax", value: sellTaxValue);
        _ = cmd.Parameters.AddWithValue(parameterName: "@transferTax", value: transferTaxValue);
        _ = cmd.Parameters.AddWithValue(parameterName: "@cannotBuy", value: cannotBuyValue);
        _ = cmd.Parameters.AddWithValue(parameterName: "@honeypotWithSameCreator", value: honeypotWithSameCreatorValue);
        _ = cmd.Parameters.AddWithValue(parameterName: "@tokenName", value: (object?)info.TokenName ?? DBNull.Value);
        _ = cmd.Parameters.AddWithValue(
            parameterName: "@tokenSymbol",
            value: (object?)info.TokenSymbol ?? DBNull.Value
        );
        _ = cmd.Parameters.AddWithValue(parameterName: "@checkedAt", value: checkedAtMs);
        _ = cmd.ExecuteNonQuery();
    }
}

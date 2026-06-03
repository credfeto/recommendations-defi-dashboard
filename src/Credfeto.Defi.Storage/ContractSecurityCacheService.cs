using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage;

public sealed class ContractSecurityCacheService
{
    private static readonly TimeSpan SecurityTtl = TimeSpan.FromHours(24);

    private readonly IDatabase _database;
    private readonly TimeProvider _timeProvider;

    public ContractSecurityCacheService(IDatabase database, TimeProvider timeProvider)
    {
        this._database = database;
        this._timeProvider = timeProvider;
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
        DateTimeOffset now = this._timeProvider.GetUtcNow();

        ContractSecurityRow? row = await this._database.ExecuteAsync(
            action: (c, ct) =>
                DefiDatabase.ContractSecurity_GetByChainAndAddressAsync(
                    connection: c,
                    chain: chain,
                    address: address.ToLowerInvariant(),
                    cancellationToken: ct
                ),
            cancellationToken: cancellationToken
        );

        if (row is null || now - row.CheckedAt >= SecurityTtl)
        {
            return null;
        }

        return MapToModel(row);
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
        IReadOnlyList<ContractSecurityRow> rows = await this._database.ExecuteAsync(
            action: (c, ct) =>
                DefiDatabase.ContractSecurity_GetChildrenByParentAddressAsync(
                    connection: c,
                    chain: chain,
                    parentAddress: parentAddress.ToLowerInvariant(),
                    cancellationToken: ct
                ),
            cancellationToken: cancellationToken
        );

        ContractSecurityInfo[] result = new ContractSecurityInfo[rows.Count];

        for (int i = 0; i < rows.Count; i++)
        {
            result[i] = MapToModel(rows[i]);
        }

        return result;
    }

    /// <summary>
    ///     Persists a <see cref="ContractSecurityInfo" /> entry to the cache.
    /// </summary>
    public ValueTask SetAsync(ContractSecurityInfo info, CancellationToken cancellationToken)
    {
        DateTimeOffset now = this._timeProvider.GetUtcNow();

        return this._database.ExecuteAsync(
            action: (c, ct) =>
                DefiDatabase.ContractSecurity_UpsertAsync(
                    connection: c,
                    chain: info.Chain,
                    address: info.Address.ToLowerInvariant(),
                    parentAddress: info.ParentAddress,
                    isOpenSource: info.IsOpenSource,
                    isHoneypot: info.IsHoneypot,
                    isProxy: info.IsProxy,
                    buyTax: info.BuyTax,
                    sellTax: info.SellTax,
                    transferTax: info.TransferTax,
                    cannotBuy: info.CannotBuy,
                    honeypotWithSameCreator: info.HoneypotWithSameCreator,
                    tokenName: info.TokenName,
                    tokenSymbol: info.TokenSymbol,
                    checkedAt: now,
                    cancellationToken: ct
                ),
            cancellationToken: cancellationToken
        );
    }

    private static ContractSecurityInfo MapToModel(ContractSecurityRow row)
    {
        return new ContractSecurityInfo
        {
            Chain = row.Chain,
            Address = row.Address,
            ParentAddress = row.ParentAddress,
            IsOpenSource = row.IsOpenSource,
            IsHoneypot = row.IsHoneypot,
            IsProxy = row.IsProxy,
            BuyTax = row.BuyTax,
            SellTax = row.SellTax,
            TransferTax = row.TransferTax,
            CannotBuy = row.CannotBuy,
            HoneypotWithSameCreator = row.HoneypotWithSameCreator,
            TokenName = row.TokenName,
            TokenSymbol = row.TokenSymbol,
        };
    }
}

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage;

public sealed class DefiLlamaProtocolStorageService : IDefiLlamaProtocolStorageService
{
    private readonly IDatabase _database;

    public DefiLlamaProtocolStorageService(IDatabase database)
    {
        this._database = database;
    }

    public async ValueTask StoreProtocolsAsync(
        IReadOnlyList<RawProtocol> protocols,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<DefiLlamaProtocolSyncRow> protocolRows = BuildProtocolRows(protocols);
        IReadOnlyList<DefiLlamaProtocolAuditLinkSyncRow> auditLinkRows = BuildAuditLinkRows(protocols);

        await this._database.ExecuteAsync(action: SyncAsync, cancellationToken: cancellationToken);

        ValueTask SyncAsync(DbConnection c, CancellationToken ct) =>
            DefiLlamaDatabase.Protocol_SyncAsync(
                connection: c,
                protocols: protocolRows,
                auditLinks: auditLinkRows,
                dataDate: dataDate,
                cancellationToken: ct
            );
    }

    public async ValueTask<IReadOnlyList<RawProtocol>> GetAllProtocolsAsync(CancellationToken cancellationToken)
    {
        ValueTask<IReadOnlyList<DefiLlamaProtocolRow>> protocolRowsTask = this._database.ExecuteAsync(
            action: DefiLlamaDatabase.Protocol_GetAllAsync,
            cancellationToken: cancellationToken
        );

        ValueTask<IReadOnlyList<DefiLlamaProtocolAuditLinkRow>> auditLinkRowsTask = this._database.ExecuteAsync(
            action: DefiLlamaDatabase.ProtocolAuditLink_GetAllAsync,
            cancellationToken: cancellationToken
        );

        IReadOnlyList<DefiLlamaProtocolRow> protocolRows = await protocolRowsTask;
        IReadOnlyList<DefiLlamaProtocolAuditLinkRow> auditLinkRows = await auditLinkRowsTask;

        return MapToRawProtocols(protocolRows: protocolRows, auditLinkRows: auditLinkRows);
    }

    private static IReadOnlyList<DefiLlamaProtocolSyncRow> BuildProtocolRows(IReadOnlyList<RawProtocol> protocols)
    {
        DefiLlamaProtocolSyncRow[] rows = new DefiLlamaProtocolSyncRow[protocols.Count];

        for (int i = 0; i < protocols.Count; i++)
        {
            RawProtocol protocol = protocols[i];
            rows[i] = new DefiLlamaProtocolSyncRow(Slug: protocol.Slug, Audits: protocol.Audits);
        }

        return rows;
    }

    private static IReadOnlyList<DefiLlamaProtocolAuditLinkSyncRow> BuildAuditLinkRows(
        IReadOnlyList<RawProtocol> protocols
    )
    {
        List<DefiLlamaProtocolAuditLinkSyncRow> rows = [];
        HashSet<(string Slug, string AuditLink)> seen = [];

        foreach (RawProtocol protocol in protocols)
        {
            if (protocol.AuditLinks is null)
            {
                continue;
            }

            rows.AddRange(
                protocol
                    .AuditLinks.Where(auditLink => seen.Add((protocol.Slug, auditLink)))
                    .Select(auditLink => new DefiLlamaProtocolAuditLinkSyncRow(
                        Slug: protocol.Slug,
                        AuditLink: auditLink
                    ))
            );
        }

        return rows;
    }

    private static IReadOnlyList<RawProtocol> MapToRawProtocols(
        IReadOnlyList<DefiLlamaProtocolRow> protocolRows,
        IReadOnlyList<DefiLlamaProtocolAuditLinkRow> auditLinkRows
    )
    {
        Dictionary<string, List<string>> auditLinksBySlug = GroupAuditLinksBySlug(auditLinkRows);

        RawProtocol[] result = new RawProtocol[protocolRows.Count];

        for (int i = 0; i < protocolRows.Count; i++)
        {
            DefiLlamaProtocolRow row = protocolRows[i];

            _ = auditLinksBySlug.TryGetValue(key: row.Slug, value: out List<string>? auditLinks);

            result[i] = new RawProtocol
            {
                Slug = row.Slug,
                Audits = row.Audits,
                AuditLinks = auditLinks?.ToArray(),
            };
        }

        return result;
    }

    private static Dictionary<string, List<string>> GroupAuditLinksBySlug(
        IReadOnlyList<DefiLlamaProtocolAuditLinkRow> auditLinkRows
    )
    {
        Dictionary<string, List<string>> result = [];

        foreach (DefiLlamaProtocolAuditLinkRow row in auditLinkRows)
        {
            if (!result.TryGetValue(key: row.Slug, value: out List<string>? auditLinks))
            {
                auditLinks = [];
                result[row.Slug] = auditLinks;
            }

            auditLinks.Add(row.AuditLink);
        }

        return result;
    }
}

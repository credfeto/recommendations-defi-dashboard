using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Database;
using Credfeto.Defi.Data.Models.Models;
using Credfeto.Defi.Storage.Database;
using Credfeto.Defi.Storage.Database.Mappers;
using Credfeto.Defi.Storage.Database.Rows;

namespace Credfeto.Defi.Storage;

public sealed class DefiLlamaHackStorageService : IDefiLlamaHackStorageService
{
    private readonly IDatabase _database;

    public DefiLlamaHackStorageService(IDatabase database)
    {
        this._database = database;
    }

    public async ValueTask StoreHacksAsync(
        IReadOnlyList<RawHack> hacks,
        DateTimeOffset? dataDate,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<DefiLlamaHackSyncRow> hackRows = BuildHackRows(hacks);

        await this._database.ExecuteAsync(action: SyncAsync, cancellationToken: cancellationToken);

        ValueTask SyncAsync(DbConnection c, CancellationToken ct) =>
            DefiLlamaDatabase.Hack_SyncAsync(connection: c, hacks: hackRows, dataDate: dataDate, cancellationToken: ct);
    }

    public async ValueTask<IReadOnlyList<RawHack>> GetAllHacksAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DefiLlamaHackRow> hackRows = await this._database.ExecuteAsync(
            action: DefiLlamaDatabase.Hack_GetAllAsync,
            cancellationToken: cancellationToken
        );

        return MapToRawHacks(hackRows);
    }

    private static IReadOnlyList<DefiLlamaHackSyncRow> BuildHackRows(IReadOnlyList<RawHack> hacks)
    {
        List<DefiLlamaHackSyncRow> rows = new(hacks.Count);
        HashSet<(string Name, long Date)> seen = new(capacity: hacks.Count);

        foreach (RawHack hack in hacks)
        {
            if (!seen.Add((hack.Name, hack.Date)))
            {
                continue;
            }

            rows.Add(
                new DefiLlamaHackSyncRow(
                    Name: hack.Name,
                    HackDate: DateTimeOffset.FromUnixTimeSeconds(hack.Date),
                    Classification: hack.Classification,
                    Technique: hack.Technique,
                    Amount: hack.Amount,
                    Source: hack.Source,
                    ParentProtocolId: hack.ParentProtocolId
                )
            );
        }

        return rows;
    }

    private static IReadOnlyList<RawHack> MapToRawHacks(IReadOnlyList<DefiLlamaHackRow> hackRows)
    {
        RawHack[] result = new RawHack[hackRows.Count];

        for (int i = 0; i < hackRows.Count; i++)
        {
            DefiLlamaHackRow row = hackRows[i];

            result[i] = new RawHack
            {
                Name = row.Name,
                Date = row.HackDate.ToUnixTimeSeconds(),
                Classification = row.Classification,
                Technique = row.Technique,
                Amount = row.Amount,
                Source = row.Source,
                ParentProtocolId = row.ParentProtocolId,
            };
        }

        return result;
    }
}

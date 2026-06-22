# Database Instructions

[Back to Local Instructions Index](index.md)

> Load when: any `.sql` file is present, a migration is being written, or database schema work is being done.

## Schema Per Data Source (MANDATORY)

Each external API source gets its own SQL schema named after the source:

| Source | Schema |
| --- | --- |
| DefiLlama | `DefiLlama` |
| CoinGecko | `CoinGecko` |
| GoPlus | `GoPlus` |
| Pendle | `Pendle` |
| Chainlink | `Chainlink` |

Do not put API-sourced tables in `dbo`. The `dbo` schema is reserved for cross-source or infrastructure tables (e.g. `ApiCache`, `ContractSecurity`).

## Table Per API Endpoint (MANDATORY)

Create at least one table per distinct API call. Decompose the response into typed columns — do not store raw JSON. If a response contains nested or repeated data (e.g. a pool with multiple tokens), split into multiple tables within the same schema linked by a shared key.

## Mandatory Timestamp Columns (MANDATORY)

Every table that holds API-sourced data must include:

| Column | Type | Meaning |
| --- | --- | --- |
| `DateCreated` | `DATETIMEOFFSET NOT NULL` | When the row was first inserted |
| `DateUpdated` | `DATETIMEOFFSET NOT NULL` | When the row was last written by an API call |
| `DataDate` | `DATETIMEOFFSET NULL` | Timestamp carried on the data itself (NULL if the API does not provide one) |

`DateCreated` must be set on INSERT and never updated. `DateUpdated` must be set on every MERGE update.

## Table-Valued Parameter (TVP) Sync Pattern (MANDATORY)

Use a user-defined table type (UDT) as the input to all bulk-write stored procedures — never pass rows one at a time.

Naming conventions:

- UDT: `<Schema>.<TableName>Row` — e.g. `DefiLlama.PoolRow`
- Sync procedure: `<Schema>.<TableName>_Sync` — e.g. `DefiLlama.Pool_Sync`
- Read procedures: `<Schema>.<TableName>_<Operation>` — e.g. `DefiLlama.Pool_GetAll`

The UDT column list must exactly match the non-identity, non-computed columns of its target table (excluding `DateCreated` and `DateUpdated`, which the procedure manages).

## Sync Stored Procedure (MANDATORY)

The `_Sync` procedure must perform a full INSERT / UPDATE / DELETE so the table always reflects the latest API response exactly. Use `MERGE`:

```sql
MERGE [Schema].[Table] AS Tgt
USING @Rows AS Src ON <primary key join>
WHEN MATCHED AND <data differs>
  THEN UPDATE SET ..., [DateUpdated] = SYSDATETIMEOFFSET()
WHEN NOT MATCHED BY TARGET
  THEN INSERT (..., [DateCreated], [DateUpdated]) VALUES (..., SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
WHEN NOT MATCHED BY SOURCE
  THEN DELETE;
```

- `DateCreated` and `DateUpdated` always use `SYSDATETIMEOFFSET()` — the database server supplies these timestamps, never the application.
- `DataDate` is the exception: it is passed as a parameter (`@DataDate DATETIMEOFFSET NULL`) because it originates from the API response, not the server clock.
- `DateCreated` is set on INSERT only; the MERGE `WHEN MATCHED` branch must never touch it.
- The `WHEN NOT MATCHED BY SOURCE` / DELETE branch removes rows that the API no longer returns, keeping the table current.

## Data Merging Across Sources

When a feature requires combining data from multiple API sources (e.g. enriching pool rows with Chainlink price data):

- Prefer a dedicated SQL stored procedure that joins the relevant tables and returns the merged result — keep the join logic in the database, not in C# LINQ.
- Name the merge procedure `<TargetSchema>.<Purpose>_Get` or place it in `dbo` if it spans multiple schemas — e.g. `dbo.EnrichedPool_GetAll`.
- If a merge cannot be expressed cleanly as a single query (e.g. it requires cursor logic or complex pivoting), perform it in the C# service layer, but document why SQL was not used.

## Migration Files

- Each migration lives in `src/Credfeto.Defi.Storage/migrations/` and is numbered sequentially: `001_InitialCreate.sql`, `002_AddDefiLlamaSchema.sql`, etc.
- A migration must be self-contained and idempotent: wrap every `CREATE TABLE` in an `IF OBJECT_ID(...) IS NULL` guard; use `CREATE OR ALTER PROCEDURE` and `CREATE OR ALTER TYPE` (or drop-and-recreate for types, since SQL Server does not support `CREATE OR ALTER TYPE`).
- Never modify an existing migration file — add a new one.

## Stored Procedure Conventions

- Always start with `SET NOCOUNT ON;`.
- Use `DATETIMEOFFSET` for all timestamps — never `DATETIME` or `DATETIME2`.
- Use `FLOAT(53)` for floating-point columns — never bare `FLOAT` or `REAL`.
- Column lists in `INSERT` statements must always be explicit — never rely on column order.

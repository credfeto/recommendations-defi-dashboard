IF
  NOT EXISTS (
    SELECT 1 FROM [sys].[schemas]
    WHERE [name] = N'HoneypotIs'
  )
  BEGIN
    EXEC ('CREATE SCHEMA [HoneypotIs]');
  END;
GO

IF OBJECT_ID(N'[HoneypotIs].[TokenSecurity]', N'U') IS NULL
  BEGIN
    CREATE TABLE [HoneypotIs].[TokenSecurity]
    (
      [Chain] NVARCHAR(100) NOT NULL,
      [Address] NVARCHAR(100) NOT NULL,
      [IsHoneypot] BIT NULL,
      [BuyTax] FLOAT(53) NULL,
      [SellTax] FLOAT(53) NULL,
      [SimulationSuccess] BIT NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_HoneypotIs_TokenSecurity] PRIMARY KEY ([Chain], [Address])
    );
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'HoneypotIs') AND [name] = N'TokenSecurityRow'
  )
  BEGIN
    DROP TYPE [HoneypotIs].[TokenSecurityRow];
  END;
GO

CREATE TYPE [HoneypotIs].[TokenSecurityRow] AS TABLE
(
  [Chain] NVARCHAR(100) NOT NULL,
  [Address] NVARCHAR(100) NOT NULL,
  [IsHoneypot] BIT NULL,
  [BuyTax] FLOAT(53) NULL,
  [SellTax] FLOAT(53) NULL,
  [SimulationSuccess] BIT NULL
);
GO

CREATE OR ALTER PROCEDURE [HoneypotIs].[TokenSecurity_Sync]
  @Rows [HoneypotIs].[TokenSecurityRow] READONLY
AS
BEGIN
  SET NOCOUNT ON;

  -- No WHEN NOT MATCHED BY SOURCE / DELETE branch: this is an on-demand per-key cache
  -- (SetAsync writes one contract at a time), not a bulk snapshot sync - see the TVP
  -- Sync Pattern exception in ai/local/database.instructions.md.
  MERGE [HoneypotIs].[TokenSecurity] AS Tgt
  USING @Rows AS Src ON Tgt.[Chain] = Src.[Chain] AND Tgt.[Address] = Src.[Address]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [IsHoneypot] = Src.[IsHoneypot],
        [BuyTax] = Src.[BuyTax],
        [SellTax] = Src.[SellTax],
        [SimulationSuccess] = Src.[SimulationSuccess],
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
      [Chain],
      [Address],
      [IsHoneypot],
      [BuyTax],
      [SellTax],
      [SimulationSuccess],
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[Chain],
      Src.[Address],
      Src.[IsHoneypot],
      Src.[BuyTax],
      Src.[SellTax],
      Src.[SimulationSuccess],
      NULL,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    );
END;
GO

CREATE OR ALTER PROCEDURE [HoneypotIs].[TokenSecurity_GetByChainAndAddress]
  @Chain NVARCHAR(100),
  @Address NVARCHAR(100)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Chain],
    [Address],
    [IsHoneypot],
    [BuyTax],
    [SellTax],
    [SimulationSuccess],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [HoneypotIs].[TokenSecurity]
  WHERE [Chain] = @Chain
    AND [Address] = @Address;
END;
GO

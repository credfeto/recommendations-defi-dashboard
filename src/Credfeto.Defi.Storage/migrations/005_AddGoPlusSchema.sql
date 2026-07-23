IF
  NOT EXISTS (
    SELECT 1 FROM [sys].[schemas]
    WHERE [name] = N'GoPlus'
  )
  BEGIN
    EXEC ('CREATE SCHEMA [GoPlus]');
  END;
GO

IF OBJECT_ID(N'[GoPlus].[TokenSecurity]', N'U') IS NULL
  BEGIN
    CREATE TABLE [GoPlus].[TokenSecurity]
    (
      [Chain] NVARCHAR(100) NOT NULL,
      [Address] NVARCHAR(100) NOT NULL,
      [ParentAddress] NVARCHAR(100) NULL,
      [IsOpenSource] BIT NULL,
      [IsHoneypot] BIT NULL,
      [IsProxy] BIT NULL,
      [BuyTax] FLOAT(53) NULL,
      [SellTax] FLOAT(53) NULL,
      [TransferTax] FLOAT(53) NULL,
      [CannotBuy] BIT NULL,
      [HoneypotWithSameCreator] BIT NULL,
      [TokenName] NVARCHAR(MAX) NULL,
      [TokenSymbol] NVARCHAR(MAX) NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_GoPlus_TokenSecurity] PRIMARY KEY ([Chain], [Address])
    );
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'GoPlus') AND [name] = N'TokenSecurityRow'
  )
  BEGIN
    DROP TYPE [GoPlus].[TokenSecurityRow];
  END;
GO

CREATE TYPE [GoPlus].[TokenSecurityRow] AS TABLE
(
  [Chain] NVARCHAR(100) NOT NULL,
  [Address] NVARCHAR(100) NOT NULL,
  [ParentAddress] NVARCHAR(100) NULL,
  [IsOpenSource] BIT NULL,
  [IsHoneypot] BIT NULL,
  [IsProxy] BIT NULL,
  [BuyTax] FLOAT(53) NULL,
  [SellTax] FLOAT(53) NULL,
  [TransferTax] FLOAT(53) NULL,
  [CannotBuy] BIT NULL,
  [HoneypotWithSameCreator] BIT NULL,
  [TokenName] NVARCHAR(MAX) NULL,
  [TokenSymbol] NVARCHAR(MAX) NULL
);
GO

CREATE OR ALTER PROCEDURE [GoPlus].[TokenSecurity_Sync]
  @Rows [GoPlus].[TokenSecurityRow] READONLY
AS
BEGIN
  SET NOCOUNT ON;

  -- No WHEN NOT MATCHED BY SOURCE / DELETE branch: this is an on-demand per-key cache
  -- (SetAsync writes one contract at a time), not a bulk snapshot sync - see the TVP
  -- Sync Pattern exception in ai/local/database.instructions.md.
  MERGE [GoPlus].[TokenSecurity] AS Tgt
  USING @Rows AS Src ON Tgt.[Chain] = Src.[Chain] AND Tgt.[Address] = Src.[Address]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [ParentAddress] = Src.[ParentAddress],
        [IsOpenSource] = Src.[IsOpenSource],
        [IsHoneypot] = Src.[IsHoneypot],
        [IsProxy] = Src.[IsProxy],
        [BuyTax] = Src.[BuyTax],
        [SellTax] = Src.[SellTax],
        [TransferTax] = Src.[TransferTax],
        [CannotBuy] = Src.[CannotBuy],
        [HoneypotWithSameCreator] = Src.[HoneypotWithSameCreator],
        [TokenName] = Src.[TokenName],
        [TokenSymbol] = Src.[TokenSymbol],
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
      [Chain],
      [Address],
      [ParentAddress],
      [IsOpenSource],
      [IsHoneypot],
      [IsProxy],
      [BuyTax],
      [SellTax],
      [TransferTax],
      [CannotBuy],
      [HoneypotWithSameCreator],
      [TokenName],
      [TokenSymbol],
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[Chain],
      Src.[Address],
      Src.[ParentAddress],
      Src.[IsOpenSource],
      Src.[IsHoneypot],
      Src.[IsProxy],
      Src.[BuyTax],
      Src.[SellTax],
      Src.[TransferTax],
      Src.[CannotBuy],
      Src.[HoneypotWithSameCreator],
      Src.[TokenName],
      Src.[TokenSymbol],
      NULL,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    );
END;
GO

CREATE OR ALTER PROCEDURE [GoPlus].[TokenSecurity_GetByChainAndAddress]
  @Chain NVARCHAR(100),
  @Address NVARCHAR(100)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Chain],
    [Address],
    [ParentAddress],
    [IsOpenSource],
    [IsHoneypot],
    [IsProxy],
    [BuyTax],
    [SellTax],
    [TransferTax],
    [CannotBuy],
    [HoneypotWithSameCreator],
    [TokenName],
    [TokenSymbol],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [GoPlus].[TokenSecurity]
  WHERE [Chain] = @Chain
    AND [Address] = @Address;
END;
GO

CREATE OR ALTER PROCEDURE [GoPlus].[TokenSecurity_GetChildrenByParentAddress]
  @Chain NVARCHAR(100),
  @ParentAddress NVARCHAR(100)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Chain],
    [Address],
    [ParentAddress],
    [IsOpenSource],
    [IsHoneypot],
    [IsProxy],
    [BuyTax],
    [SellTax],
    [TransferTax],
    [CannotBuy],
    [HoneypotWithSameCreator],
    [TokenName],
    [TokenSymbol],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [GoPlus].[TokenSecurity]
  WHERE [Chain] = @Chain
    AND [ParentAddress] = @ParentAddress;
END;
GO

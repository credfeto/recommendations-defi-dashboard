IF
  NOT EXISTS (
    SELECT 1 FROM [sys].[schemas]
    WHERE [name] = N'CoinGecko'
  )
  BEGIN
    EXEC ('CREATE SCHEMA [CoinGecko]');
  END;
GO

IF OBJECT_ID(N'[CoinGecko].[Coin]', N'U') IS NULL
  BEGIN
    CREATE TABLE [CoinGecko].[Coin]
    (
      [Id] NVARCHAR(100) NOT NULL,
      [Symbol] NVARCHAR(50) NOT NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_CoinGecko_Coin] PRIMARY KEY ([Id])
    );
  END;
GO

IF OBJECT_ID(N'[CoinGecko].[CoinPlatformAddress]', N'U') IS NULL
  BEGIN
    CREATE TABLE [CoinGecko].[CoinPlatformAddress]
    (
      [CoinId] NVARCHAR(100) NOT NULL,
      [Platform] NVARCHAR(100) NOT NULL,
      [ContractAddress] NVARCHAR(200) NOT NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_CoinGecko_CoinPlatformAddress] PRIMARY KEY ([CoinId], [Platform], [ContractAddress]),
      CONSTRAINT [FK_CoinGecko_CoinPlatformAddress_Coin] FOREIGN KEY ([CoinId]) REFERENCES [CoinGecko].[Coin] ([Id]) ON DELETE CASCADE
    );
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'CoinGecko') AND [name] = N'CoinRow'
  )
  BEGIN
    DROP TYPE [CoinGecko].[CoinRow];
  END;
GO

CREATE TYPE [CoinGecko].[CoinRow] AS TABLE
(
  [Id] NVARCHAR(100) NOT NULL,
  [Symbol] NVARCHAR(50) NOT NULL
);
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'CoinGecko') AND [name] = N'CoinPlatformAddressRow'
  )
  BEGIN
    DROP TYPE [CoinGecko].[CoinPlatformAddressRow];
  END;
GO

CREATE TYPE [CoinGecko].[CoinPlatformAddressRow] AS TABLE
(
  [CoinId] NVARCHAR(100) NOT NULL,
  [Platform] NVARCHAR(100) NOT NULL,
  [ContractAddress] NVARCHAR(200) NOT NULL
);
GO

CREATE OR ALTER PROCEDURE [CoinGecko].[Coin_Sync]
  @Coins [CoinGecko].[CoinRow] READONLY,
  @Addresses [CoinGecko].[CoinPlatformAddressRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [CoinGecko].[Coin] AS Tgt
  USING @Coins AS Src ON Tgt.[Id] = Src.[Id]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Symbol] = Src.[Symbol],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([Id], [Symbol], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[Id], Src.[Symbol], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;

  MERGE [CoinGecko].[CoinPlatformAddress] AS Tgt
  USING @Addresses AS Src
  ON Tgt.[CoinId] = Src.[CoinId]
  AND Tgt.[Platform] = Src.[Platform]
  AND Tgt.[ContractAddress] = Src.[ContractAddress]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([CoinId], [Platform], [ContractAddress], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[CoinId], Src.[Platform], Src.[ContractAddress], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
GO

CREATE OR ALTER PROCEDURE [CoinGecko].[Coin_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Id],
    [Symbol],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [CoinGecko].[Coin];
END;
GO

CREATE OR ALTER PROCEDURE [CoinGecko].[CoinPlatformAddress_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [CoinId],
    [Platform],
    [ContractAddress],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [CoinGecko].[CoinPlatformAddress]
  ORDER BY [CoinId], [Platform];
END;
GO

CREATE OR ALTER PROCEDURE [CoinGecko].[CoinPlatformAddress_GetByContractAddress]
  @ContractAddress NVARCHAR(200)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [CoinId],
    [Platform],
    [ContractAddress],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [CoinGecko].[CoinPlatformAddress]
  WHERE [ContractAddress] = @ContractAddress;
END;
GO

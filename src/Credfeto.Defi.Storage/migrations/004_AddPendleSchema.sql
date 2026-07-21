IF
  NOT EXISTS (
    SELECT 1 FROM [sys].[schemas]
    WHERE [name] = N'Pendle'
  )
  BEGIN
    EXEC ('CREATE SCHEMA [Pendle]');
  END;
GO

IF OBJECT_ID(N'[Pendle].[Market]', N'U') IS NULL
  BEGIN
    CREATE TABLE [Pendle].[Market]
    (
      [Address] NVARCHAR(100) NOT NULL,
      [ChainId] INT NOT NULL,
      [SimpleSymbol] NVARCHAR(200) NOT NULL,
      [Expiry] NVARCHAR(50) NULL,
      [IsActive] BIT NOT NULL,
      [LiquidityUsd] FLOAT(53) NULL,
      [AggregatedApy] FLOAT(53) NOT NULL,
      [UnderlyingApy] FLOAT(53) NOT NULL,
      [PendleApy] FLOAT(53) NOT NULL,
      [LpRewardApy] FLOAT(53) NOT NULL,
      [SwapFeeApy] FLOAT(53) NOT NULL,
      [TradingVolumeUsd] FLOAT(53) NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_Pendle_Market] PRIMARY KEY ([Address], [ChainId])
    );
  END;
GO

IF OBJECT_ID(N'[Pendle].[MarketCategory]', N'U') IS NULL
  BEGIN
    CREATE TABLE [Pendle].[MarketCategory]
    (
      [Address] NVARCHAR(100) NOT NULL,
      [ChainId] INT NOT NULL,
      [CategoryId] NVARCHAR(100) NOT NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_Pendle_MarketCategory] PRIMARY KEY ([Address], [ChainId], [CategoryId]),
      CONSTRAINT [FK_Pendle_MarketCategory_Market] FOREIGN KEY ([Address], [ChainId]) REFERENCES [Pendle].[Market] ([Address], [ChainId]) ON DELETE CASCADE
    );
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'Pendle') AND [name] = N'MarketRow'
  )
  BEGIN
    DROP TYPE [Pendle].[MarketRow];
  END;
GO

CREATE TYPE [Pendle].[MarketRow] AS TABLE
(
  [Address] NVARCHAR(100) NOT NULL,
  [ChainId] INT NOT NULL,
  [SimpleSymbol] NVARCHAR(200) NOT NULL,
  [Expiry] NVARCHAR(50) NULL,
  [IsActive] BIT NOT NULL,
  [LiquidityUsd] FLOAT(53) NULL,
  [AggregatedApy] FLOAT(53) NOT NULL,
  [UnderlyingApy] FLOAT(53) NOT NULL,
  [PendleApy] FLOAT(53) NOT NULL,
  [LpRewardApy] FLOAT(53) NOT NULL,
  [SwapFeeApy] FLOAT(53) NOT NULL,
  [TradingVolumeUsd] FLOAT(53) NULL
);
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'Pendle') AND [name] = N'MarketCategoryRow'
  )
  BEGIN
    DROP TYPE [Pendle].[MarketCategoryRow];
  END;
GO

CREATE TYPE [Pendle].[MarketCategoryRow] AS TABLE
(
  [Address] NVARCHAR(100) NOT NULL,
  [ChainId] INT NOT NULL,
  [CategoryId] NVARCHAR(100) NOT NULL
);
GO

CREATE OR ALTER PROCEDURE [Pendle].[Market_Sync]
  @Markets [Pendle].[MarketRow] READONLY,
  @Categories [Pendle].[MarketCategoryRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [Pendle].[Market] AS Tgt
  USING @Markets AS Src ON Tgt.[Address] = Src.[Address] AND Tgt.[ChainId] = Src.[ChainId]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [SimpleSymbol] = Src.[SimpleSymbol],
        [Expiry] = Src.[Expiry],
        [IsActive] = Src.[IsActive],
        [LiquidityUsd] = Src.[LiquidityUsd],
        [AggregatedApy] = Src.[AggregatedApy],
        [UnderlyingApy] = Src.[UnderlyingApy],
        [PendleApy] = Src.[PendleApy],
        [LpRewardApy] = Src.[LpRewardApy],
        [SwapFeeApy] = Src.[SwapFeeApy],
        [TradingVolumeUsd] = Src.[TradingVolumeUsd],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
      [Address],
      [ChainId],
      [SimpleSymbol],
      [Expiry],
      [IsActive],
      [LiquidityUsd],
      [AggregatedApy],
      [UnderlyingApy],
      [PendleApy],
      [LpRewardApy],
      [SwapFeeApy],
      [TradingVolumeUsd],
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[Address],
      Src.[ChainId],
      Src.[SimpleSymbol],
      Src.[Expiry],
      Src.[IsActive],
      Src.[LiquidityUsd],
      Src.[AggregatedApy],
      Src.[UnderlyingApy],
      Src.[PendleApy],
      Src.[LpRewardApy],
      Src.[SwapFeeApy],
      Src.[TradingVolumeUsd],
      @DataDate,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    )
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;

  MERGE [Pendle].[MarketCategory] AS Tgt
  USING @Categories AS Src ON Tgt.[Address] = Src.[Address] AND Tgt.[ChainId] = Src.[ChainId] AND Tgt.[CategoryId] = Src.[CategoryId]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([Address], [ChainId], [CategoryId], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[Address], Src.[ChainId], Src.[CategoryId], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
GO

CREATE OR ALTER PROCEDURE [Pendle].[Market_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Address],
    [ChainId],
    [SimpleSymbol],
    [Expiry],
    [IsActive],
    [LiquidityUsd],
    [AggregatedApy],
    [UnderlyingApy],
    [PendleApy],
    [LpRewardApy],
    [SwapFeeApy],
    [TradingVolumeUsd],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [Pendle].[Market]
  WHERE [IsActive] = 1;
END;
GO

CREATE OR ALTER PROCEDURE [Pendle].[MarketCategory_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Address],
    [ChainId],
    [CategoryId],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [Pendle].[MarketCategory]
  ORDER BY [Address], [ChainId], [CategoryId];
END;
GO

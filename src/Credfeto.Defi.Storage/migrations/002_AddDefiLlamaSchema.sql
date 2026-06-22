IF
  NOT EXISTS (
    SELECT 1 FROM [sys].[schemas]
    WHERE [name] = N'DefiLlama'
  )
  BEGIN
    EXEC ('CREATE SCHEMA [DefiLlama]');
  END;
GO

IF OBJECT_ID(N'[DefiLlama].[Pool]', N'U') IS NULL
  BEGIN
    CREATE TABLE [DefiLlama].[Pool]
    (
      [PoolId] NVARCHAR(100) NOT NULL,
      [Chain] NVARCHAR(100) NOT NULL,
      [Project] NVARCHAR(100) NOT NULL,
      [Symbol] NVARCHAR(200) NOT NULL,
      [TvlUsd] FLOAT(53) NOT NULL,
      [ApyBase] FLOAT(53) NULL,
      [ApyReward] FLOAT(53) NULL,
      [Apy] FLOAT(53) NOT NULL,
      [ApyPct1D] FLOAT(53) NULL,
      [ApyPct7D] FLOAT(53) NULL,
      [ApyPct30D] FLOAT(53) NULL,
      [Stablecoin] BIT NOT NULL,
      [IlRisk] NVARCHAR(50) NOT NULL,
      [Exposure] NVARCHAR(50) NULL,
      [PredictedClass] NVARCHAR(50) NULL,
      [PredictedProbability] FLOAT(53) NULL,
      [BinnedConfidence] FLOAT(53) NULL,
      [PoolMeta] NVARCHAR(MAX) NULL,
      [Mu] FLOAT(53) NOT NULL,
      [Sigma] FLOAT(53) NOT NULL,
      [Count] INT NOT NULL,
      [Outlier] BIT NOT NULL,
      [Il7d] FLOAT(53) NULL,
      [ApyBase7d] FLOAT(53) NULL,
      [ApyMean30d] FLOAT(53) NOT NULL,
      [VolumeUsd1d] FLOAT(53) NULL,
      [VolumeUsd7d] FLOAT(53) NULL,
      [ApyBaseInception] FLOAT(53) NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_DefiLlama_Pool] PRIMARY KEY ([PoolId])
    );
  END;
GO

IF OBJECT_ID(N'[DefiLlama].[PoolRewardToken]', N'U') IS NULL
  BEGIN
    CREATE TABLE [DefiLlama].[PoolRewardToken]
    (
      [PoolId] NVARCHAR(100) NOT NULL,
      [SortOrder] INT NOT NULL,
      [TokenAddress] NVARCHAR(200) NOT NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_DefiLlama_PoolRewardToken] PRIMARY KEY ([PoolId], [SortOrder]),
      CONSTRAINT [FK_DefiLlama_PoolRewardToken_Pool] FOREIGN KEY ([PoolId]) REFERENCES [DefiLlama].[Pool] ([PoolId]) ON DELETE CASCADE
    );
  END;
GO

IF OBJECT_ID(N'[DefiLlama].[PoolUnderlyingToken]', N'U') IS NULL
  BEGIN
    CREATE TABLE [DefiLlama].[PoolUnderlyingToken]
    (
      [PoolId] NVARCHAR(100) NOT NULL,
      [SortOrder] INT NOT NULL,
      [TokenAddress] NVARCHAR(200) NOT NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_DefiLlama_PoolUnderlyingToken] PRIMARY KEY ([PoolId], [SortOrder]),
      CONSTRAINT [FK_DefiLlama_PoolUnderlyingToken_Pool] FOREIGN KEY ([PoolId]) REFERENCES [DefiLlama].[Pool] ([PoolId]) ON DELETE CASCADE
    );
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'DefiLlama') AND [name] = N'PoolRow'
  )
  BEGIN
    DROP TYPE [DefiLlama].[PoolRow];
  END;
GO

CREATE TYPE [DefiLlama].[PoolRow] AS TABLE
(
  [PoolId] NVARCHAR(100) NOT NULL,
  [Chain] NVARCHAR(100) NOT NULL,
  [Project] NVARCHAR(100) NOT NULL,
  [Symbol] NVARCHAR(200) NOT NULL,
  [TvlUsd] FLOAT(53) NOT NULL,
  [ApyBase] FLOAT(53) NULL,
  [ApyReward] FLOAT(53) NULL,
  [Apy] FLOAT(53) NOT NULL,
  [ApyPct1D] FLOAT(53) NULL,
  [ApyPct7D] FLOAT(53) NULL,
  [ApyPct30D] FLOAT(53) NULL,
  [Stablecoin] BIT NOT NULL,
  [IlRisk] NVARCHAR(50) NOT NULL,
  [Exposure] NVARCHAR(50) NULL,
  [PredictedClass] NVARCHAR(50) NULL,
  [PredictedProbability] FLOAT(53) NULL,
  [BinnedConfidence] FLOAT(53) NULL,
  [PoolMeta] NVARCHAR(MAX) NULL,
  [Mu] FLOAT(53) NOT NULL,
  [Sigma] FLOAT(53) NOT NULL,
  [Count] INT NOT NULL,
  [Outlier] BIT NOT NULL,
  [Il7d] FLOAT(53) NULL,
  [ApyBase7d] FLOAT(53) NULL,
  [ApyMean30d] FLOAT(53) NOT NULL,
  [VolumeUsd1d] FLOAT(53) NULL,
  [VolumeUsd7d] FLOAT(53) NULL,
  [ApyBaseInception] FLOAT(53) NULL
);
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'DefiLlama') AND [name] = N'PoolRewardTokenRow'
  )
  BEGIN
    DROP TYPE [DefiLlama].[PoolRewardTokenRow];
  END;
GO

CREATE TYPE [DefiLlama].[PoolRewardTokenRow] AS TABLE
(
  [PoolId] NVARCHAR(100) NOT NULL,
  [SortOrder] INT NOT NULL,
  [TokenAddress] NVARCHAR(200) NOT NULL
);
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'DefiLlama') AND [name] = N'PoolUnderlyingTokenRow'
  )
  BEGIN
    DROP TYPE [DefiLlama].[PoolUnderlyingTokenRow];
  END;
GO

CREATE TYPE [DefiLlama].[PoolUnderlyingTokenRow] AS TABLE
(
  [PoolId] NVARCHAR(100) NOT NULL,
  [SortOrder] INT NOT NULL,
  [TokenAddress] NVARCHAR(200) NOT NULL
);
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[Pool_Sync]
  @Rows [DefiLlama].[PoolRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [DefiLlama].[Pool] AS Tgt
  USING @Rows AS Src ON Tgt.[PoolId] = Src.[PoolId]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Chain] = Src.[Chain],
        [Project] = Src.[Project],
        [Symbol] = Src.[Symbol],
        [TvlUsd] = Src.[TvlUsd],
        [ApyBase] = Src.[ApyBase],
        [ApyReward] = Src.[ApyReward],
        [Apy] = Src.[Apy],
        [ApyPct1D] = Src.[ApyPct1D],
        [ApyPct7D] = Src.[ApyPct7D],
        [ApyPct30D] = Src.[ApyPct30D],
        [Stablecoin] = Src.[Stablecoin],
        [IlRisk] = Src.[IlRisk],
        [Exposure] = Src.[Exposure],
        [PredictedClass] = Src.[PredictedClass],
        [PredictedProbability] = Src.[PredictedProbability],
        [BinnedConfidence] = Src.[BinnedConfidence],
        [PoolMeta] = Src.[PoolMeta],
        [Mu] = Src.[Mu],
        [Sigma] = Src.[Sigma],
        [Count] = Src.[Count],
        [Outlier] = Src.[Outlier],
        [Il7d] = Src.[Il7d],
        [ApyBase7d] = Src.[ApyBase7d],
        [ApyMean30d] = Src.[ApyMean30d],
        [VolumeUsd1d] = Src.[VolumeUsd1d],
        [VolumeUsd7d] = Src.[VolumeUsd7d],
        [ApyBaseInception] = Src.[ApyBaseInception],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
      [PoolId],
      [Chain],
      [Project],
      [Symbol],
      [TvlUsd],
      [ApyBase],
      [ApyReward],
      [Apy],
      [ApyPct1D],
      [ApyPct7D],
      [ApyPct30D],
      [Stablecoin],
      [IlRisk],
      [Exposure],
      [PredictedClass],
      [PredictedProbability],
      [BinnedConfidence],
      [PoolMeta],
      [Mu],
      [Sigma],
      [Count],
      [Outlier],
      [Il7d],
      [ApyBase7d],
      [ApyMean30d],
      [VolumeUsd1d],
      [VolumeUsd7d],
      [ApyBaseInception],
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[PoolId],
      Src.[Chain],
      Src.[Project],
      Src.[Symbol],
      Src.[TvlUsd],
      Src.[ApyBase],
      Src.[ApyReward],
      Src.[Apy],
      Src.[ApyPct1D],
      Src.[ApyPct7D],
      Src.[ApyPct30D],
      Src.[Stablecoin],
      Src.[IlRisk],
      Src.[Exposure],
      Src.[PredictedClass],
      Src.[PredictedProbability],
      Src.[BinnedConfidence],
      Src.[PoolMeta],
      Src.[Mu],
      Src.[Sigma],
      Src.[Count],
      Src.[Outlier],
      Src.[Il7d],
      Src.[ApyBase7d],
      Src.[ApyMean30d],
      Src.[VolumeUsd1d],
      Src.[VolumeUsd7d],
      Src.[ApyBaseInception],
      @DataDate,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    )
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[PoolRewardToken_Sync]
  @Rows [DefiLlama].[PoolRewardTokenRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [DefiLlama].[PoolRewardToken] AS Tgt
  USING @Rows AS Src ON Tgt.[PoolId] = Src.[PoolId] AND Tgt.[SortOrder] = Src.[SortOrder]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [TokenAddress] = Src.[TokenAddress],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([PoolId], [SortOrder], [TokenAddress], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[PoolId], Src.[SortOrder], Src.[TokenAddress], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[PoolUnderlyingToken_Sync]
  @Rows [DefiLlama].[PoolUnderlyingTokenRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [DefiLlama].[PoolUnderlyingToken] AS Tgt
  USING @Rows AS Src ON Tgt.[PoolId] = Src.[PoolId] AND Tgt.[SortOrder] = Src.[SortOrder]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [TokenAddress] = Src.[TokenAddress],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([PoolId], [SortOrder], [TokenAddress], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[PoolId], Src.[SortOrder], Src.[TokenAddress], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[Pool_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [PoolId],
    [Chain],
    [Project],
    [Symbol],
    [TvlUsd],
    [ApyBase],
    [ApyReward],
    [Apy],
    [ApyPct1D],
    [ApyPct7D],
    [ApyPct30D],
    [Stablecoin],
    [IlRisk],
    [Exposure],
    [PredictedClass],
    [PredictedProbability],
    [BinnedConfidence],
    [PoolMeta],
    [Mu],
    [Sigma],
    [Count],
    [Outlier],
    [Il7d],
    [ApyBase7d],
    [ApyMean30d],
    [VolumeUsd1d],
    [VolumeUsd7d],
    [ApyBaseInception],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[Pool];
END;
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[PoolRewardToken_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [PoolId],
    [SortOrder],
    [TokenAddress],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[PoolRewardToken]
  ORDER BY [PoolId], [SortOrder];
END;
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[PoolUnderlyingToken_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [PoolId],
    [SortOrder],
    [TokenAddress],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[PoolUnderlyingToken]
  ORDER BY [PoolId], [SortOrder];
END;
GO

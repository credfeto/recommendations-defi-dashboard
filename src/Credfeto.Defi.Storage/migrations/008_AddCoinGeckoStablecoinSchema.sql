IF OBJECT_ID(N'[CoinGecko].[Stablecoin]', N'U') IS NULL
  BEGIN
    CREATE TABLE [CoinGecko].[Stablecoin]
    (
      [Id] NVARCHAR(100) NOT NULL,
      [Symbol] NVARCHAR(50) NOT NULL,
      [Name] NVARCHAR(200) NOT NULL,
      [CurrentPrice] DECIMAL(28, 8) NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_CoinGecko_Stablecoin] PRIMARY KEY ([Id])
    );
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'CoinGecko') AND [name] = N'StablecoinRow'
  )
  BEGIN
    DROP TYPE [CoinGecko].[StablecoinRow];
  END;
GO

CREATE TYPE [CoinGecko].[StablecoinRow] AS TABLE
(
  [Id] NVARCHAR(100) NOT NULL,
  [Symbol] NVARCHAR(50) NOT NULL,
  [Name] NVARCHAR(200) NOT NULL,
  [CurrentPrice] DECIMAL(28, 8) NULL
);
GO

CREATE OR ALTER PROCEDURE [CoinGecko].[Stablecoin_Sync]
  @Rows [CoinGecko].[StablecoinRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [CoinGecko].[Stablecoin] AS Tgt
  USING @Rows AS Src ON Tgt.[Id] = Src.[Id]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Symbol] = Src.[Symbol],
        [Name] = Src.[Name],
        [CurrentPrice] = Src.[CurrentPrice],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
      [Id],
      [Symbol],
      [Name],
      [CurrentPrice],
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[Id],
      Src.[Symbol],
      Src.[Name],
      Src.[CurrentPrice],
      @DataDate,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    )
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
GO

CREATE OR ALTER PROCEDURE [CoinGecko].[Stablecoin_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Id],
    [Symbol],
    [Name],
    [CurrentPrice],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [CoinGecko].[Stablecoin];
END;
GO

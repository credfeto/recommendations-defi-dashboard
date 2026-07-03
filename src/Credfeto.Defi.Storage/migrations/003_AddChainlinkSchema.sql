IF
  NOT EXISTS (
    SELECT 1 FROM [sys].[schemas]
    WHERE [name] = N'Chainlink'
  )
  BEGIN
    EXEC ('CREATE SCHEMA [Chainlink]');
  END;
GO

IF OBJECT_ID(N'[Chainlink].[PriceFeed]', N'U') IS NULL
  BEGIN
    CREATE TABLE [Chainlink].[PriceFeed]
    (
      [Symbol] NVARCHAR(50) NOT NULL,
      [CurrentPrice] DECIMAL(28, 18) NOT NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_Chainlink_PriceFeed] PRIMARY KEY ([Symbol])
    );
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'Chainlink') AND [name] = N'PriceFeedRow'
  )
  BEGIN
    DROP TYPE [Chainlink].[PriceFeedRow];
  END;
GO

CREATE TYPE [Chainlink].[PriceFeedRow] AS TABLE
(
  [Symbol] NVARCHAR(50) NOT NULL,
  [CurrentPrice] DECIMAL(28, 18) NOT NULL
);
GO

CREATE OR ALTER PROCEDURE [Chainlink].[PriceFeed_Sync]
  @Rows [Chainlink].[PriceFeedRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [Chainlink].[PriceFeed] AS Tgt
  USING @Rows AS Src ON Tgt.[Symbol] = Src.[Symbol]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [CurrentPrice] = Src.[CurrentPrice],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT (
      [Symbol],
      [CurrentPrice],
      [DataDate],
      [DateCreated],
      [DateUpdated]
    )
    VALUES (
      Src.[Symbol],
      Src.[CurrentPrice],
      @DataDate,
      SYSDATETIMEOFFSET(),
      SYSDATETIMEOFFSET()
    )
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
GO

CREATE OR ALTER PROCEDURE [Chainlink].[PriceFeed_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Symbol],
    [CurrentPrice],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [Chainlink].[PriceFeed];
END;
GO

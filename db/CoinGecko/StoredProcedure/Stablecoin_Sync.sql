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

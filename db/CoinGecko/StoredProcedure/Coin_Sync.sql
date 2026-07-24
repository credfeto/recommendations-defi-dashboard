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

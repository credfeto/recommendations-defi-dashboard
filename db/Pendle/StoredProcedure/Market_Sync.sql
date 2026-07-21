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

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

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

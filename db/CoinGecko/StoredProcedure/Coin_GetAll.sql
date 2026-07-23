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

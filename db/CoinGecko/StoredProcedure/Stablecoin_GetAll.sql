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

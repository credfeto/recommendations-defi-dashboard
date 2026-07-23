CREATE OR ALTER PROCEDURE [CoinGecko].[CoinPlatformAddress_GetByContractAddress]
  @ContractAddress NVARCHAR(200)
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
  WHERE [ContractAddress] = @ContractAddress;
END;

CREATE TYPE [CoinGecko].[CoinPlatformAddressRow] AS TABLE
(
  [CoinId] NVARCHAR(100) NOT NULL,
  [Platform] NVARCHAR(100) NOT NULL,
  [ContractAddress] NVARCHAR(200) NOT NULL
);

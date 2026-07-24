CREATE TABLE [CoinGecko].[CoinPlatformAddress]
(
  [CoinId] NVARCHAR(100) NOT NULL,
  [Platform] NVARCHAR(100) NOT NULL,
  [ContractAddress] NVARCHAR(200) NOT NULL,
  [DateCreated] DATETIMEOFFSET NOT NULL,
  [DateUpdated] DATETIMEOFFSET NOT NULL,
  [DataDate] DATETIMEOFFSET NULL,
  CONSTRAINT [PK_CoinGecko_CoinPlatformAddress] PRIMARY KEY ([CoinId], [Platform], [ContractAddress]),
  CONSTRAINT [FK_CoinGecko_CoinPlatformAddress_Coin] FOREIGN KEY ([CoinId]) REFERENCES [CoinGecko].[Coin] ([Id]) ON DELETE CASCADE
);

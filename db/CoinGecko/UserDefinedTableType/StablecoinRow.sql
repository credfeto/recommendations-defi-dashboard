CREATE TYPE [CoinGecko].[StablecoinRow] AS TABLE
(
  [Id] NVARCHAR(100) NOT NULL,
  [Symbol] NVARCHAR(50) NOT NULL,
  [Name] NVARCHAR(200) NOT NULL,
  [CurrentPrice] DECIMAL(28, 8) NULL
);

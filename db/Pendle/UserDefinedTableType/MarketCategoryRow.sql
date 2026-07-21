CREATE TYPE [Pendle].[MarketCategoryRow] AS TABLE
(
  [Address] NVARCHAR(100) NOT NULL,
  [ChainId] INT NOT NULL,
  [CategoryId] NVARCHAR(100) NOT NULL
);

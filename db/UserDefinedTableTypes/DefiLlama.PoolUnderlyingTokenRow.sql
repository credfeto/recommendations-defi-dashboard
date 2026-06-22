CREATE TYPE [DefiLlama].[PoolUnderlyingTokenRow] AS TABLE
(
  [PoolId] NVARCHAR(100) NOT NULL,
  [SortOrder] INT NOT NULL,
  [TokenAddress] NVARCHAR(200) NOT NULL
);

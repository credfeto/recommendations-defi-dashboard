CREATE TABLE [DefiLlama].[PoolRewardToken]
(
  [PoolId] NVARCHAR(100) NOT NULL,
  [SortOrder] INT NOT NULL,
  [TokenAddress] NVARCHAR(200) NOT NULL,
  [DateCreated] DATETIMEOFFSET NOT NULL,
  [DateUpdated] DATETIMEOFFSET NOT NULL,
  [DataDate] DATETIMEOFFSET NULL,
  CONSTRAINT [PK_DefiLlama_PoolRewardToken] PRIMARY KEY ([PoolId], [SortOrder]),
  CONSTRAINT [FK_DefiLlama_PoolRewardToken_Pool] FOREIGN KEY ([PoolId]) REFERENCES [DefiLlama].[Pool] ([PoolId]) ON DELETE CASCADE
);

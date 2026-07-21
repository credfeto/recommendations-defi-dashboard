CREATE TABLE [Pendle].[MarketCategory]
(
  [Address] NVARCHAR(100) NOT NULL,
  [ChainId] INT NOT NULL,
  [CategoryId] NVARCHAR(100) NOT NULL,
  [DateCreated] DATETIMEOFFSET NOT NULL,
  [DateUpdated] DATETIMEOFFSET NOT NULL,
  [DataDate] DATETIMEOFFSET NULL,
  CONSTRAINT [PK_Pendle_MarketCategory] PRIMARY KEY ([Address], [ChainId], [CategoryId]),
  CONSTRAINT [FK_Pendle_MarketCategory_Market] FOREIGN KEY ([Address], [ChainId]) REFERENCES [Pendle].[Market] ([Address], [ChainId]) ON DELETE CASCADE
);

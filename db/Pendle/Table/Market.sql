CREATE TABLE [Pendle].[Market]
(
  [Address] NVARCHAR(100) NOT NULL,
  [ChainId] INT NOT NULL,
  [SimpleSymbol] NVARCHAR(200) NOT NULL,
  [Expiry] NVARCHAR(50) NULL,
  [IsActive] BIT NOT NULL,
  [LiquidityUsd] FLOAT(53) NULL,
  [AggregatedApy] FLOAT(53) NOT NULL,
  [UnderlyingApy] FLOAT(53) NOT NULL,
  [PendleApy] FLOAT(53) NOT NULL,
  [LpRewardApy] FLOAT(53) NOT NULL,
  [SwapFeeApy] FLOAT(53) NOT NULL,
  [TradingVolumeUsd] FLOAT(53) NULL,
  [DateCreated] DATETIMEOFFSET NOT NULL,
  [DateUpdated] DATETIMEOFFSET NOT NULL,
  [DataDate] DATETIMEOFFSET NULL,
  CONSTRAINT [PK_Pendle_Market] PRIMARY KEY ([Address], [ChainId])
);

CREATE TABLE [GoPlus].[TokenSecurity]
(
  [Chain] NVARCHAR(100) NOT NULL,
  [Address] NVARCHAR(100) NOT NULL,
  [ParentAddress] NVARCHAR(100) NULL,
  [IsOpenSource] BIT NULL,
  [IsHoneypot] BIT NULL,
  [IsProxy] BIT NULL,
  [BuyTax] FLOAT(53) NULL,
  [SellTax] FLOAT(53) NULL,
  [TransferTax] FLOAT(53) NULL,
  [CannotBuy] BIT NULL,
  [HoneypotWithSameCreator] BIT NULL,
  [TokenName] NVARCHAR(MAX) NULL,
  [TokenSymbol] NVARCHAR(MAX) NULL,
  [DateCreated] DATETIMEOFFSET NOT NULL,
  [DateUpdated] DATETIMEOFFSET NOT NULL,
  [DataDate] DATETIMEOFFSET NULL,
  CONSTRAINT [PK_GoPlus_TokenSecurity] PRIMARY KEY ([Chain], [Address])
);

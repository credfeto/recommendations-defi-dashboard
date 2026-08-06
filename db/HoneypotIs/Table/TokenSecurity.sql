CREATE TABLE [HoneypotIs].[TokenSecurity]
(
  [Chain] NVARCHAR(100) NOT NULL,
  [Address] NVARCHAR(100) NOT NULL,
  [IsHoneypot] BIT NULL,
  [BuyTax] FLOAT(53) NULL,
  [SellTax] FLOAT(53) NULL,
  [SimulationSuccess] BIT NULL,
  [DateCreated] DATETIMEOFFSET NOT NULL,
  [DateUpdated] DATETIMEOFFSET NOT NULL,
  CONSTRAINT [PK_HoneypotIs_TokenSecurity] PRIMARY KEY ([Chain], [Address])
);

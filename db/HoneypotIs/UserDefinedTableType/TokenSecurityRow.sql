CREATE TYPE [HoneypotIs].[TokenSecurityRow] AS TABLE
(
  [Chain] NVARCHAR(100) NOT NULL,
  [Address] NVARCHAR(100) NOT NULL,
  [IsHoneypot] BIT NULL,
  [BuyTax] FLOAT(53) NULL,
  [SellTax] FLOAT(53) NULL,
  [SimulationSuccess] BIT NULL
);

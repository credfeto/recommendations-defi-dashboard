CREATE TYPE [DefiLlama].[HackRow] AS TABLE
(
  [Name] NVARCHAR(200) NOT NULL,
  [HackDate] DATETIMEOFFSET NOT NULL,
  [Classification] NVARCHAR(100) NULL,
  [Technique] NVARCHAR(200) NULL,
  [Amount] DECIMAL(28, 8) NOT NULL,
  [Source] NVARCHAR(500) NOT NULL,
  [ParentProtocolId] NVARCHAR(200) NULL
);

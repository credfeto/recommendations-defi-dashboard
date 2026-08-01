CREATE TABLE [DefiLlama].[Hack]
(
  [Name] NVARCHAR(200) NOT NULL,
  [HackDate] DATETIMEOFFSET NOT NULL,
  [Classification] NVARCHAR(100) NULL,
  [Technique] NVARCHAR(200) NULL,
  [Amount] DECIMAL(28, 8) NOT NULL,
  [Source] NVARCHAR(500) NOT NULL,
  [ParentProtocolId] NVARCHAR(200) NULL,
  [DateCreated] DATETIMEOFFSET NOT NULL,
  [DateUpdated] DATETIMEOFFSET NOT NULL,
  [DataDate] DATETIMEOFFSET NULL,
  CONSTRAINT [PK_DefiLlama_Hack] PRIMARY KEY ([Name], [HackDate])
);

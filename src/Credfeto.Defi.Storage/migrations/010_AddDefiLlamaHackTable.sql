IF OBJECT_ID(N'[DefiLlama].[Hack]', N'U') IS NULL
  BEGIN
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
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'DefiLlama') AND [name] = N'HackRow'
  )
  BEGIN
    DROP TYPE [DefiLlama].[HackRow];
  END;
GO

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
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[Hack_Sync]
  @Hacks [DefiLlama].[HackRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [DefiLlama].[Hack] AS Tgt
  USING @Hacks AS Src ON Tgt.[Name] = Src.[Name] AND Tgt.[HackDate] = Src.[HackDate]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Classification] = Src.[Classification],
        [Technique] = Src.[Technique],
        [Amount] = Src.[Amount],
        [Source] = Src.[Source],
        [ParentProtocolId] = Src.[ParentProtocolId],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([Name], [HackDate], [Classification], [Technique], [Amount], [Source], [ParentProtocolId], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[Name], Src.[HackDate], Src.[Classification], Src.[Technique], Src.[Amount], Src.[Source], Src.[ParentProtocolId], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[Hack_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Name],
    [HackDate],
    [Classification],
    [Technique],
    [Amount],
    [Source],
    [ParentProtocolId],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[Hack];
END;
GO

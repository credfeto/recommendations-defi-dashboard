IF OBJECT_ID(N'[DefiLlama].[Protocol]', N'U') IS NULL
  BEGIN
    CREATE TABLE [DefiLlama].[Protocol]
    (
      [Slug] NVARCHAR(200) NOT NULL,
      [Audits] NVARCHAR(10) NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_DefiLlama_Protocol] PRIMARY KEY ([Slug])
    );
  END;
GO

IF OBJECT_ID(N'[DefiLlama].[ProtocolAuditLink]', N'U') IS NULL
  BEGIN
    CREATE TABLE [DefiLlama].[ProtocolAuditLink]
    (
      [Slug] NVARCHAR(200) NOT NULL,
      [AuditLink] NVARCHAR(500) NOT NULL,
      [DateCreated] DATETIMEOFFSET NOT NULL,
      [DateUpdated] DATETIMEOFFSET NOT NULL,
      [DataDate] DATETIMEOFFSET NULL,
      CONSTRAINT [PK_DefiLlama_ProtocolAuditLink] PRIMARY KEY ([Slug], [AuditLink]),
      CONSTRAINT [FK_DefiLlama_ProtocolAuditLink_Protocol] FOREIGN KEY ([Slug]) REFERENCES [DefiLlama].[Protocol] ([Slug]) ON DELETE CASCADE
    );
  END;
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'DefiLlama') AND [name] = N'ProtocolRow'
  )
  BEGIN
    DROP TYPE [DefiLlama].[ProtocolRow];
  END;
GO

CREATE TYPE [DefiLlama].[ProtocolRow] AS TABLE
(
  [Slug] NVARCHAR(200) NOT NULL,
  [Audits] NVARCHAR(10) NULL
);
GO

IF
  EXISTS (
    SELECT 1 FROM [sys].[types]
    WHERE [schema_id] = SCHEMA_ID(N'DefiLlama') AND [name] = N'ProtocolAuditLinkRow'
  )
  BEGIN
    DROP TYPE [DefiLlama].[ProtocolAuditLinkRow];
  END;
GO

CREATE TYPE [DefiLlama].[ProtocolAuditLinkRow] AS TABLE
(
  [Slug] NVARCHAR(200) NOT NULL,
  [AuditLink] NVARCHAR(500) NOT NULL
);
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[Protocol_Sync]
  @Protocols [DefiLlama].[ProtocolRow] READONLY,
  @AuditLinks [DefiLlama].[ProtocolAuditLinkRow] READONLY,
  @DataDate DATETIMEOFFSET NULL
AS
BEGIN
  SET NOCOUNT ON;

  MERGE [DefiLlama].[Protocol] AS Tgt
  USING @Protocols AS Src ON Tgt.[Slug] = Src.[Slug]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [Audits] = Src.[Audits],
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([Slug], [Audits], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[Slug], Src.[Audits], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;

  MERGE [DefiLlama].[ProtocolAuditLink] AS Tgt
  USING @AuditLinks AS Src ON Tgt.[Slug] = Src.[Slug] AND Tgt.[AuditLink] = Src.[AuditLink]
  WHEN MATCHED
    THEN
    UPDATE
      SET
        [DataDate] = @DataDate,
        [DateUpdated] = SYSDATETIMEOFFSET()
  WHEN NOT MATCHED BY TARGET
    THEN
    INSERT ([Slug], [AuditLink], [DataDate], [DateCreated], [DateUpdated])
    VALUES (Src.[Slug], Src.[AuditLink], @DataDate, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET())
  WHEN NOT MATCHED BY SOURCE
    THEN DELETE;
END;
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[Protocol_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Slug],
    [Audits],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[Protocol];
END;
GO

CREATE OR ALTER PROCEDURE [DefiLlama].[ProtocolAuditLink_GetAll]
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [Slug],
    [AuditLink],
    [DateCreated],
    [DateUpdated],
    [DataDate]
  FROM [DefiLlama].[ProtocolAuditLink]
  ORDER BY [Slug], [AuditLink];
END;
GO

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

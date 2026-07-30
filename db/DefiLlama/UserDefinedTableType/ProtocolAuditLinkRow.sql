CREATE TYPE [DefiLlama].[ProtocolAuditLinkRow] AS TABLE
(
  [Slug] NVARCHAR(200) NOT NULL,
  [AuditLink] NVARCHAR(500) NOT NULL
);

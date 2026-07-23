IF OBJECT_ID(N'[dbo].[ContractSecurity_Upsert]', N'P') IS NOT NULL
  BEGIN
    DROP PROCEDURE [dbo].[ContractSecurity_Upsert];
  END;
GO

IF OBJECT_ID(N'[dbo].[ContractSecurity_GetByChainAndAddress]', N'P') IS NOT NULL
  BEGIN
    DROP PROCEDURE [dbo].[ContractSecurity_GetByChainAndAddress];
  END;
GO

IF OBJECT_ID(N'[dbo].[ContractSecurity_GetChildrenByParentAddress]', N'P') IS NOT NULL
  BEGIN
    DROP PROCEDURE [dbo].[ContractSecurity_GetChildrenByParentAddress];
  END;
GO

IF OBJECT_ID(N'[dbo].[ContractSecurity]', N'U') IS NOT NULL
  BEGIN
    DROP TABLE [dbo].[ContractSecurity];
  END;
GO

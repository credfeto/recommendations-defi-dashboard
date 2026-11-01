using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Defi.Data.Models.Models;
using FunFair.Test.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Credfeto.Defi.Storage.Tests;

public sealed class ContractSecurityCacheServiceWebacyDatabaseTests : TestBase
{
    private readonly ContractSecurityCacheService _cache;
    private readonly ExecutingDatabase _database;

    public ContractSecurityCacheServiceWebacyDatabaseTests()
    {
        this._database = new ExecutingDatabase();
        this._cache = new ContractSecurityCacheService(
            database: this._database,
            timeProvider: new FakeTimeProvider(
                startDateTime: new DateTimeOffset(
                    year: 2024,
                    month: 6,
                    day: 1,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero
                )
            )
        );
    }

    [Fact]
    public async Task GetWebacyAsync_NoRow_QueriesStoredProcedureWithLowerCaseAddressAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        using DataTable emptyResult = CreateTokenRiskResultSchema();
        this._database.Connection.ResultSet = emptyResult;

        ContractSecurityInfo? result = await this._cache.GetWebacyAsync(
            chain: "Ethereum",
            address: "0xABCDEF0000000000000000000000000000000001",
            cancellationToken: cancellationToken
        );

        Assert.Null(result);

        FakeDbExecution execution = Assert.Single(this._database.Connection.Executions);
        Assert.Contains(
            expectedSubstring: "Webacy.TokenRisk_GetByChainAndAddress",
            actualString: execution.CommandText,
            comparisonType: StringComparison.Ordinal
        );
        Assert.Contains(
            execution.Parameters,
            filter: p => string.Equals(p.Value as string, "Ethereum", StringComparison.Ordinal)
        );
        Assert.Contains(
            execution.Parameters,
            filter: p =>
                string.Equals(p.Value as string, "0xabcdef0000000000000000000000000000000001", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task SetWebacyAsync_MapsRowToStructuredTableParameterAsync()
    {
        CancellationToken cancellationToken = this.CancellationToken();

        ContractSecurityInfo info = new()
        {
            Chain = "Ethereum",
            Address = "0xabc123def456abc123def456abc123def456abc1",
            Source = ContractSecuritySource.Webacy,
            RiskScore = 75.5,
            RiskLevel = "High",
            IssueCount = 4,
            HighRiskIssueCount = 2,
            MediumRiskIssueCount = 1,
        };

        await this._cache.SetWebacyAsync(info: info, cancellationToken: cancellationToken);

        FakeDbExecution execution = Assert.Single(this._database.Connection.Executions);
        Assert.Contains(
            expectedSubstring: "Webacy.TokenRisk_Sync",
            actualString: execution.CommandText,
            comparisonType: StringComparison.Ordinal
        );

        SqlParameter parameter = Assert.IsType<SqlParameter>(Assert.Single(execution.Parameters));
        Assert.Equal(expected: SqlDbType.Structured, actual: parameter.SqlDbType);

        DataTable table = Assert.IsType<DataTable>(parameter.Value);
        Assert.Equal(expected: "Webacy.TokenRiskRow", actual: table.TableName);

        DataRow row = Assert.Single(table.Rows.Cast<DataRow>());
        Assert.Equal(expected: "Ethereum", actual: row["Chain"]);
        Assert.Equal(expected: "0xabc123def456abc123def456abc123def456abc1", actual: row["Address"]);
        Assert.Equal(expected: 75.5, actual: row["RiskScore"]);
        Assert.Equal(expected: "High", actual: row["RiskLevel"]);
        Assert.Equal(expected: 4, actual: row["IssueCount"]);
        Assert.Equal(expected: 2, actual: row["HighRiskIssueCount"]);
        Assert.Equal(expected: 1, actual: row["MediumRiskIssueCount"]);
    }

    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007: Handle disposal correctly",
        Justification = "Columns are owned and disposed by the table, which the caller disposes"
    )]
    private static DataTable CreateTokenRiskResultSchema()
    {
        DataTable table = new();
        table.Columns.Add(columnName: "Chain", type: typeof(string));
        table.Columns.Add(columnName: "Address", type: typeof(string));
        table.Columns.Add(columnName: "RiskScore", type: typeof(double));
        table.Columns.Add(columnName: "RiskLevel", type: typeof(string));
        table.Columns.Add(columnName: "IssueCount", type: typeof(int));
        table.Columns.Add(columnName: "HighRiskIssueCount", type: typeof(int));
        table.Columns.Add(columnName: "MediumRiskIssueCount", type: typeof(int));
        table.Columns.Add(columnName: "DateCreated", type: typeof(DateTimeOffset));
        table.Columns.Add(columnName: "DateUpdated", type: typeof(DateTimeOffset));
        table.Columns.Add(columnName: "DataDate", type: typeof(DateTimeOffset));

        return table;
    }
}

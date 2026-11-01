using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Credfeto.Defi.Storage.Tests;

internal sealed class FakeDbConnection : DbConnection
{
    private readonly List<FakeDbExecution> _executions = [];

    public IReadOnlyList<FakeDbExecution> Executions => this._executions;

    public DataTable? ResultSet { get; set; }

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => "Fake";

    public override string DataSource => "Fake";

    public override string ServerVersion => "0.0";

    public override ConnectionState State => ConnectionState.Open;

    public void RecordExecution(string commandText, IReadOnlyList<DbParameter> parameters)
    {
        this._executions.Add(new(CommandText: commandText, Parameters: parameters));
    }

    public override void ChangeDatabase(string databaseName)
    {
        // Single fake database: nothing to change.
    }

    public override void Close()
    {
        // Always open: nothing to close.
    }

    public override void Open()
    {
        // Always open: nothing to open.
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        throw new System.NotSupportedException("Transactions are not supported by the fake connection");
    }

    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007: Handle disposal correctly",
        Justification = "Command is disposed by the caller"
    )]
    protected override DbCommand CreateDbCommand()
    {
        return new FakeDbCommand(this);
    }
}

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Credfeto.Defi.Storage.Tests;

internal sealed class FakeDbCommand : DbCommand
{
    private readonly SqlCommand _parameterSource;

    public FakeDbCommand(DbConnection connection)
    {
        this.DbConnection = connection;
        this._parameterSource = new SqlCommand();
    }

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; }

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }

    protected override DbParameterCollection DbParameterCollection => this._parameterSource.Parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
        // Nothing to cancel: the command never runs against a real server.
    }

    public override int ExecuteNonQuery()
    {
        _ = this.RecordExecution();

        return 0;
    }

    public override object? ExecuteScalar()
    {
        return null;
    }

    public override void Prepare()
    {
        // Nothing to prepare: the command never runs against a real server.
    }

    protected override DbParameter CreateDbParameter()
    {
        return new SqlParameter();
    }

    [SuppressMessage(
        category: "SmartAnalyzers.CSharpExtensions.Annotations",
        checkId: "CSE007: Handle disposal correctly",
        Justification = "Reader is disposed by the caller"
    )]
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        FakeDbConnection? fakeConnection = this.RecordExecution();

        if (fakeConnection?.ResultSet is not null)
        {
            return fakeConnection.ResultSet.CreateDataReader();
        }

        using DataTable empty = new();

        return empty.CreateDataReader();
    }

    private FakeDbConnection? RecordExecution()
    {
        if (this.DbConnection is FakeDbConnection fakeConnection)
        {
            fakeConnection.RecordExecution(
                commandText: this.CommandText,
                parameters: [.. this.Parameters.Cast<DbParameter>()]
            );

            return fakeConnection;
        }

        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._parameterSource.Dispose();
        }

        base.Dispose(disposing);
    }
}

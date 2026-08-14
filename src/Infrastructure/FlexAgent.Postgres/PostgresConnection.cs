using System.Data;
using Npgsql;

namespace FlexAgent.Postgres;

public sealed class PostgresDataSourceFactory
{
    public NpgsqlDataSource Create(string connectionString) =>
        NpgsqlDataSource.Create(connectionString);
}

public sealed class PostgresConnectionAccessor(NpgsqlDataSource dataSource)
{
    public NpgsqlDataSource DataSource { get; } = dataSource;

    public ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
        DataSource.OpenConnectionAsync(cancellationToken);
}

public sealed class PostgresTransactionScope : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    private PostgresTransactionScope(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public NpgsqlConnection Connection => _connection;

    public NpgsqlTransaction Transaction => _transaction;

    public static Task<PostgresTransactionScope> BeginAsync(
        PostgresConnectionAccessor accessor,
        CancellationToken cancellationToken = default) =>
        BeginAsync(accessor, IsolationLevel.Unspecified, cancellationToken);

    public static async Task<PostgresTransactionScope> BeginAsync(
        PostgresConnectionAccessor accessor,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        var connection = await accessor.OpenConnectionAsync(cancellationToken);
        var transaction = isolationLevel == IsolationLevel.Unspecified
            ? await connection.BeginTransactionAsync(cancellationToken)
            : await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new PostgresTransactionScope(connection, transaction);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default) =>
        await _transaction.CommitAsync(cancellationToken);

    public async Task RollbackAsync(CancellationToken cancellationToken = default) =>
        await _transaction.RollbackAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

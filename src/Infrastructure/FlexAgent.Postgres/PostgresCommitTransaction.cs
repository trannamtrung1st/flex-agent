using Npgsql;

namespace FlexAgent.Postgres;

public static class PostgresCommitTransaction
{
    public static NpgsqlTransaction? Optional(object? commitTransaction) =>
        commitTransaction switch
        {
            null => null,
            NpgsqlTransaction transaction => transaction,
            _ => throw new InvalidOperationException("commit.transaction.invalid"),
        };

    public static NpgsqlTransaction Required(object? commitTransaction) =>
        Optional(commitTransaction) ?? throw new InvalidOperationException("commit.transaction.required");
}

using System.Data;
using Dapper;

namespace FlexAgent.Postgres;

public static class PostgresUtcTime
{
    private static int _handlersRegistered;

    public static void EnsureDapperHandlers()
    {
        if (Interlocked.CompareExchange(ref _handlersRegistered, 1, 0) != 0)
        {
            return;
        }

        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new UtcDateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new NullableUtcDateTimeOffsetHandler());
    }

    public static DateTimeOffset ToUtcOffset(object? value) =>
        value switch
        {
            DateTimeOffset instant => instant.ToUniversalTime(),
            DateTime { Kind: DateTimeKind.Utc } utc => new DateTimeOffset(utc, TimeSpan.Zero),
            DateTime { Kind: DateTimeKind.Local } local => new DateTimeOffset(local).ToUniversalTime(),
            DateTime unspecified => new DateTimeOffset(DateTime.SpecifyKind(unspecified, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException("PostgreSQL did not return a UTC instant."),
        };
}

internal sealed class UtcDateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.Value = value.UtcDateTime;
        parameter.DbType = DbType.DateTime;
    }

    public override DateTimeOffset Parse(object value) => PostgresUtcTime.ToUtcOffset(value);
}

internal sealed class NullableUtcDateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset?>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
    {
        parameter.Value = value?.UtcDateTime ?? (object)DBNull.Value;
        if (value.HasValue)
        {
            parameter.DbType = DbType.DateTime;
        }
    }

    public override DateTimeOffset? Parse(object value) =>
        value is null or DBNull ? null : PostgresUtcTime.ToUtcOffset(value);
}

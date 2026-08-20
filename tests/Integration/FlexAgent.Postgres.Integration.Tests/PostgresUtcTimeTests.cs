using FlexAgent.Postgres;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class PostgresUtcTimeTests
{
    [Fact]
    public void ToUtcOffset_accepts_utc_datetime_and_datetimeoffset()
    {
        var utc = new DateTime(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);
        var offset = new DateTimeOffset(2026, 8, 20, 17, 0, 0, TimeSpan.FromHours(7));

        Assert.Equal(new DateTimeOffset(utc, TimeSpan.Zero), PostgresUtcTime.ToUtcOffset(utc));
        Assert.Equal(new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero), PostgresUtcTime.ToUtcOffset(offset));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ToUtcOffset_rejects_non_utc_datetime_kinds(DateTimeKind kind)
    {
        var value = DateTime.SpecifyKind(new DateTime(2026, 8, 20, 10, 0, 0), kind);

        var error = Assert.Throws<InvalidOperationException>(() => PostgresUtcTime.ToUtcOffset(value));
        Assert.Contains("UTC", error.Message, StringComparison.Ordinal);
    }
}

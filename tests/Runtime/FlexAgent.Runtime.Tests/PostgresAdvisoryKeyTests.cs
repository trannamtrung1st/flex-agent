using FlexAgent.IdentityAccess.Infrastructure;

namespace FlexAgent.Runtime.Tests;

public sealed class PostgresAdvisoryKeyTests
{
    [Fact]
    public void Advisory_keys_are_two_32_bit_integers()
    {
        var key = PostgresAdvisoryKeys.Create("provider", "sid-digest");

        Assert.IsType<int>(key.K1);
        Assert.IsType<int>(key.K2);
        Assert.NotEqual(0, key.K1 | key.K2);
        Assert.Equal(key, PostgresAdvisoryKeys.Create("provider", "sid-digest"));
        Assert.NotEqual(key, PostgresAdvisoryKeys.Create("identity", "sid-digest"));
    }
}

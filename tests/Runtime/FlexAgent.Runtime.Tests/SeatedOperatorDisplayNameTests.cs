using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.Runtime.Tests;

public sealed class SeatedOperatorDisplayNameTests
{
    [Fact]
    public void Given_and_family_name_compose_the_seated_label()
    {
        Assert.Equal("Demo Participant", SeatedOperatorDisplayName.Compose("Demo", "Participant", "demo.participant"));
    }

    [Fact]
    public void Username_is_used_when_given_and_family_are_absent()
    {
        Assert.Equal("demo.participant", SeatedOperatorDisplayName.Compose(null, "  ", "demo.participant"));
    }

    [Fact]
    public void Single_name_part_is_used_without_inventing_the_other()
    {
        Assert.Equal("Demo", SeatedOperatorDisplayName.Compose("Demo", null, "demo.participant"));
        Assert.Equal("Participant", SeatedOperatorDisplayName.Compose(" ", "Participant", "demo.participant"));
    }

    [Fact]
    public void Missing_claims_yield_no_display_name()
    {
        Assert.Null(SeatedOperatorDisplayName.Compose(null, null, null));
        Assert.Null(SeatedOperatorDisplayName.Compose("  ", "\t", ""));
    }

    [Fact]
    public void Overlong_values_are_truncated()
    {
        var given = new string('A', 80);
        var family = new string('B', 80);
        var composed = SeatedOperatorDisplayName.Compose(given, family, "user");
        Assert.NotNull(composed);
        Assert.Equal(SeatedOperatorDisplayName.MaxLength, composed.Length);
    }
}

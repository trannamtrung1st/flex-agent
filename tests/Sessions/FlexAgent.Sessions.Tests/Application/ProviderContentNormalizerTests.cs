using FlexAgent.Sessions.Application;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class ProviderContentNormalizerTests
{
    [Fact]
    public void Text_delta_is_published_as_the_exact_provider_suffix()
    {
        var result = ProviderContentNormalizer.Normalize(
            new ModelContentTextDelta("Hel"),
            assembledPrefix: string.Empty);

        var delta = Assert.IsType<NormalizedContentDelta>(result);
        Assert.Equal("Hel", delta.ExactUtf8Text);
    }

    [Fact]
    public void Cumulative_snapshot_emits_only_the_new_suffix()
    {
        var first = ProviderContentNormalizer.Normalize(
            new ModelContentCumulativeSnapshot("Hel"),
            assembledPrefix: string.Empty);
        Assert.Equal("Hel", Assert.IsType<NormalizedContentDelta>(first).ExactUtf8Text);

        var second = ProviderContentNormalizer.Normalize(
            new ModelContentCumulativeSnapshot("Hello"),
            assembledPrefix: "Hel");
        Assert.Equal("lo", Assert.IsType<NormalizedContentDelta>(second).ExactUtf8Text);
    }

    [Fact]
    public void Unchanged_cumulative_snapshot_is_skipped()
    {
        var result = ProviderContentNormalizer.Normalize(
            new ModelContentCumulativeSnapshot("Hel"),
            assembledPrefix: "Hel");

        Assert.IsType<NormalizedContentSkipped>(result);
    }

    [Fact]
    public void Cumulative_prefix_divergence_fails_closed()
    {
        var result = ProviderContentNormalizer.Normalize(
            new ModelContentCumulativeSnapshot("Hey"),
            assembledPrefix: "Hel");

        var failed = Assert.IsType<NormalizedContentFailed>(result);
        Assert.Equal(ContentStreamFailureReasons.PrefixDivergence, failed.ReasonCategory);
        Assert.DoesNotContain("Hey", failed.ReasonCategory, StringComparison.Ordinal);
    }

    [Fact]
    public void Metadata_only_events_do_not_become_fragments()
    {
        var result = ProviderContentNormalizer.Normalize(
            new ModelContentMetadata(),
            assembledPrefix: "Hel");

        Assert.IsType<NormalizedContentSkipped>(result);
    }

    [Fact]
    public void Completed_event_seals_the_stream()
    {
        Assert.IsType<NormalizedContentCompleted>(
            ProviderContentNormalizer.Normalize(new ModelContentCompleted(), "Hello"));
    }

    [Fact]
    public void Empty_delta_is_skipped_rather_than_published()
    {
        Assert.IsType<NormalizedContentSkipped>(
            ProviderContentNormalizer.Normalize(new ModelContentTextDelta(string.Empty), string.Empty));
    }
}

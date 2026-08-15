namespace FlexAgent.Sessions.Application;

public static class ContentStreamFailureReasons
{
    public const string PrefixDivergence = "content_stream.prefix_divergence";
}

public abstract record NormalizedContentResult;

public sealed record NormalizedContentDelta(string ExactUtf8Text) : NormalizedContentResult;

public sealed record NormalizedContentSkipped : NormalizedContentResult;

public sealed record NormalizedContentCompleted : NormalizedContentResult;

public sealed record NormalizedContentFailed(string ReasonCategory) : NormalizedContentResult;

public static class ProviderContentNormalizer
{
    public static NormalizedContentResult Normalize(ModelContentEvent contentEvent, string assembledPrefix)
    {
        ArgumentNullException.ThrowIfNull(contentEvent);
        assembledPrefix ??= string.Empty;

        return contentEvent switch
        {
            ModelContentMetadata => new NormalizedContentSkipped(),
            ModelContentCompleted => new NormalizedContentCompleted(),
            ModelContentTextDelta delta when string.IsNullOrEmpty(delta.ExactUtf8Text) =>
                new NormalizedContentSkipped(),
            ModelContentTextDelta delta => new NormalizedContentDelta(delta.ExactUtf8Text),
            ModelContentCumulativeSnapshot snapshot => NormalizeSnapshot(snapshot.ExactUtf8Text, assembledPrefix),
            _ => new NormalizedContentFailed(ContentStreamFailureReasons.PrefixDivergence),
        };
    }

    private static NormalizedContentResult NormalizeSnapshot(string snapshot, string assembledPrefix)
    {
        snapshot ??= string.Empty;
        if (string.Equals(snapshot, assembledPrefix, StringComparison.Ordinal))
        {
            return new NormalizedContentSkipped();
        }

        if (!snapshot.StartsWith(assembledPrefix, StringComparison.Ordinal))
        {
            return new NormalizedContentFailed(ContentStreamFailureReasons.PrefixDivergence);
        }

        var suffix = snapshot[assembledPrefix.Length..];
        return string.IsNullOrEmpty(suffix)
            ? new NormalizedContentSkipped()
            : new NormalizedContentDelta(suffix);
    }
}

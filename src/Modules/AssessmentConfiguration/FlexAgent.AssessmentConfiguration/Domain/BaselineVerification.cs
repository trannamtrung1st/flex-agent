namespace FlexAgent.AssessmentConfiguration.Domain;

public sealed record BaselineDigestCheck(string StoredDigest, string? RecomputedDigest);

public static class BaselineVerification
{
    public const string Verified = "verified";
    public const string Degraded = "degraded";

    public static IReadOnlyList<ExactSourceRef> References(ActivityDraft draft)
    {
        var references = new List<ExactSourceRef>
        {
            draft.Content.OrganizationPolicy,
            draft.Content.Agent,
            draft.Content.Harness,
            draft.Content.Workflow,
            draft.Content.AdaptiveFollowUp,
            draft.Content.Rubric,
            draft.Content.ModelDeployment,
            draft.Content.CapabilityProfile,
            draft.Content.ReviewRelease,
            draft.Content.Task.RequirementSource,
        };
        references.AddRange(draft.Content.Knowledge);
        if (draft.Content.Memory.Snapshot is { } snapshot)
        {
            references.Add(snapshot);
        }

        return references;
    }

    public static string? Status(
        ActivityDraft draft,
        IReadOnlyList<TrustedSourceDescriptor> sources,
        BaselineDigestCheck? digest = null)
    {
        if (!draft.HasActivatedCohort)
        {
            return null;
        }

        foreach (var reference in References(draft))
        {
            var match = sources.FirstOrDefault(source => source.Matches(reference));
            if (match is null
                || match.LifecycleState is SourceLifecycleStates.Revoked or SourceLifecycleStates.Unavailable)
            {
                return Degraded;
            }
        }

        if (digest is not null
            && (string.IsNullOrWhiteSpace(digest.RecomputedDigest)
                || !string.Equals(digest.StoredDigest, digest.RecomputedDigest, StringComparison.Ordinal)))
        {
            return Degraded;
        }

        return Verified;
    }
}

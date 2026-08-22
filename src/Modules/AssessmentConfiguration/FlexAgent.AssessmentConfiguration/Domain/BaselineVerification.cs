namespace FlexAgent.AssessmentConfiguration.Domain;

public sealed record BaselineDigestCheck(
    string StoredDigest,
    string? RecomputedDigest,
    bool BindingPresent,
    Guid? BoundRevisionId,
    Guid? DraftRevisionId)
{
    public static BaselineDigestCheck Missing() =>
        new(string.Empty, null, false, null, null);

    public static BaselineDigestCheck Present(
        string storedDigest,
        string? recomputedDigest,
        Guid boundRevisionId,
        Guid draftRevisionId) =>
        new(storedDigest, recomputedDigest, true, boundRevisionId, draftRevisionId);
}

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

        if (digest is null
            || !digest.BindingPresent
            || digest.BoundRevisionId is null
            || digest.DraftRevisionId is null
            || digest.BoundRevisionId != digest.DraftRevisionId
            || string.IsNullOrWhiteSpace(digest.StoredDigest)
            || string.IsNullOrWhiteSpace(digest.RecomputedDigest)
            || !string.Equals(digest.StoredDigest, digest.RecomputedDigest, StringComparison.Ordinal))
        {
            return Degraded;
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

        return Verified;
    }
}

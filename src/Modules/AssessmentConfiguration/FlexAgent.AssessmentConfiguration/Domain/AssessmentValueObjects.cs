namespace FlexAgent.AssessmentConfiguration.Domain;

public sealed record AssessmentDecision<T>(
    bool Succeeded,
    string OutcomeCode,
    T? Value,
    string? Field = null)
{
    public static AssessmentDecision<T> Ok(T value, string outcomeCode = "assessment.ok") =>
        new(true, outcomeCode, value);

    public static AssessmentDecision<T> Fail(string outcomeCode, string? field = null) =>
        new(false, outcomeCode, default, field);
}

public sealed record ExactSourceRef(Guid SourceId, Guid VersionId, string ContentDigest)
{
    public static ExactSourceRef? TryCreate(Guid? sourceId, Guid? versionId, string? contentDigest)
    {
        if (sourceId is null || sourceId == Guid.Empty
            || versionId is null || versionId == Guid.Empty
            || string.IsNullOrWhiteSpace(contentDigest)
            || contentDigest.Length != 64
            || contentDigest != contentDigest.ToLowerInvariant())
        {
            return null;
        }

        return new ExactSourceRef(sourceId.Value, versionId.Value, contentDigest);
    }
}

public sealed record TaskBinding(
    Guid TaskId,
    string Title,
    string SubmissionRequirementSummary,
    ExactSourceRef RequirementSource);

public sealed record MemoryChoice(
    string Mode,
    ExactSourceRef? Snapshot)
{
    public static MemoryChoice Disabled { get; } = new(MemoryReadModes.Disabled, null);
}

public sealed record TimingRules(
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    DateTimeOffset DeadlineUtc,
    string TimeZoneId,
    int AttemptLimit,
    int? PerAttemptDurationSeconds)
{
    public bool IsValid(out string failureCode)
    {
        if (string.IsNullOrWhiteSpace(TimeZoneId) || !IsKnownTimeZone(TimeZoneId))
        {
            failureCode = AssessmentFailureCodes.InvalidTiming;
            return false;
        }

        if (StartsAtUtc.Offset != TimeSpan.Zero
            || EndsAtUtc.Offset != TimeSpan.Zero
            || DeadlineUtc.Offset != TimeSpan.Zero)
        {
            failureCode = AssessmentFailureCodes.InvalidTiming;
            return false;
        }

        if (EndsAtUtc <= StartsAtUtc || DeadlineUtc < StartsAtUtc || DeadlineUtc > EndsAtUtc)
        {
            failureCode = AssessmentFailureCodes.InvalidTiming;
            return false;
        }

        if (AttemptLimit is < 1 or > 10)
        {
            failureCode = AssessmentFailureCodes.InvalidField;
            return false;
        }

        if (PerAttemptDurationSeconds is <= 0)
        {
            failureCode = AssessmentFailureCodes.InvalidField;
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    private static bool IsKnownTimeZone(string timeZoneId)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}

public sealed record CapabilityBounds(
    bool TextEnabled,
    bool VoiceEnabled,
    bool ToolsEnabled,
    bool DynamicMemoryWritesEnabled,
    bool SharedSessionEnabled,
    bool DirectDeploymentEnabled,
    IReadOnlyList<string> PermittedTools)
{
    public static CapabilityBounds P0Assessment { get; } = new(
        TextEnabled: true,
        VoiceEnabled: false,
        ToolsEnabled: false,
        DynamicMemoryWritesEnabled: false,
        SharedSessionEnabled: false,
        DirectDeploymentEnabled: false,
        PermittedTools: []);

    public static CapabilityBounds MostRestrictive(CapabilityBounds upper, CapabilityBounds lower)
    {
        var tools = upper.PermittedTools
            .Intersect(lower.PermittedTools, StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        return new CapabilityBounds(
            TextEnabled: upper.TextEnabled && lower.TextEnabled,
            VoiceEnabled: upper.VoiceEnabled && lower.VoiceEnabled,
            ToolsEnabled: upper.ToolsEnabled && lower.ToolsEnabled,
            DynamicMemoryWritesEnabled: upper.DynamicMemoryWritesEnabled && lower.DynamicMemoryWritesEnabled,
            SharedSessionEnabled: upper.SharedSessionEnabled && lower.SharedSessionEnabled,
            DirectDeploymentEnabled: upper.DirectDeploymentEnabled && lower.DirectDeploymentEnabled,
            PermittedTools: tools);
    }

    public bool Widens(CapabilityBounds upper)
    {
        return (TextEnabled && !upper.TextEnabled)
            || (VoiceEnabled && !upper.VoiceEnabled)
            || (ToolsEnabled && !upper.ToolsEnabled)
            || (DynamicMemoryWritesEnabled && !upper.DynamicMemoryWritesEnabled)
            || (SharedSessionEnabled && !upper.SharedSessionEnabled)
            || (DirectDeploymentEnabled && !upper.DirectDeploymentEnabled)
            || PermittedTools.Any(tool => !upper.PermittedTools.Contains(tool, StringComparer.Ordinal));
    }

    public bool ViolatesP0Profile()
    {
        return !TextEnabled
            || VoiceEnabled
            || ToolsEnabled
            || DynamicMemoryWritesEnabled
            || SharedSessionEnabled
            || DirectDeploymentEnabled
            || PermittedTools.Count > 0;
    }
}

public sealed record TrustedSourceDescriptor(
    Guid OrganizationId,
    Guid SourceId,
    Guid VersionId,
    string SourceKind,
    string Category,
    string ContentDigest,
    string LifecycleState,
    string CompatibilityKey,
    CapabilityBounds Capabilities,
    IReadOnlyDictionary<string, string> EffectiveValues,
    bool TransactionallyRevalidatable,
    bool ProductionEligible)
{
    public bool Matches(ExactSourceRef reference) =>
        SourceId == reference.SourceId
        && VersionId == reference.VersionId
        && string.Equals(ContentDigest, reference.ContentDigest, StringComparison.Ordinal);
}

public sealed record AssessmentDraftContent(
    string Title,
    ExactSourceRef OrganizationPolicy,
    ExactSourceRef Agent,
    ExactSourceRef Harness,
    TaskBinding Task,
    ExactSourceRef Workflow,
    ExactSourceRef AdaptiveFollowUp,
    ExactSourceRef Rubric,
    ExactSourceRef ModelDeployment,
    IReadOnlyList<ExactSourceRef> Knowledge,
    ExactSourceRef CapabilityProfile,
    ExactSourceRef ReviewRelease,
    MemoryChoice Memory,
    TimingRules Timing,
    CapabilityBounds RequestedCapabilities,
    ExactSourceRef? ApprovedException);

using System.Text;
using System.Text.Json;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed record AttemptReadinessProjection(
    Guid EnrollmentId,
    string ReadinessState,
    int NextOrdinal,
    int RemainingEntitlement,
    string EntitlementSource,
    int BaselineAttemptLimit,
    Guid? ActiveAttemptId,
    Guid? ActiveSessionId,
    string StartCommandDigest,
    IReadOnlyList<AcceptedVersionSummary> BoundVersionCandidates,
    IReadOnlyList<AttemptHistoryItem> History,
    IReadOnlyList<RequiredNoticeProjection> RequiredNotices,
    IReadOnlyList<string> PermittedActions);

public sealed record AttemptHistoryItem(
    Guid AttemptId,
    int Ordinal,
    string Status,
    bool Consumed,
    Guid? SessionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? TerminalAtUtc,
    string? TerminalReasonCategory);

public sealed record RequiredNoticeProjection(
    Guid NoticeId,
    string NoticeType,
    string RequiredOutcome,
    string ProtectedContentRef,
    Guid SourceVersionId,
    string ContentDigest,
    Guid SourceId,
    string? CurrentOutcome = null);

public sealed record AcknowledgeAttemptNoticeCommand(
    EnrollmentActorContext Actor,
    Guid EnrollmentId,
    Guid NoticeId,
    Guid SourceVersionId,
    string Outcome,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record AcknowledgmentMutationOutcome(
    bool Succeeded,
    string OutcomeCode,
    Guid? RecordId,
    string? Outcome);

public sealed record StartAttemptCommand(
    EnrollmentActorContext Actor,
    Guid EnrollmentId,
    string IdempotencyKey,
    string TrustedCommandDigest);

public sealed record StartAttemptOutcome(
    bool Succeeded,
    string OutcomeCode,
    string? ReadinessState,
    Guid? AttemptId,
    int? Ordinal,
    Guid? SessionId,
    int RemainingEntitlement,
    IReadOnlyList<string> PermittedActions);

public interface IAttemptReadinessQuery
{
    Task<QueryResult<AttemptReadinessProjection>> GetAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken = default);
}

public interface IAttemptAcknowledgmentCoordinator
{
    Task<AcknowledgmentMutationOutcome> RecordAsync(
        AcknowledgeAttemptNoticeCommand command,
        CancellationToken cancellationToken = default);
}

public interface IAttemptStartCoordinator
{
    Task<StartAttemptOutcome> StartAsync(
        StartAttemptCommand command,
        CancellationToken cancellationToken = default);

    Task<StartAttemptOutcome> ReconcileAsync(
        StartAttemptCommand command,
        CancellationToken cancellationToken = default);
}

public interface IAttemptStore
{
    Task<IReadOnlyList<Attempt>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);

    Task InsertAsync(
        Attempt attempt,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<Attempt?> FindByIdAsync(
        Guid organizationId,
        Guid attemptId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task UpdateTerminalAsync(
        Attempt attempt,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IStartOperationStore
{
    Task AcquireLockAsync(
        Guid organizationId,
        Guid enrollmentId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<StartOperation?> FindAsync(
        Guid organizationId,
        Guid enrollmentId,
        string idempotencyKey,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StartOperation>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        StartOperation operation,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default);
}

public interface IRetryEntitlementReader
{
    Task<IReadOnlyList<RetryEntitlementFact>> ListUnusedAsync(
        Guid organizationId,
        Guid enrollmentId,
        DateTimeOffset nowUtc,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);
}

public interface IParticipantNoticePort
{
    Task<IReadOnlyList<RequiredNoticeProjection>?> ListRequiredAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid baselineId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken = default);
}

public interface IAcknowledgmentLifecyclePort
{
    Task<AcknowledgmentMutationOutcome> RecordAsync(
        AcknowledgeAttemptNoticeCommand command,
        RequiredNoticeProjection notice,
        object commitTransaction,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CurrentAcknowledgmentFact>> ListCurrentAsync(
        Guid organizationId,
        Guid enrollmentId,
        Guid participantActorId,
        IReadOnlyList<RequiredNoticeProjection> notices,
        object? commitTransaction,
        CancellationToken cancellationToken = default);

    Task<string?> BindToAttemptAsync(
        IReadOnlyList<CurrentAcknowledgmentFact> records,
        Guid attemptId,
        Guid enrollmentId,
        Guid participantActorId,
        object commitTransaction,
        CancellationToken cancellationToken = default);
}

public sealed record CurrentAcknowledgmentFact(
    Guid RecordId,
    Guid EnrollmentId,
    Guid ParticipantActorId,
    Guid NoticeId,
    Guid SourceVersionId,
    string ContentDigest,
    string Outcome,
    DateTimeOffset RecordedAtUtc,
    Guid? BoundAttemptId);

public sealed record SessionStartCommitRequest(
    Guid AttemptId,
    Guid SessionId,
    Guid ConfigurationId,
    Guid ManifestId,
    SubmissionParentScope Scope,
    IReadOnlyList<AttemptSubmissionBinding> SubmissionBindings,
    DateTimeOffset StartedAtUtc,
    string FrozenTimingDocument = "");

public static class AttemptFrozenTimingOutcomeCodes
{
    public const string Captured = "attempt_start.frozen_timing_captured";
    public const string Unavailable = "attempt_start.frozen_timing_unavailable";
}

public sealed record FrozenAttemptTimingCaptureResult(
    bool Succeeded,
    string? Document,
    string OutcomeCode)
{
    public static FrozenAttemptTimingCaptureResult Failed(string outcomeCode = AttemptFrozenTimingOutcomeCodes.Unavailable) =>
        new(false, null, outcomeCode);

    public static FrozenAttemptTimingCaptureResult FromDocument(string? documentJson) =>
        FrozenAttemptTimingDocuments.TryValidateAuthoritative(documentJson, out var normalized)
            ? new(true, normalized, AttemptFrozenTimingOutcomeCodes.Captured)
            : Failed();
}

public interface IFrozenAttemptTimingCapture
{
    Task<FrozenAttemptTimingCaptureResult> CaptureAsync(
        EffectiveTiming effectiveTiming,
        ActivatedCohortBinding binding,
        object commitTransaction,
        CancellationToken cancellationToken = default);
}

public static class FrozenAttemptTimingDocuments
{
    public static bool TryValidateAuthoritative(string? documentJson, out string normalizedDocument)
    {
        normalizedDocument = string.Empty;
        if (string.IsNullOrWhiteSpace(documentJson))
        {
            return false;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(documentJson);
            var root = document.RootElement;
            if (!TryGetProperty(root, "reconstruction", out var reconstructionElement)
                || reconstructionElement.GetString() is not { } reconstruction)
            {
                return false;
            }

            if (!TryParseHardEnd(root, out var hardEndAtUtc, out var hardEndValid))
            {
                return false;
            }

            if (!hardEndValid)
            {
                return false;
            }

            return reconstruction switch
            {
                "unbounded" => AcceptUnbounded(root, hardEndAtUtc, out normalizedDocument),
                "timed" => AcceptTimed(root, hardEndAtUtc, out normalizedDocument),
                _ => false,
            };
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }

    private static bool AcceptUnbounded(
        System.Text.Json.JsonElement root,
        DateTimeOffset? hardEndAtUtc,
        out string normalizedDocument)
    {
        normalizedDocument = string.Empty;
        if (TryGetProperty(root, "budget_seconds", out var budgetElement)
            && budgetElement.ValueKind != JsonValueKind.Null)
        {
            return false;
        }

        normalizedDocument = ComposeAuthoritativeDocument("unbounded", null, [], hardEndAtUtc);
        return true;
    }

    private static bool AcceptTimed(
        System.Text.Json.JsonElement root,
        DateTimeOffset? hardEndAtUtc,
        out string normalizedDocument)
    {
        normalizedDocument = string.Empty;
        if (!TryGetProperty(root, "budget_seconds", out var budgetElement)
            || budgetElement.ValueKind != JsonValueKind.Number
            || !budgetElement.TryGetInt32(out var budgetSeconds)
            || budgetSeconds <= 0)
        {
            return false;
        }

        if (!TryReadWarnings(root, out var approaching, out var imminent))
        {
            return false;
        }

        if (approaching <= 0
            || imminent <= 0
            || approaching >= budgetSeconds
            || imminent >= budgetSeconds
            || approaching == imminent)
        {
            return false;
        }

        normalizedDocument = ComposeAuthoritativeDocument(
            "timed",
            budgetSeconds,
            [("approaching", approaching), ("imminent", imminent)],
            hardEndAtUtc);
        return true;
    }

    private static bool TryReadWarnings(
        System.Text.Json.JsonElement root,
        out int approaching,
        out int imminent)
    {
        approaching = 0;
        imminent = 0;
        if (!TryGetProperty(root, "warnings", out var warningsElement)
            || warningsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in warningsElement.EnumerateArray())
        {
            if (!TryGetProperty(item, "code", out var codeElement)
                || !TryGetProperty(item, "remaining_seconds", out var secondsElement)
                || codeElement.GetString() is not { Length: > 0 } code
                || !secondsElement.TryGetInt32(out var remainingSeconds))
            {
                continue;
            }

            if (string.Equals(code, "approaching", StringComparison.Ordinal))
            {
                approaching = remainingSeconds;
            }
            else if (string.Equals(code, "imminent", StringComparison.Ordinal))
            {
                imminent = remainingSeconds;
            }
        }

        return approaching > 0 && imminent > 0;
    }

    private static bool TryParseHardEnd(
        System.Text.Json.JsonElement root,
        out DateTimeOffset? hardEndAtUtc,
        out bool valid)
    {
        hardEndAtUtc = null;
        valid = true;
        if (!TryGetProperty(root, "hard_end_at_utc", out var hardEndElement))
        {
            return true;
        }

        if (hardEndElement.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (hardEndElement.ValueKind != JsonValueKind.String
            || !DateTimeOffset.TryParse(
                hardEndElement.GetString(),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            valid = false;
            return true;
        }

        hardEndAtUtc = parsed.ToUniversalTime();
        return true;
    }

    public static string ComposeAuthoritativeDocument(
        string reconstruction,
        int? budgetSeconds,
        IReadOnlyList<(string Code, int RemainingSeconds)> warnings,
        DateTimeOffset? hardEndAtUtc) =>
        BuildDocument(
            reconstruction,
            budgetSeconds,
            warnings.Select(item => new WarningEntry(item.Code, item.RemainingSeconds)).ToArray(),
            hardEndAtUtc);

    private static string BuildDocument(
        string reconstruction,
        int? budgetSeconds,
        IReadOnlyList<WarningEntry> warnings,
        DateTimeOffset? hardEndAtUtc)
    {
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("reconstruction", reconstruction);
            if (budgetSeconds is int budget)
            {
                writer.WriteNumber("budget_seconds", budget);
            }
            else
            {
                writer.WriteNull("budget_seconds");
            }

            writer.WriteStartArray("warnings");
            foreach (var warning in warnings)
            {
                writer.WriteStartObject();
                writer.WriteString("code", warning.Code);
                writer.WriteNumber("remaining_seconds", warning.RemainingSeconds);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            if (hardEndAtUtc is DateTimeOffset hardEnd)
            {
                writer.WriteString("hard_end_at_utc", hardEnd.ToString("O"));
            }
            else
            {
                writer.WriteNull("hard_end_at_utc");
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static bool TryGetProperty(System.Text.Json.JsonElement element, string name, out System.Text.Json.JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private readonly record struct WarningEntry(string Code, int RemainingSeconds);
}

public sealed class UnavailableFrozenAttemptTimingCapture : IFrozenAttemptTimingCapture
{
    public static UnavailableFrozenAttemptTimingCapture Instance { get; } = new();

    public Task<FrozenAttemptTimingCaptureResult> CaptureAsync(
        EffectiveTiming effectiveTiming,
        ActivatedCohortBinding binding,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        _ = (effectiveTiming, binding, commitTransaction, cancellationToken);
        return Task.FromResult(FrozenAttemptTimingCaptureResult.Failed());
    }
}

public sealed class DevelopmentSyntheticFrozenAttemptTimingCapture : IFrozenAttemptTimingCapture
{
    public static DevelopmentSyntheticFrozenAttemptTimingCapture Instance { get; } = new();

    public Task<FrozenAttemptTimingCaptureResult> CaptureAsync(
        EffectiveTiming effectiveTiming,
        ActivatedCohortBinding binding,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        _ = (binding, commitTransaction, cancellationToken);
        ArgumentNullException.ThrowIfNull(effectiveTiming);
        var preset = AssessmentDevelopmentTimingPresets.SyntheticTimedV1Rules();
        var hardEnd = effectiveTiming.EffectiveAttemptStartExclusiveEndUtc
            <= effectiveTiming.EffectiveSubmissionExclusiveEndUtc
            ? effectiveTiming.EffectiveAttemptStartExclusiveEndUtc
            : effectiveTiming.EffectiveSubmissionExclusiveEndUtc;
        if (effectiveTiming.EffectivePerAttemptDurationSeconds is not > 0)
        {
            return Task.FromResult(FrozenAttemptTimingCaptureResult.FromDocument(
                FrozenAttemptTimingDocuments.ComposeAuthoritativeDocument(
                    "unbounded",
                    null,
                    [],
                    hardEnd)));
        }

        return Task.FromResult(FrozenAttemptTimingCaptureResult.FromDocument(
            FrozenAttemptTimingDocuments.ComposeAuthoritativeDocument(
                "timed",
                effectiveTiming.EffectivePerAttemptDurationSeconds,
                [
                    ("approaching", preset.WarningApproachingRemainingSeconds!.Value),
                    ("imminent", preset.WarningImminentRemainingSeconds!.Value),
                ],
                hardEnd)));
    }
}

public sealed record SessionStartCommitResult(
    bool Succeeded,
    string OutcomeCode,
    string? ConfigurationDigest,
    string? ManifestDigest);

public interface ISessionStartCommitPort
{
    bool CanCommit { get; }

    Task<SessionStartCommitResult> CommitActiveAsync(
        SessionStartCommitRequest request,
        object commitTransaction,
        CancellationToken cancellationToken = default);
}

public interface IAttemptTerminalMappingPort
{
    Task MapTerminalAsync(
        Guid organizationId,
        Guid attemptId,
        string terminalStatus,
        string reasonCategory,
        DateTimeOffset terminalAtUtc,
        object commitTransaction,
        CancellationToken cancellationToken = default);
}

namespace FlexAgent.Submissions.Domain;

public sealed record Accommodation(
    Guid AccommodationId,
    AccommodationParentBinding Parent,
    string Dimension,
    string NormalizedValue,
    AccommodationPolicyIdentity FrozenPolicy,
    AccommodationPolicyIdentity DecisionPolicy,
    string ReasonCategory,
    string Status,
    long Revision,
    Guid RequesterActorId,
    Guid? ApproverActorId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DecidedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? SupersededByAccommodationId,
    bool FairnessException,
    Guid LifecyclePolicyId,
    int LifecyclePolicyVersion)
{
    public static EnrollmentDecision<Accommodation> Request(
        AccommodationParentBinding parent,
        string dimension,
        string requestedValue,
        AccommodationPolicyIdentity frozenPolicy,
        NormalizedAccommodationPolicy currentPolicy,
        string reasonCategory,
        DateTimeOffset nowUtc,
        DateTimeOffset? expiresAtUtc,
        Guid requesterActorId,
        long revision,
        bool fairnessException = false)
    {
        if (!AccommodationDimensions.All.Contains(dimension)
            || !currentPolicy.Dimensions.TryGetValue(dimension, out var bounds)
            || !bounds.Enabled)
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.UnsupportedDimension);
        }

        if (!currentPolicy.EnvironmentEligible || !ReasonAllowed(reasonCategory, currentPolicy))
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.InvalidReason);
        }

        if (!TryNormalize(requestedValue, bounds.ValueKind, out var normalized))
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.InvalidValue);
        }

        if (currentPolicy.RequiresExpiry && expiresAtUtc is null)
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.InvalidValue);
        }

        if (expiresAtUtc is { } expiry && expiry.ToUniversalTime() <= nowUtc.ToUniversalTime())
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.InvalidValue);
        }

        var inRoutine = Inside(normalized, bounds.RoutineMin, bounds.RoutineMax, bounds.ValueKind);
        var inHard = Inside(normalized, bounds.HardMin, bounds.HardMax, bounds.ValueKind);
        if (!inHard)
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.OutsideBounds);
        }

        if (!inRoutine)
        {
            if (!fairnessException || currentPolicy.FairnessExceptionRuleId is null)
            {
                return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.OutsideBounds);
            }

            return EnrollmentDecision<Accommodation>.Ok(
                Create(
                    parent,
                    dimension,
                    normalized,
                    frozenPolicy,
                    currentPolicy.Identity,
                    reasonCategory,
                    AccommodationStates.PendingApproval,
                    revision,
                    requesterActorId,
                    null,
                    nowUtc,
                    null,
                    expiresAtUtc,
                    fairnessException: true),
                AccommodationOutcomes.ApprovalRequired);
        }

        return EnrollmentDecision<Accommodation>.Ok(
            Create(
                parent,
                dimension,
                normalized,
                frozenPolicy,
                currentPolicy.Identity,
                reasonCategory,
                AccommodationStates.Granted,
                revision,
                requesterActorId,
                null,
                nowUtc,
                nowUtc,
                expiresAtUtc,
                fairnessException: false),
            AccommodationOutcomes.Granted);
    }

    public static EnrollmentDecision<Accommodation> CreateGranted(
        AccommodationParentBinding parent,
        string dimension,
        string normalizedValue,
        AccommodationPolicyIdentity frozenPolicy,
        AccommodationPolicyIdentity decisionPolicy,
        string reasonCategory,
        DateTimeOffset nowUtc,
        DateTimeOffset? expiresAtUtc,
        Guid requesterActorId,
        long revision) =>
        EnrollmentDecision<Accommodation>.Ok(
            Create(
                parent,
                dimension,
                normalizedValue,
                frozenPolicy,
                decisionPolicy,
                reasonCategory,
                AccommodationStates.Granted,
                revision,
                requesterActorId,
                null,
                nowUtc,
                nowUtc,
                expiresAtUtc,
                fairnessException: false),
            AccommodationOutcomes.Granted);

    public EnrollmentDecision<Accommodation> Decide(
        Guid approverActorId,
        bool approve,
        AccommodationPolicyIdentity frozenPolicy,
        NormalizedAccommodationPolicy currentPolicy,
        long expectedRevision,
        DateTimeOffset nowUtc)
    {
        if (Status != AccommodationStates.PendingApproval)
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.Denied);
        }

        if (expectedRevision != Revision)
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.StaleRevision);
        }

        if (approverActorId == RequesterActorId)
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.DistinctApproverRequired);
        }

        if (!currentPolicy.EnvironmentEligible
            || currentPolicy.FairnessExceptionRuleId is null
            || frozenPolicy != FrozenPolicy)
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.PolicyUnavailable);
        }

        if (!currentPolicy.Dimensions.TryGetValue(Dimension, out var bounds) || !bounds.Enabled)
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.UnsupportedDimension);
        }

        if (approve && IsExpiredAt(nowUtc))
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.InvalidValue);
        }

        if (approve && !Inside(NormalizedValue, bounds.HardMin, bounds.HardMax, bounds.ValueKind))
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.OutsideBounds);
        }

        var status = approve ? AccommodationStates.Granted : AccommodationStates.Rejected;
        var outcome = approve ? AccommodationOutcomes.Granted : AccommodationOutcomes.Rejected;
        return EnrollmentDecision<Accommodation>.Ok(
            this with
            {
                Status = status,
                Revision = Revision + 1,
                ApproverActorId = approverActorId,
                DecidedAtUtc = nowUtc,
                DecisionPolicy = currentPolicy.Identity,
            },
            outcome);
    }

    public EnrollmentDecision<Accommodation> Revoke(Guid actorId, DateTimeOffset nowUtc)
    {
        _ = actorId;
        if (Status != AccommodationStates.Granted)
        {
            return EnrollmentDecision<Accommodation>.Fail(AccommodationFailureCodes.Denied);
        }

        return EnrollmentDecision<Accommodation>.Ok(
            this with
            {
                Status = AccommodationStates.Revoked,
                Revision = Revision + 1,
                RevokedAtUtc = nowUtc,
            },
            AccommodationOutcomes.Revoked);
    }

    public Accommodation Supersede(Guid successorId, DateTimeOffset nowUtc) =>
        this with
        {
            Status = AccommodationStates.Superseded,
            Revision = Revision + 1,
            SupersededByAccommodationId = successorId,
            RevokedAtUtc = nowUtc,
        };

    public bool IsExpiredAt(DateTimeOffset nowUtc) =>
        ExpiresAtUtc is { } expiry && nowUtc >= expiry;

    public bool AffectsEligibilityAt(DateTimeOffset nowUtc, NormalizedAccommodationPolicy currentPolicy)
    {
        if (Status != AccommodationStates.Granted
            || CreatedAtUtc > nowUtc
            || IsExpiredAt(nowUtc)
            || !currentPolicy.EnvironmentEligible
            || !currentPolicy.Dimensions.TryGetValue(Dimension, out var bounds)
            || !bounds.Enabled)
        {
            return false;
        }

        return FairnessException
            ? Inside(NormalizedValue, bounds.HardMin, bounds.HardMax, bounds.ValueKind)
            : Inside(NormalizedValue, bounds.RoutineMin, bounds.RoutineMax, bounds.ValueKind);
    }

    private static Accommodation Create(
        AccommodationParentBinding parent,
        string dimension,
        string normalizedValue,
        AccommodationPolicyIdentity frozenPolicy,
        AccommodationPolicyIdentity decisionPolicy,
        string reasonCategory,
        string status,
        long revision,
        Guid requesterActorId,
        Guid? approverActorId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? decidedAtUtc,
        DateTimeOffset? expiresAtUtc,
        bool fairnessException) =>
        new(
            Guid.CreateVersion7(),
            parent,
            dimension,
            normalizedValue,
            frozenPolicy,
            decisionPolicy,
            reasonCategory,
            status,
            revision,
            requesterActorId,
            approverActorId,
            createdAtUtc,
            decidedAtUtc,
            expiresAtUtc,
            null,
            null,
            fairnessException,
            AccommodationLifecyclePolicy.HistoryRetentionPolicyId,
            AccommodationLifecyclePolicy.HistoryRetentionVersion);

    private static bool ReasonAllowed(string reasonCategory, NormalizedAccommodationPolicy currentPolicy)
    {
        if (currentPolicy.SyntheticDevelopmentOnly
            && string.Equals(currentPolicy.Environment, "production", StringComparison.Ordinal)
            && string.Equals(reasonCategory, AccommodationReasonCategories.DevelopmentSynthetic, StringComparison.Ordinal))
        {
            return false;
        }

        return currentPolicy.ReasonCategories.Contains(reasonCategory, StringComparer.Ordinal);
    }

    private static bool TryNormalize(string requestedValue, string valueKind, out string normalized)
    {
        normalized = string.Empty;
        if (valueKind == AccommodationValueKinds.UtcInstant)
        {
            if (!AccommodationPolicyNormalizer.TryParseInstant(requestedValue, out var instant))
            {
                return false;
            }

            normalized = AccommodationPolicyNormalizer.FormatInstant(instant);
            return true;
        }

        if (!AccommodationPolicyNormalizer.TryParseDuration(requestedValue, out var seconds))
        {
            return false;
        }

        normalized = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }

    private static bool Inside(string value, string min, string max, string valueKind)
    {
        if (valueKind == AccommodationValueKinds.PositiveSeconds
            && AccommodationPolicyNormalizer.TryParseDuration(value, out var seconds)
            && AccommodationPolicyNormalizer.TryParseDuration(min, out var minSeconds)
            && AccommodationPolicyNormalizer.TryParseDuration(max, out var maxSeconds))
        {
            return seconds >= minSeconds && seconds <= maxSeconds;
        }

        if (AccommodationPolicyNormalizer.TryParseInstant(value, out var instant)
            && AccommodationPolicyNormalizer.TryParseInstant(min, out var minInstant)
            && AccommodationPolicyNormalizer.TryParseInstant(max, out var maxInstant))
        {
            return instant >= minInstant && instant <= maxInstant;
        }

        return false;
    }
}

namespace FlexAgent.Sessions.Domain;

public sealed class ResponseSlot
{
    internal ResponseSlot(string responseSlotId)
    {
        ResponseSlotId = responseSlotId;
        State = ResponseSlotStates.Open;
    }

    public string ResponseSlotId { get; }

    public string State { get; private set; }

    public string? ClaimedByInvocationId { get; private set; }

    internal void ClaimForPublication(string invocationId)
    {
        State = ResponseSlotStates.ClaimedForPublication;
        ClaimedByInvocationId = invocationId;
    }

    internal void MarkIntentionalNoAction()
    {
        State = ResponseSlotStates.IntentionalNoAction;
        ClaimedByInvocationId = null;
    }

    internal void Cancel()
    {
        if (State == ResponseSlotStates.Open)
        {
            State = ResponseSlotStates.Cancelled;
        }
    }

    internal static ResponseSlot Rehydrate(string responseSlotId, string state, string? claimedByInvocationId)
    {
        var slot = new ResponseSlot(responseSlotId);
        slot.State = state;
        slot.ClaimedByInvocationId = claimedByInvocationId;
        return slot;
    }
}

public sealed class Turn
{
    internal Turn(string turnId, string kind, string? triggerInvocationId, ResponseSlot responseSlot, long createdSessionSequence)
    {
        TurnId = turnId;
        Kind = kind;
        TriggerInvocationId = triggerInvocationId;
        State = kind == TurnKinds.Participant ? TurnStates.Accepted : TurnStates.WorkQueued;
        ResponseSlot = responseSlot;
        CreatedSessionSequence = createdSessionSequence;
        IsDirty = true;
    }

    public string TurnId { get; }

    public string Kind { get; }

    public string? TriggerInvocationId { get; }

    public string State { get; private set; }

    public long CreatedSessionSequence { get; }

    public ResponseSlot ResponseSlot { get; }

    internal bool IsDirty { get; private set; }

    internal void MarkWorkQueued()
    {
        State = TurnStates.WorkQueued;
        IsDirty = true;
    }

    internal void MarkComplete()
    {
        State = TurnStates.Complete;
        IsDirty = true;
    }

    internal void MarkDirty() => IsDirty = true;

    internal void MarkClean() => IsDirty = false;

    internal void Cancel()
    {
        if (State is TurnStates.Accepted or TurnStates.WorkQueued)
        {
            State = TurnStates.Cancelled;
            ResponseSlot.Cancel();
            IsDirty = true;
        }
    }

    internal static Turn Rehydrate(
        string turnId,
        string kind,
        string state,
        string? triggerInvocationId,
        ResponseSlot responseSlot,
        long createdSessionSequence)
    {
        var turn = new Turn(turnId, kind, triggerInvocationId, responseSlot, createdSessionSequence);
        turn.State = state;
        turn.IsDirty = false;
        return turn;
    }
}

public sealed class InvocationExecutionAttempt
{
    internal InvocationExecutionAttempt(int attemptOrdinal, string outcomeCategory, string? agentDecisionId)
    {
        AttemptOrdinal = attemptOrdinal;
        OutcomeCategory = outcomeCategory;
        AgentDecisionId = agentDecisionId;
    }

    public int AttemptOrdinal { get; }

    public string OutcomeCategory { get; }

    public string? AgentDecisionId { get; }
}

public sealed class AgentDecisionRecord
{
    internal AgentDecisionRecord(DecisionRecommendation recommendation, string? payloadDigest = null)
    {
        DecisionId = recommendation.DecisionId;
        DecisionType = recommendation.DecisionType;
        ProducedAt = recommendation.ProducedAt;
        NextTimer = recommendation.NextTimer;
        Recommendation = recommendation;
        PayloadDigest = payloadDigest ?? DecisionRecommendationDigestComputer.Compute(recommendation);
    }

    public string DecisionId { get; }

    public string DecisionType { get; }

    public DateTimeOffset ProducedAt { get; }

    public NextTimerRecommendation? NextTimer { get; }

    public string PayloadDigest { get; }

    public long CommittedSessionVersion { get; private set; }

    public long CommittedSessionSequence { get; private set; }

    internal DecisionRecommendation Recommendation { get; }

    internal void BindCommitState(long sessionVersion, long sessionSequence)
    {
        CommittedSessionVersion = sessionVersion;
        CommittedSessionSequence = sessionSequence;
    }
}

public sealed class ExecutionOutcomeRecord
{
    internal ExecutionOutcomeRecord(string executionOutcomeId, string outcomeCategory, string reasonCategory)
    {
        ExecutionOutcomeId = executionOutcomeId;
        OutcomeCategory = outcomeCategory;
        ReasonCategory = reasonCategory;
    }

    public string ExecutionOutcomeId { get; }

    public string OutcomeCategory { get; }

    public string ReasonCategory { get; }

    public long CommittedSessionVersion { get; private set; }

    public long CommittedSessionSequence { get; private set; }

    internal void BindCommitState(long sessionVersion, long sessionSequence)
    {
        CommittedSessionVersion = sessionVersion;
        CommittedSessionSequence = sessionSequence;
    }
}

public sealed class DecisionValidationEffectRecord
{
    internal DecisionValidationEffectRecord(
        string validationOutcome,
        string effectOutcome,
        string timerValidationOutcome,
        string? rejectionReasonCategory,
        IReadOnlyList<OutputItemValidation>? outputValidations = null,
        IReadOnlyList<RequestedActionItemValidation>? requestedActionValidations = null)
    {
        ValidationOutcome = validationOutcome;
        EffectOutcome = effectOutcome;
        TimerValidationOutcome = timerValidationOutcome;
        RejectionReasonCategory = rejectionReasonCategory;
        OutputValidations = outputValidations ?? [];
        RequestedActionValidations = requestedActionValidations ?? [];
    }

    public int RevisionOrdinal { get; private set; }

    public long ValidatedAtSessionVersion { get; private set; }

    public long ValidatedAtSessionSequence { get; private set; }

    public string ValidationOutcome { get; private set; }

    public string EffectOutcome { get; private set; }

    public string TimerValidationOutcome { get; }

    public string? RejectionReasonCategory { get; }

    public IReadOnlyList<OutputItemValidation> OutputValidations { get; private set; }

    public IReadOnlyList<RequestedActionItemValidation> RequestedActionValidations { get; private set; }

    internal void SetEffectOutcome(string effectOutcome, string? appliedTurnId = null, string? appliedResponseSlotId = null)
    {
        EffectOutcome = effectOutcome;
        if (appliedTurnId is not null)
        {
            AppliedTurnId = appliedTurnId;
        }

        if (appliedResponseSlotId is not null)
        {
            AppliedResponseSlotId = appliedResponseSlotId;
        }

        OutputValidations = OutputValidations
            .Select(item => item with { EffectOutcome = DeriveOutputEffect(item, effectOutcome) })
            .ToArray();
        RequestedActionValidations = RequestedActionValidations
            .Select(item => item with { EffectOutcome = DecisionEffectOutcomes.NotAttempted })
            .ToArray();
    }

    internal void RestorePersistedEffect(
        string effectOutcome,
        string? appliedTurnId,
        string? appliedResponseSlotId)
    {
        EffectOutcome = effectOutcome;
        if (appliedTurnId is not null)
        {
            AppliedTurnId = appliedTurnId;
        }

        if (appliedResponseSlotId is not null)
        {
            AppliedResponseSlotId = appliedResponseSlotId;
        }
    }

    private static string DeriveOutputEffect(OutputItemValidation item, string decisionEffect)
    {
        if (!string.Equals(item.ValidationOutcome, DecisionValidationOutcomes.Accepted, StringComparison.Ordinal)
            || !string.Equals(item.Kind, AgentOutputKinds.Message, StringComparison.Ordinal))
        {
            return DecisionEffectOutcomes.NotAttempted;
        }

        return decisionEffect is DecisionEffectOutcomes.Applied
            or DecisionEffectOutcomes.NoDomainEffect
            or DecisionEffectOutcomes.EffectFailed
            ? decisionEffect
            : DecisionEffectOutcomes.NotAttempted;
    }

    internal void BindAuthoritativeState(int revisionOrdinal, long sessionVersion, long sessionSequence)
    {
        RevisionOrdinal = revisionOrdinal;
        ValidatedAtSessionVersion = sessionVersion;
        ValidatedAtSessionSequence = sessionSequence;
    }

    internal void BindEffectCommitState(long sessionVersion, long sessionSequence)
    {
        EffectCommitSessionVersion = sessionVersion;
        EffectCommitSessionSequence = sessionSequence;
    }

    public string? AppliedTurnId { get; private set; }

    public string? AppliedResponseSlotId { get; private set; }

    public long? EffectCommitSessionVersion { get; private set; }

    public long? EffectCommitSessionSequence { get; private set; }
}

public sealed class AgentInvocation
{
    private readonly List<InvocationExecutionAttempt> _attempts = [];
    private readonly List<DecisionValidationEffectRecord> _validations = [];

    internal AgentInvocation(
        string agentInvocationId,
        SessionOwnership ownership,
        TrustedTrigger trigger,
        string idempotencyKey,
        string policyDigest,
        long sessionSequence)
    {
        AgentInvocationId = agentInvocationId;
        Ownership = ownership;
        Trigger = trigger;
        IdempotencyKey = idempotencyKey;
        PolicyDigest = policyDigest;
        SessionSequence = sessionSequence;
        Status = AgentInvocationStatuses.Admitted;
    }

    internal static AgentInvocation Rehydrate(
        string agentInvocationId,
        SessionOwnership ownership,
        TrustedTrigger trigger,
        string idempotencyKey,
        string policyDigest,
        long sessionSequence,
        string status,
        AgentDecisionRecord? decision = null,
        ExecutionOutcomeRecord? executionOutcome = null,
        IReadOnlyList<InvocationExecutionAttempt>? attempts = null,
        IReadOnlyList<DecisionValidationEffectRecord>? validations = null)
    {
        var invocation = new AgentInvocation(
            agentInvocationId,
            ownership,
            trigger,
            idempotencyKey,
            policyDigest,
            sessionSequence);
        invocation.Status = status;
        invocation.SessionSequence = sessionSequence;
        if (decision is not null)
        {
            invocation.Decision = decision;
            invocation.AgentDecisionId = decision.DecisionId;
        }

        if (executionOutcome is not null)
        {
            invocation.ExecutionOutcome = executionOutcome;
            invocation.ExecutionOutcomeId = executionOutcome.ExecutionOutcomeId;
        }

        if (attempts is not null)
        {
            invocation._attempts.AddRange(attempts);
        }

        if (validations is not null)
        {
            invocation._validations.AddRange(validations);
        }

        return invocation;
    }

    public string AgentInvocationId { get; }

    public SessionOwnership Ownership { get; }

    public TrustedTrigger Trigger { get; }

    public string IdempotencyKey { get; }

    public string PolicyDigest { get; }

    public long SessionSequence { get; private set; }

    public string Status { get; private set; }

    public string? AgentDecisionId { get; private set; }

    public string? ExecutionOutcomeId { get; private set; }

    public AgentDecisionRecord? Decision { get; private set; }

    public ExecutionOutcomeRecord? ExecutionOutcome { get; private set; }

    public DecisionValidationEffectRecord? ValidationEffect =>
        _validations.Count == 0 ? null : _validations[^1];

    public IReadOnlyList<DecisionValidationEffectRecord> ValidationHistory => _validations;

    public IReadOnlyList<InvocationExecutionAttempt> Attempts => _attempts;

    public bool IsTerminal =>
        Status is AgentInvocationStatuses.Decided
            or AgentInvocationStatuses.ExecutionFailed
            or AgentInvocationStatuses.Cancelled;

    internal string IdentityKey =>
        $"{Trigger.TriggerFamily}|{Trigger.TriggerType}|{Trigger.TriggerId}|{Trigger.Purpose}|{PolicyDigest}";

    internal void AttachDecision(AgentDecisionRecord decision, long sessionSequence)
    {
        Decision = decision;
        AgentDecisionId = decision.DecisionId;
        SessionSequence = sessionSequence;
        Status = AgentInvocationStatuses.DecisionRecorded;
        _attempts.Add(new InvocationExecutionAttempt(
            _attempts.Count + 1,
            ExecutionAttemptOutcomeCategories.DecisionProduced,
            decision.DecisionId));
    }

    internal void MarkPipelineComplete()
    {
        if (Status == AgentInvocationStatuses.DecisionRecorded)
        {
            Status = AgentInvocationStatuses.Decided;
        }
    }

    internal void AttachExecutionOutcome(ExecutionOutcomeRecord outcome, long sessionSequence, string status)
    {
        ExecutionOutcome = outcome;
        ExecutionOutcomeId = outcome.ExecutionOutcomeId;
        SessionSequence = sessionSequence;
        Status = status;
    }

    internal void AddFailedAttempt(string outcomeCategory)
    {
        _attempts.Add(new InvocationExecutionAttempt(_attempts.Count + 1, outcomeCategory, null));
    }

    internal void AppendValidation(DecisionValidationEffectRecord validationEffect) =>
        _validations.Add(validationEffect);
}

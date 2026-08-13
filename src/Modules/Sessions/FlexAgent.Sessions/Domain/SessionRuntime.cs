namespace FlexAgent.Sessions.Domain;

public sealed class SessionRuntime
{
    private static readonly P0TextSessionRuntimeCapabilityPolicy P0Kernel =
        P0TextSessionRuntimeCapabilityPolicy.Create();

    private static readonly HashSet<string> KnownTriggerFamilies =
    [
        RuntimeTriggerIdentifiers.ParticipantInputFamily,
        RuntimeTriggerIdentifiers.WorkflowEventFamily,
        RuntimeTriggerIdentifiers.TimerEventFamily,
        "interaction_signal",
        "tool_result",
        "system_event",
    ];

    private readonly List<AgentInvocation> _invocations = [];
    private readonly List<Turn> _turns = [];
    private readonly List<VisibleTranscriptItemRef> _visibleTranscript = [];
    private readonly Dictionary<string, DateTimeOffset> _lastAdmittedAtByFamily = new(StringComparer.Ordinal);

    private SessionRuntime(TrustedSessionBinding binding, DateTimeOffset startedAt)
    {
        Binding = binding;
        Ownership = binding.Ownership;
        LifecycleState = SessionLifecycleState.Active;
        LastCommittedAt = startedAt;
    }

    public TrustedSessionBinding Binding { get; }

    public SessionOwnership Ownership { get; }

    public FrozenTextSessionRuntimePolicy Policy => Binding.Policy;

    public SessionLifecycleState LifecycleState { get; private set; }

    public long SessionVersion { get; private set; }

    public long SessionSequence { get; private set; }

    public long? CutoffSequence { get; private set; }

    public DateTimeOffset LastCommittedAt { get; private set; }

    public IReadOnlyList<AgentInvocation> Invocations => _invocations;

    public IReadOnlyList<Turn> Turns => _turns;

    public IReadOnlyList<VisibleTranscriptItemRef> VisibleTranscript => _visibleTranscript;

    public static SessionRuntime CreateActive(TrustedSessionBinding binding, DateTimeOffset startedAt)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (!IsUtc(startedAt))
        {
            throw new ArgumentException("Authoritative Session time must be UTC.", nameof(startedAt));
        }

        return new SessionRuntime(binding, startedAt);
    }

    public TriggerAdmissionResult AcceptParticipantMessage(
        string participantMessageId,
        string turnId,
        string responseSlotId,
        string triggerId,
        string idempotencyKey,
        DateTimeOffset authoritativeUtc)
    {
        var trigger = new TrustedTrigger(
            RuntimeTriggerIdentifiers.ParticipantInputFamily,
            RuntimeTriggerIdentifiers.ParticipantMessageType,
            triggerId,
            InvocationPurposes.ParticipantTurnRespond,
            turnId,
            responseSlotId);
        var identityKey = InvocationIdentityKey(trigger);
        var existingByIdentity = FindInvocationByIdentity(identityKey);
        var existingByKey = FindInvocationByIdempotencyKey(idempotencyKey);
        var existingTurn = FindTurn(turnId);
        var existingMessage = _visibleTranscript.FirstOrDefault(item =>
            string.Equals(item.MessageId, participantMessageId, StringComparison.Ordinal));

        if (existingByIdentity is not null
            || existingByKey is not null
            || existingTurn is not null
            || existingMessage is not null)
        {
            if (IsExactParticipantAdmission(
                    participantMessageId,
                    turnId,
                    responseSlotId,
                    idempotencyKey,
                    identityKey,
                    existingByIdentity ?? existingByKey,
                    existingTurn,
                    existingMessage))
            {
                return AdmitTrustedTrigger(trigger, idempotencyKey, authoritativeUtc);
            }

            return AdmissionFailure(TriggerAdmissionOutcomeCodes.IdempotencyConflict);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: true))
        {
            return AdmissionFailure(clockFailure);
        }

        if (LifecycleState != SessionLifecycleState.Active)
        {
            return AdmissionFailure(TriggerAdmissionOutcomeCodes.LifecycleIneligible);
        }

        _turns.Add(new Turn(turnId, TurnKinds.Participant, triggerId, new ResponseSlot(responseSlotId)));
        _visibleTranscript.Add(new VisibleTranscriptItemRef(
            participantMessageId,
            TranscriptAuthorTypes.Participant,
            turnId,
            new ProtectedContentRef(
                $"msg:{participantMessageId}",
                ProtectedContentRef.DigestForReference($"msg:{participantMessageId}"))));

        var result = AdmitTrustedTrigger(trigger, idempotencyKey, authoritativeUtc);
        if (!result.Succeeded)
        {
            _turns.RemoveAll(turn => string.Equals(turn.TurnId, turnId, StringComparison.Ordinal));
            _visibleTranscript.RemoveAll(item =>
                string.Equals(item.MessageId, participantMessageId, StringComparison.Ordinal));
        }

        return result;
    }

    public TriggerAdmissionResult AdmitTrustedTrigger(
        TrustedTrigger trigger,
        string idempotencyKey,
        DateTimeOffset authoritativeUtc,
        long? expectedSessionVersion = null)
    {
        ArgumentNullException.ThrowIfNull(trigger);

        var identityKey = InvocationIdentityKey(trigger);
        var existingByIdentity = FindInvocationByIdentity(identityKey);
        var existingByKey = FindInvocationByIdempotencyKey(idempotencyKey);
        var existing = existingByIdentity ?? existingByKey;
        if (existing is not null)
        {
            if (existing.IdentityKey == identityKey
                && string.Equals(existing.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)
                && string.Equals(existing.Trigger.TurnId, trigger.TurnId, StringComparison.Ordinal)
                && string.Equals(existing.Trigger.ResponseSlotId, trigger.ResponseSlotId, StringComparison.Ordinal))
            {
                return new TriggerAdmissionResult(
                    true,
                    TriggerAdmissionOutcomeCodes.Reconciled,
                    existing,
                    existing.SessionSequence,
                    SessionVersion);
            }

            return AdmissionFailure(TriggerAdmissionOutcomeCodes.IdempotencyConflict);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: true))
        {
            return AdmissionFailure(clockFailure);
        }

        if (LifecycleState != SessionLifecycleState.Active)
        {
            return AdmissionFailure(TriggerAdmissionOutcomeCodes.LifecycleIneligible);
        }

        if (!TryClassifyTrigger(trigger, out var classificationFailure))
        {
            return AdmissionFailure(classificationFailure);
        }

        if (IsParticipantTrigger(trigger))
        {
            var turn = trigger.TurnId is null ? null : FindTurn(trigger.TurnId);
            if (turn is null
                || trigger.ResponseSlotId is null
                || !string.Equals(turn.ResponseSlot.ResponseSlotId, trigger.ResponseSlotId, StringComparison.Ordinal))
            {
                return AdmissionFailure(TriggerAdmissionOutcomeCodes.MissingTurn);
            }
        }

        if (expectedSessionVersion is not null && expectedSessionVersion.Value != SessionVersion)
        {
            return AdmissionFailure(TriggerAdmissionOutcomeCodes.StaleVersion);
        }

        if (_invocations.Count >= Policy.InvocationBounds.MaxChainedInvocationsPerSession)
        {
            return AdmissionFailure(TriggerAdmissionOutcomeCodes.BudgetExhausted);
        }

        if (IsTimerTrigger(trigger)
            && Policy.TimerLane is not null
            && _invocations.Count(invocation => IsTimerTrigger(invocation.Trigger))
                >= Policy.TimerLane.Budgets.MaxTimerTriggeredInvocationsPerSession)
        {
            return AdmissionFailure(TriggerAdmissionOutcomeCodes.BudgetExhausted);
        }

        if (_lastAdmittedAtByFamily.TryGetValue(trigger.TriggerFamily, out var lastAdmitted)
            && authoritativeUtc < lastAdmitted.AddSeconds(Policy.InvocationBounds.CooldownSeconds))
        {
            return AdmissionFailure(TriggerAdmissionOutcomeCodes.CooldownActive);
        }

        var invocation = new AgentInvocation(
            Guid.NewGuid().ToString("N"),
            Ownership,
            trigger,
            idempotencyKey,
            Policy.PolicyDigest,
            NextSequence(authoritativeUtc));
        _invocations.Add(invocation);
        _lastAdmittedAtByFamily[trigger.TriggerFamily] = authoritativeUtc;
        SessionVersion++;

        return new TriggerAdmissionResult(
            true,
            TriggerAdmissionOutcomeCodes.Succeeded,
            invocation,
            invocation.SessionSequence,
            SessionVersion);
    }

    public void Pause(DateTimeOffset authoritativeUtc)
    {
        EnsureAuthoritativeClock(authoritativeUtc);
        if (LifecycleState != SessionLifecycleState.Active)
        {
            return;
        }

        LifecycleState = SessionLifecycleState.Paused;
        Touch(authoritativeUtc);
    }

    public void BeginCompleting(DateTimeOffset authoritativeUtc)
    {
        EnsureAuthoritativeClock(authoritativeUtc);
        if (LifecycleState is SessionLifecycleState.Completed
            or SessionLifecycleState.Terminated
            or SessionLifecycleState.Aborted)
        {
            return;
        }

        LifecycleState = SessionLifecycleState.Completing;
        CutoffSequence = SessionSequence;
        foreach (var turn in _turns)
        {
            turn.Cancel();
        }

        Touch(authoritativeUtc);
    }

    public void Complete(DateTimeOffset authoritativeUtc)
    {
        EnsureAuthoritativeClock(authoritativeUtc);
        if (LifecycleState != SessionLifecycleState.Completing)
        {
            return;
        }

        LifecycleState = SessionLifecycleState.Completed;
        Touch(authoritativeUtc);
    }

    public void Terminate(DateTimeOffset authoritativeUtc)
    {
        EnsureAuthoritativeClock(authoritativeUtc);
        if (LifecycleState != SessionLifecycleState.Completing)
        {
            return;
        }

        LifecycleState = SessionLifecycleState.Terminated;
        Touch(authoritativeUtc);
    }

    public void Abort(DateTimeOffset authoritativeUtc)
    {
        EnsureAuthoritativeClock(authoritativeUtc);
        if (LifecycleState is SessionLifecycleState.Completed
            or SessionLifecycleState.Terminated
            or SessionLifecycleState.Aborted)
        {
            return;
        }

        LifecycleState = SessionLifecycleState.Aborted;
        CutoffSequence ??= SessionSequence;
        foreach (var turn in _turns)
        {
            turn.Cancel();
        }

        Touch(authoritativeUtc);
    }

    public InvocationCompletionResult CompleteInvocation(
        string agentInvocationId,
        DecisionRecommendation recommendation,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(recommendation);
        var invocation = FindInvocation(agentInvocationId);
        if (invocation is null)
        {
            return CompletionFailure(InvocationCompletionOutcomeCodes.AlreadyTerminal, null);
        }

        if (invocation.IsTerminal)
        {
            return ReconcileTerminalDecision(invocation, recommendation);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return CompletionFailure(clockFailure, invocation);
        }

        if (!string.Equals(recommendation.InvocationId, agentInvocationId, StringComparison.Ordinal))
        {
            return CompletionFailure(InvocationCompletionOutcomeCodes.IdentityMismatch, invocation);
        }

        if (invocation.Decision is not null && !IsEquivalentRecordedDecision(invocation, recommendation))
        {
            return CompletionFailure(InvocationCompletionOutcomeCodes.IdentityMismatch, invocation);
        }

        if (HasCutoff() && invocation.Decision is null)
        {
            return RecordLateResult(invocation, authoritativeUtc);
        }

        if (invocation.Decision is null)
        {
            var recorded = RecordDecision(agentInvocationId, recommendation, authoritativeUtc);
            if (!recorded.Succeeded)
            {
                return recorded;
            }
        }

        ValidateDecision(agentInvocationId, authoritativeUtc);
        var applied = ApplyDecisionEffect(agentInvocationId, authoritativeUtc);
        invocation.MarkPipelineComplete();
        if (applied.OutcomeCode == InvocationCompletionOutcomeCodes.EffectFailed)
        {
            return new InvocationCompletionResult(
                false,
                InvocationCompletionOutcomeCodes.EffectFailed,
                invocation,
                invocation.Decision,
                invocation.ExecutionOutcome,
                invocation.ValidationEffect,
                applied.PublicationPathClaimed,
                applied.AgentMessagePublished);
        }

        return new InvocationCompletionResult(
            true,
            InvocationCompletionOutcomeCodes.Decided,
            invocation,
            invocation.Decision,
            invocation.ExecutionOutcome,
            invocation.ValidationEffect,
            applied.PublicationPathClaimed,
            applied.AgentMessagePublished);
    }

    public InvocationCompletionResult CompleteInvocation(
        string agentInvocationId,
        ExecutionFailureCompletion failure,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(failure);
        var invocation = FindInvocation(agentInvocationId);
        if (invocation is null || invocation.IsTerminal || invocation.Decision is not null)
        {
            return CompletionFailure(InvocationCompletionOutcomeCodes.AlreadyTerminal, invocation);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return CompletionFailure(clockFailure, invocation);
        }

        if (HasCutoff())
        {
            return RecordLateResult(invocation, authoritativeUtc);
        }

        invocation.AddFailedAttempt(failure.ReasonCategory);
        var outcome = new ExecutionOutcomeRecord(
            Guid.NewGuid().ToString("N"),
            ExecutionOutcomeCategories.ExecutionFailed,
            failure.ReasonCategory);
        invocation.AttachExecutionOutcome(outcome, NextSequence(authoritativeUtc), AgentInvocationStatuses.ExecutionFailed);
        SessionVersion++;
        return new InvocationCompletionResult(
            true,
            InvocationCompletionOutcomeCodes.ExecutionFailed,
            invocation,
            ExecutionOutcome: outcome);
    }

    public InvocationCompletionResult RecordFailedAttempt(
        string agentInvocationId,
        string reasonCategory,
        DateTimeOffset authoritativeUtc)
    {
        var invocation = FindInvocation(agentInvocationId);
        if (invocation is null || invocation.IsTerminal || invocation.Decision is not null)
        {
            return CompletionFailure(InvocationCompletionOutcomeCodes.AlreadyTerminal, invocation);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return CompletionFailure(clockFailure, invocation);
        }

        if (HasCutoff())
        {
            return RecordLateResult(invocation, authoritativeUtc);
        }

        invocation.AddFailedAttempt(reasonCategory);
        if (invocation.Attempts.Count < Policy.InvocationBounds.MaxAttemptsPerInvocation)
        {
            Touch(authoritativeUtc);
            return new InvocationCompletionResult(
                true,
                InvocationCompletionOutcomeCodes.AttemptRecorded,
                invocation);
        }

        var outcome = new ExecutionOutcomeRecord(
            Guid.NewGuid().ToString("N"),
            ExecutionOutcomeCategories.AttemptsExhausted,
            "retry_budget_exhausted");
        invocation.AttachExecutionOutcome(outcome, NextSequence(authoritativeUtc), AgentInvocationStatuses.ExecutionFailed);
        SessionVersion++;
        return new InvocationCompletionResult(
            true,
            InvocationCompletionOutcomeCodes.AttemptsExhausted,
            invocation,
            ExecutionOutcome: outcome);
    }

    public InvocationCompletionResult RecordDecision(
        string agentInvocationId,
        DecisionRecommendation recommendation,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(recommendation);
        var invocation = FindInvocation(agentInvocationId);
        if (invocation is null)
        {
            return CompletionFailure(InvocationCompletionOutcomeCodes.AlreadyTerminal, null);
        }

        if (invocation.Decision is not null)
        {
            if (IsEquivalentRecordedDecision(invocation, recommendation))
            {
                return new InvocationCompletionResult(
                    true,
                    InvocationCompletionOutcomeCodes.Decided,
                    invocation,
                    invocation.Decision,
                    invocation.ExecutionOutcome,
                    invocation.ValidationEffect);
            }

            return CompletionFailure(InvocationCompletionOutcomeCodes.IdentityMismatch, invocation);
        }

        if (invocation.IsTerminal)
        {
            return CompletionFailure(InvocationCompletionOutcomeCodes.AlreadyTerminal, invocation);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return CompletionFailure(clockFailure, invocation);
        }

        if (!string.Equals(recommendation.InvocationId, agentInvocationId, StringComparison.Ordinal))
        {
            return CompletionFailure(InvocationCompletionOutcomeCodes.IdentityMismatch, invocation);
        }

        if (HasCutoff())
        {
            return RecordLateResult(invocation, authoritativeUtc);
        }

        var decision = new AgentDecisionRecord(recommendation);
        invocation.AttachDecision(decision, NextSequence(authoritativeUtc));
        SessionVersion++;
        return new InvocationCompletionResult(
            true,
            InvocationCompletionOutcomeCodes.Decided,
            invocation,
            decision);
    }

    public DecisionValidationResult ValidateDecision(string agentInvocationId, DateTimeOffset authoritativeUtc)
    {
        var invocation = FindInvocation(agentInvocationId);
        if (invocation?.Decision is null)
        {
            return new DecisionValidationResult(
                false,
                InvocationCompletionOutcomeCodes.AlreadyTerminal,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.StateIneligible,
                TimerValidationOutcomes.NotPresent);
        }

        if (invocation.ValidationEffect is not null
            && invocation.ValidationEffect.EffectOutcome is DecisionEffectOutcomes.Applied
                or DecisionEffectOutcomes.NoDomainEffect
                or DecisionEffectOutcomes.EffectFailed)
        {
            return new DecisionValidationResult(
                invocation.ValidationEffect.ValidationOutcome == DecisionValidationOutcomes.Accepted,
                InvocationCompletionOutcomeCodes.Decided,
                invocation.ValidationEffect.ValidationOutcome,
                invocation.ValidationEffect.RejectionReasonCategory,
                invocation.ValidationEffect.TimerValidationOutcome);
        }

        if (invocation.ValidationEffect is not null
            && invocation.ValidationEffect.EffectOutcome == DecisionEffectOutcomes.NotAttempted
            && invocation.ValidationEffect.ValidatedAtSessionVersion == SessionVersion
            && invocation.ValidationEffect.ValidatedAtSessionSequence == SessionSequence)
        {
            return new DecisionValidationResult(
                invocation.ValidationEffect.ValidationOutcome == DecisionValidationOutcomes.Accepted,
                InvocationCompletionOutcomeCodes.Decided,
                invocation.ValidationEffect.ValidationOutcome,
                invocation.ValidationEffect.RejectionReasonCategory,
                invocation.ValidationEffect.TimerValidationOutcome);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return new DecisionValidationResult(
                false,
                clockFailure,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.StateIneligible,
                TimerValidationOutcomes.NotPresent);
        }

        var recommendation = invocation.Decision.Recommendation;
        var timerOutcome = ValidateTimerRecommendation(recommendation.NextTimer);
        if (!Policy.PermittedDecisionTypes.Contains(recommendation.DecisionType, StringComparer.Ordinal)
            || !P0Kernel.IsDecisionTypeSupportedByP0(recommendation.DecisionType))
        {
            return StoreValidation(
                invocation,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.CapabilityDisabled,
                timerOutcome,
                authoritativeUtc);
        }

        if (recommendation is NoActionRecommendation && !Policy.NoActionPermitted)
        {
            return StoreValidation(
                invocation,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.PolicyProhibited,
                timerOutcome,
                authoritativeUtc);
        }

        if (recommendation is EmitMessageRecommendation emitMessage
            && string.IsNullOrWhiteSpace(emitMessage.CommunicationPurpose))
        {
            return StoreValidation(
                invocation,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.PayloadInvalid,
                timerOutcome,
                authoritativeUtc);
        }

        if (HasCutoff())
        {
            return StoreValidation(
                invocation,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.CutoffExceeded,
                timerOutcome,
                authoritativeUtc);
        }

        if (LifecycleState != SessionLifecycleState.Active)
        {
            return StoreValidation(
                invocation,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.StateIneligible,
                timerOutcome,
                authoritativeUtc);
        }

        return StoreValidation(
            invocation,
            DecisionValidationOutcomes.Accepted,
            null,
            timerOutcome,
            authoritativeUtc);
    }

    public DecisionEffectResult ApplyDecisionEffect(string agentInvocationId, DateTimeOffset authoritativeUtc)
    {
        var invocation = FindInvocation(agentInvocationId);
        if (invocation?.Decision is null || invocation.ValidationEffect is null)
        {
            return new DecisionEffectResult(
                false,
                InvocationCompletionOutcomeCodes.AlreadyTerminal,
                DecisionEffectOutcomes.NotAttempted);
        }

        if (invocation.ValidationEffect.EffectOutcome is DecisionEffectOutcomes.Applied
            or DecisionEffectOutcomes.NoDomainEffect)
        {
            return ReconcileAppliedEffect(invocation);
        }

        if (invocation.ValidationEffect.EffectOutcome == DecisionEffectOutcomes.EffectFailed)
        {
            return new DecisionEffectResult(
                false,
                InvocationCompletionOutcomeCodes.EffectFailed,
                DecisionEffectOutcomes.EffectFailed);
        }

        if (invocation.ValidationEffect.ValidationOutcome != DecisionValidationOutcomes.Accepted)
        {
            return new DecisionEffectResult(
                true,
                InvocationCompletionOutcomeCodes.Decided,
                DecisionEffectOutcomes.NotAttempted);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return new DecisionEffectResult(false, clockFailure, DecisionEffectOutcomes.NotAttempted);
        }

        var recommendation = invocation.Decision.Recommendation;
        if (recommendation is NoActionRecommendation)
        {
            Turn? turn = null;
            if (IsParticipantTrigger(invocation.Trigger))
            {
                turn = FindTurn(invocation.Trigger.TurnId!);
                if (turn is null || turn.ResponseSlot.State != ResponseSlotStates.Open)
                {
                    return FailEffect(invocation, authoritativeUtc);
                }

                turn.ResponseSlot.MarkIntentionalNoAction();
                turn.MarkComplete();
            }

            invocation.ValidationEffect.SetEffectOutcome(
                DecisionEffectOutcomes.NoDomainEffect,
                turn?.TurnId,
                turn?.ResponseSlot.ResponseSlotId);
            Touch(authoritativeUtc);
            invocation.MarkPipelineComplete();
            return new DecisionEffectResult(
                true,
                InvocationCompletionOutcomeCodes.Decided,
                DecisionEffectOutcomes.NoDomainEffect);
        }

        if (recommendation is EmitMessageRecommendation)
        {
            if (LifecycleState != SessionLifecycleState.Active)
            {
                return FailEffect(invocation, authoritativeUtc);
            }

            Turn turn;
            if (IsParticipantTrigger(invocation.Trigger))
            {
                var existing = FindTurn(invocation.Trigger.TurnId!);
                if (existing is null)
                {
                    return FailEffect(invocation, authoritativeUtc);
                }

                if (existing.ResponseSlot.State == ResponseSlotStates.ClaimedForPublication
                    && string.Equals(
                        existing.ResponseSlot.ClaimedByInvocationId,
                        invocation.AgentInvocationId,
                        StringComparison.Ordinal))
                {
                    invocation.ValidationEffect.SetEffectOutcome(
                        DecisionEffectOutcomes.Applied,
                        existing.TurnId,
                        existing.ResponseSlot.ResponseSlotId);
                    return ReconcileAppliedEffect(invocation);
                }

                if (existing.ResponseSlot.State != ResponseSlotStates.Open)
                {
                    return FailEffect(invocation, authoritativeUtc);
                }

                turn = existing;
            }
            else
            {
                var turnId = AgentInitiatedTurnId(invocation);
                var existing = FindTurn(turnId);
                if (existing is not null)
                {
                    turn = existing;
                }
                else
                {
                    turn = new Turn(
                        turnId,
                        AgentTurnKind(invocation.Trigger),
                        invocation.AgentInvocationId,
                        new ResponseSlot(AgentInitiatedSlotId(invocation)));
                    _turns.Add(turn);
                }
            }

            turn.ResponseSlot.ClaimForPublication(invocation.AgentInvocationId);
            turn.MarkWorkQueued();
            invocation.ValidationEffect.SetEffectOutcome(
                DecisionEffectOutcomes.Applied,
                turn.TurnId,
                turn.ResponseSlot.ResponseSlotId);
            Touch(authoritativeUtc);
            invocation.MarkPipelineComplete();
            return new DecisionEffectResult(
                true,
                InvocationCompletionOutcomeCodes.Decided,
                DecisionEffectOutcomes.Applied,
                PublicationPathClaimed: true);
        }

        invocation.ValidationEffect.SetEffectOutcome(DecisionEffectOutcomes.NotAttempted);
        return new DecisionEffectResult(
            true,
            InvocationCompletionOutcomeCodes.Decided,
            DecisionEffectOutcomes.NotAttempted);
    }

    private DecisionEffectResult ReconcileAppliedEffect(AgentInvocation invocation)
    {
        invocation.MarkPipelineComplete();
        var applied = invocation.ValidationEffect!.EffectOutcome == DecisionEffectOutcomes.Applied;
        return new DecisionEffectResult(
            true,
            InvocationCompletionOutcomeCodes.Decided,
            invocation.ValidationEffect.EffectOutcome,
            PublicationPathClaimed: applied);
    }

    private DecisionEffectResult FailEffect(AgentInvocation invocation, DateTimeOffset authoritativeUtc)
    {
        invocation.ValidationEffect!.SetEffectOutcome(DecisionEffectOutcomes.EffectFailed);
        Touch(authoritativeUtc);
        invocation.MarkPipelineComplete();
        return new DecisionEffectResult(
            false,
            InvocationCompletionOutcomeCodes.EffectFailed,
            DecisionEffectOutcomes.EffectFailed);
    }

    private DecisionValidationResult StoreValidation(
        AgentInvocation invocation,
        string validationOutcome,
        string? rejectionReason,
        string timerOutcome,
        DateTimeOffset authoritativeUtc)
    {
        var record = new DecisionValidationEffectRecord(
            validationOutcome,
            DecisionEffectOutcomes.NotAttempted,
            timerOutcome,
            rejectionReason);
        invocation.AppendValidation(record);
        Touch(authoritativeUtc);
        record.BindAuthoritativeState(
            invocation.ValidationHistory.Count,
            SessionVersion,
            SessionSequence);
        if (validationOutcome != DecisionValidationOutcomes.Accepted)
        {
            invocation.MarkPipelineComplete();
        }

        return new DecisionValidationResult(
            validationOutcome == DecisionValidationOutcomes.Accepted,
            InvocationCompletionOutcomeCodes.Decided,
            validationOutcome,
            rejectionReason,
            timerOutcome);
    }

    private string ValidateTimerRecommendation(NextTimerRecommendation? nextTimer)
    {
        if (nextTimer is null)
        {
            return TimerValidationOutcomes.NotPresent;
        }

        if (Policy.TimerLane is not { IsEnabled: true }
            || LifecycleState != SessionLifecycleState.Active
            || !Iso8601PositiveDuration.TryParse(nextTimer.RelativeDelay, out var delay)
            || delay.CompareTo(Policy.TimerLane.MinRequestedDelay) < 0
            || delay.CompareTo(Policy.TimerLane.MaxRequestedDelay) > 0)
        {
            return TimerValidationOutcomes.Rejected;
        }

        return TimerValidationOutcomes.Accepted;
    }

    private InvocationCompletionResult RecordLateResult(AgentInvocation invocation, DateTimeOffset authoritativeUtc)
    {
        var outcome = new ExecutionOutcomeRecord(
            Guid.NewGuid().ToString("N"),
            ExecutionOutcomeCategories.LateResult,
            "late_provider_result");
        invocation.AddFailedAttempt(ExecutionAttemptOutcomeCategories.LateResult);
        invocation.AttachExecutionOutcome(outcome, NextSequence(authoritativeUtc), AgentInvocationStatuses.Cancelled);
        SessionVersion++;
        return new InvocationCompletionResult(
            true,
            InvocationCompletionOutcomeCodes.LateResult,
            invocation,
            ExecutionOutcome: outcome);
    }

    private bool TryClassifyTrigger(TrustedTrigger trigger, out string failureCode)
    {
        failureCode = TriggerAdmissionOutcomeCodes.UnknownTrigger;
        var supportedByP0 = P0Kernel.IsTriggerSupportedByP0(trigger.TriggerFamily, trigger.TriggerType)
            || P0Kernel.IsTimerTriggerSupportedByP0(trigger.TriggerFamily, trigger.TriggerType);
        if (!supportedByP0)
        {
            failureCode = KnownTriggerFamilies.Contains(trigger.TriggerFamily)
                ? TriggerAdmissionOutcomeCodes.ProhibitedTrigger
                : TriggerAdmissionOutcomeCodes.UnknownTrigger;
            return false;
        }

        if (IsTimerTrigger(trigger))
        {
            if (!Policy.IsTimerTriggerPermitted)
            {
                failureCode = TriggerAdmissionOutcomeCodes.ProhibitedTrigger;
                return false;
            }

            return true;
        }

        if (!Policy.PermittedNonTimerTriggers.Any(descriptor =>
                descriptor.TriggerFamily == trigger.TriggerFamily
                && descriptor.TriggerType == trigger.TriggerType))
        {
            failureCode = TriggerAdmissionOutcomeCodes.ProhibitedTrigger;
            return false;
        }

        if (trigger.TriggerType == RuntimeTriggerIdentifiers.AgentOpeningType
            && !Policy.AgentInitiatedOpeningPermitted)
        {
            failureCode = TriggerAdmissionOutcomeCodes.ProhibitedTrigger;
            return false;
        }

        if (trigger.TriggerType == RuntimeTriggerIdentifiers.AgentClosingType
            && !Policy.AgentInitiatedClosingPermitted)
        {
            failureCode = TriggerAdmissionOutcomeCodes.ProhibitedTrigger;
            return false;
        }

        return true;
    }

    private static bool IsParticipantTrigger(TrustedTrigger trigger) =>
        trigger.TriggerFamily == RuntimeTriggerIdentifiers.ParticipantInputFamily
        && trigger.TriggerType == RuntimeTriggerIdentifiers.ParticipantMessageType;

    private static bool IsTimerTrigger(TrustedTrigger trigger) =>
        trigger.TriggerFamily == RuntimeTriggerIdentifiers.TimerEventFamily
        && trigger.TriggerType == RuntimeTriggerIdentifiers.TimerLaneDefaultType;

    private static string AgentTurnKind(TrustedTrigger trigger) =>
        trigger.TriggerType switch
        {
            RuntimeTriggerIdentifiers.AgentOpeningType => TurnKinds.AgentOpening,
            RuntimeTriggerIdentifiers.AgentClosingType => TurnKinds.AgentClosing,
            RuntimeTriggerIdentifiers.TimerLaneDefaultType => TurnKinds.AgentTimer,
            _ => TurnKinds.AgentOpening,
        };

    private Turn? FindTurn(string turnId) =>
        _turns.FirstOrDefault(turn => string.Equals(turn.TurnId, turnId, StringComparison.Ordinal));

    private AgentInvocation? FindInvocation(string agentInvocationId) =>
        _invocations.FirstOrDefault(invocation =>
            string.Equals(invocation.AgentInvocationId, agentInvocationId, StringComparison.Ordinal));

    private AgentInvocation? FindInvocationByIdentity(string identityKey) =>
        _invocations.FirstOrDefault(invocation => invocation.IdentityKey == identityKey);

    private AgentInvocation? FindInvocationByIdempotencyKey(string idempotencyKey) =>
        _invocations.FirstOrDefault(invocation =>
            string.Equals(invocation.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));

    private string InvocationIdentityKey(TrustedTrigger trigger) =>
        $"{trigger.TriggerFamily}|{trigger.TriggerType}|{trigger.TriggerId}|{trigger.Purpose}|{Policy.PolicyDigest}";

    private static string AgentInitiatedTurnId(AgentInvocation invocation) =>
        $"turn.agent.{invocation.AgentInvocationId}";

    private static string AgentInitiatedSlotId(AgentInvocation invocation) =>
        $"slot.agent.{invocation.AgentInvocationId}";

    private static bool IsExactParticipantAdmission(
        string participantMessageId,
        string turnId,
        string responseSlotId,
        string idempotencyKey,
        string identityKey,
        AgentInvocation? existingInvocation,
        Turn? existingTurn,
        VisibleTranscriptItemRef? existingMessage) =>
        existingInvocation is not null
        && existingTurn is not null
        && existingMessage is not null
        && existingInvocation.IdentityKey == identityKey
        && string.Equals(existingInvocation.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)
        && string.Equals(existingInvocation.Trigger.TurnId, turnId, StringComparison.Ordinal)
        && string.Equals(existingInvocation.Trigger.ResponseSlotId, responseSlotId, StringComparison.Ordinal)
        && string.Equals(existingTurn.ResponseSlot.ResponseSlotId, responseSlotId, StringComparison.Ordinal)
        && string.Equals(existingMessage.TurnId, turnId, StringComparison.Ordinal)
        && string.Equals(existingMessage.MessageId, participantMessageId, StringComparison.Ordinal);

    private bool HasCutoff() =>
        CutoffSequence is not null
        || LifecycleState is SessionLifecycleState.Completing
            or SessionLifecycleState.Completed
            or SessionLifecycleState.Terminated
            or SessionLifecycleState.Aborted;

    private long NextSequence(DateTimeOffset authoritativeUtc)
    {
        EnsureAuthoritativeClock(authoritativeUtc);
        SessionSequence++;
        LastCommittedAt = authoritativeUtc;
        return SessionSequence;
    }

    private void Touch(DateTimeOffset authoritativeUtc)
    {
        EnsureAuthoritativeClock(authoritativeUtc);
        LastCommittedAt = authoritativeUtc;
        SessionVersion++;
    }

    // In-memory stand-in for persistence: PostgreSQL must supply commit time/order
    // from the transaction (database_now / clock_timestamp), not worker UtcNow.
    private bool TryAuthorizeClock(DateTimeOffset timestamp, out string failureCode, bool admission)
    {
        if (!IsUtc(timestamp))
        {
            failureCode = admission
                ? TriggerAdmissionOutcomeCodes.NonUtcClock
                : InvocationCompletionOutcomeCodes.NonUtcClock;
            return false;
        }

        if (timestamp < LastCommittedAt)
        {
            failureCode = admission
                ? TriggerAdmissionOutcomeCodes.StaleClock
                : InvocationCompletionOutcomeCodes.StaleClock;
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    private static bool IsUtc(DateTimeOffset timestamp) => timestamp.Offset == TimeSpan.Zero;

    private void EnsureAuthoritativeClock(DateTimeOffset timestamp)
    {
        if (!TryAuthorizeClock(timestamp, out var failureCode, admission: true))
        {
            throw new ArgumentException(
                failureCode == TriggerAdmissionOutcomeCodes.StaleClock
                    ? "Authoritative Session time must not precede LastCommittedAt."
                    : "Authoritative Session time must be UTC.",
                nameof(timestamp));
        }
    }

    private static TriggerAdmissionResult AdmissionFailure(string outcomeCode) =>
        new(false, outcomeCode, null, null);

    private static InvocationCompletionResult CompletionFailure(string outcomeCode, AgentInvocation? invocation) =>
        new(false, outcomeCode, invocation);

    private static bool HasSameDecisionIdentity(AgentInvocation invocation, DecisionRecommendation recommendation) =>
        invocation.Decision is not null
        && string.Equals(invocation.Decision.DecisionId, recommendation.DecisionId, StringComparison.Ordinal)
        && string.Equals(recommendation.InvocationId, invocation.AgentInvocationId, StringComparison.Ordinal);

    private static bool IsEquivalentRecordedDecision(
        AgentInvocation invocation,
        DecisionRecommendation recommendation) =>
        HasSameDecisionIdentity(invocation, recommendation)
        && string.Equals(
            invocation.Decision!.PayloadDigest,
            DecisionRecommendationDigestComputer.Compute(recommendation),
            StringComparison.Ordinal);

    private static InvocationCompletionResult ReconcileTerminalDecision(
        AgentInvocation invocation,
        DecisionRecommendation recommendation)
    {
        if (!IsEquivalentRecordedDecision(invocation, recommendation))
        {
            return CompletionFailure(
                HasSameDecisionIdentity(invocation, recommendation)
                    ? InvocationCompletionOutcomeCodes.IdentityMismatch
                    : InvocationCompletionOutcomeCodes.AlreadyTerminal,
                invocation);
        }

        if (invocation.ValidationEffect?.EffectOutcome == DecisionEffectOutcomes.EffectFailed)
        {
            return new InvocationCompletionResult(
                false,
                InvocationCompletionOutcomeCodes.EffectFailed,
                invocation,
                invocation.Decision,
                invocation.ExecutionOutcome,
                invocation.ValidationEffect);
        }

        return new InvocationCompletionResult(
            true,
            InvocationCompletionOutcomeCodes.Decided,
            invocation,
            invocation.Decision,
            invocation.ExecutionOutcome,
            invocation.ValidationEffect,
            PublicationPathClaimed: invocation.ValidationEffect?.EffectOutcome == DecisionEffectOutcomes.Applied);
    }
}

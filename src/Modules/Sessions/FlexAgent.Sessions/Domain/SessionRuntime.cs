namespace FlexAgent.Sessions.Domain;

public sealed partial class SessionRuntime
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
    private readonly List<AgentResponseMessage> _agentMessages = [];
    private readonly HashSet<AgentResponseMessage> _pendingPublicationWork = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Turn> _dirtyTurns = new(ReferenceEqualityComparer.Instance);
    private readonly Queue<VisibleTranscriptItemRef> _pendingTranscript = new();
    private readonly Dictionary<string, DateTimeOffset> _lastAdmittedAtByFamily = new(StringComparer.Ordinal);
    private readonly List<TimerScheduleRevision> _timerSchedules = [];

    private SessionRuntime(TrustedSessionBinding binding, DateTimeOffset startedAt)
    {
        Binding = binding;
        Ownership = binding.Ownership;
        LifecycleState = SessionLifecycleState.Active;
        LastCommittedAt = startedAt;
    }

    private SessionRuntime(
        TrustedSessionBinding binding,
        SessionLifecycleState lifecycleState,
        long sessionVersion,
        long sessionSequence,
        long? cutoffSequence,
        DateTimeOffset lastCommittedAt,
        IReadOnlyList<AgentInvocation> invocations,
        IReadOnlyList<Turn> turns,
        IReadOnlyList<VisibleTranscriptItemRef> transcript,
        IReadOnlyDictionary<string, DateTimeOffset> lastAdmittedAtByFamily,
        IReadOnlyList<AgentResponseMessage> agentMessages,
        IReadOnlyList<TimerScheduleRevision> timerSchedules)
    {
        Binding = binding;
        Ownership = binding.Ownership;
        LifecycleState = lifecycleState;
        SessionVersion = sessionVersion;
        SessionSequence = sessionSequence;
        CutoffSequence = cutoffSequence;
        LastCommittedAt = lastCommittedAt;
        _invocations.AddRange(invocations);
        _turns.AddRange(turns);
        _visibleTranscript.AddRange(transcript);
        _agentMessages.AddRange(agentMessages);
        _timerSchedules.AddRange(timerSchedules);
        foreach (var pair in lastAdmittedAtByFamily)
        {
            _lastAdmittedAtByFamily[pair.Key] = pair.Value;
        }
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

    public IReadOnlyList<AgentResponseMessage> AgentMessages => _agentMessages;

    public IReadOnlyList<TimerScheduleRevision> TimerSchedules => _timerSchedules;

    public TimerScheduleRevision? CurrentTimerLane =>
        _timerSchedules.LastOrDefault(revision => revision.IsOpen)
        ?? _timerSchedules.LastOrDefault(revision => revision.LaneState == TimerLaneStates.Fired);

    public int PendingTimerCount =>
        _timerSchedules.Count(revision => revision.IsOpen);

    internal IReadOnlyCollection<AgentResponseMessage> PendingPublicationWork => _pendingPublicationWork;

    internal IReadOnlyCollection<Turn> DirtyTurns => _dirtyTurns;

    internal IReadOnlyCollection<VisibleTranscriptItemRef> PendingTranscript => _pendingTranscript;

    internal IEnumerable<TimerScheduleRevision> DirtyTimerSchedules =>
        _timerSchedules.Where(revision => revision.PendingInsert || revision.IsDirty);

    public static SessionRuntime CreateActive(TrustedSessionBinding binding, DateTimeOffset startedAt)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (!IsUtc(startedAt))
        {
            throw new ArgumentException("Authoritative Session time must be UTC.", nameof(startedAt));
        }

        var session = new SessionRuntime(binding, startedAt);
        session.ArmDefaultCadence(startedAt, TimerRequestedByCategories.DefaultCadence);
        return session;
    }

    public static SessionRuntime Rehydrate(
        TrustedSessionBinding binding,
        SessionLifecycleState lifecycleState,
        long sessionVersion,
        long sessionSequence,
        long? cutoffSequence,
        DateTimeOffset lastCommittedAt,
        IReadOnlyList<AgentInvocation>? invocations = null,
        IReadOnlyList<Turn>? turns = null,
        IReadOnlyList<VisibleTranscriptItemRef>? transcript = null,
        IReadOnlyDictionary<string, DateTimeOffset>? lastAdmittedAtByFamily = null,
        IReadOnlyList<AgentResponseMessage>? agentMessages = null,
        IReadOnlyList<TimerScheduleRevision>? timerSchedules = null)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (!IsUtc(lastCommittedAt))
        {
            throw new ArgumentException("Authoritative Session time must be UTC.", nameof(lastCommittedAt));
        }

        return new SessionRuntime(
            binding,
            lifecycleState,
            sessionVersion,
            sessionSequence,
            cutoffSequence,
            lastCommittedAt,
            invocations ?? [],
            turns ?? [],
            transcript ?? [],
            lastAdmittedAtByFamily ?? new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal),
            agentMessages ?? [],
            timerSchedules ?? []);
    }

    internal void ReplaceLastCommittedAtFromDatabase(DateTimeOffset lastCommittedAt)
    {
        if (!IsUtc(lastCommittedAt))
        {
            throw new ArgumentException("Authoritative Session time must be UTC.", nameof(lastCommittedAt));
        }

        LastCommittedAt = lastCommittedAt;
    }

    public TriggerAdmissionResult AcceptParticipantMessage(
        string participantMessageId,
        string turnId,
        string responseSlotId,
        string triggerId,
        string idempotencyKey,
        DateTimeOffset authoritativeUtc,
        long? expectedSessionVersion = null)
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

        _turns.Add(new Turn(turnId, TurnKinds.Participant, triggerId, new ResponseSlot(responseSlotId), NextAdmissionSequence()));
        var participantTranscript = new VisibleTranscriptItemRef(
            participantMessageId,
            TranscriptAuthorTypes.Participant,
            turnId,
            new ProtectedContentRef(
                $"msg:{participantMessageId}",
                ProtectedContentRef.DigestForReference($"msg:{participantMessageId}")));
        _visibleTranscript.Add(participantTranscript);

        var result = AdmitTrustedTrigger(trigger, idempotencyKey, authoritativeUtc, expectedSessionVersion);
        if (!result.Succeeded)
        {
            _turns.RemoveAll(turn => string.Equals(turn.TurnId, turnId, StringComparison.Ordinal));
            _visibleTranscript.RemoveAll(item =>
                string.Equals(item.MessageId, participantMessageId, StringComparison.Ordinal));
        }
        else
        {
            TrackTurn(_turns.First(turn => string.Equals(turn.TurnId, turnId, StringComparison.Ordinal)));
            TrackTranscript(participantTranscript);
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

        if (IsTimerTrigger(trigger) && !HasRemainingTimerInvocationBudget())
        {
            return AdmissionFailure(TriggerAdmissionOutcomeCodes.BudgetExhausted);
        }

        if (_lastAdmittedAtByFamily.TryGetValue(trigger.TriggerFamily, out var lastAdmitted)
            && authoritativeUtc < lastAdmitted.AddSeconds(Policy.InvocationBounds.CooldownSeconds))
        {
            return AdmissionFailure(TriggerAdmissionOutcomeCodes.CooldownActive);
        }

        var invocation = new AgentInvocation(
            $"ainv.{Guid.NewGuid():N}",
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

        FreezeOpenTimerRemaining(authoritativeUtc, wasActive: true);
        LifecycleState = SessionLifecycleState.Paused;
        Touch(authoritativeUtc);
    }

    public void Resume(DateTimeOffset authoritativeUtc)
    {
        EnsureAuthoritativeClock(authoritativeUtc);
        if (LifecycleState != SessionLifecycleState.Paused)
        {
            return;
        }

        LifecycleState = SessionLifecycleState.Active;
        foreach (var revision in _timerSchedules.Where(item => item.LaneState == TimerLaneStates.Pending))
        {
            revision.ResumeRemaining(authoritativeUtc);
        }

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
        CancelOpenTimerLane();
        SealOpenAgentMessagesIncomplete(authoritativeUtc);
        foreach (var turn in _turns)
        {
            turn.Cancel();
            TrackTurn(turn);
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
        CancelOpenTimerLane();
        SealOpenAgentMessagesIncomplete(authoritativeUtc);
        foreach (var turn in _turns)
        {
            turn.Cancel();
            TrackTurn(turn);
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

        if (invocation.IsTerminal && invocation.ValidationEffect?.HasPendingIndependentActionEffect != true)
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
        ArmDefaultSuccessorIfTimerTerminal(invocation, authoritativeUtc);
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
        // Execution-failure retries after a terminal Invocation return AlreadyTerminal
        // with Succeeded=false. That is a safe acknowledgement for at-least-once
        // workers, not a retryable error. Decision retries instead reconcile as
        // Succeeded=true when identity and payload digest match.
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
        outcome.BindCommitState(SessionVersion, invocation.SessionSequence);
        ArmDefaultSuccessorIfTimerTerminal(invocation, authoritativeUtc);
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
        outcome.BindCommitState(SessionVersion, invocation.SessionSequence);
        ArmDefaultSuccessorIfTimerTerminal(invocation, authoritativeUtc);
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
        decision.BindCommitState(SessionVersion, invocation.SessionSequence);
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
            && (invocation.ValidationEffect.EffectOutcome is DecisionEffectOutcomes.Applied
                    or DecisionEffectOutcomes.NoDomainEffect
                    or DecisionEffectOutcomes.EffectFailed
                || HasAppliedTimerAction(invocation.ValidationEffect)))
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
        var envelope = HistoricalDecisionEnvelopeMapper.ToEnvelope(recommendation);
        var profile = P0DecisionProfileValidator.Validate(
            envelope,
            Policy,
            P0Kernel.IsDecisionTypeSupportedByP0,
            allocateRuntimeOutputIds: recommendation is EnvelopeRecommendation);
        var executableTimer = EnvelopeRecommendationMapping.ResolveExecutableNextTimer(
            envelope.RequestedActions,
            profile.RequestedActions);
        var timerOutcome = CombineTimerOutcomes(
            ValidateTimerRecommendation(invocation, executableTimer, authoritativeUtc),
            profile.TimerValidationOutcome);
        if (!Policy.PermittedDecisionTypes.Contains(recommendation.DecisionType, StringComparer.Ordinal)
            || !P0Kernel.IsDecisionTypeSupportedByP0(recommendation.DecisionType))
        {
            return StoreValidation(
                invocation,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.CapabilityDisabled,
                timerOutcome,
                authoritativeUtc,
                profile.Outputs,
                profile.RequestedActions);
        }

        if (HasCutoff())
        {
            return StoreValidation(
                invocation,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.CutoffExceeded,
                timerOutcome,
                authoritativeUtc,
                profile.Outputs,
                profile.RequestedActions);
        }

        if (LifecycleState != SessionLifecycleState.Active)
        {
            return StoreValidation(
                invocation,
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.StateIneligible,
                timerOutcome,
                authoritativeUtc,
                profile.Outputs,
                profile.RequestedActions);
        }

        return StoreValidation(
            invocation,
            profile.CommunicationOutcome,
            profile.CommunicationRejectionReason,
            timerOutcome,
            authoritativeUtc,
            profile.Outputs,
            profile.RequestedActions);
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
            return ApplyIndependentRequestedActionEffects(invocation, authoritativeUtc);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return new DecisionEffectResult(false, clockFailure, DecisionEffectOutcomes.NotAttempted);
        }

        var recommendation = invocation.Decision.Recommendation;
        if (recommendation is NoActionRecommendation
            || recommendation is EnvelopeRecommendation envelope
                && string.Equals(envelope.Disposition, DecisionDispositions.NoAction, StringComparison.Ordinal))
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
                turn.MarkDirty();
                TrackTurn(turn);
            }

            invocation.ValidationEffect.SetEffectOutcome(
                DecisionEffectOutcomes.NoDomainEffect,
                turn?.TurnId,
                turn?.ResponseSlot.ResponseSlotId);
            ApplyAcceptedTimerReplacement(invocation, authoritativeUtc);
            Touch(authoritativeUtc);
            invocation.ValidationEffect.BindEffectCommitState(SessionVersion, SessionSequence);
            invocation.MarkPipelineComplete();
            return new DecisionEffectResult(
                true,
                InvocationCompletionOutcomeCodes.Decided,
                DecisionEffectOutcomes.NoDomainEffect);
        }

        if (recommendation is EmitMessageRecommendation || HasAcceptedMessage(invocation.ValidationEffect))
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
                        new ResponseSlot(AgentInitiatedSlotId(invocation)),
                        ClaimedSessionSequence());
                    _turns.Add(turn);
                }
            }

            turn.ResponseSlot.ClaimForPublication(invocation.AgentInvocationId);
            turn.MarkWorkQueued();
            turn.MarkDirty();
            TrackTurn(turn);
            invocation.ValidationEffect.SetEffectOutcome(
                DecisionEffectOutcomes.Applied,
                turn.TurnId,
                turn.ResponseSlot.ResponseSlotId);
            ApplyAcceptedTimerReplacement(invocation, authoritativeUtc);
            Touch(authoritativeUtc);
            invocation.ValidationEffect.BindEffectCommitState(SessionVersion, SessionSequence);
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

    public AgentResponseFragmentCommitResult CommitAgentResponseFragment(
        AgentResponseFragmentCommit commit,
        DateTimeOffset authoritativeUtc)
    {
        ArgumentNullException.ThrowIfNull(commit);
        var invocation = FindInvocation(commit.AgentInvocationId);
        var existing = FindAgentMessageByInvocation(commit.AgentInvocationId);
        if (existing is { IsTerminal: true })
        {
            return ReconcileOrRejectTerminalFragment(existing, commit);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return FragmentFailure(MapFragmentClockFailure(clockFailure), existing);
        }

        if (string.IsNullOrEmpty(commit.ExactUtf8Text))
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.EmptyDelta, existing);
        }

        if (string.IsNullOrWhiteSpace(commit.GenerationAttemptId))
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.CompetingAttempt, existing);
        }

        if (existing is not null)
        {
            if (!string.Equals(existing.GenerationAttemptId, commit.GenerationAttemptId, StringComparison.Ordinal))
            {
                return FragmentFailure(FragmentCommitOutcomeCodes.CompetingAttempt, existing);
            }

            var duplicate = existing.Fragments.FirstOrDefault(fragment =>
                fragment.FragmentOrdinal == commit.FragmentOrdinal);
            if (duplicate is not null)
            {
                if (!string.Equals(
                        duplicate.ContentDigest,
                        ProtectedContentRef.DigestUtf8(commit.ExactUtf8Text),
                        StringComparison.Ordinal))
                {
                    return FragmentFailure(FragmentCommitOutcomeCodes.DigestMismatch, existing, duplicate);
                }

                return new AgentResponseFragmentCommitResult(
                    true,
                    FragmentCommitOutcomeCodes.Reconciled,
                    existing,
                    duplicate,
                    AgentMessagePublished: true);
            }

            if (commit.FragmentOrdinal != existing.LastFragmentOrdinal + 1)
            {
                return FragmentFailure(FragmentCommitOutcomeCodes.Gap, existing);
            }

            if (!CanPublishNewFragment())
            {
                return FragmentFailure(FragmentCommitOutcomeCodes.Cutoff, existing);
            }

            var boundFailure = IncrementalPublicationValidator.RejectDelta(
                commit.ExactUtf8Text,
                existing.AssembleExactText(),
                Policy.StreamingPublicationBounds,
                _agentMessages,
                existing,
                authoritativeUtc);
            if (boundFailure is not null)
            {
                return FragmentFailure(boundFailure, existing);
            }

            var appended = existing.AppendFragment(
                commit.FragmentOrdinal,
                NextSequence(authoritativeUtc),
                commit.ExactUtf8Text,
                authoritativeUtc);
            TrackPublication(existing);
            Touch(authoritativeUtc);
            return new AgentResponseFragmentCommitResult(
                true,
                FragmentCommitOutcomeCodes.Succeeded,
                existing,
                appended,
                AgentMessagePublished: true);
        }

        if (commit.FragmentOrdinal != 1)
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.Gap, null);
        }

        if (!CanPublishNewFragment())
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.Cutoff, null);
        }

        if (invocation is null || !IsPublicationClaimed(invocation))
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.PublicationNotClaimed, null);
        }

        var firstBoundFailure = IncrementalPublicationValidator.RejectDelta(
            commit.ExactUtf8Text,
            string.Empty,
            Policy.StreamingPublicationBounds,
            _agentMessages,
            existing: null,
            authoritativeUtc);
        if (firstBoundFailure is not null)
        {
            return FragmentFailure(firstBoundFailure, null);
        }

        var turn = FindPublicationTurn(invocation)!;
        var messageId = AllocateOrReuseOutputId(invocation);
        var acceptedOutputId = invocation.ValidationEffect?.OutputValidations.FirstOrDefault(item =>
            string.Equals(item.AgentOutputId, messageId, StringComparison.Ordinal))
            ?.AgentOutputId;
        var message = new AgentResponseMessage(
            messageId,
            commit.GenerationAttemptId,
            invocation.AgentInvocationId,
            invocation.Decision!.DecisionId,
            turn.TurnId,
            turn.ResponseSlot.ResponseSlotId,
            acceptedOutputId);
        var fragment = message.AppendFragment(1, NextSequence(authoritativeUtc), commit.ExactUtf8Text, authoritativeUtc);
        _agentMessages.Add(message);
        TrackPublication(message);
        var agentTranscript = new VisibleTranscriptItemRef(
            message.MessageId,
            TranscriptAuthorTypes.Agent,
            turn.TurnId,
            new ProtectedContentRef(
                $"msg:{message.MessageId}",
                ProtectedContentRef.DigestForReference($"msg:{message.MessageId}")));
        _visibleTranscript.Add(agentTranscript);
        TrackTranscript(agentTranscript);
        Touch(authoritativeUtc);
        return new AgentResponseFragmentCommitResult(
            true,
            FragmentCommitOutcomeCodes.Succeeded,
            message,
            fragment,
            AgentMessagePublished: true);
    }

    public AgentResponseFragmentCommitResult CompleteAgentResponseMessage(
        string agentInvocationId,
        DateTimeOffset authoritativeUtc)
    {
        var message = FindAgentMessageByInvocation(agentInvocationId);
        if (message is null || message.Fragments.Count == 0)
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.PublicationNotClaimed, message);
        }

        if (message.IsTerminal)
        {
            if (message.CompletionState == AgentMessageCompletionStates.Complete)
            {
                return new AgentResponseFragmentCommitResult(
                    true,
                    FragmentCommitOutcomeCodes.Reconciled,
                    message,
                    message.Fragments[^1],
                    AgentMessagePublished: true);
            }

            return FragmentFailure(FragmentCommitOutcomeCodes.AlreadyTerminal, message);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return FragmentFailure(MapFragmentClockFailure(clockFailure), message);
        }

        if (!CanPublishNewFragment())
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.Cutoff, message);
        }

        if (!IncrementalPublicationValidator.IsRecordableAssembled(message.AssembleExactText()))
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.ValidationFailed, message);
        }

        return SealAgentMessage(message, AgentMessageCompletionStates.Complete, authoritativeUtc);
    }

    public AgentResponseFragmentCommitResult MarkAgentResponseIncomplete(
        string agentInvocationId,
        DateTimeOffset authoritativeUtc)
    {
        var message = FindAgentMessageByInvocation(agentInvocationId);
        if (message is null || message.Fragments.Count == 0)
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.PublicationNotClaimed, message);
        }

        if (message.IsTerminal)
        {
            if (message.CompletionState == AgentMessageCompletionStates.Incomplete)
            {
                return new AgentResponseFragmentCommitResult(
                    true,
                    FragmentCommitOutcomeCodes.Reconciled,
                    message,
                    message.Fragments[^1],
                    AgentMessagePublished: true);
            }

            return FragmentFailure(FragmentCommitOutcomeCodes.AlreadyTerminal, message);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return FragmentFailure(MapFragmentClockFailure(clockFailure), message);
        }

        return SealAgentMessage(message, AgentMessageCompletionStates.Incomplete, authoritativeUtc);
    }

    public AgentResponseFragmentCommitResult FailUnpublishedAgentResponse(
        string agentInvocationId,
        DateTimeOffset authoritativeUtc)
    {
        var invocation = FindInvocation(agentInvocationId);
        var message = FindAgentMessageByInvocation(agentInvocationId);
        if (message is { Fragments.Count: > 0 })
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.AlreadyTerminal, message);
        }

        var turn = invocation is null ? null : FindPublicationTurn(invocation);
        if (turn is { State: TurnStates.Cancelled }
            && turn.ResponseSlot.State == ResponseSlotStates.Cancelled)
        {
            return new AgentResponseFragmentCommitResult(
                true,
                FragmentCommitOutcomeCodes.Reconciled,
                AgentMessagePublished: false);
        }

        if (invocation is null || !IsPublicationClaimed(invocation) || turn is null)
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.PublicationNotClaimed, message);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return FragmentFailure(MapFragmentClockFailure(clockFailure), message);
        }

        turn.Cancel();
        TrackTurn(turn);
        Touch(authoritativeUtc);
        return new AgentResponseFragmentCommitResult(
            true,
            FragmentCommitOutcomeCodes.UnpublishedFailed,
            AgentMessagePublished: false);
    }

    private DecisionEffectResult ApplyIndependentRequestedActionEffects(
        AgentInvocation invocation,
        DateTimeOffset authoritativeUtc)
    {
        if (HasAppliedTimerAction(invocation.ValidationEffect!))
        {
            invocation.MarkPipelineComplete();
            return new DecisionEffectResult(
                true,
                InvocationCompletionOutcomeCodes.Decided,
                DecisionEffectOutcomes.NotAttempted);
        }

        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: false))
        {
            return new DecisionEffectResult(false, clockFailure, DecisionEffectOutcomes.NotAttempted);
        }

        ApplyAcceptedTimerReplacement(invocation, authoritativeUtc);
        if (!HasAppliedTimerAction(invocation.ValidationEffect!))
        {
            invocation.MarkPipelineComplete();
            return new DecisionEffectResult(
                true,
                InvocationCompletionOutcomeCodes.Decided,
                DecisionEffectOutcomes.NotAttempted);
        }

        Touch(authoritativeUtc);
        invocation.ValidationEffect!.BindEffectCommitState(SessionVersion, SessionSequence);
        invocation.MarkPipelineComplete();
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
            PublicationPathClaimed: applied,
            AgentMessagePublished: HasPublishedAgentMessage(invocation));
    }

    private DecisionEffectResult FailEffect(AgentInvocation invocation, DateTimeOffset authoritativeUtc)
    {
        invocation.ValidationEffect!.SetEffectOutcome(DecisionEffectOutcomes.EffectFailed);
        Touch(authoritativeUtc);
        invocation.ValidationEffect.BindEffectCommitState(SessionVersion, SessionSequence);
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
        DateTimeOffset authoritativeUtc,
        IReadOnlyList<OutputItemValidation>? outputValidations = null,
        IReadOnlyList<RequestedActionItemValidation>? requestedActionValidations = null)
    {
        var record = new DecisionValidationEffectRecord(
            validationOutcome,
            DecisionEffectOutcomes.NotAttempted,
            timerOutcome,
            rejectionReason,
            outputValidations,
            requestedActionValidations);
        invocation.AppendValidation(record);
        Touch(authoritativeUtc);
        record.BindAuthoritativeState(
            invocation.ValidationHistory.Count,
            SessionVersion,
            SessionSequence);
        if (validationOutcome != DecisionValidationOutcomes.Accepted
            && !record.HasPendingIndependentActionEffect)
        {
            invocation.MarkPipelineComplete();
        }

        return new DecisionValidationResult(
            validationOutcome == DecisionValidationOutcomes.Accepted,
            InvocationCompletionOutcomeCodes.Decided,
            validationOutcome,
            rejectionReason,
            timerOutcome,
            record.OutputValidations,
            record.RequestedActionValidations);
    }

    private static string CombineTimerOutcomes(string laneOutcome, string profileOutcome)
    {
        if (laneOutcome == TimerValidationOutcomes.Rejected
            || profileOutcome == TimerValidationOutcomes.Rejected)
        {
            return TimerValidationOutcomes.Rejected;
        }

        if (laneOutcome == TimerValidationOutcomes.Accepted
            || profileOutcome == TimerValidationOutcomes.Accepted)
        {
            return TimerValidationOutcomes.Accepted;
        }

        return TimerValidationOutcomes.NotPresent;
    }

    private static bool HasAcceptedMessage(DecisionValidationEffectRecord validation) =>
        validation.OutputValidations.Any(item =>
            string.Equals(item.Kind, AgentOutputKinds.Message, StringComparison.Ordinal)
            && string.Equals(item.ValidationOutcome, DecisionValidationOutcomes.Accepted, StringComparison.Ordinal));

    private static bool HasAppliedTimerAction(DecisionValidationEffectRecord validation) =>
        validation.RequestedActionValidations.Any(item =>
            string.Equals(item.Kind, AgentRequestedActionKinds.NextTimerRequest, StringComparison.Ordinal)
            && string.Equals(item.ValidationOutcome, DecisionValidationOutcomes.Accepted, StringComparison.Ordinal)
            && string.Equals(item.EffectOutcome, DecisionEffectOutcomes.Applied, StringComparison.Ordinal));

    private string ValidateTimerRecommendation(
        AgentInvocation invocation,
        NextTimerRecommendation? nextTimer,
        DateTimeOffset authoritativeUtc)
    {
        if (nextTimer is null)
        {
            return TimerValidationOutcomes.NotPresent;
        }

        if (Policy.TimerLane is not { IsEnabled: true }
            || LifecycleState != SessionLifecycleState.Active
            || HasCutoff()
            || !Policy.TimerLane.PermittedStages.Contains("active", StringComparer.Ordinal)
            || !Iso8601PositiveDuration.TryParse(nextTimer.RelativeDelay, out var delay)
            || delay.CompareTo(Policy.TimerLane.MinRequestedDelay) < 0
            || delay.CompareTo(Policy.TimerLane.MaxRequestedDelay) > 0
            || !MatchesExpectedScheduleRevision(nextTimer.ExpectedScheduleRevision)
            || CountAcceptedReplacements() >= Policy.TimerLane.Budgets.MaxAcceptedReplacementsPerSession
            || CountInFlightEligibleReplacementInvocations(invocation)
                >= Policy.TimerLane.Budgets.MaxConcurrentReplacements
            || IsTimerReplacementCooldownActive(authoritativeUtc)
            || IsDuplicateReplacementSuppressed(nextTimer, authoritativeUtc)
            || OpenTimerLane()?.LaneState == TimerLaneStates.Claimed
            || !HasRemainingTimerInvocationBudget())
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
        outcome.BindCommitState(SessionVersion, invocation.SessionSequence);
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

    private bool HasRemainingTimerInvocationBudget() =>
        Policy.TimerLane is { IsEnabled: true }
        && _invocations.Count(invocation => IsTimerTrigger(invocation.Trigger))
            < Policy.TimerLane.Budgets.MaxTimerTriggeredInvocationsPerSession;

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

    private AgentResponseMessage? FindAgentMessageByInvocation(string agentInvocationId) =>
        _agentMessages.FirstOrDefault(message =>
            string.Equals(message.DrivingInvocationId, agentInvocationId, StringComparison.Ordinal));

    private Turn? FindPublicationTurn(AgentInvocation invocation)
    {
        var turnId = invocation.ValidationEffect?.AppliedTurnId ?? invocation.Trigger.TurnId;
        return turnId is null ? null : FindTurn(turnId);
    }

    public bool HasOpenAgentContentPublication(string agentInvocationId)
    {
        var invocation = FindInvocation(agentInvocationId);
        if (invocation is null || !IsPublicationClaimed(invocation))
        {
            return false;
        }

        var message = FindAgentMessageByInvocation(agentInvocationId);
        return message is null || !message.IsTerminal;
    }

    private bool IsPublicationClaimed(AgentInvocation invocation)
    {
        if (invocation.Decision is null
            || invocation.ValidationEffect?.EffectOutcome != DecisionEffectOutcomes.Applied)
        {
            return false;
        }

        var turn = FindPublicationTurn(invocation);
        return turn is not null
            && turn.ResponseSlot.State == ResponseSlotStates.ClaimedForPublication
            && string.Equals(
                turn.ResponseSlot.ClaimedByInvocationId,
                invocation.AgentInvocationId,
                StringComparison.Ordinal);
    }

    private bool CanPublishNewFragment() =>
        LifecycleState == SessionLifecycleState.Active && !HasCutoff();

    private static string AllocateOrReuseOutputId(AgentInvocation invocation)
    {
        var allocated = invocation.ValidationEffect?.OutputValidations.FirstOrDefault(item =>
            string.Equals(item.Kind, AgentOutputKinds.Message, StringComparison.Ordinal)
            && string.Equals(item.ValidationOutcome, DecisionValidationOutcomes.Accepted, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(item.AgentOutputId));
        return allocated?.AgentOutputId ?? $"aout.{Guid.NewGuid():N}"[..21];
    }

    private bool HasPublishedAgentMessage(AgentInvocation invocation) =>
        FindAgentMessageByInvocation(invocation.AgentInvocationId) is { Fragments.Count: > 0 };

    private void SealOpenAgentMessagesIncomplete(DateTimeOffset authoritativeUtc)
    {
        foreach (var message in _agentMessages)
        {
            if (!message.IsTerminal && message.Fragments.Count > 0)
            {
                SealAgentMessage(message, AgentMessageCompletionStates.Incomplete, authoritativeUtc);
            }
        }
    }

    private static AgentResponseFragmentCommitResult ReconcileOrRejectTerminalFragment(
        AgentResponseMessage message,
        AgentResponseFragmentCommit commit)
    {
        if (!string.Equals(message.GenerationAttemptId, commit.GenerationAttemptId, StringComparison.Ordinal))
        {
            return FragmentFailure(FragmentCommitOutcomeCodes.CompetingAttempt, message);
        }

        var duplicate = message.Fragments.FirstOrDefault(fragment =>
            fragment.FragmentOrdinal == commit.FragmentOrdinal);
        if (duplicate is not null)
        {
            if (!string.Equals(
                    duplicate.ContentDigest,
                    ProtectedContentRef.DigestUtf8(commit.ExactUtf8Text),
                    StringComparison.Ordinal))
            {
                return FragmentFailure(FragmentCommitOutcomeCodes.DigestMismatch, message, duplicate);
            }

            return new AgentResponseFragmentCommitResult(
                true,
                FragmentCommitOutcomeCodes.Reconciled,
                message,
                duplicate,
                AgentMessagePublished: true);
        }

        return FragmentFailure(FragmentCommitOutcomeCodes.AlreadyTerminal, message);
    }

    private AgentResponseFragmentCommitResult SealAgentMessage(
        AgentResponseMessage message,
        string completionState,
        DateTimeOffset authoritativeUtc)
    {
        var sealedSessionSequence = NextSequence(authoritativeUtc);
        message.Seal(completionState, sealedSessionSequence, authoritativeUtc);
        TrackPublication(message);
        var turn = FindTurn(message.TurnId);
        if (turn is { State: TurnStates.WorkQueued })
        {
            turn.MarkComplete();
            TrackTurn(turn);
        }

        Touch(authoritativeUtc);
        return new AgentResponseFragmentCommitResult(
            true,
            FragmentCommitOutcomeCodes.Succeeded,
            message,
            message.Fragments[^1],
            AgentMessagePublished: true);
    }

    private static AgentResponseFragmentCommitResult FragmentFailure(
        string outcomeCode,
        AgentResponseMessage? message,
        AgentResponseFragment? fragment = null) =>
        new(false, outcomeCode, message, fragment, AgentMessagePublished: message is { Fragments.Count: > 0 });

    private static string MapFragmentClockFailure(string clockFailure) =>
        clockFailure == InvocationCompletionOutcomeCodes.StaleClock
            ? FragmentCommitOutcomeCodes.StaleClock
            : FragmentCommitOutcomeCodes.NonUtcClock;

    internal void TrackPublication(AgentResponseMessage message) => _pendingPublicationWork.Add(message);

    internal void RemoveCleanPublicationWork() =>
        _pendingPublicationWork.RemoveWhere(message => !message.HasPendingPublicationWork);

    internal void TrackTurn(Turn turn) => _dirtyTurns.Add(turn);

    internal void RemoveCleanTurns() => _dirtyTurns.RemoveWhere(turn => !turn.IsDirty);

    internal void TrackTranscript(VisibleTranscriptItemRef item) => _pendingTranscript.Enqueue(item);

    internal void ClearPendingTranscript() => _pendingTranscript.Clear();

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

    // Participant Turns are created before AdmitTrustedTrigger claims the next
    // sequence. Stamp the sequence that admission is about to occupy so it cannot
    // collide with an agent-initiated Turn already stamped at SessionSequence.
    private long NextAdmissionSequence() => SessionSequence + 1;

    // Agent-initiated Turns are created after RecordDecision has already claimed
    // SessionSequence. Touch() increments version only, so this value is the
    // Decision sequence, not the next admission sequence.
    private long ClaimedSessionSequence() => SessionSequence;

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

    private InvocationCompletionResult ReconcileTerminalDecision(
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
            PublicationPathClaimed: invocation.ValidationEffect?.EffectOutcome == DecisionEffectOutcomes.Applied,
            AgentMessagePublished: HasPublishedAgentMessage(invocation));
    }
}

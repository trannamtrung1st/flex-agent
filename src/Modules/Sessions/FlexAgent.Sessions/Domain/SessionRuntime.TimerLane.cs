using System.Globalization;

namespace FlexAgent.Sessions.Domain;

public sealed partial class SessionRuntime
{
    public TimerFireResult FireDueTimer(DateTimeOffset authoritativeUtc)
    {
        if (!TryAuthorizeClock(authoritativeUtc, out var clockFailure, admission: true))
        {
            return new TimerFireResult(
                false,
                clockFailure == TriggerAdmissionOutcomeCodes.NonUtcClock
                    ? TimerFireOutcomeCodes.NonUtcClock
                    : TimerFireOutcomeCodes.StaleClock);
        }

        if (LifecycleState != SessionLifecycleState.Active || HasCutoff() || Policy.TimerLane is not { IsEnabled: true })
        {
            return new TimerFireResult(false, TimerFireOutcomeCodes.LifecycleIneligible);
        }

        var current = OpenTimerLane()
            ?? _timerSchedules.LastOrDefault(revision => revision.LaneState == TimerLaneStates.Fired);
        if (current is { LaneState: TimerLaneStates.Fired, FiredInvocationId: not null })
        {
            var existing = FindInvocation(current.FiredInvocationId);
            if (existing is not null)
            {
                return new TimerFireResult(
                    true,
                    TimerFireOutcomeCodes.Reconciled,
                    current,
                    new TriggerAdmissionResult(
                        true,
                        TriggerAdmissionOutcomeCodes.Reconciled,
                        existing,
                        existing.SessionSequence,
                        SessionVersion));
            }
        }

        if (current is null || current.LaneState != TimerLaneStates.Pending)
        {
            return new TimerFireResult(false, TimerFireOutcomeCodes.NotDue, current);
        }

        if (current.RemainingAt(authoritativeUtc, isActive: true) > 0)
        {
            return new TimerFireResult(false, TimerFireOutcomeCodes.NotDue, current);
        }

        if (Policy.TimerLane.Budgets.MaxTimerTriggeredInvocationsPerSession
            <= _invocations.Count(invocation => IsTimerTrigger(invocation.Trigger)))
        {
            return new TimerFireResult(false, TimerFireOutcomeCodes.BudgetExhausted, current);
        }

        current.Claim();
        var trigger = new TrustedTrigger(
            RuntimeTriggerIdentifiers.TimerEventFamily,
            RuntimeTriggerIdentifiers.TimerLaneDefaultType,
            $"trig.timer.{current.ScheduleRevisionId}",
            InvocationPurposes.TimerLaneCheck,
            TurnId: null,
            ResponseSlotId: null);
        var admitted = AdmitTrustedTrigger(
            trigger,
            $"idem.timer.{current.ScheduleRevisionId}",
            authoritativeUtc);
        if (!admitted.Succeeded || admitted.Invocation is null)
        {
            current.Unclaim();
            return new TimerFireResult(false, admitted.OutcomeCode, current, admitted);
        }

        current.Fire(admitted.Invocation.AgentInvocationId);
        return new TimerFireResult(true, TimerFireOutcomeCodes.Succeeded, current, admitted);
    }

    private void ArmDefaultCadence(DateTimeOffset utc, string requestedByCategory)
    {
        if (Policy.TimerLane is not { IsEnabled: true }
            || HasCutoff()
            || OpenTimerLane() is not null
            || LifecycleState is not SessionLifecycleState.Active and not SessionLifecycleState.Paused)
        {
            return;
        }

        var delay = Policy.TimerLane.DefaultDelay;
        DateTimeOffset? dueAt = LifecycleState == SessionLifecycleState.Active
            ? utc.AddSeconds(delay.TotalSeconds)
            : null;
        _timerSchedules.Add(
            new TimerScheduleRevision(
                $"tsrev.{Guid.NewGuid():N}",
                NextScheduleRevision(),
                delay.WireValue,
                delay.TotalSeconds,
                dueAt,
                utc,
                requestedByCategory,
                drivingDecisionId: null,
                utc));
    }

    private void ArmDefaultSuccessorIfTimerTerminal(AgentInvocation invocation, DateTimeOffset utc)
    {
        if (!invocation.IsTerminal
            || !IsTimerTrigger(invocation.Trigger)
            || HasCutoff()
            || LifecycleState is not SessionLifecycleState.Active and not SessionLifecycleState.Paused)
        {
            return;
        }

        ArmDefaultCadence(utc, TimerRequestedByCategories.SuccessorAfterFire);
    }

    private void ApplyAcceptedTimerReplacement(AgentInvocation invocation, DateTimeOffset utc)
    {
        var validation = invocation.ValidationEffect;
        var nextTimer = invocation.Decision?.NextTimer;
        if (validation is null
            || nextTimer is null
            || validation.TimerValidationOutcome != TimerValidationOutcomes.Accepted
            || !Iso8601PositiveDuration.TryParse(nextTimer.RelativeDelay, out var delay))
        {
            validation?.BindTimerActionEffect(applied: false);
            return;
        }

        OpenTimerLane()?.Supersede();
        _timerSchedules.Add(
            new TimerScheduleRevision(
                $"tsrev.{Guid.NewGuid():N}",
                NextScheduleRevision(),
                delay.WireValue,
                delay.TotalSeconds,
                utc.AddSeconds(delay.TotalSeconds),
                utc,
                TimerRequestedByCategories.AgentRecommendation,
                invocation.Decision!.DecisionId,
                utc));
        validation.BindTimerActionEffect(applied: true);
    }

    private void FreezeOpenTimerRemaining(DateTimeOffset utc, bool wasActive)
    {
        foreach (var revision in _timerSchedules.Where(item => item.LaneState == TimerLaneStates.Pending))
        {
            revision.FreezeRemaining(utc, wasActive);
        }
    }

    private void CancelOpenTimerLane()
    {
        foreach (var revision in _timerSchedules.Where(item => item.IsOpen))
        {
            revision.Cancel();
        }
    }

    private TimerScheduleRevision? OpenTimerLane() =>
        _timerSchedules.LastOrDefault(revision => revision.IsOpen);

    private long NextScheduleRevision() =>
        _timerSchedules.Count == 0 ? 1 : _timerSchedules.Max(revision => revision.ScheduleRevision) + 1;

    private bool MatchesExpectedScheduleRevision(string expectedRevision)
    {
        var current = OpenTimerLane()
            ?? _timerSchedules.LastOrDefault(revision => revision.LaneState == TimerLaneStates.Fired);
        return current is not null
            && string.Equals(
                current.ScheduleRevision.ToString(CultureInfo.InvariantCulture),
                expectedRevision,
                StringComparison.Ordinal);
    }

    private int CountAcceptedReplacements() =>
        _timerSchedules.Count(revision =>
            revision.RequestedByCategory == TimerRequestedByCategories.AgentRecommendation);

    private DateTimeOffset? LastAcceptedReplacementAt()
    {
        var last = _timerSchedules.LastOrDefault(revision =>
            revision.RequestedByCategory == TimerRequestedByCategories.AgentRecommendation);
        return last?.CreatedAt;
    }

    private bool IsTimerReplacementCooldownActive(DateTimeOffset utc)
    {
        if (Policy.TimerLane is null || Policy.TimerLane.Budgets.CooldownSeconds <= 0)
        {
            return false;
        }

        var last = LastAcceptedReplacementAt();
        return last is not null && utc < last.Value.AddSeconds(Policy.TimerLane.Budgets.CooldownSeconds);
    }

    private bool IsDuplicateReplacementSuppressed(DateTimeOffset utc)
    {
        if (Policy.TimerLane is null || Policy.TimerLane.Budgets.DuplicateSuppressionWindowSeconds <= 0)
        {
            return false;
        }

        var last = LastAcceptedReplacementAt();
        return last is not null
            && utc < last.Value.AddSeconds(Policy.TimerLane.Budgets.DuplicateSuppressionWindowSeconds);
    }
}

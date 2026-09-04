using System.Data;
using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresHostedSessionSnapshotQuery(
    IHostedSessionSubjectSource subjects,
    IHostedSessionAccess access,
    ITrustedSessionBindingSource bindings,
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    IHostedSessionFrozenTimingSource? frozenTimingSource = null) : IHostedSessionSnapshotQuery
{
    public async Task<HostedSessionQueryResult> GetAsync(
        TrustedRuntimeActor actor,
        Guid untrustedSessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var subject = await subjects.ResolveCurrentAsync(actor, untrustedSessionId, cancellationToken);
        if (subject is null)
        {
            return new HostedSessionQueryResult(false, "session.denied", null);
        }

        var projectionKind = HostedSessionRelationships.ProjectionKind(subject.Relationship);
        var action = projectionKind switch
        {
            HostedSessionProjectionKinds.Administrator => "session.operations.read",
            HostedSessionProjectionKinds.Historical => "session.transcript.read",
            _ => "session.snapshot.read",
        };
        if (!await access.HasCurrentPermissionAsync(
                actor,
                subject.OrganizationId,
                untrustedSessionId,
                action,
                cancellationToken))
        {
            return new HostedSessionQueryResult(false, "session.denied", null);
        }

        var binding = await bindings.GetForOrganizationSessionAsync(
            subject.OrganizationId,
            untrustedSessionId,
            cancellationToken);
        if (binding is null)
        {
            return new HostedSessionQueryResult(false, "session.denied", null);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(
            connectionAccessor,
            IsolationLevel.RepeatableRead,
            cancellationToken);
        try
        {
            var session = await runtimeRepository.LoadSnapshotAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                cancellationToken);
            if (session is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new HostedSessionQueryResult(false, "session.denied", null);
            }

            var observedAt = await runtimeRepository.ReadAuthoritativeUtcAsync(scope.Transaction, cancellationToken);
            var startedAt = await scope.Transaction.Connection!.QuerySingleAsync<DateTimeOffset>(
                new CommandDefinition(
                    """
                    SELECT created_at
                    FROM session_runtimes
                    WHERE organization_id = @OrganizationId
                      AND session_id = @SessionId
                    """,
                    new
                    {
                        session.Ownership.OrganizationId,
                        session.Ownership.SessionId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken));
            var warningOccurrences = (await scope.Transaction.Connection!.QueryAsync<HostedSessionWarningOccurrence>(
                new CommandDefinition(
                    """
                    SELECT warning_threshold_id AS WarningThresholdId,
                           warning_code AS WarningCode,
                           remaining_seconds_threshold AS RemainingSecondsThreshold,
                           due_at AS DueAt,
                           committed_at AS CommittedAt,
                           session_sequence AS SessionSequence,
                           remaining_seconds_at_commit AS RemainingSecondsAtCommit,
                           delivery_status AS DeliveryStatus
                    FROM session_warning_occurrences
                    WHERE organization_id = @OrganizationId
                      AND session_id = @SessionId
                    ORDER BY session_sequence
                    """,
                    new
                    {
                        session.Ownership.OrganizationId,
                        session.Ownership.SessionId,
                    },
                    scope.Transaction,
                    cancellationToken: cancellationToken))).AsList();
            await scope.CommitAsync(cancellationToken);
            var frozenTiming = frozenTimingSource is null
                ? HostedFrozenTimingPolicy.UnavailablePolicy
                : await frozenTimingSource.LoadAsync(
                    session.Ownership.OrganizationId,
                    session.Ownership.SessionId,
                    startedAt,
                    cancellationToken);
            return new HostedSessionQueryResult(
                true,
                "session.snapshot.loaded",
                HostedSessionSnapshotProjector.Project(
                    session,
                    projectionKind,
                    observedAt,
                    startedAt,
                    frozenTiming,
                    warningOccurrences));
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class PostgresHostedSessionCommandCoordinator(
    IHostedSessionSubjectSource subjects,
    IHostedSessionAccess access,
    ITrustedSessionBindingSource bindings,
    PostgresAcceptParticipantMessageCoordinator messages,
    PostgresSessionLifecycleCoordinator lifecycle,
    IHostedSessionSnapshotQuery snapshots,
    HostedSessionExpirySettings? expiry = null) : IHostedSessionCommandCoordinator
{
    private readonly HostedSessionExpirySettings? _expiry = expiry;

    public async Task<HostedSessionCommandResult?> SubmitAsync(
        TrustedRuntimeActor actor,
        Guid routeSessionId,
        string commandType,
        string commandId,
        string idempotencyKey,
        long expectedSessionVersion,
        string? messageText,
        string? pauseReasonCode,
        string? terminateReasonCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var subject = await subjects.ResolveCurrentAsync(actor, routeSessionId, cancellationToken);
        if (subject is null)
        {
            return Denied();
        }

        var action = commandType switch
        {
            "session.message.send.v1" => "session.message.send",
            "session.pause.v1" => "session.pause",
            "session.resume.v1" => "session.resume",
            "session.complete.v1" => "session.complete",
            "session.terminate.v1" => "session.terminate",
            "session.reconcile.v1" => "session.reconcile",
            _ => null,
        };
        if (action is null)
        {
            return null;
        }

        if (!await access.HasCurrentPermissionAsync(
                actor,
                subject.OrganizationId,
                routeSessionId,
                action,
                cancellationToken))
        {
            return Denied();
        }

        var binding = await bindings.GetForOrganizationSessionAsync(
            subject.OrganizationId,
            routeSessionId,
            cancellationToken);
        if (binding is null)
        {
            return Denied();
        }

        var projectionKind = HostedSessionRelationships.ProjectionKind(subject.Relationship);
        var live = await snapshots.GetAsync(actor, routeSessionId, cancellationToken);
        if (!live.Found || live.Snapshot is null)
        {
            return Denied();
        }

        if (!HostedSessionCommandAdmission.IsPermitted(
                commandType,
                projectionKind,
                subject.Relationship,
                live.Snapshot.PermittedActions))
        {
            return Denied();
        }

        if (HostedSessionTimingAuthority.IsUnavailable(live.Snapshot.TimingPolicy)
            && commandType is "session.message.send.v1" or "session.resume.v1")
        {
            return new HostedSessionCommandResult(
                false,
                "rejected",
                "session.timing.unavailable",
                "none",
                live.Snapshot.PermittedActions,
                live.Snapshot.SessionVersion,
                live.Snapshot.SessionSequence);
        }

        if (HostedSessionCutoffAdmission.ShouldExpireLiveSession(
                live.Snapshot.LifecycleState,
                live.Snapshot.RemainingSeconds))
        {
            await TryExpireDueAsync(binding, live.Snapshot, cancellationToken);
            if (HostedSessionCutoffAdmission.ShouldRejectCommand(
                    commandType,
                    live.Snapshot.LifecycleState,
                    live.Snapshot.RemainingSeconds))
            {
                var afterCutoff = await snapshots.GetAsync(actor, routeSessionId, cancellationToken);
                return new HostedSessionCommandResult(
                    false,
                    "rejected",
                    "session.cutoff.passed",
                    "none",
                    afterCutoff.Snapshot?.PermittedActions ?? [],
                    afterCutoff.Snapshot?.SessionVersion,
                    afterCutoff.Snapshot?.SessionSequence);
            }
        }

        if (commandType == "session.reconcile.v1")
        {
            var current = (await snapshots.GetAsync(actor, routeSessionId, cancellationToken)).Snapshot
                ?? live.Snapshot;
            return new HostedSessionCommandResult(
                true,
                "accepted",
                "session.reconcile.succeeded",
                "none",
                current.PermittedActions,
                current.SessionVersion,
                current.SessionSequence);
        }

        if (commandType == "session.message.send.v1")
        {
            if (string.IsNullOrWhiteSpace(messageText) || messageText.Length > 16384)
            {
                return null;
            }

            var messageId = HostedSessionSnapshotProjector.ToStableId(commandId, "msg");
            var turnId = HostedSessionSnapshotProjector.ToStableId(commandId, "turn");
            var slotId = HostedSessionSnapshotProjector.ToStableId(commandId, "slot");
            var triggerId = HostedSessionSnapshotProjector.ToStableId(commandId, "trig");
            var admitted = await messages.AcceptAsync(
                new AcceptParticipantMessageCommand(
                    actor,
                    binding.Ownership,
                    expectedSessionVersion,
                    messageId,
                    turnId,
                    slotId,
                    triggerId,
                    idempotencyKey,
                    HostedSessionCommandCorrelation.ForCommandId(commandId),
                    "http.session_command",
                    messageText),
                binding,
                cancellationToken);
            return MapAdmission(admitted, projectionKind, messageId);
        }

        var transitions = HostedSessionLifecycleSequence.Transitions(commandType);
        if (transitions.Count == 0)
        {
            return null;
        }

        var expectedVersion = expectedSessionVersion;
        HostedSessionCommandResult? last = null;
        foreach (var transition in transitions)
        {
            var step = await lifecycle.ChangeAsync(
                new ChangeSessionLifecycleCommand(
                    actor,
                    binding.Ownership,
                    expectedVersion,
                    transition,
                    HostedSessionCommandCorrelation.ForCommandId(commandId),
                    "http.session_command",
                    transition switch
                    {
                        SessionLifecycleTransitions.Terminate => terminateReasonCode,
                        SessionLifecycleTransitions.Pause => pauseReasonCode,
                        _ => null,
                    }),
                binding,
                cancellationToken);
            last = MapLifecycle(step, projectionKind);
            if (!step.Succeeded)
            {
                return last;
            }

            expectedVersion = step.SessionVersion;
        }

        return last;
    }

    private async Task TryExpireDueAsync(
        TrustedSessionBinding binding,
        HostedSessionSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (_expiry is null
            || !HostedSessionCutoffAdmission.ShouldExpireLiveSession(
                snapshot.LifecycleState,
                snapshot.RemainingSeconds))
        {
            return;
        }

        var expiryVersion = snapshot.SessionVersion;
        foreach (var transition in new[]
                 {
                     SessionLifecycleTransitions.BeginCompleting,
                     SessionLifecycleTransitions.Complete,
                 })
        {
            var step = await lifecycle.ChangeAsync(
                new ChangeSessionLifecycleCommand(
                    _expiry.ServiceActor,
                    binding.Ownership,
                    expiryVersion,
                    transition,
                    HostedSessionCommandCorrelation.ForCommandId(
                        HostedSessionCommandCorrelation.ExpiryCommandId(
                            binding.Ownership.SessionId,
                            transition)),
                    _expiry.SourceChannel,
                    transition == SessionLifecycleTransitions.Complete
                        ? TerminalReasonCategories.TimeExpiry
                        : null),
                binding,
                cancellationToken);
            if (!step.Succeeded && step.OutcomeCode != SessionLifecycleOutcomeCodes.Reconciled)
            {
                break;
            }

            expiryVersion = step.SessionVersion;
        }
    }

    private static HostedSessionCommandResult Denied() =>
        new(false, "rejected", "session.denied", "none", []);

    private static HostedSessionCommandResult MapAdmission(
        TriggerAdmissionResult result,
        string projectionKind,
        string messageId)
    {
        var category = result.OutcomeCode switch
        {
            TriggerAdmissionOutcomeCodes.Succeeded => "accepted",
            TriggerAdmissionOutcomeCodes.Reconciled => "duplicate",
            TriggerAdmissionOutcomeCodes.StaleVersion or TriggerAdmissionOutcomeCodes.IdempotencyConflict => "conflict",
            TriggerAdmissionOutcomeCodes.CutoffPassed => "rejected",
            TriggerAdmissionOutcomeCodes.TimingUnavailable => "rejected",
            TriggerAdmissionOutcomeCodes.Denied or TriggerAdmissionOutcomeCodes.OwnershipMismatch => "rejected",
            _ => result.Succeeded ? "accepted" : "rejected",
        };
        var recovery = category switch
        {
            "conflict" => "reconcile_snapshot",
            "rejected" => "none",
            _ => "none",
        };
        var lifecycle = SessionLifecycleState.Active;
        var outcomeCode = result.OutcomeCode switch
        {
            TriggerAdmissionOutcomeCodes.CutoffPassed => "session.cutoff.passed",
            TriggerAdmissionOutcomeCodes.TimingUnavailable => "session.timing.unavailable",
            _ => result.OutcomeCode.Replace('_', '.'),
        };
        return new HostedSessionCommandResult(
            result.Succeeded,
            category,
            outcomeCode,
            recovery,
            SessionPermittedActionsProjector.Project(projectionKind, lifecycle),
            result.SessionVersion,
            result.SessionSequence,
            result.Succeeded ? messageId : null);
    }

    private static HostedSessionCommandResult MapLifecycle(
        SessionLifecycleChangeResult result,
        string projectionKind)
    {
        var category = result.OutcomeCode switch
        {
            SessionLifecycleOutcomeCodes.Succeeded => "accepted",
            SessionLifecycleOutcomeCodes.Reconciled => "duplicate",
            SessionLifecycleOutcomeCodes.StaleVersion => "conflict",
            _ => result.Succeeded ? "accepted" : "rejected",
        };
        return new HostedSessionCommandResult(
            result.Succeeded,
            category,
            result.OutcomeCode.Replace('_', '.'),
            category == "conflict" ? "reconcile_snapshot" : "none",
            SessionPermittedActionsProjector.Project(projectionKind, result.LifecycleState),
            result.SessionVersion);
    }

}

public sealed class PostgresHostedSessionSubjectSource(
    PostgresSessionActorRelationshipStore relationships,
    PostgresConnectionAccessor connectionAccessor) : IHostedSessionSubjectSource
{
    private const string ResolveAdministratorSql = """
        SELECT
            runtime.organization_id,
            runtime.participant_id
        FROM session_runtimes AS runtime
        INNER JOIN assessment_activities AS activity
            ON activity.organization_id = runtime.organization_id
           AND activity.activity_id = runtime.activity_id
        INNER JOIN assessment_activity_revisions AS revision
            ON revision.organization_id = activity.organization_id
           AND revision.activity_id = activity.activity_id
           AND revision.revision_id = activity.current_revision_id
        INNER JOIN actor_organization_grants AS grants
            ON grants.organization_id = runtime.organization_id
           AND grants.actor_id = @ActorId
           AND grants.granted_action = 'session.operations.read'
           AND grants.revoked_at IS NULL
        WHERE runtime.session_id = @SessionId
          AND revision.actor_id = @ActorId;
        """;

    public async Task<SessionEventSubject?> ResolveCurrentAsync(
        TrustedRuntimeActor actor,
        Guid untrustedSessionId,
        CancellationToken cancellationToken = default)
    {
        var current = await relationships.ResolveCurrentAsync(actor, untrustedSessionId, cancellationToken);
        if (current is not null)
        {
            return current;
        }

        if (actor.ActorId == Guid.Empty || untrustedSessionId == Guid.Empty)
        {
            return null;
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<AdministratorSubjectRow>(
            new CommandDefinition(
                ResolveAdministratorSql,
                new { ActorId = actor.ActorId, SessionId = untrustedSessionId },
                cancellationToken: cancellationToken))).AsList();
        if (rows.Count != 1 || rows[0].organization_id == Guid.Empty)
        {
            return null;
        }

        return new SessionEventSubject(
            actor.ActorId,
            actor.ActorType,
            rows[0].organization_id,
            rows[0].participant_id == Guid.Empty ? null : rows[0].participant_id,
            SessionEventSubscriptionRelationships.Administrator);
    }

    private sealed record AdministratorSubjectRow(Guid organization_id, Guid participant_id);
}

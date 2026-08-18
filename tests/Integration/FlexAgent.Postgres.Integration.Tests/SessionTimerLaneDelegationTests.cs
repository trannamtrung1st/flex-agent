using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionTimerLaneDelegationTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Due_timer_with_production_binding_admits_one_invocation()
    {
        var prepared = await InsertDelegatedSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Ownership);
        await MarkScheduleDueAsync(prepared.Ownership);
        var coordinator = CreateCoordinator(new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor));

        var result = await coordinator.TryFireNextDueAsync(FireCommand(prepared.Organization.ActorId), CancellationToken);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(TimerFireOutcomeCodes.Succeeded, result.OutcomeCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var invocationCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_invocations
            WHERE organization_id = @OrganizationId AND session_id = @SessionId;
            """,
            new { prepared.Ownership.OrganizationId, prepared.Ownership.SessionId });
        var fireAudit = await connection.QuerySingleAsync<(Guid? ReferenceId, string? ReferenceType)>(
            """
            SELECT authorization_reference_id, authorization_reference_type
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND resource_id = @SessionId
              AND action = @Action;
            """,
            new
            {
                prepared.Ownership.OrganizationId,
                prepared.Ownership.SessionId,
                Action = AuthorizationActions.FireSessionTimerLane,
            });
        Assert.Equal(1, invocationCount);
        Assert.Equal(AuthorizationReferenceTypes.ServiceDelegation, fireAudit.ReferenceType);
        Assert.Equal(prepared.DelegationId, fireAudit.ReferenceId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("wrong-service")]
    [InlineData("wrong-action")]
    [InlineData("wrong-organization")]
    [InlineData("wrong-session")]
    public async Task Invalid_delegation_denies_without_mutating_due_work(string fault)
    {
        var prepared = await InsertDelegatedSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Ownership);
        await MarkScheduleDueAsync(prepared.Ownership);
        var actorId = prepared.Organization.ActorId;
        await ApplyDelegationFaultAsync(prepared, fault);
        if (fault == "wrong-service")
        {
            actorId = await InsertActorAsync();
        }

        var before = await CaptureWorkAsync(prepared.Ownership);
        var result = await CreateCoordinator().TryFireNextDueAsync(FireCommand(actorId), CancellationToken);
        var after = await CaptureWorkAsync(prepared.Ownership);

        Assert.False(result.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.Idle, result.OutcomeCode);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Revocation_after_admission_and_before_commit_denies_without_invocation()
    {
        var prepared = await InsertDelegatedSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Ownership);
        await MarkScheduleDueAsync(prepared.Ownership);
        var coordinator = CreateCoordinator();
        coordinator.AfterAdmissionAuthorizedAsync = async () =>
        {
            Assert.True(await MutateDelegationAsync(prepared));
        };

        var before = await CaptureWorkAsync(prepared.Ownership);
        var result = await coordinator.TryFireNextDueAsync(FireCommand(prepared.Organization.ActorId), CancellationToken);
        var after = await CaptureWorkAsync(prepared.Ownership);

        Assert.False(result.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.AuthorityDenied, result.OutcomeCode);
        Assert.Equal(before.SessionVersion, after.SessionVersion);
        Assert.Equal(0, after.InvocationCount);
        Assert.Equal("pending", after.LaneState);
    }

    [Fact]
    public async Task Expiry_after_persistence_and_before_commit_denies_without_invocation()
    {
        var prepared = await InsertDelegatedSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Ownership);
        await MarkScheduleDueAsync(prepared.Ownership);
        var coordinator = CreateCoordinator();
        coordinator.AfterPersistenceBeforeCommitAsync = async () =>
        {
            await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
            var updated = await connection.ExecuteAsync(
                """
                UPDATE service_delegations
                SET expires_at = clock_timestamp() - INTERVAL '1 second'
                WHERE delegation_id = @DelegationId;
                """,
                new { prepared.DelegationId });
            Assert.Equal(1, updated);
        };

        var before = await CaptureWorkAsync(prepared.Ownership);
        var result = await coordinator.TryFireNextDueAsync(FireCommand(prepared.Organization.ActorId), CancellationToken);
        var after = await CaptureWorkAsync(prepared.Ownership);

        Assert.False(result.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.AuthorityDenied, result.OutcomeCode);
        Assert.Equal(before.SessionVersion, after.SessionVersion);
        Assert.Equal(0, after.InvocationCount);
        Assert.Equal("pending", after.LaneState);
    }

    [Fact]
    public async Task Admission_authorization_denial_rolls_back_work_and_persists_denial_audit()
    {
        var prepared = await InsertDelegatedSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Ownership);
        await MarkScheduleDueAsync(prepared.Ownership);
        var correlationId = Guid.NewGuid();
        var coordinator = CreateCoordinator();
        coordinator.AfterDueClaimedAsync = async () =>
        {
            Assert.True(await MutateDelegationAsync(prepared));
        };

        var before = await CaptureWorkAsync(prepared.Ownership);
        var result = await coordinator.TryFireNextDueAsync(
            FireCommand(prepared.Organization.ActorId, correlationId),
            CancellationToken);
        var after = await CaptureWorkAsync(prepared.Ownership);
        var denial = await ReadTimerLaneDenialAuditAsync(prepared.Ownership, correlationId);

        Assert.False(result.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.AuthorityDenied, result.OutcomeCode);
        Assert.Equal(before.SessionVersion, after.SessionVersion);
        Assert.Equal(0, after.InvocationCount);
        Assert.Equal("pending", after.LaneState);
        Assert.Equal(before.OutboxCount, after.OutboxCount);
        Assert.Equal(AuthorizationOutcomes.Deny, denial.Outcome);
        Assert.Equal(AuthorizationActions.FireSessionTimerLane, denial.Action);
        Assert.Equal(AuthorizationResourceTypes.Session, denial.ResourceType);
        Assert.Equal(prepared.Ownership.SessionId, denial.ResourceId);
        Assert.Equal(prepared.Ownership.OrganizationId, denial.OrganizationId);
        Assert.Equal(prepared.Organization.ActorId, denial.ActorId);
        Assert.Equal("synthetic.test_actor", denial.ActorType);
        Assert.Equal(AuthorizationReasonCodes.RevokedDelegation, denial.ReasonCode);
        Assert.Equal("integration.test", denial.SourceChannel);
        Assert.Equal(AuthorizationReferenceTypes.ServiceDelegation, denial.ReferenceType);
        Assert.Equal(prepared.DelegationId, denial.ReferenceId);
        Assert.Equal(0, await CountSucceededTimerLaneFireAuditsAsync(prepared.Ownership));
    }

    [Fact]
    public async Task Commit_reauthorization_denial_rolls_back_success_audit_and_persists_denial_audit()
    {
        var prepared = await InsertDelegatedSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Ownership);
        await MarkScheduleDueAsync(prepared.Ownership);
        var correlationId = Guid.NewGuid();
        var coordinator = CreateCoordinator();
        coordinator.AfterPersistenceBeforeCommitAsync = async () =>
        {
            await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
            var updated = await connection.ExecuteAsync(
                """
                UPDATE service_delegations
                SET expires_at = clock_timestamp() - INTERVAL '1 second'
                WHERE delegation_id = @DelegationId;
                """,
                new { prepared.DelegationId });
            Assert.Equal(1, updated);
        };

        var before = await CaptureWorkAsync(prepared.Ownership);
        var result = await coordinator.TryFireNextDueAsync(
            FireCommand(prepared.Organization.ActorId, correlationId),
            CancellationToken);
        var after = await CaptureWorkAsync(prepared.Ownership);
        var denial = await ReadTimerLaneDenialAuditAsync(prepared.Ownership, correlationId);

        Assert.False(result.Succeeded);
        Assert.Equal(TimerFireOutcomeCodes.AuthorityDenied, result.OutcomeCode);
        Assert.Equal(before.SessionVersion, after.SessionVersion);
        Assert.Equal(0, after.InvocationCount);
        Assert.Equal("pending", after.LaneState);
        Assert.Equal(before.OutboxCount, after.OutboxCount);
        Assert.Equal(AuthorizationOutcomes.Deny, denial.Outcome);
        Assert.Equal(AuthorizationReasonCodes.ExpiredDelegation, denial.ReasonCode);
        Assert.Equal(prepared.DelegationId, denial.ReferenceId);
        Assert.Equal(0, await CountSucceededTimerLaneFireAuditsAsync(prepared.Ownership));
    }

    [Fact]
    public async Task Denial_audit_failure_after_commit_reauthorization_deny_does_not_mutate_or_leave_audit()
    {
        var prepared = await InsertDelegatedSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Ownership);
        await MarkScheduleDueAsync(prepared.Ownership);
        var correlationId = Guid.NewGuid();
        var coordinator = CreateCoordinator(
            auditEventWriter: new DenyAuditFaultInjectingWriter(new PostgresAuditEventWriter()));
        coordinator.AfterPersistenceBeforeCommitAsync = async () =>
        {
            await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
            var updated = await connection.ExecuteAsync(
                """
                UPDATE service_delegations
                SET expires_at = clock_timestamp() - INTERVAL '1 second'
                WHERE delegation_id = @DelegationId;
                """,
                new { prepared.DelegationId });
            Assert.Equal(1, updated);
        };

        var before = await CaptureWorkAsync(prepared.Ownership);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.TryFireNextDueAsync(
                FireCommand(prepared.Organization.ActorId, correlationId),
                CancellationToken));
        var after = await CaptureWorkAsync(prepared.Ownership);

        Assert.Equal("Injected denial-audit failure.", exception.Message);
        Assert.Equal(before.SessionVersion, after.SessionVersion);
        Assert.Equal(0, after.InvocationCount);
        Assert.Equal("pending", after.LaneState);
        Assert.Equal(before.OutboxCount, after.OutboxCount);
        Assert.Equal(0, await CountSucceededTimerLaneFireAuditsAsync(prepared.Ownership));
        Assert.Equal(0, await CountTimerLaneDenialAuditsAsync(prepared.Ownership, correlationId));
    }

    [Fact]
    public async Task Issue_records_mutation_coupled_audit_against_the_authorizing_grant()
    {
        var prepared = await InsertDelegatedSessionAsync();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var grantId = await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT grant_id
            FROM actor_organization_grants
            WHERE organization_id = @OrganizationId
              AND actor_id = @ActorId
              AND granted_action = @GrantedAction
              AND revoked_at IS NULL;
            """,
            new
            {
                prepared.Ownership.OrganizationId,
                ActorId = prepared.Organization.ActorId,
                GrantedAction = AuthorizationActions.IssueServiceDelegation,
            });
        var audit = await connection.QuerySingleAsync<(
            string Action,
            string ResourceType,
            Guid ResourceId,
            Guid ActorId,
            Guid CorrelationId,
            string SourceChannel,
            string? ReferenceType,
            Guid? ReferenceId,
            long? Version)>(
            """
            SELECT action, resource_type, resource_id, actor_id, correlation_id, source_channel,
                   authorization_reference_type, authorization_reference_id, relationship_version
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND resource_id = @DelegationId;
            """,
            new
            {
                prepared.Ownership.OrganizationId,
                Action = AuthorizationActions.IssueServiceDelegation,
                prepared.DelegationId,
            });
        var transition = await connection.QuerySingleAsync<(string Kind, string? PreviousAction, string NewAction)>(
            """
            SELECT mutation_kind, previous_allowed_action, new_allowed_action
            FROM service_delegation_transitions
            WHERE organization_id = @OrganizationId AND delegation_id = @DelegationId;
            """,
            new { prepared.Ownership.OrganizationId, prepared.DelegationId });
        Assert.Equal(AuthorizationActions.IssueServiceDelegation, audit.Action);
        Assert.Equal(AuthorizationResourceTypes.ServiceDelegation, audit.ResourceType);
        Assert.Equal(prepared.DelegationId, audit.ResourceId);
        Assert.Equal(prepared.Organization.ActorId, audit.ActorId);
        Assert.Equal(prepared.CorrelationId, audit.CorrelationId);
        Assert.Equal("session.start", audit.SourceChannel);
        Assert.Equal(AuthorizationReferenceTypes.ActorOrganizationGrant, audit.ReferenceType);
        Assert.Equal(grantId, audit.ReferenceId);
        Assert.Equal(1, audit.Version);
        Assert.Equal("issue", transition.Kind);
        Assert.Null(transition.PreviousAction);
        Assert.Equal(AuthorizationActions.FireSessionTimerLane, transition.NewAction);
    }

    [Fact]
    public async Task Issue_without_grant_is_denied_and_does_not_insert_a_session()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var startedAt = new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
        var session = SessionRuntime.CreateActive(binding, startedAt);
        var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
        var denied = await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken,
                CreateAuthorizedIssue(
                    organization,
                    Guid.NewGuid(),
                    organization.ActorId,
                    clock,
                    Guid.NewGuid()),
                CommitKernel()));
        try
        {
            await scope.CommitAsync(CancellationToken);
        }
        catch (Exception)
        {
        }

        Assert.Equal(AuthorizationReasonCodes.DeniedNoGrant, denied.ReasonCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var sessionCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM session_runtimes
            WHERE organization_id = @OrganizationId AND session_id = @SessionId;
            """,
            new { binding.Ownership.OrganizationId, binding.Ownership.SessionId });
        Assert.Equal(0, sessionCount);
    }

    [Fact]
    public async Task Commit_after_final_reauthorization_denial_cannot_persist_session_or_delegation()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.IssueServiceDelegation);
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        var delegationId = Guid.NewGuid();
        PostgresSessionRuntimeRepository.AfterPersistenceBeforeDelegationReauthAsync = async () =>
        {
            await new PostgresGrantRepository(Fixture.Services.ConnectionAccessor).RevokeAsync(
                organization.OrganizationId,
                organization.ActorId,
                AuthorizationActions.IssueServiceDelegation,
                CancellationToken);
        };

        try
        {
            await using var scope = await PostgresTransactionScope.BeginAsync(
                Fixture.Services.ConnectionAccessor,
                CancellationToken);
            var startedAt = new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
            var session = SessionRuntime.CreateActive(binding, startedAt);
            var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            var denied = await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
                repository.InsertActiveAsync(
                    binding.Ownership,
                    session,
                    SessionPersistenceFixtures.Actor(organization.ActorId),
                    scope.Transaction,
                    CancellationToken,
                    CreateAuthorizedIssue(
                        organization,
                        delegationId,
                        organization.ActorId,
                        clock,
                        Guid.NewGuid()),
                    CommitKernel()));
            try
            {
                await scope.CommitAsync(CancellationToken.None);
            }
            catch (Exception)
            {
            }

            Assert.Equal(AuthorizationReasonCodes.DeniedNoGrant, denied.ReasonCode);
        }
        finally
        {
            PostgresSessionRuntimeRepository.AfterPersistenceBeforeDelegationReauthAsync = null;
        }

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var counts = await connection.QuerySingleAsync<(int Sessions, int Delegations, int Audits)>(
            """
            SELECT
                (SELECT COUNT(*) FROM session_runtimes
                 WHERE organization_id = @OrganizationId AND session_id = @SessionId)::int,
                (SELECT COUNT(*) FROM service_delegations
                 WHERE organization_id = @OrganizationId AND delegation_id = @DelegationId)::int,
                (SELECT COUNT(*) FROM audit_events
                 WHERE organization_id = @OrganizationId AND resource_id = @DelegationId)::int;
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
                DelegationId = delegationId,
            });
        Assert.Equal(0, counts.Sessions);
        Assert.Equal(0, counts.Delegations);
        Assert.Equal(0, counts.Audits);
    }

    [Fact]
    public async Task Canceled_request_cannot_prevent_abort_after_final_authorization_denial()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.IssueServiceDelegation);
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        var delegationId = Guid.NewGuid();
        using var canceled = new CancellationTokenSource();
        var kernel = new CancelOnFinalDenyKernel(
            CommitKernel(),
            canceled,
            organization.OrganizationId,
            organization.ActorId,
            Fixture.Services.ConnectionAccessor);

        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken.None);
        var startedAt = new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
        var session = SessionRuntime.CreateActive(binding, startedAt);
        var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken.None);
        var denied = await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                canceled.Token,
                CreateAuthorizedIssue(
                    organization,
                    delegationId,
                    organization.ActorId,
                    clock,
                    Guid.NewGuid()),
                kernel));
        try
        {
            await scope.CommitAsync(CancellationToken.None);
        }
        catch (Exception)
        {
        }

        Assert.Equal(AuthorizationReasonCodes.DeniedNoGrant, denied.ReasonCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken.None);
        var counts = await connection.QuerySingleAsync<(int Sessions, int Delegations, int Audits)>(
            """
            SELECT
                (SELECT COUNT(*) FROM session_runtimes
                 WHERE organization_id = @OrganizationId AND session_id = @SessionId)::int,
                (SELECT COUNT(*) FROM service_delegations
                 WHERE organization_id = @OrganizationId AND delegation_id = @DelegationId)::int,
                (SELECT COUNT(*) FROM audit_events
                 WHERE organization_id = @OrganizationId AND resource_id = @DelegationId)::int;
            """,
            new
            {
                binding.Ownership.OrganizationId,
                binding.Ownership.SessionId,
                DelegationId = delegationId,
            });
        Assert.Equal(0, counts.Sessions);
        Assert.Equal(0, counts.Delegations);
        Assert.Equal(0, counts.Audits);
    }

    [Fact]
    public async Task Revoke_without_grant_is_denied_and_leaves_the_delegation_active()
    {
        var prepared = await InsertDelegatedSessionAsync(grantRevoke: false);
        var denied = await Assert.ThrowsAsync<AuthorizationDeniedException>(() => MutateDelegationAsync(prepared));
        Assert.Equal(AuthorizationReasonCodes.DeniedNoGrant, denied.ReasonCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var revokedAt = await connection.ExecuteScalarAsync<DateTime?>(
            """
            SELECT revoked_at
            FROM service_delegations
            WHERE delegation_id = @DelegationId;
            """,
            new { prepared.DelegationId });
        Assert.Null(revokedAt);
    }

    [Fact]
    public async Task Revoke_records_mutation_coupled_audit_against_the_authorizing_grant()
    {
        var prepared = await InsertDelegatedSessionAsync();
        Assert.True(await MutateDelegationAsync(prepared));
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var grantId = await connection.ExecuteScalarAsync<Guid>(
            """
            SELECT grant_id
            FROM actor_organization_grants
            WHERE organization_id = @OrganizationId
              AND actor_id = @ActorId
              AND granted_action = @GrantedAction
              AND revoked_at IS NULL;
            """,
            new
            {
                prepared.Ownership.OrganizationId,
                ActorId = prepared.Organization.ActorId,
                GrantedAction = AuthorizationActions.RevokeServiceDelegation,
            });
        var audit = await connection.QuerySingleAsync<(string ResourceType, Guid ResourceId, string? ReferenceType, Guid? ReferenceId)>(
            """
            SELECT resource_type, resource_id, authorization_reference_type, authorization_reference_id
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND action = @Action
              AND resource_id = @DelegationId;
            """,
            new
            {
                prepared.Ownership.OrganizationId,
                Action = AuthorizationActions.RevokeServiceDelegation,
                prepared.DelegationId,
            });
        Assert.Equal(AuthorizationResourceTypes.ServiceDelegation, audit.ResourceType);
        Assert.Equal(prepared.DelegationId, audit.ResourceId);
        Assert.Equal(AuthorizationReferenceTypes.ActorOrganizationGrant, audit.ReferenceType);
        Assert.Equal(grantId, audit.ReferenceId);
    }

    [Fact]
    public async Task Historical_schedule_without_delegation_stays_pending()
    {
        var prepared = await InsertDelegatedSessionAsync();
        await using var otherDue = await HoldOtherDueSchedulesAsync(prepared.Ownership);
        await MarkScheduleDueAsync(prepared.Ownership);
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            var updated = await connection.ExecuteAsync(
                """
                UPDATE session_timer_schedules
                SET timer_lane_delegation_id = NULL
                WHERE organization_id = @OrganizationId AND session_id = @SessionId;
                """,
                new { prepared.Ownership.OrganizationId, prepared.Ownership.SessionId });
            Assert.Equal(1, updated);
        }

        var first = await CreateCoordinator().TryFireNextDueAsync(
            FireCommand(prepared.Organization.ActorId),
            CancellationToken);
        var second = await CreateCoordinator().TryFireNextDueAsync(
            FireCommand(prepared.Organization.ActorId),
            CancellationToken);

        Assert.Equal(TimerFireOutcomeCodes.Idle, first.OutcomeCode);
        Assert.Equal(TimerFireOutcomeCodes.Idle, second.OutcomeCode);
        var after = await CaptureWorkAsync(prepared.Ownership);
        Assert.Equal(0, after.InvocationCount);
        Assert.Equal("pending", after.LaneState);
    }

    [Fact]
    public async Task Revoked_due_row_does_not_head_of_line_block_another_session()
    {
        var blocked = await InsertDelegatedSessionAsync();
        var eligible = await InsertDelegatedSessionAsync(blocked.Organization.ActorId);
        await using var otherDue = await HoldOtherDueSchedulesAsync(blocked.Ownership, eligible.Ownership);
        await MarkScheduleDueAsync(blocked.Ownership, olderBy: TimeSpan.FromMinutes(2));
        await MarkScheduleDueAsync(eligible.Ownership);
        await ApplyDelegationFaultAsync(blocked, "revoked");
        var coordinator = CreateCoordinator(new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor));

        var first = await coordinator.TryFireNextDueAsync(
            FireCommand(blocked.Organization.ActorId),
            CancellationToken);
        var second = await coordinator.TryFireNextDueAsync(
            FireCommand(blocked.Organization.ActorId),
            CancellationToken);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.Equal(eligible.Ownership.SessionId, first.Admission!.Invocation!.Ownership.SessionId);
        Assert.Equal(TimerFireOutcomeCodes.Idle, second.OutcomeCode);
        var blockedAfter = await CaptureWorkAsync(blocked.Ownership);
        Assert.Equal(0, blockedAfter.InvocationCount);
        Assert.Equal("pending", blockedAfter.LaneState);
    }

    private async Task ApplyDelegationFaultAsync(PreparedDelegatedSession prepared, string fault)
    {
        switch (fault)
        {
            case "missing":
                await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
                {
                    await connection.ExecuteAsync(
                        """
                        UPDATE session_timer_schedules
                        SET timer_lane_delegation_id = NULL
                        WHERE organization_id = @OrganizationId AND session_id = @SessionId;
                        """,
                        new { prepared.Ownership.OrganizationId, prepared.Ownership.SessionId });
                }

                break;
            case "expired":
                await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
                {
                    await connection.ExecuteAsync(
                        """
                        UPDATE service_delegations
                        SET
                            effective_at = clock_timestamp() - INTERVAL '2 minutes',
                            expires_at = clock_timestamp() - INTERVAL '1 minute'
                        WHERE delegation_id = @DelegationId;
                        """,
                        new { prepared.DelegationId });
                }

                break;
            case "revoked":
                Assert.True(await MutateDelegationAsync(prepared));
                break;
            case "wrong-service":
                break;
            case "wrong-action":
                await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
                {
                    var updated = await connection.ExecuteAsync(
                        """
                        UPDATE service_delegations
                        SET allowed_action = @AllowedAction
                        WHERE delegation_id = @DelegationId;
                        """,
                        new
                        {
                            prepared.DelegationId,
                            AllowedAction = AuthorizationActions.SubscribeSessionEvents,
                        });
                    Assert.Equal(1, updated);
                }

                break;
            case "wrong-organization":
            case "wrong-session":
                var other = await InsertDelegatedSessionAsync();
                await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
                {
                    var updated = await connection.ExecuteAsync(
                        """
                        UPDATE session_timer_schedules
                        SET timer_lane_delegation_id = @OtherDelegationId
                        WHERE organization_id = @OrganizationId AND session_id = @SessionId;
                        """,
                        new
                        {
                            prepared.Ownership.OrganizationId,
                            prepared.Ownership.SessionId,
                            OtherDelegationId = other.DelegationId,
                        });
                    Assert.Equal(1, updated);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown delegation fault.");
        }
    }

    private async Task<PreparedDelegatedSession> InsertDelegatedSessionAsync(
        Guid? timerServiceActorId = null,
        bool grantRevoke = true)
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.IssueServiceDelegation);
        if (grantRevoke)
        {
            await Fixture.GrantOrganizationActionAsync(
                organization.OrganizationId,
                organization.ActorId,
                AuthorizationActions.RevokeServiceDelegation);
        }

        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        var delegationId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var startedAt = new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
            var session = SessionRuntime.CreateActive(binding, startedAt);
            var delegationClock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            await repository.InsertActiveAsync(
                binding.Ownership,
                session,
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken,
                CreateAuthorizedIssue(
                    organization,
                    delegationId,
                    timerServiceActorId ?? organization.ActorId,
                    delegationClock,
                    correlationId),
                CommitKernel(),
                InvocationExecuteDelegationSupport.CreateIssue(
                    organization,
                    timerServiceActorId ?? organization.ActorId,
                    delegationClock));
            await scope.CommitAsync(CancellationToken);
        }

        return new PreparedDelegatedSession(
            organization,
            binding.Ownership,
            binding,
            delegationId,
            correlationId,
            repository);
    }

    [Fact]
    public void Timer_lane_fire_issue_requires_bounded_expiry()
    {
        var startedAt = new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
        var missingExpiry = new ServiceDelegationIssue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AuthorizationActions.FireSessionTimerLane,
            "session.timer_lane.scheduler",
            "system.session_runtime",
            startedAt);
        var overLong = missingExpiry with { ExpiresAt = startedAt.AddDays(8) };
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PostgresServiceDelegationCoordinator.ValidateTimerLaneFireLifetime(missingExpiry));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PostgresServiceDelegationCoordinator.ValidateTimerLaneFireLifetime(overLong));
    }

    private async Task<bool> MutateDelegationAsync(PreparedDelegatedSession prepared)
    {
        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var mutated = await PostgresServiceDelegationCoordinator.RevokeInTransactionAsync(
            prepared.Ownership.OrganizationId,
            prepared.Ownership.SessionId,
            prepared.DelegationId,
            new ServiceDelegationMutationContext(
                new TrustedActor(prepared.Organization.ActorId, "integration.test"),
                Guid.NewGuid(),
                "integration.test",
                "timer.lane.test.revoke"),
            CommitKernel(),
            scope.Transaction,
            CancellationToken);
        await scope.CommitAsync(CancellationToken);
        return mutated;
    }

    private static AuthorizedServiceDelegationIssue CreateAuthorizedIssue(
        SeededOrganization organization,
        Guid delegationId,
        Guid serviceActorId,
        DateTimeOffset clock,
        Guid correlationId) =>
        new(
            new ServiceDelegationIssue(
                delegationId,
                serviceActorId,
                AuthorizationActions.FireSessionTimerLane,
                "session.timer_lane.scheduler",
                "system.session_runtime",
                clock.AddMinutes(-1),
                clock.AddDays(6)),
            new ServiceDelegationMutationContext(
                new TrustedActor(organization.ActorId, "integration.test"),
                correlationId,
                "session.start",
                "session.start.timer_lane"));

    private async Task MarkScheduleDueAsync(SessionOwnership ownership, TimeSpan? olderBy = null)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var delay = olderBy ?? TimeSpan.FromSeconds(1);
        var updated = await connection.ExecuteAsync(
            """
            UPDATE session_timer_schedules
            SET fire_at = clock_timestamp() - @OlderBy
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND state = 'pending';
            """,
            new { ownership.OrganizationId, ownership.SessionId, OlderBy = delay });
        Assert.Equal(1, updated);
    }

    private Task<IAsyncDisposable> HoldOtherDueSchedulesAsync(SessionOwnership ownership) =>
        HoldOtherDueSchedulesAsync(ownership, other: null);

    private async Task<IAsyncDisposable> HoldOtherDueSchedulesAsync(
        SessionOwnership ownership,
        SessionOwnership? other)
    {
        var connection = new Npgsql.NpgsqlConnection(Fixture.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        var transaction = await connection.BeginTransactionAsync(CancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                SELECT schedule_revision
                FROM session_timer_schedules
                WHERE NOT (
                        (organization_id = @OrganizationId AND session_id = @SessionId)
                        OR (
                            @OtherOrganizationId IS NOT NULL
                            AND organization_id = @OtherOrganizationId
                            AND session_id = @OtherSessionId
                        )
                      )
                  AND (
                        (state = 'pending' AND fire_at IS NOT NULL AND fire_at <= clock_timestamp())
                        OR state = 'claimed'
                      )
                FOR UPDATE;
                """,
                new
                {
                    ownership.OrganizationId,
                    ownership.SessionId,
                    OtherOrganizationId = other?.OrganizationId,
                    OtherSessionId = other?.SessionId,
                },
                transaction,
                cancellationToken: CancellationToken));
        return new HeldDueScope(connection, transaction);
    }

    private async Task<WorkSnapshot> CaptureWorkAsync(SessionOwnership ownership)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var row = await connection.QuerySingleAsync<(long SessionVersion, int InvocationCount, string LaneState, int AuditCount, int OutboxCount)>(
            """
            SELECT
                runtime.session_version,
                (SELECT COUNT(*) FROM session_invocations i
                 WHERE i.organization_id = runtime.organization_id AND i.session_id = runtime.session_id)::int,
                schedule.lane_state,
                (SELECT COUNT(*) FROM audit_events a
                 WHERE a.organization_id = runtime.organization_id AND a.resource_id = runtime.session_id)::int,
                (SELECT COUNT(*) FROM outbox_items o
                 WHERE o.organization_id = runtime.organization_id AND o.aggregate_id = runtime.session_id)::int
            FROM session_runtimes AS runtime
            INNER JOIN session_timer_schedules AS schedule
                ON schedule.organization_id = runtime.organization_id
               AND schedule.session_id = runtime.session_id
               AND schedule.state IN ('pending', 'claimed')
            WHERE runtime.organization_id = @OrganizationId
              AND runtime.session_id = @SessionId;
            """,
            new { ownership.OrganizationId, ownership.SessionId });
        return new WorkSnapshot(row.SessionVersion, row.InvocationCount, row.LaneState, row.AuditCount, row.OutboxCount);
    }

    private async Task<(
        string Outcome,
        string Action,
        string ResourceType,
        Guid ResourceId,
        Guid OrganizationId,
        Guid ActorId,
        string ActorType,
        string? ReasonCode,
        string SourceChannel,
        string? ReferenceType,
        Guid? ReferenceId)> ReadTimerLaneDenialAuditAsync(
        SessionOwnership ownership,
        Guid correlationId)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        return await connection.QuerySingleAsync<(
            string Outcome,
            string Action,
            string ResourceType,
            Guid ResourceId,
            Guid OrganizationId,
            Guid ActorId,
            string ActorType,
            string? ReasonCode,
            string SourceChannel,
            string? ReferenceType,
            Guid? ReferenceId)>(
            """
            SELECT
                outcome,
                action,
                resource_type,
                resource_id,
                organization_id,
                actor_id,
                actor_type,
                reason_code,
                source_channel,
                authorization_reference_type,
                authorization_reference_id
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND resource_id = @SessionId
              AND correlation_id = @CorrelationId
              AND action = @Action
              AND outcome = @Outcome;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
                CorrelationId = correlationId,
                Action = AuthorizationActions.FireSessionTimerLane,
                Outcome = AuthorizationOutcomes.Deny,
            });
    }

    private async Task<int> CountSucceededTimerLaneFireAuditsAsync(SessionOwnership ownership)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND resource_id = @SessionId
              AND action = @Action
              AND outcome = @Outcome;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
                Action = AuthorizationActions.FireSessionTimerLane,
                Outcome = "succeeded",
            });
    }

    private async Task<int> CountTimerLaneDenialAuditsAsync(SessionOwnership ownership, Guid correlationId)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND resource_id = @SessionId
              AND correlation_id = @CorrelationId
              AND action = @Action
              AND outcome = @Outcome;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
                CorrelationId = correlationId,
                Action = AuthorizationActions.FireSessionTimerLane,
                Outcome = AuthorizationOutcomes.Deny,
            });
    }

    private async Task<Guid> InsertActorAsync()
    {
        var actorId = Guid.NewGuid();
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        await connection.ExecuteAsync(
            "INSERT INTO actors (id, created_at) VALUES (@ActorId, clock_timestamp());",
            new { ActorId = actorId });
        return actorId;
    }

    private PostgresFireDueTimerCoordinator CreateCoordinator(
        ITrustedSessionBindingSource? bindingSource = null,
        IAuditEventWriter? auditEventWriter = null) =>
        new(
            Fixture.Services.ConnectionAccessor,
            new PostgresSessionRuntimeRepository(),
            bindingSource ?? new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor),
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            auditEventWriter);

    private ICommitAuthorizationKernel CommitKernel() =>
        (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel;

    private sealed class CancelOnFinalDenyKernel(
        ICommitAuthorizationKernel inner,
        CancellationTokenSource canceled,
        Guid organizationId,
        Guid actorId,
        PostgresConnectionAccessor connectionAccessor) : ICommitAuthorizationKernel
    {
        public Task<AuthorizationDecision> AuthorizeAsync(
            AuthorizationRequest request,
            CancellationToken cancellationToken = default) =>
            inner.AuthorizeAsync(request, cancellationToken);

        public Task<AuthorizationDecision> AuthorizeInTransactionAsync(
            AuthorizationRequest request,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default) =>
            inner.AuthorizeInTransactionAsync(request, transaction, cancellationToken);

        public async Task<AuthorizationDecision> ReauthorizeInTransactionAsync(
            AuthorizationRequest request,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            await new PostgresGrantRepository(connectionAccessor).RevokeAsync(
                organizationId,
                actorId,
                AuthorizationActions.IssueServiceDelegation,
                CancellationToken.None);
            var decision = await inner.ReauthorizeInTransactionAsync(request, transaction, cancellationToken);
            if (!decision.IsPermitted)
            {
                canceled.Cancel();
            }

            return decision;
        }
    }

    private FireDueTimerCommand FireCommand(Guid actorId, Guid? correlationId = null) =>
        new(SessionPersistenceFixtures.Actor(actorId), correlationId ?? Guid.NewGuid(), "integration.test");

    private sealed record PreparedDelegatedSession(
        SeededOrganization Organization,
        SessionOwnership Ownership,
        TrustedSessionBinding Binding,
        Guid DelegationId,
        Guid CorrelationId,
        PostgresSessionRuntimeRepository Repository);

    private sealed record WorkSnapshot(
        long SessionVersion,
        int InvocationCount,
        string LaneState,
        int AuditCount,
        int OutboxCount);

    private sealed class DenyAuditFaultInjectingWriter(IAuditEventWriter inner) : IAuditEventWriter
    {
        public Task InsertAsync(
            AuditEventWriteModel auditEvent,
            NpgsqlTransaction transaction,
            CancellationToken cancellationToken = default)
        {
            if (auditEvent.Outcome == AuthorizationOutcomes.Deny)
            {
                throw new InvalidOperationException("Injected denial-audit failure.");
            }

            return inner.InsertAsync(auditEvent, transaction, cancellationToken);
        }
    }

    private sealed class HeldDueScope(Npgsql.NpgsqlConnection connection, Npgsql.NpgsqlTransaction transaction)
        : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}

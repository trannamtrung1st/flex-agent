using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class WorkerInvocationExecuteDelegationTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Historical_work_without_delegation_is_not_claimed()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var workerActorId = await Fixture.SeedWorkerActorAsync();
        await InvocationExecuteDelegationSupport.InsertSessionWithExecutionDelegationAsync(
            Fixture,
            organization,
            binding,
            CancellationToken,
            workerActorId);
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            var inserted = await connection.ExecuteAsync(
                """
                INSERT INTO session_durable_work (
                    organization_id, activity_id, participant_id, attempt_id, session_id,
                    work_id, work_type, business_key, state)
                VALUES (
                    @OrganizationId, @ActivityId, @ParticipantId, @AttemptId, @SessionId,
                    @WorkId, @WorkType, 'ainv.historical.null0001', 'pending');
                """,
                new
                {
                    binding.Ownership.OrganizationId,
                    binding.Ownership.ActivityId,
                    binding.Ownership.ParticipantId,
                    binding.Ownership.AttemptId,
                    binding.Ownership.SessionId,
                    WorkId = Guid.NewGuid(),
                    WorkType = DurableSessionWorkTypes.ExecuteInvocation,
                });
            Assert.Equal(1, inserted);
        }

        var store = new PostgresDurableInvocationWorkStore(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.Actor(workerActorId),
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel);
        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);

        Assert.Null(claimed);
    }

    [Fact]
    public async Task Principal_binding_provision_is_audited_and_stores_no_secret()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.ProvisionServicePrincipalBinding);
        var workerActorId = await Fixture.SeedWorkerActorAsync();
        var bindingId = Guid.NewGuid();
        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        await PostgresServicePrincipalBindingCoordinator.ProvisionInTransactionAsync(
            organization.OrganizationId,
            new ServicePrincipalBindingProvision(
                bindingId,
                WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                "https://issuer.example/realms/flex-agent",
                "worker-client",
                "worker-client",
                "flex-agent-worker",
                workerActorId,
                "worker.session_runtime",
                DateTimeOffset.UtcNow),
            new ServiceDelegationMutationContext(
                organization.Actor,
                Guid.NewGuid(),
                "operator.command",
                "provision.worker.binding"),
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            scope.Transaction,
            CancellationToken);
        await scope.CommitAsync(CancellationToken);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var stored = await connection.QuerySingleAsync<(Guid ActorId, string Issuer, string Subject)>(
            """
            SELECT service_actor_id, issuer, external_subject
            FROM service_principal_bindings
            WHERE binding_id = @BindingId;
            """,
            new { BindingId = bindingId });
        var audit = await connection.QuerySingleAsync<(string Action, string ResourceType)>(
            """
            SELECT action, resource_type
            FROM audit_events
            WHERE organization_id = @OrganizationId
              AND resource_id = @BindingId;
            """,
            new { organization.OrganizationId, BindingId = bindingId });
        Assert.Equal(workerActorId, stored.ActorId);
        Assert.Equal("worker-client", stored.Subject);
        Assert.Equal(AuthorizationActions.ProvisionServicePrincipalBinding, audit.Action);
        Assert.Equal(AuthorizationResourceTypes.ServicePrincipalBinding, audit.ResourceType);
        var secretish = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM audit_events
            WHERE payload_digest ILIKE '%secret%'
               OR payload_digest ILIKE '%eyJ%';
            """);
        Assert.Equal(0, secretish);
    }

    [Fact]
    public async Task Principal_binding_revoke_stops_current_lookup()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.ProvisionServicePrincipalBinding);
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.RevokeServicePrincipalBinding);
        var workerActorId = await Fixture.SeedWorkerActorAsync();
        var bindingId = Guid.NewGuid();
        var mutation = new ServiceDelegationMutationContext(
            organization.Actor,
            Guid.NewGuid(),
            "operator.command",
            "revoke.worker.binding");
        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        await PostgresServicePrincipalBindingCoordinator.ProvisionInTransactionAsync(
            organization.OrganizationId,
            new ServicePrincipalBindingProvision(
                bindingId,
                WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                "https://issuer.example/realms/flex-agent",
                "worker-client-revoke",
                "worker-client-revoke",
                "flex-agent-worker",
                workerActorId,
                "worker.session_runtime",
                DateTimeOffset.UtcNow),
            mutation,
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            scope.Transaction,
            CancellationToken);
        await PostgresServicePrincipalBindingCoordinator.RevokeInTransactionAsync(
            organization.OrganizationId,
            bindingId,
            mutation,
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            scope.Transaction,
            CancellationToken);
        await scope.CommitAsync(CancellationToken);

        await using var lookup = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var current = await PostgresServicePrincipalBindingCoordinator.LoadCurrentAsync(
            WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
            "https://issuer.example/realms/flex-agent",
            "worker-client-revoke",
            "flex-agent-worker",
            lookup.Transaction,
            CancellationToken);
        await lookup.RollbackAsync(CancellationToken);
        Assert.Null(current);
    }

    [Fact]
    public async Task Principal_binding_replace_changes_the_current_actor()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.ProvisionServicePrincipalBinding);
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.ReplaceServicePrincipalBinding);
        var originalActorId = await Fixture.SeedWorkerActorAsync();
        var replacementActorId = await Fixture.SeedWorkerActorAsync();
        var bindingId = Guid.NewGuid();
        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        await PostgresServicePrincipalBindingCoordinator.ProvisionInTransactionAsync(
            organization.OrganizationId,
            new ServicePrincipalBindingProvision(
                bindingId,
                WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                "https://issuer.example/realms/flex-agent",
                "worker-client-replace",
                "worker-client-replace",
                "flex-agent-worker",
                originalActorId,
                "worker.session_runtime",
                DateTimeOffset.UtcNow),
            new ServiceDelegationMutationContext(
                organization.Actor,
                Guid.NewGuid(),
                "operator.command",
                "replace.worker.binding"),
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            scope.Transaction,
            CancellationToken);
        await PostgresServicePrincipalBindingCoordinator.ReplaceInTransactionAsync(
            organization.OrganizationId,
            bindingId,
            replacementActorId,
            new ServiceDelegationMutationContext(
                organization.Actor,
                Guid.NewGuid(),
                "operator.command",
                "replace.worker.binding"),
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            scope.Transaction,
            CancellationToken);
        await scope.CommitAsync(CancellationToken);

        await using var lookup = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var current = await PostgresServicePrincipalBindingCoordinator.LoadCurrentAsync(
            WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
            "https://issuer.example/realms/flex-agent",
            "worker-client-replace",
            "flex-agent-worker",
            lookup.Transaction,
            CancellationToken);
        await lookup.RollbackAsync(CancellationToken);
        Assert.NotNull(current);
        Assert.Equal(replacementActorId, current!.ServiceActorId);
        Assert.Equal(2, current.BindingVersion);
    }

    [Fact]
    public async Task Expired_workload_identity_does_not_claim_authorized_work()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var workerActorId = await Fixture.SeedWorkerActorAsync();
        await InvocationExecuteDelegationSupport.InsertSessionWithExecutionDelegationAsync(
            Fixture,
            organization,
            binding,
            CancellationToken,
            workerActorId);
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            new PostgresSessionRuntimeRepository(),
            new AdmitTrustedTriggerHandler());
        var admitted = await coordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger("trig.identity.expired"),
                "idem.identity.expired",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var store = new PostgresDurableInvocationWorkStore(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.Actor(workerActorId),
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            new ExpiredWorkloadIdentitySource(workerActorId));
        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        Assert.Null(claimed);
    }

    [Fact]
    public async Task Cached_oauth_proof_cannot_claim_after_principal_binding_revoke()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.ProvisionServicePrincipalBinding);
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.RevokeServicePrincipalBinding);
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var workerActorId = await Fixture.SeedWorkerActorAsync();
        var principalBindingId = Guid.NewGuid();
        var mutation = new ServiceDelegationMutationContext(
            organization.Actor,
            Guid.NewGuid(),
            "operator.command",
            "revoke.cached.worker.binding");
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await PostgresServicePrincipalBindingCoordinator.ProvisionInTransactionAsync(
                organization.OrganizationId,
                new ServicePrincipalBindingProvision(
                    principalBindingId,
                    WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                    WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                    "https://issuer.example/realms/flex-agent",
                    "worker-client-cached-revoke",
                    "worker-client-cached-revoke",
                    "flex-agent-worker",
                    workerActorId,
                    "worker.session_runtime",
                    DateTimeOffset.UtcNow),
                mutation,
                (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
                scope.Transaction,
                CancellationToken);
            await PostgresServicePrincipalBindingCoordinator.RevokeInTransactionAsync(
                organization.OrganizationId,
                principalBindingId,
                mutation,
                (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await InvocationExecuteDelegationSupport.InsertSessionWithExecutionDelegationAsync(
            Fixture,
            organization,
            binding,
            CancellationToken,
            workerActorId);
        var coordinator = new PostgresAdmitTrustedTriggerCoordinator(
            Fixture.Services.ConnectionAccessor,
            new PostgresSessionRuntimeRepository(),
            new AdmitTrustedTriggerHandler());
        var admitted = await coordinator.AdmitAsync(
            new AdmitTrustedTriggerCommand(
                SessionPersistenceFixtures.Actor(organization.ActorId),
                binding.Ownership,
                0,
                SessionPersistenceFixtures.OpeningTrigger("trig.identity.cached-revoke"),
                "idem.identity.cached-revoke",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var identity = new CachedOAuthWorkloadIdentitySource(workerActorId, principalBindingId, 1);
        var store = new PostgresDurableInvocationWorkStore(
            Fixture.Services.ConnectionAccessor,
            SessionPersistenceFixtures.Actor(workerActorId),
            (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            identity);
        var claimed = await store.TryClaimExecuteInvocationAsync(TimeSpan.FromSeconds(30), CancellationToken);
        Assert.Null(claimed);
    }

    [Fact]
    public async Task Model_disclosure_admission_denies_after_principal_binding_revoke()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.ProvisionServicePrincipalBinding);
        await Fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.RevokeServicePrincipalBinding);
        var sessionBinding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var workerActorId = await Fixture.SeedWorkerActorAsync();
        var principalBindingId = Guid.NewGuid();
        var mutation = new ServiceDelegationMutationContext(
            organization.Actor,
            Guid.NewGuid(),
            "operator.command",
            "revoke.disclosure.worker.binding");
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await PostgresServicePrincipalBindingCoordinator.ProvisionInTransactionAsync(
                organization.OrganizationId,
                new ServicePrincipalBindingProvision(
                    principalBindingId,
                    WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                    WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                    "https://issuer.example/realms/flex-agent",
                    "worker-client-disclosure",
                    "worker-client-disclosure",
                    "flex-agent-worker",
                    workerActorId,
                    "worker.session_runtime",
                    DateTimeOffset.UtcNow),
                mutation,
                (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        await InvocationExecuteDelegationSupport.InsertSessionWithExecutionDelegationAsync(
            Fixture,
            organization,
            sessionBinding,
            CancellationToken,
            workerActorId);
        var identity = new CachedOAuthWorkloadIdentitySource(workerActorId, principalBindingId, 1);
        var bindingSource = new MemoryTrustedSessionBindingSource();
        bindingSource.Register(sessionBinding);
        var settings = new DurableInvocationWorkSettings(
            SessionPersistenceFixtures.Actor(workerActorId),
            "worker.session_runtime",
            65_536);
        var gateway = new PostgresInvocationWorkSessionGateway(
            Fixture.Services.ConnectionAccessor,
            new PostgresSessionRuntimeRepository(),
            bindingSource,
            settings,
            authorizationKernel: (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
            workloadIdentity: identity);

        var loaded = await gateway.LoadAsync(sessionBinding.Ownership, CancellationToken);
        Assert.NotNull(loaded);

        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await PostgresServicePrincipalBindingCoordinator.RevokeInTransactionAsync(
                organization.OrganizationId,
                principalBindingId,
                mutation,
                (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        Assert.False(await gateway.TryAuthorizeModelDisclosureAsync(
            sessionBinding.Ownership,
            CancellationToken));
    }

    private class CachedOAuthWorkloadIdentitySource(
        Guid actorId,
        Guid bindingId,
        long bindingVersion,
        bool expired = false) : IAuthenticatedWorkloadContextSource
    {
        public Task<AuthenticatedWorkloadContext?> TryGetCurrentAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult<AuthenticatedWorkloadContext?>(
                new AuthenticatedWorkloadContext(
                    WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                    WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                    "https://issuer.example/realms/flex-agent",
                    "worker-client",
                    "worker-client",
                    "flex-agent-worker",
                    expired ? now.AddMinutes(-10) : now,
                    expired ? now.AddMinutes(-10) : now,
                    expired ? now.AddMinutes(-1) : now.AddMinutes(5),
                    now,
                    actorId,
                    bindingId,
                    bindingVersion,
                    expired ? "expired" : "cached"));
        }
    }

    private sealed class ExpiredWorkloadIdentitySource(Guid actorId)
        : CachedOAuthWorkloadIdentitySource(actorId, Guid.NewGuid(), 1, expired: true);
}

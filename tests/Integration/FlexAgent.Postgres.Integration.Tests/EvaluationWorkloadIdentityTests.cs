using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Infrastructure;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using Dapper;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class EvaluationWorkloadIdentityTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Expired_workload_identity_does_not_claim_authorized_evaluation_work()
    {
        var prepared = await SeedAdmittedWorkAsync();

        var store = CreateWorkStore(new ExpiredEvaluationWorkloadIdentityGate(prepared.WorkerActorId));
        var claimed = await store.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.Null(claimed);
    }

    [Fact]
    public async Task Expired_workload_identity_blocks_lease_renewal_for_owned_claim()
    {
        var prepared = await SeedAdmittedWorkAsync();
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);

        var store = CreateWorkStore(new ExpiredEvaluationWorkloadIdentityGate(prepared.WorkerActorId));
        var renewed = await store.TryRenewAsync(
            claimed!,
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            CancellationToken);
        Assert.Null(renewed);
    }

    [Fact]
    public async Task Expired_workload_identity_blocks_release_for_owned_claim()
    {
        var prepared = await SeedAdmittedWorkAsync();
        var claimed = await prepared.Work.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.NotNull(claimed);

        var store = CreateWorkStore(new ExpiredEvaluationWorkloadIdentityGate(prepared.WorkerActorId));
        var released = await store.ReleaseForRetryAsync(
            claimed!,
            prepared.WorkerActorId,
            "worker.identity_expired",
            CancellationToken);
        Assert.False(released);
    }

    [Fact]
    public async Task Expired_workload_identity_blocks_protected_deterministic_disclosure()
    {
        var execution = await DeterministicPayloadTestSupport.ExecuteAndPersistAsync(
            Fixture,
            CancellationToken);
        Assert.NotNull(execution.First.Value!.OutputContentDigest);
        var outputStore = new PostgresProtectedDeterministicOutputStore(
            Fixture.Services.ConnectionAccessor,
            new ExpiredEvaluationWorkloadIdentityGate(execution.WorkerActorId),
            protectedDisclosureActorId: execution.WorkerActorId);

        var material = await outputStore.TryLoadVerifiedMaterialAsync(
            execution.Claimed.Ownership,
            execution.Claimed.RequestId,
            execution.First.Value!.DeterministicAttemptId,
            execution.First.Value.OutputContentDigest!,
            execution.Request.CriterionId,
            execution.Request.CriterionVersion,
            CancellationToken);
        Assert.Null(material);
    }

    [Fact]
    public async Task Cached_oauth_proof_cannot_claim_evaluation_work_after_principal_binding_revoke()
    {
        var prepared = await SeedAdmittedWorkAsync();
        var organizationId = prepared.Request.FrozenInput.Ownership.OrganizationId;
        var organizationActorId = await ReadOrganizationActorAsync(organizationId);
        await Fixture.GrantOrganizationActionAsync(
            organizationId,
            organizationActorId,
            AuthorizationActions.ProvisionServicePrincipalBinding);
        await Fixture.GrantOrganizationActionAsync(
            organizationId,
            organizationActorId,
            AuthorizationActions.RevokeServicePrincipalBinding);

        var principalBindingId = Guid.NewGuid();
        var mutation = new ServiceDelegationMutationContext(
            new TrustedActor(organizationActorId, "synthetic.test_actor"),
            Guid.NewGuid(),
            "operator.command",
            "revoke.cached.evaluation.worker.binding");
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            await PostgresServicePrincipalBindingCoordinator.ProvisionInTransactionAsync(
                organizationId,
                new ServicePrincipalBindingProvision(
                    principalBindingId,
                    WorkloadIdentityProfiles.OAuthClientCredentialsJwt,
                    WorkloadAuthenticationMethods.OAuthClientCredentialsSignedJwt,
                    "https://issuer.example/realms/flex-agent",
                    "worker-client-cached-eval-revoke",
                    "worker-client-cached-eval-revoke",
                    "flex-agent-worker",
                    prepared.WorkerActorId,
                    "worker.evaluation_runtime",
                    DateTimeOffset.UtcNow),
                mutation,
                (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
                scope.Transaction,
                CancellationToken);
            await PostgresServicePrincipalBindingCoordinator.RevokeInTransactionAsync(
                organizationId,
                principalBindingId,
                mutation,
                (ICommitAuthorizationKernel)Fixture.Services.AuthorizationKernel,
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var store = CreateWorkStore(
            new CachedOAuthEvaluationWorkloadIdentityGate(
                prepared.WorkerActorId,
                principalBindingId,
                bindingVersion: 1));
        var claimed = await store.TryClaimAsync(
            prepared.WorkerActorId,
            TimeSpan.FromSeconds(30),
            perOrganizationConcurrency: 1,
            CancellationToken);
        Assert.Null(claimed);
    }

    private async Task<Guid> ReadOrganizationActorAsync(Guid organizationId)
    {
        await using var connection = await Fixture.Services.ConnectionAccessor
            .OpenConnectionAsync(CancellationToken);
        return await connection.QuerySingleAsync<Guid>(
            """
            SELECT actor_id
            FROM actor_organization_grants
            WHERE organization_id = @OrganizationId
            ORDER BY created_at
            LIMIT 1;
            """,
            new { OrganizationId = organizationId });
    }

    private async Task<EvaluationPersistenceTestSeed.PreparedEvaluation> SeedAdmittedWorkAsync()
    {
        var prepared = await EvaluationPersistenceTestSeed.CreateAsync(
            Fixture,
            Guid.CreateVersion7().ToString("N"),
            CancellationToken);
        Assert.True((await prepared.Admission.AdmitAsync(prepared.Command(), CancellationToken)).Succeeded);
        return prepared;
    }

    private PostgresEvaluationDurableWorkStore CreateWorkStore(IEvaluationWorkloadIdentityGate gate) =>
        new(Fixture.Services.ConnectionAccessor, gate);

    private sealed class ExpiredEvaluationWorkloadIdentityGate(Guid actorId)
        : IEvaluationWorkloadIdentityGate
    {
        private readonly AuthenticatedWorkloadTransactionGuard _guard = new(
            new ExpiredWorkloadIdentitySource(actorId));

        public Task<bool> IsCurrentForWorkerActorAsync(
            Guid workerActorId,
            CancellationToken cancellationToken) =>
            _guard.IsCurrentForActorAsync(workerActorId, cancellationToken);

        public Task<bool> IsCurrentForWorkerActorInTransactionAsync(
            Guid workerActorId,
            System.Data.IDbTransaction transaction,
            CancellationToken cancellationToken) =>
            _guard.IsCurrentForActorInTransactionAsync(workerActorId, transaction, cancellationToken);
    }

    private sealed class CachedOAuthEvaluationWorkloadIdentityGate(
        Guid actorId,
        Guid bindingId,
        long bindingVersion) : IEvaluationWorkloadIdentityGate
    {
        private readonly AuthenticatedWorkloadTransactionGuard _guard = new(
            new CachedOAuthWorkloadIdentitySource(actorId, bindingId, bindingVersion));

        public Task<bool> IsCurrentForWorkerActorAsync(
            Guid workerActorId,
            CancellationToken cancellationToken) =>
            _guard.IsCurrentForActorAsync(workerActorId, cancellationToken);

        public Task<bool> IsCurrentForWorkerActorInTransactionAsync(
            Guid workerActorId,
            System.Data.IDbTransaction transaction,
            CancellationToken cancellationToken) =>
            _guard.IsCurrentForActorInTransactionAsync(workerActorId, transaction, cancellationToken);
    }

    private sealed class CachedOAuthWorkloadIdentitySource(
        Guid actorId,
        Guid bindingId,
        long bindingVersion) : IAuthenticatedWorkloadContextSource
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
                    now,
                    now,
                    now.AddMinutes(5),
                    now,
                    actorId,
                    bindingId,
                    bindingVersion,
                    "cached"));
        }
    }

    private sealed class ExpiredWorkloadIdentitySource(Guid actorId) : IAuthenticatedWorkloadContextSource
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
                    now.AddMinutes(-10),
                    now.AddMinutes(-10),
                    now.AddMinutes(-1),
                    now,
                    actorId,
                    Guid.NewGuid(),
                    1,
                    "expired"));
        }
    }
}

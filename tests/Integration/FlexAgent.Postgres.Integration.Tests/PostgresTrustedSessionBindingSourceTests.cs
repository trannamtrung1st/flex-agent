using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class PostgresTrustedSessionBindingSourceTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Rehydrates_exact_immutable_policy_and_protected_refs()
    {
        var prepared = await InsertSessionAsync();
        var source = new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor);

        var loaded = await source.GetAsync(prepared.Binding.Ownership, CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(prepared.Binding.Ownership, loaded!.Ownership);
        Assert.Equal(prepared.Binding.ConfigurationId, loaded.ConfigurationId);
        Assert.Equal(prepared.Binding.ConfigurationDigest, loaded.ConfigurationDigest);
        Assert.Equal(prepared.Binding.ManifestId, loaded.ManifestId);
        Assert.Equal(prepared.Binding.Policy.PolicyDigest, loaded.Policy.PolicyDigest);
        Assert.NotNull(loaded.FrozenModelDeployment);
        Assert.Equal(prepared.Binding.FrozenModelDeployment!.ProfileDigest, loaded.FrozenModelDeployment!.ProfileDigest);
        Assert.Equal(prepared.Binding.FrozenModelDeployment.CredentialBindingReference, loaded.FrozenModelDeployment.CredentialBindingReference);
    }

    [Fact]
    public async Task Missing_snapshot_returns_no_binding()
    {
        var loaded = await new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor)
            .GetAsync(
                new SessionOwnership(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task Distinct_configuration_and_policy_digests_round_trip()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var configurationDigest = new string('d', 64);
        var binding = SessionPersistenceFixtures.CreateBinding(
            organization.OrganizationId,
            cooldownSeconds: 0,
            configurationDigest: configurationDigest);
        Assert.NotEqual(binding.Policy.PolicyDigest, binding.ConfigurationDigest);
        var repository = new PostgresSessionRuntimeRepository();
        await using (var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken))
        {
            var startedAt = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
            await repository.InsertActiveAsync(
                binding.Ownership,
                SessionRuntime.CreateActive(binding, startedAt),
                SessionPersistenceFixtures.Actor(organization.ActorId),
                scope.Transaction,
                CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var loaded = await new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor)
            .GetAsync(binding.Ownership, CancellationToken);

        Assert.NotNull(loaded);
        Assert.Equal(configurationDigest, loaded!.ConfigurationDigest);
        Assert.Equal(binding.Policy.PolicyDigest, loaded.Policy.PolicyDigest);
        Assert.NotEqual(loaded.ConfigurationDigest, loaded.Policy.PolicyDigest);
    }

    [Fact]
    public async Task Ownership_mismatch_returns_no_binding()
    {
        var prepared = await InsertSessionAsync();
        var mismatched = prepared.Binding.Ownership with { ActivityId = Guid.NewGuid() };

        var loaded = await new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor)
            .GetAsync(mismatched, CancellationToken);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task Digest_mismatch_returns_no_binding()
    {
        var prepared = await InsertSessionAsync();
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            await connection.ExecuteAsync(
                """
                UPDATE session_runtimes
                SET configuration_digest = repeat('ab', 32)
                WHERE organization_id = @OrganizationId AND session_id = @SessionId;
                """,
                new
                {
                    prepared.Binding.Ownership.OrganizationId,
                    prepared.Binding.Ownership.SessionId,
                });
        }

        var loaded = await new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor)
            .GetAsync(prepared.Binding.Ownership, CancellationToken);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task Cross_organization_session_id_returns_no_binding()
    {
        var first = await InsertSessionAsync();
        var second = await InsertSessionAsync();

        var loaded = await new PostgresTrustedSessionBindingSource(Fixture.Services.ConnectionAccessor)
            .GetForOrganizationSessionAsync(
                first.Binding.Ownership.OrganizationId,
                second.Binding.Ownership.SessionId,
                CancellationToken);

        Assert.Null(loaded);
    }

    private async Task<(SeededOrganization Organization, TrustedSessionBinding Binding)> InsertSessionAsync()
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        await using var scope = await PostgresTransactionScope.BeginAsync(
            Fixture.Services.ConnectionAccessor,
            CancellationToken);
        var startedAt = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, CancellationToken);
        await repository.InsertActiveAsync(
            binding.Ownership,
            SessionRuntime.CreateActive(binding, startedAt),
            SessionPersistenceFixtures.Actor(organization.ActorId),
            scope.Transaction,
            CancellationToken);
        await scope.CommitAsync(CancellationToken);
        return (organization, binding);
    }
}

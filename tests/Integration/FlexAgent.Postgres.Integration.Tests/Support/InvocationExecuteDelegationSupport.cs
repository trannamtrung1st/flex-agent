using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class InvocationExecuteDelegationSupport
{
    public static AuthorizedServiceDelegationIssue CreateIssue(
        SeededOrganization organization,
        Guid serviceActorId,
        DateTimeOffset clock,
        Guid? delegationId = null,
        Guid? correlationId = null) =>
        new(
            new ServiceDelegationIssue(
                delegationId ?? Guid.NewGuid(),
                serviceActorId,
                AuthorizationActions.ExecuteSessionInvocation,
                "session.invocation.worker",
                "system.session_runtime",
                clock.AddMinutes(-1),
                clock.AddHours(12)),
            new ServiceDelegationMutationContext(
                new TrustedActor(organization.ActorId, "integration.test"),
                correlationId ?? Guid.NewGuid(),
                "session.start",
                "session.start.invocation_execute"));

    public static async Task<Guid> InsertSessionWithExecutionDelegationAsync(
        PostgresIntegrationFixture fixture,
        SeededOrganization organization,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken,
        Guid? serviceActorId = null)
    {
        await fixture.GrantOrganizationActionAsync(
            organization.OrganizationId,
            organization.ActorId,
            AuthorizationActions.IssueServiceDelegation);
        var repository = SessionPersistenceFixtures.RuntimeRepository();
        var issue = CreateIssue(organization, serviceActorId ?? organization.ActorId, DateTimeOffset.UtcNow);
        await using var scope = await PostgresTransactionScope.BeginAsync(
            fixture.Services.ConnectionAccessor,
            cancellationToken);
        var session = SessionRuntime.CreateActive(
            binding,
            new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));
        var clock = await repository.ReadAuthoritativeUtcAsync(scope.Transaction, cancellationToken);
        var timedIssue = CreateIssue(
            organization,
            serviceActorId ?? organization.ActorId,
            clock,
            issue.Issue.DelegationId,
            issue.Mutation.CorrelationId);
        await SessionPersistenceFixtures.InsertActiveAsync(repository, 
            binding.Ownership,
            session,
            SessionPersistenceFixtures.Actor(organization.ActorId),
            scope.Transaction,
            cancellationToken,
            timerLaneDelegation: null,
            authorizationKernel: (ICommitAuthorizationKernel)fixture.Services.AuthorizationKernel,
            invocationExecuteDelegation: timedIssue);
        await scope.CommitAsync(cancellationToken);
        return timedIssue.Issue.DelegationId;
    }
}

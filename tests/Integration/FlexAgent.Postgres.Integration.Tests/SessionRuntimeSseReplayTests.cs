using System.Globalization;
using Dapper;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Integration.Tests.Support;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class SessionRuntimeSseReplayTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Replay_after_commit_returns_authorized_fragments_and_seal_from_the_primary_store()
    {
        var ready = await PrepareReadyToPublishAsync("sse");
        var published = await ready.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                ready.Actor,
                ready.Binding.Ownership,
                ready.SessionVersion,
                ready.InvocationId,
                1,
                "Hel",
                "agen.sse.1",
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken);
        Assert.True(published.Succeeded, published.OutcomeCode);

        long versionAfterFragment;
        long firstSequence;
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            versionAfterFragment = await ReadSessionVersionAsync(connection, ready.Binding.Ownership);
            firstSequence = await ReadFragmentSequenceAsync(connection, ready.Binding.Ownership, 1);
        }

        Assert.True((await ready.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                ready.Actor,
                ready.Binding.Ownership,
                versionAfterFragment,
                ready.InvocationId,
                2,
                "lo",
                "agen.sse.1",
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken)).Succeeded);

        long versionAfterSecond;
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            versionAfterSecond = await ReadSessionVersionAsync(connection, ready.Binding.Ownership);
        }

        Assert.True((await ready.Publisher.SealAsync(
            new SealAgentResponseCommand(
                ready.Actor,
                ready.Binding.Ownership,
                versionAfterSecond,
                ready.InvocationId,
                AgentMessageCompletionStates.Complete,
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken)).Succeeded);

        var replay = new PostgresReplayAuthorizedSessionEventsCoordinator(
            Fixture.Services.ConnectionAccessor,
            ready.Repository,
            new ReplayAuthorizedSessionEventsHandler());
        var fromStart = await replay.ReplayAsync(
            new ReplayAuthorizedSessionEventsCommand(ready.Actor, ready.Binding.Ownership, null),
            ready.Binding,
            CancellationToken);
        var afterFirst = await replay.ReplayAsync(
            new ReplayAuthorizedSessionEventsCommand(
                ready.Actor,
                ready.Binding.Ownership,
                firstSequence.ToString(CultureInfo.InvariantCulture)),
            ready.Binding,
            CancellationToken);

        Assert.True(fromStart.Succeeded, fromStart.OutcomeCode);
        Assert.Equal(3, fromStart.Events.Count);
        Assert.Equal("Hel", fromStart.Events[0].TextDelta);
        Assert.Equal("lo", fromStart.Events[1].TextDelta);
        Assert.Equal(AuthorizedSessionEventTypes.AgentComplete, fromStart.Events[2].EventType);
        Assert.Equal(2, fromStart.Events[2].FragmentCount);
        Assert.True(
            long.Parse(fromStart.Events[2].SessionSequence, CultureInfo.InvariantCulture)
            > long.Parse(fromStart.Events[1].SessionSequence, CultureInfo.InvariantCulture));
        Assert.Equal(fromStart.Events[2].SessionSequence, afterFirst.Events[1].SessionSequence);
        Assert.Equal(2, afterFirst.Events.Count);
        Assert.Equal("lo", afterFirst.Events[0].TextDelta);
        Assert.DoesNotContain(afterFirst.Events, evt => evt.TextDelta == "Hel");
    }

    [Fact]
    public async Task Replay_denies_wrong_session_ownership_and_does_not_project_cross_session_text()
    {
        var first = await PrepareReadyToPublishAsync("iso-a");
        Assert.True((await first.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                first.Actor,
                first.Binding.Ownership,
                first.SessionVersion,
                first.InvocationId,
                1,
                "secret-a",
                "agen.iso.a",
                Guid.NewGuid(),
                "integration.test"),
            first.Binding,
            CancellationToken)).Succeeded);

        var second = await PrepareReadyToPublishAsync("iso-b");
        Assert.True((await second.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                second.Actor,
                second.Binding.Ownership,
                second.SessionVersion,
                second.InvocationId,
                1,
                "secret-b",
                "agen.iso.b",
                Guid.NewGuid(),
                "integration.test"),
            second.Binding,
            CancellationToken)).Succeeded);

        var replay = new PostgresReplayAuthorizedSessionEventsCoordinator(
            Fixture.Services.ConnectionAccessor,
            first.Repository,
            new ReplayAuthorizedSessionEventsHandler());
        var crossOwnership = first.Binding.Ownership with { SessionId = second.Binding.Ownership.SessionId };
        var denied = await replay.ReplayAsync(
            new ReplayAuthorizedSessionEventsCommand(first.Actor, crossOwnership, null),
            first.Binding,
            CancellationToken);
        var scoped = await replay.ReplayAsync(
            new ReplayAuthorizedSessionEventsCommand(first.Actor, first.Binding.Ownership, null),
            first.Binding,
            CancellationToken);

        Assert.Equal(SessionEventReplayOutcomeCodes.OwnershipMismatch, denied.OutcomeCode);
        Assert.Empty(denied.Events);
        Assert.DoesNotContain(scoped.Events, evt => evt.TextDelta == "secret-b");
        Assert.Contains(scoped.Events, evt => evt.TextDelta == "secret-a");
    }

    [Fact]
    public async Task Replay_snapshot_does_not_mix_a_later_committed_fragment_into_an_earlier_head()
    {
        var ready = await PrepareReadyToPublishAsync("rr");
        Assert.True((await ready.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                ready.Actor,
                ready.Binding.Ownership,
                ready.SessionVersion,
                ready.InvocationId,
                1,
                "Hel",
                "agen.rr.1",
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken)).Succeeded);

        long versionAfterFirst;
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            versionAfterFirst = await ReadSessionVersionAsync(connection, ready.Binding.Ownership);
        }

        string? isolation = null;
        PostgresSessionRuntimeRepository.AfterHeadLoadedAsync = async transaction =>
        {
            isolation = await transaction.Connection!.ExecuteScalarAsync<string>(
                new CommandDefinition(
                    "SHOW transaction_isolation;",
                    transaction: transaction,
                    cancellationToken: CancellationToken));
            var second = await ready.Publisher.PublishFragmentAsync(
                new PublishAgentResponseFragmentCommand(
                    ready.Actor,
                    ready.Binding.Ownership,
                    versionAfterFirst,
                    ready.InvocationId,
                    2,
                    "lo",
                    "agen.rr.1",
                    Guid.NewGuid(),
                    "integration.test"),
                ready.Binding,
                CancellationToken);
            Assert.True(second.Succeeded, second.OutcomeCode);

            long versionAfterSecond;
            await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
            {
                versionAfterSecond = await ReadSessionVersionAsync(connection, ready.Binding.Ownership);
            }

            Assert.True((await ready.Publisher.SealAsync(
                new SealAgentResponseCommand(
                    ready.Actor,
                    ready.Binding.Ownership,
                    versionAfterSecond,
                    ready.InvocationId,
                    AgentMessageCompletionStates.Complete,
                    Guid.NewGuid(),
                    "integration.test"),
                ready.Binding,
                CancellationToken)).Succeeded);
        };

        try
        {
            var replay = await new PostgresReplayAuthorizedSessionEventsCoordinator(
                Fixture.Services.ConnectionAccessor,
                ready.Repository,
                new ReplayAuthorizedSessionEventsHandler()).ReplayAsync(
                new ReplayAuthorizedSessionEventsCommand(ready.Actor, ready.Binding.Ownership, null),
                ready.Binding,
                CancellationToken);

            Assert.Equal("repeatable read", isolation);
            Assert.True(replay.Succeeded, replay.OutcomeCode);
            var fragment = Assert.Single(replay.Events);
            Assert.Equal("Hel", fragment.TextDelta);
            Assert.DoesNotContain(replay.Events, evt => evt.TextDelta == "lo");
            Assert.DoesNotContain(
                replay.Events,
                evt => evt.EventType == AuthorizedSessionEventTypes.AgentComplete);
        }
        finally
        {
            PostgresSessionRuntimeRepository.AfterHeadLoadedAsync = null;
        }
    }

    [Fact]
    public async Task Unknown_future_cursor_reconciles_from_the_loaded_session()
    {
        var ready = await PrepareReadyToPublishAsync("future");
        Assert.True((await ready.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                ready.Actor,
                ready.Binding.Ownership,
                ready.SessionVersion,
                ready.InvocationId,
                1,
                "Hel",
                "agen.future.1",
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken)).Succeeded);

        long sessionSequence;
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            sessionSequence = await connection.ExecuteScalarAsync<long>(
                """
                SELECT session_sequence
                FROM session_runtimes
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId;
                """,
                ready.Binding.Ownership);
        }

        var replay = await new PostgresReplayAuthorizedSessionEventsCoordinator(
            Fixture.Services.ConnectionAccessor,
            ready.Repository,
            new ReplayAuthorizedSessionEventsHandler()).ReplayAsync(
            new ReplayAuthorizedSessionEventsCommand(
                ready.Actor,
                ready.Binding.Ownership,
                (sessionSequence + 1).ToString(CultureInfo.InvariantCulture)),
            ready.Binding,
            CancellationToken);

        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, replay.OutcomeCode);
        Assert.Empty(replay.Events);
    }

    [Fact]
    public async Task In_range_non_stream_session_sequence_reconciles_from_the_loaded_session()
    {
        var ready = await PrepareReadyToPublishAsync("nonstream");
        Assert.True((await ready.Publisher.PublishFragmentAsync(
            new PublishAgentResponseFragmentCommand(
                ready.Actor,
                ready.Binding.Ownership,
                ready.SessionVersion,
                ready.InvocationId,
                1,
                "Hel",
                "agen.nonstream.1",
                Guid.NewGuid(),
                "integration.test"),
            ready.Binding,
            CancellationToken)).Succeeded);

        long invocationSequence;
        long fragmentSequence;
        await using (var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken))
        {
            invocationSequence = await connection.ExecuteScalarAsync<long>(
                """
                SELECT admitted_session_sequence
                FROM session_invocations
                WHERE organization_id = @OrganizationId
                  AND session_id = @SessionId
                  AND agent_invocation_id = @InvocationId;
                """,
                new
                {
                    ready.Binding.Ownership.OrganizationId,
                    ready.Binding.Ownership.SessionId,
                    InvocationId = ready.InvocationId,
                });
            fragmentSequence = await ReadFragmentSequenceAsync(connection, ready.Binding.Ownership, 1);
        }

        Assert.InRange(invocationSequence, 1, fragmentSequence - 1);

        var replay = await new PostgresReplayAuthorizedSessionEventsCoordinator(
            Fixture.Services.ConnectionAccessor,
            ready.Repository,
            new ReplayAuthorizedSessionEventsHandler()).ReplayAsync(
            new ReplayAuthorizedSessionEventsCommand(
                ready.Actor,
                ready.Binding.Ownership,
                invocationSequence.ToString(CultureInfo.InvariantCulture)),
            ready.Binding,
            CancellationToken);

        Assert.Equal(SessionEventReplayOutcomeCodes.Reconcile, replay.OutcomeCode);
        Assert.Empty(replay.Events);
    }

    private async Task<ReadyPublication> PrepareReadyToPublishAsync(string key)
    {
        var organization = await Fixture.SeedOrganizationAsync();
        var binding = SessionPersistenceFixtures.CreateBinding(organization.OrganizationId, cooldownSeconds: 0);
        var repository = new PostgresSessionRuntimeRepository();
        var actor = SessionPersistenceFixtures.Actor(organization.ActorId);
        var acceptCoordinator = new PostgresAcceptParticipantMessageCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new AcceptParticipantMessageHandler());
        var completeCoordinator = new PostgresCompleteInvocationCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new CompleteInvocationHandler());
        var publisher = new PostgresPublishAgentResponseCoordinator(
            Fixture.Services.ConnectionAccessor,
            repository,
            new PublishAgentResponseFragmentHandler());
        var session = SessionRuntime.CreateActive(binding, new DateTimeOffset(2026, 8, 13, 0, 0, 0, TimeSpan.Zero));

        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            await repository.InsertActiveAsync(binding.Ownership, session, scope.Transaction, CancellationToken);
            await scope.CommitAsync(CancellationToken);
        }

        var admitted = await acceptCoordinator.AcceptAsync(
            new AcceptParticipantMessageCommand(
                actor,
                binding.Ownership,
                ExpectedSessionVersion: 0,
                $"msg.p.{key}",
                $"turn.{key}",
                $"slot.{key}",
                $"trig.participant.{key}",
                $"idem.p.{key}",
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(admitted.Succeeded, admitted.OutcomeCode);

        var completed = await completeCoordinator.CompleteAsync(
            new CompleteInvocationCommand(
                actor,
                binding.Ownership,
                admitted.SessionVersion!.Value,
                admitted.Invocation!.AgentInvocationId,
                new EnvelopeRecommendation(
                    $"adec.{key}.0001",
                    admitted.Invocation.AgentInvocationId,
                    new DateTimeOffset(2026, 8, 13, 0, 0, 2, TimeSpan.Zero),
                    DecisionDispositions.Respond,
                    [
                        new OutputRecommendation(
                            AgentOutputKinds.Message,
                            "out.message.primary",
                            "participant_reply",
                            $"turn.{key}",
                            $"slot.{key}"),
                    ],
                    []),
                null,
                Guid.NewGuid(),
                "integration.test"),
            binding,
            CancellationToken);
        Assert.True(completed.Succeeded, completed.OutcomeCode);

        long sessionVersion;
        await using (var scope = await PostgresTransactionScope.BeginAsync(Fixture.Services.ConnectionAccessor, CancellationToken))
        {
            var loaded = await repository.LoadForUpdateAsync(
                binding.Ownership,
                binding,
                scope.Transaction,
                CancellationToken);
            Assert.NotNull(loaded);
            sessionVersion = loaded!.SessionVersion;
            await scope.CommitAsync(CancellationToken);
        }

        return new ReadyPublication(
            binding,
            actor,
            admitted.Invocation.AgentInvocationId,
            sessionVersion,
            repository,
            publisher);
    }

    private static async Task<long> ReadSessionVersionAsync(NpgsqlConnection connection, SessionOwnership ownership) =>
        await connection.ExecuteScalarAsync<long>(
            """
            SELECT session_version
            FROM session_runtimes
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId;
            """,
            ownership);

    private static async Task<long> ReadFragmentSequenceAsync(
        NpgsqlConnection connection,
        SessionOwnership ownership,
        int fragmentOrdinal) =>
        await connection.ExecuteScalarAsync<long>(
            """
            SELECT session_sequence
            FROM session_message_fragments
            WHERE organization_id = @OrganizationId
              AND session_id = @SessionId
              AND fragment_ordinal = @FragmentOrdinal;
            """,
            new
            {
                ownership.OrganizationId,
                ownership.SessionId,
                FragmentOrdinal = fragmentOrdinal,
            });

    private sealed record ReadyPublication(
        TrustedSessionBinding Binding,
        TrustedRuntimeActor Actor,
        string InvocationId,
        long SessionVersion,
        PostgresSessionRuntimeRepository Repository,
        PostgresPublishAgentResponseCoordinator Publisher);
}

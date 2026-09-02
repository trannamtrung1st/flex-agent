using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Tests;

public sealed class AttemptTerminalMappingPortTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-02T08:00:00Z");

    [Fact]
    public async Task Completed_session_maps_to_consumed_completed_attempt()
    {
        var store = new InMemoryAttemptStore();
        var attempt = Activate();
        await store.InsertAsync(attempt, new InMemoryEnrollmentTransaction(), TestContext.Current.CancellationToken);
        var port = new AttemptTerminalMappingPort(store);

        await port.MapTerminalAsync(
            attempt.OrganizationId,
            attempt.AttemptId,
            AttemptStates.Completed,
            "participant_completed",
            Now.AddMinutes(5),
            new InMemoryEnrollmentTransaction(),
            TestContext.Current.CancellationToken);

        var mapped = Assert.Single(store.Items);
        Assert.Equal(AttemptStates.Completed, mapped.Status);
        Assert.True(mapped.Consumed);
        Assert.Equal(attempt.Ordinal, mapped.Ordinal);
        Assert.Equal(attempt.Binding.SessionId, mapped.Binding.SessionId);
    }

    [Fact]
    public async Task Duplicate_terminal_mapping_reconciles_without_restoring_entitlement()
    {
        var store = new InMemoryAttemptStore();
        var attempt = Activate().Abort(Now.AddMinutes(1), "integrity_abort").Value!;
        await store.InsertAsync(attempt, new InMemoryEnrollmentTransaction(), TestContext.Current.CancellationToken);
        var port = new AttemptTerminalMappingPort(store);

        await port.MapTerminalAsync(
            attempt.OrganizationId,
            attempt.AttemptId,
            AttemptStates.Aborted,
            "integrity_abort",
            Now.AddMinutes(2),
            new InMemoryEnrollmentTransaction(),
            TestContext.Current.CancellationToken);

        var mapped = Assert.Single(store.Items);
        Assert.Equal(AttemptStates.Aborted, mapped.Status);
        Assert.True(mapped.Consumed);
        Assert.Equal(attempt.TerminalAtUtc, mapped.TerminalAtUtc);
    }

    [Fact]
    public async Task Missing_attempt_fails_the_terminal_mapping()
    {
        var port = new AttemptTerminalMappingPort(new InMemoryAttemptStore());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            port.MapTerminalAsync(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                AttemptStates.Aborted,
                "integrity_abort",
                Now,
                new InMemoryEnrollmentTransaction(),
                TestContext.Current.CancellationToken));
    }

    private static Attempt Activate() =>
        Attempt.Activate(
            Guid.CreateVersion7(),
            new SubmissionParentScope(
                Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa8"),
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa5"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa6"),
                new string('a', 64)),
            1,
            AttemptEntitlementSources.Baseline,
            null,
            Now,
            Now,
            new AttemptBinding(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), new string('a', 64), new string('a', 64)),
            [new AttemptSubmissionBinding(Guid.CreateVersion7(), 1, 1, new string('a', 64))]).Value!;
}

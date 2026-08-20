using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class InMemoryModelProviderAttemptProvenanceWriterTests
{
    [Fact]
    public async Task Expired_claim_cannot_reserve_a_started_fact()
    {
        var writer = new InMemoryModelProviderAttemptProvenanceWriter();
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var expired = new DurableInvocationWorkItem(
            Guid.NewGuid(),
            ownership,
            "ainv.reserve.expired",
            DurableSessionWorkStates.Claimed,
            DateTimeOffset.UtcNow.AddSeconds(-1));
        var current = new DurableInvocationWorkItem(
            Guid.NewGuid(),
            ownership,
            "ainv.reserve.expired",
            DurableSessionWorkStates.Claimed,
            DateTimeOffset.UtcNow.AddSeconds(30));

        var lost = await writer.TryReserveAsync(
            expired,
            expired.AgentInvocationId,
            1,
            2,
            Started("prat.stale"),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);
        var reserved = await writer.TryReserveAsync(
            current,
            current.AgentInvocationId,
            1,
            2,
            Started("prat.current"),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.True(lost.LostClaimAuthority);
        Assert.False(lost.Reserved);
        Assert.True(reserved.Reserved);
        Assert.Equal(1, await writer.CountAsync(ownership, current.AgentInvocationId, CancellationToken.None));
    }

    [Fact]
    public async Task Revoked_authorization_cannot_reserve_a_started_fact()
    {
        var writer = new InMemoryModelProviderAttemptProvenanceWriter
        {
            IsReservationAuthorized = static () => false,
        };
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var work = new DurableInvocationWorkItem(
            Guid.NewGuid(),
            ownership,
            "ainv.reserve.denied",
            DurableSessionWorkStates.Claimed,
            DateTimeOffset.UtcNow.AddSeconds(30));

        var denied = await writer.TryReserveAsync(
            work,
            work.AgentInvocationId,
            1,
            2,
            Started("prat.denied"),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.True(denied.LostClaimAuthority);
        Assert.False(denied.Reserved);
        Assert.Null(denied.RenewedClaimLeaseUntil);
        Assert.Equal(0, await writer.CountAsync(ownership, work.AgentInvocationId, CancellationToken.None));
    }

    [Fact]
    public async Task Mismatched_invocation_cannot_reserve_against_claimed_work()
    {
        var writer = new InMemoryModelProviderAttemptProvenanceWriter();
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var work = new DurableInvocationWorkItem(
            Guid.NewGuid(),
            ownership,
            "ainv.reserve.claimed",
            DurableSessionWorkStates.Claimed,
            DateTimeOffset.UtcNow.AddSeconds(30));

        var denied = await writer.TryReserveAsync(
            work,
            "ainv.reserve.other",
            1,
            2,
            Started("prat.mismatch"),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.True(denied.LostClaimAuthority);
        Assert.False(denied.Reserved);
        Assert.Null(denied.RenewedClaimLeaseUntil);
        Assert.Equal(0, await writer.CountAsync(ownership, work.AgentInvocationId, CancellationToken.None));
        Assert.Equal(0, await writer.CountAsync(ownership, "ainv.reserve.other", CancellationToken.None));
    }

    [Fact]
    public async Task Concurrent_reservations_cannot_exceed_the_request_budget()
    {
        var writer = new InMemoryModelProviderAttemptProvenanceWriter();
        var ownership = SessionRuntimeTestFixtures.CreateOwnership();
        var lease = DateTimeOffset.UtcNow.AddSeconds(30);
        var work = new DurableInvocationWorkItem(
            Guid.NewGuid(),
            ownership,
            "ainv.reserve.race",
            DurableSessionWorkStates.Claimed,
            lease);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(index =>
                writer.TryReserveAsync(
                    work,
                    work.AgentInvocationId,
                    1,
                    maxProviderRequestAttempts: 1,
                    Started($"prat.race.{index}"),
                    TimeSpan.FromSeconds(30),
                    CancellationToken.None)));

        Assert.Equal(1, results.Count(result => result.Reserved));
        Assert.Equal(7, results.Count(result => !result.Reserved && !result.LostClaimAuthority));
        Assert.Equal(1, await writer.CountAsync(ownership, work.AgentInvocationId, CancellationToken.None));
    }

    private static ModelProviderAttemptProvenance Started(string providerRequestId)
    {
        var profile = SessionRuntimeTestFixtures.CreateInstalledProfile();
        var at = DateTimeOffset.UtcNow;
        return new ModelProviderAttemptProvenance(
            profile.AdapterKind,
            profile.AdapterContractVersion,
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.RequestedModel,
            profile.ResolvedModelVersion,
            ExecutionAttemptOutcomeCategories.ProviderRequestStarted,
            null,
            null,
            $"pref.{providerRequestId}",
            at,
            at,
            ModelProviderRequestPhases.Control,
            providerRequestId,
            ModelProviderRequestFacts.Started);
    }
}

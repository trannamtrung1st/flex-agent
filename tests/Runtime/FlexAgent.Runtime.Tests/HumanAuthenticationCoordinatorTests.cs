using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;

namespace FlexAgent.Runtime.Tests;

public sealed class HumanAuthenticationCoordinatorTests
{
    private static readonly ExactIssuerSubject Identity = new(
        "https://issuer.example/realms/flex",
        "subject-1");

    [Fact]
    public async Task Login_creates_digest_only_session_and_rejects_unknown_or_ambiguous_context()
    {
        var harness = CreateHarness();
        var unknown = await harness.Coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(Identity, AuthenticationStrength.Empty, "sid-1"),
            null,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HumanAuthenticationReasonCodes.UnknownSubject, unknown.ReasonCode);

        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        harness.Bindings.RegisterActor(actorId);
        harness.Bindings.GrantOrganization(actorId, organizationId);
        Assert.Null(await harness.Bindings.TryProvisionAsync(
            new HumanIdentityBinding(Guid.NewGuid(), Identity, actorId, DateTimeOffset.UtcNow, null),
            TestContext.Current.CancellationToken));

        var login = await harness.Coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(Identity, new AuthenticationStrength("acr:mfa", ["mfa"]), "sid-1"),
            null,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        Assert.True(login.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(login.RawCredential));
        Assert.All(harness.Sessions.Snapshot, session =>
        {
            Assert.NotEqual(login.RawCredential, session.CredentialDigest);
            Assert.Equal(64, session.CredentialDigest!.Length);
        });

        var extraOrg = Guid.NewGuid();
        harness.Bindings.GrantOrganization(actorId, extraOrg);
        var ambiguous = await harness.Coordinator.CompleteLoginAsync(
            new ValidatedHumanLogin(Identity, AuthenticationStrength.Empty, null),
            null,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        Assert.Equal(HumanAuthenticationReasonCodes.AmbiguousOrganizationContext, ambiguous.ReasonCode);
        Assert.Contains(
            harness.Audit.Events,
            item => item.EventType == AuthenticationSecurityEventTypes.LoginDenied);
    }

    [Fact]
    public async Task Concurrent_sessions_rotate_and_revoke_individually()
    {
        var harness = CreateHarness();
        var actorId = Guid.NewGuid();
        harness.Bindings.RegisterActor(actorId);
        harness.Bindings.GrantOrganization(actorId, Guid.NewGuid());
        await harness.Bindings.TryProvisionAsync(
            new HumanIdentityBinding(Guid.NewGuid(), Identity, actorId, DateTimeOffset.UtcNow, null),
            TestContext.Current.CancellationToken);

        var first = await harness.Coordinator.CompleteLoginAsync(
            Login("sid-1"),
            null,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        var second = await harness.Coordinator.CompleteLoginAsync(
            Login("sid-2"),
            null,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        Assert.NotEqual(first.RawCredential, second.RawCredential);
        Assert.NotNull(await harness.Coordinator.AuthenticateAsync(first.RawCredential!, false, TestContext.Current.CancellationToken));
        Assert.NotNull(await harness.Coordinator.AuthenticateAsync(second.RawCredential!, false, TestContext.Current.CancellationToken));

        var rotated = await harness.Coordinator.RotateAsync(
            first.ApplicationSessionId!.Value,
            ApplicationSessionTerminalReasons.PrivilegeChange,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        Assert.True(rotated.Succeeded);
        Assert.Null(await harness.Coordinator.AuthenticateAsync(first.RawCredential!, false, TestContext.Current.CancellationToken));
        Assert.NotNull(await harness.Coordinator.AuthenticateAsync(rotated.RawCredential!, false, TestContext.Current.CancellationToken));
        Assert.NotNull(await harness.Coordinator.AuthenticateAsync(second.RawCredential!, false, TestContext.Current.CancellationToken));

        await harness.Coordinator.ApplyProviderForcedLogoutAsync("sid-1", Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.Null(await harness.Coordinator.AuthenticateAsync(rotated.RawCredential!, false, TestContext.Current.CancellationToken));
        Assert.NotNull(await harness.Coordinator.AuthenticateAsync(second.RawCredential!, false, TestContext.Current.CancellationToken));

        Assert.True(await harness.Coordinator.LogoutAsync(second.RawCredential!, Guid.NewGuid(), TestContext.Current.CancellationToken));
        Assert.Null(await harness.Coordinator.AuthenticateAsync(second.RawCredential!, false, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Concurrent_rotations_of_one_predecessor_create_exactly_one_successor()
    {
        var harness = CreateHarness();
        var actorId = Guid.NewGuid();
        harness.Bindings.RegisterActor(actorId);
        harness.Bindings.GrantOrganization(actorId, Guid.NewGuid());
        await harness.Bindings.TryProvisionAsync(
            new HumanIdentityBinding(Guid.NewGuid(), Identity, actorId, DateTimeOffset.UtcNow, null),
            TestContext.Current.CancellationToken);
        var login = await harness.Coordinator.CompleteLoginAsync(
            Login(),
            null,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        var first = harness.Coordinator.RotateAsync(
            login.ApplicationSessionId!.Value,
            ApplicationSessionTerminalReasons.PrivilegeChange,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        var second = harness.Coordinator.RotateAsync(
            login.ApplicationSessionId.Value,
            ApplicationSessionTerminalReasons.SensitiveReauthentication,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(result => result.Succeeded));
        Assert.Equal(1, results.Count(result => result.ReasonCode == HumanAuthenticationReasonCodes.MissingSession));
        Assert.Equal(
            1,
            harness.Sessions.Snapshot.Count(session => session.PredecessorSessionId == login.ApplicationSessionId));
        Assert.Equal(1, harness.Sessions.Snapshot.Count(session => session.IsLive));
    }

    [Fact]
    public async Task Back_channel_sub_only_logout_revokes_identity_sessions_and_rejects_replay()
    {
        var harness = CreateHarness();
        var actorId = Guid.NewGuid();
        harness.Bindings.RegisterActor(actorId);
        harness.Bindings.GrantOrganization(actorId, Guid.NewGuid());
        await harness.Bindings.TryProvisionAsync(
            new HumanIdentityBinding(Guid.NewGuid(), Identity, actorId, DateTimeOffset.UtcNow, null),
            TestContext.Current.CancellationToken);
        var login = await harness.Coordinator.CompleteLoginAsync(
            Login(),
            null,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        var token = new ValidatedLogoutToken(Identity.Issuer, Identity.Subject, null, "jti-1");

        var first = await harness.Coordinator.ApplyBackChannelLogoutAsync(
            token,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);
        var replay = await harness.Coordinator.ApplyBackChannelLogoutAsync(
            token,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        Assert.True(first.Accepted);
        Assert.Equal(1, first.RevokedCount);
        Assert.False(replay.Accepted);
        Assert.Equal(HumanAuthenticationReasonCodes.ReplayOrConsumedTransaction, replay.ReasonCode);
        Assert.Null(await harness.Coordinator.AuthenticateAsync(login.RawCredential!, false, TestContext.Current.CancellationToken));
        var actor = await harness.Bindings.GetActorStateAsync(actorId, TestContext.Current.CancellationToken);
        Assert.False(actor.Disabled);
    }

    [Fact]
    public async Task Provider_lifecycle_revokes_matching_sessions_without_restoring_them()
    {
        var harness = CreateHarness();
        var actorId = Guid.NewGuid();
        harness.Bindings.RegisterActor(actorId);
        harness.Bindings.GrantOrganization(actorId, Guid.NewGuid());
        await harness.Bindings.TryProvisionAsync(
            new HumanIdentityBinding(Guid.NewGuid(), Identity, actorId, DateTimeOffset.UtcNow, null),
            TestContext.Current.CancellationToken);
        var login = await harness.Coordinator.CompleteLoginAsync(
            Login(),
            null,
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        await harness.Coordinator.ApplyProviderForcedLogoutAsync("sid-1", Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.Null(await harness.Coordinator.AuthenticateAsync(login.RawCredential!, false, TestContext.Current.CancellationToken));

        var rebound = await harness.Bindings.TryProvisionAsync(
            new HumanIdentityBinding(Guid.NewGuid(), Identity, Guid.NewGuid(), DateTimeOffset.UtcNow, null),
            TestContext.Current.CancellationToken);
        Assert.Equal(HumanAuthenticationReasonCodes.ReboundIdentity, rebound);
    }

    private static ValidatedHumanLogin Login(string providerSessionId = "sid-1") =>
        new(Identity, new AuthenticationStrength("acr:mfa", ["mfa"]), providerSessionId);

    private static Harness CreateHarness()
    {
        var bindings = new MemoryHumanIdentityBindingStore();
        var sessions = new MemoryApplicationSessionStore();
        var audit = new MemoryAuthenticationSecurityEventWriter();
        var coordinator = new HumanAuthenticationCoordinator(
            bindings,
            sessions,
            audit,
            new MemoryLogoutTokenReplayStore(),
            new HmacLookupDigestCalculator("test-lookup-key-32-bytes-minimum!"u8.ToArray()),
            new SystemDatabaseClock(TimeProvider.System),
            new HumanAuthenticationOptions { Issuer = Identity.Issuer });
        return new Harness(coordinator, bindings, sessions, audit);
    }

    private sealed record Harness(
        HumanAuthenticationCoordinator Coordinator,
        MemoryHumanIdentityBindingStore Bindings,
        MemoryApplicationSessionStore Sessions,
        MemoryAuthenticationSecurityEventWriter Audit);
}

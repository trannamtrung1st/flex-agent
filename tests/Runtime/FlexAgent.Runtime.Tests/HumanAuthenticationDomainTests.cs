using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.Runtime.Tests;

public sealed class HumanAuthenticationDomainTests
{
    private static readonly ExactIssuerSubject Identity = new(
        "https://issuer.example/realms/flex",
        "subject-1");

    [Fact]
    public void Exact_issuer_and_subject_are_case_and_unicode_sensitive()
    {
        var left = ExactIssuerSubject.TryCreate("https://Issuer.example/realms/flex", "subject-1");
        var right = new ExactIssuerSubject("https://issuer.example/realms/flex", "subject-1");
        var unicode = new ExactIssuerSubject(Identity.Issuer, "sübject-1");

        Assert.NotNull(left);
        Assert.False(left!.Matches(right.Issuer, right.Subject));
        Assert.False(Identity.Matches(Identity.Issuer, unicode.Subject));
        Assert.False(Identity.Matches(Identity.Issuer + " ", Identity.Subject));
    }

    [Fact]
    public void Unknown_disabled_and_rebound_subjects_fail_closed()
    {
        var binding = new HumanIdentityBinding(Guid.NewGuid(), Identity, Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        var unknown = HumanIdentityResolver.Resolve(
            Identity,
            Identity.Issuer,
            binding: null,
            actorExists: true,
            actorDisabled: false,
            [Guid.NewGuid()],
            clientSuppliedOrganizationId: null);
        var disabled = HumanIdentityResolver.Resolve(
            Identity,
            Identity.Issuer,
            binding with { DisabledAt = DateTimeOffset.UtcNow },
            actorExists: true,
            actorDisabled: false,
            [Guid.NewGuid()],
            null);
        var disabledActor = HumanIdentityResolver.Resolve(
            Identity,
            Identity.Issuer,
            binding,
            actorExists: true,
            actorDisabled: true,
            [Guid.NewGuid()],
            null);

        Assert.Equal(HumanAuthenticationReasonCodes.UnknownSubject, unknown.ReasonCode);
        Assert.Equal(HumanAuthenticationReasonCodes.DisabledIdentity, disabled.ReasonCode);
        Assert.Equal(HumanAuthenticationReasonCodes.DisabledIdentity, disabledActor.ReasonCode);
    }

    [Fact]
    public void Issuer_substitution_and_client_organization_are_rejected()
    {
        var binding = new HumanIdentityBinding(Guid.NewGuid(), Identity, Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        var substituted = HumanIdentityResolver.Resolve(
            new ExactIssuerSubject("https://other.example/realms/flex", Identity.Subject),
            Identity.Issuer,
            binding,
            true,
            false,
            [Guid.NewGuid()],
            null);
        var clientOrg = HumanIdentityResolver.Resolve(
            Identity,
            Identity.Issuer,
            binding,
            true,
            false,
            [Guid.NewGuid()],
            Guid.NewGuid());

        Assert.Equal(HumanAuthenticationReasonCodes.IssuerMismatch, substituted.ReasonCode);
        Assert.Equal(HumanAuthenticationReasonCodes.ClientSuppliedOrganizationRejected, clientOrg.ReasonCode);
    }

    [Fact]
    public void Zero_and_multiple_organization_contexts_fail_closed()
    {
        var binding = new HumanIdentityBinding(Guid.NewGuid(), Identity, Guid.NewGuid(), DateTimeOffset.UtcNow, null);
        var zero = HumanIdentityResolver.Resolve(Identity, Identity.Issuer, binding, true, false, [], null);
        var many = HumanIdentityResolver.Resolve(
            Identity,
            Identity.Issuer,
            binding,
            true,
            false,
            [Guid.NewGuid(), Guid.NewGuid()],
            null);

        Assert.Equal(HumanAuthenticationReasonCodes.ZeroOrganizationContext, zero.ReasonCode);
        Assert.Equal(HumanAuthenticationReasonCodes.AmbiguousOrganizationContext, many.ReasonCode);
    }

    [Fact]
    public void One_eligible_organization_binds_immutably_to_the_pre_provisioned_actor()
    {
        var actorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var binding = new HumanIdentityBinding(Guid.NewGuid(), Identity, actorId, DateTimeOffset.UtcNow, null);

        var resolved = HumanIdentityResolver.Resolve(
            Identity,
            Identity.Issuer,
            binding,
            true,
            false,
            [organizationId],
            null);

        Assert.True(resolved.Succeeded);
        Assert.Equal(actorId, resolved.ActorId);
        Assert.Equal(organizationId, resolved.OrganizationId);
    }

    [Fact]
    public void Session_policy_shortens_but_does_not_widen_lifetime_bounds()
    {
        var widened = ApplicationSessionPolicy.CreateLifetime(
            DateTimeOffset.Parse("2026-08-20T00:00:00Z"),
            TimeSpan.FromHours(2),
            TimeSpan.FromHours(24));
        var shortened = ApplicationSessionPolicy.CreateLifetime(
            DateTimeOffset.Parse("2026-08-20T00:00:00Z"),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromHours(2));

        Assert.Equal(TimeSpan.FromMinutes(30), widened.IdleExpiresAt - widened.CreatedAt);
        Assert.Equal(TimeSpan.FromHours(12), widened.AbsoluteExpiresAt - widened.CreatedAt);
        Assert.Equal(TimeSpan.FromMinutes(10), shortened.IdleExpiresAt - shortened.CreatedAt);
        Assert.Equal(TimeSpan.FromHours(2), shortened.AbsoluteExpiresAt - shortened.CreatedAt);
    }

    [Fact]
    public void Expired_revoked_and_rotated_sessions_are_not_authentic()
    {
        var now = DateTimeOffset.Parse("2026-08-20T12:00:00Z");
        var live = CreateSession(now.AddMinutes(-5), now.AddMinutes(-5), now.AddMinutes(25), now.AddHours(11));
        var idle = CreateSession(now.AddMinutes(-40), now.AddMinutes(-40), now.AddMinutes(-10), now.AddHours(11));
        var revoked = live with { RevokedAt = now };
        var rotated = live with { RotatedAt = now, CredentialDigest = null };

        Assert.Null(ApplicationSessionPolicy.AuthenticateFailureReason(live, now));
        Assert.Equal(HumanAuthenticationReasonCodes.ExpiredSession, ApplicationSessionPolicy.AuthenticateFailureReason(idle, now));
        Assert.Equal(HumanAuthenticationReasonCodes.RevokedSession, ApplicationSessionPolicy.AuthenticateFailureReason(revoked, now));
        Assert.Equal(HumanAuthenticationReasonCodes.RotatedSession, ApplicationSessionPolicy.AuthenticateFailureReason(rotated, now));
        Assert.Equal(HumanAuthenticationReasonCodes.MissingSession, ApplicationSessionPolicy.AuthenticateFailureReason(null, now));
    }

    [Fact]
    public void Activity_touch_cannot_extend_beyond_absolute_expiry()
    {
        var created = DateTimeOffset.Parse("2026-08-20T00:00:00Z");
        var lifetime = ApplicationSessionPolicy.CreateLifetime(created, TimeSpan.FromMinutes(30), TimeSpan.FromHours(1));
        var touched = ApplicationSessionPolicy.TouchActivity(lifetime, created.AddMinutes(50), TimeSpan.FromMinutes(30));

        Assert.Equal(created.AddHours(1), touched.IdleExpiresAt);
        Assert.Equal(created.AddMinutes(50), touched.LastSeenAt);
    }

    [Fact]
    public void Reviewer_and_administrator_access_require_allowlisted_mfa_evidence()
    {
        var allowedAcr = new HashSet<string>(StringComparer.Ordinal) { "acr:mfa" };
        var allowedAmr = new HashSet<string>(StringComparer.Ordinal) { "mfa", "otp" };
        var missing = AuthenticationStrengthEvaluator.Evaluate(
            AuthenticationStrength.Empty,
            AuthenticationStrengthEvaluator.ReviewerRelationship,
            AuthorizationActions.SubscribeSessionEvents,
            allowedAcr,
            allowedAmr);
        var forged = AuthenticationStrengthEvaluator.Evaluate(
            new AuthenticationStrength("gold", ["pwd"]),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            AuthorizationActions.SubscribeSessionEvents,
            allowedAcr,
            allowedAmr);
        var accepted = AuthenticationStrengthEvaluator.Evaluate(
            new AuthenticationStrength("acr:mfa", ["pwd", "mfa"]),
            AuthenticationStrengthEvaluator.ReviewerRelationship,
            AuthorizationActions.SubscribeSessionEvents,
            allowedAcr,
            allowedAmr);
        var participant = AuthenticationStrengthEvaluator.Evaluate(
            AuthenticationStrength.Empty,
            "participant",
            AuthorizationActions.SubscribeSessionEvents,
            allowedAcr,
            allowedAmr);

        Assert.Equal(HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, missing);
        Assert.Equal(HumanAuthenticationReasonCodes.UnrecognizedAuthenticationStrength, forged);
        Assert.Null(accepted);
        Assert.Null(participant);
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil")]
    [InlineData("/path%0d%0aLocation:%20https://evil.example")]
    [InlineData("/%09//evil.example")]
    public void Unsafe_return_paths_are_rejected(string returnPath)
    {
        if (returnPath.Contains("%0d", StringComparison.Ordinal))
        {
            returnPath = Uri.UnescapeDataString(returnPath);
        }

        Assert.False(SafeReturnPaths.TryNormalize(returnPath, out _));
    }

    [Fact]
    public void Relative_same_origin_return_paths_are_accepted()
    {
        Assert.True(SafeReturnPaths.TryNormalize("/work", out var normalized));
        Assert.Equal("/work", normalized);
        Assert.True(SafeReturnPaths.TryNormalize(null, out var fallback));
        Assert.Equal("/", fallback);
    }

    private static ApplicationSessionRecord CreateSession(
        DateTimeOffset createdAt,
        DateTimeOffset lastSeenAt,
        DateTimeOffset idleExpiresAt,
        DateTimeOffset absoluteExpiresAt) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Identity,
            "abc",
            AuthenticationStrength.Empty,
            null,
            new ApplicationSessionLifetime(createdAt, lastSeenAt, idleExpiresAt, absoluteExpiresAt),
            null,
            null,
            null,
            null);
}

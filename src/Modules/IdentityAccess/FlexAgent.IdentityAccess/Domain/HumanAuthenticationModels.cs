namespace FlexAgent.IdentityAccess.Domain;

public static class HumanInteractiveActorTypes
{
    public const string Interactive = "human.interactive";
}

public static class HumanAuthenticationReasonCodes
{
    public const string UnknownSubject = "authn.unknown_subject";
    public const string DisabledIdentity = "authn.disabled_identity";
    public const string ReboundIdentity = "authn.rebound_identity";
    public const string IssuerMismatch = "authn.issuer_mismatch";
    public const string ZeroOrganizationContext = "authn.zero_organization_context";
    public const string AmbiguousOrganizationContext = "authn.ambiguous_organization_context";
    public const string ClientSuppliedOrganizationRejected = "authn.client_organization_rejected";
    public const string ExpiredSession = "authn.expired_session";
    public const string RevokedSession = "authn.revoked_session";
    public const string RotatedSession = "authn.rotated_session";
    public const string MissingSession = "authn.missing_session";
    public const string InconsistentSessionState = "authn.inconsistent_session_state";
    public const string InsufficientAuthenticationStrength = "authn.insufficient_strength";
    public const string UnrecognizedAuthenticationStrength = "authn.unrecognized_strength";
    public const string InvalidProviderResponse = "authn.invalid_provider_response";
    public const string ProviderUnavailable = "authn.provider_unavailable";
    public const string UnsafeReturnPath = "authn.unsafe_return_path";
    public const string ReplayOrConsumedTransaction = "authn.replayed_transaction";
    public const string ConfigurationUnavailable = "authn.configuration_unavailable";
}

public static class ApplicationSessionTerminalReasons
{
    public const string LoginRotation = "login_rotation";
    public const string PrivilegeChange = "privilege_change";
    public const string SensitiveReauthentication = "sensitive_reauthentication";
    public const string Logout = "logout";
    public const string Revoked = "revoked";
    public const string Expired = "expired";
    public const string AccountDisabled = "account_disabled";
    public const string ProviderForcedLogout = "provider_forced_logout";
}

public static class AuthenticationSecurityEventTypes
{
    public const string LoginStarted = "login_started";
    public const string LoginCompleted = "login_completed";
    public const string LoginDenied = "login_denied";
    public const string SessionRotated = "session_rotated";
    public const string SessionRevoked = "session_revoked";
    public const string LogoutCompleted = "logout_completed";
    public const string ProviderLifecycleApplied = "provider_lifecycle_applied";
}

public sealed record ExactIssuerSubject(string Issuer, string Subject)
{
    public static ExactIssuerSubject? TryCreate(string? issuer, string? subject)
    {
        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(subject))
        {
            return null;
        }

        return new ExactIssuerSubject(issuer, subject);
    }

    public bool Matches(string issuer, string subject) =>
        string.Equals(Issuer, issuer, StringComparison.Ordinal)
        && string.Equals(Subject, subject, StringComparison.Ordinal);
}

public sealed record HumanIdentityBinding(
    Guid BindingId,
    ExactIssuerSubject Identity,
    Guid ActorId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DisabledAt)
{
    public bool IsEnabled => DisabledAt is null;
}

public sealed record AuthenticationStrength(string? Acr, IReadOnlyList<string> Amr)
{
    public static AuthenticationStrength Empty { get; } = new(null, []);

    public bool HasRecognizedEvidence(IReadOnlySet<string> allowedAcr, IReadOnlySet<string> allowedAmr)
    {
        ArgumentNullException.ThrowIfNull(allowedAcr);
        ArgumentNullException.ThrowIfNull(allowedAmr);
        if (!string.IsNullOrEmpty(Acr) && allowedAcr.Contains(Acr))
        {
            return true;
        }

        return Amr.Any(value => allowedAmr.Contains(value));
    }
}

public sealed record ApplicationSessionLifetime(
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset IdleExpiresAt,
    DateTimeOffset AbsoluteExpiresAt);

public sealed record ApplicationSessionRecord(
    Guid ApplicationSessionId,
    Guid ActorId,
    Guid OrganizationId,
    ExactIssuerSubject Identity,
    string? CredentialDigest,
    AuthenticationStrength Strength,
    string? ProviderSessionDigest,
    byte[]? ProviderIdTokenCiphertext,
    ApplicationSessionLifetime Lifetime,
    DateTimeOffset? RevokedAt,
    DateTimeOffset? RotatedAt,
    Guid? PredecessorSessionId,
    string? TerminalReason,
    string? SeatedDisplayName = null)
{
    public bool IsLive => RevokedAt is null && RotatedAt is null && CredentialDigest is not null;
}

public sealed record HumanIdentityResolution(
    bool Succeeded,
    string? ReasonCode,
    Guid? ActorId,
    Guid? OrganizationId)
{
    public static HumanIdentityResolution Permit(Guid actorId, Guid organizationId) =>
        new(true, null, actorId, organizationId);

    public static HumanIdentityResolution Deny(string reasonCode) =>
        new(false, reasonCode, null, null);
}

public static class SignInCompletionRecovery
{
    public const string QueryName = "signin";
    public const string DeniedValue = "denied";
    public const string DeniedPath = "/?signin=denied";
}

public static class SafeReturnPaths
{
    public static bool TryNormalize(string? returnPath, out string normalized)
    {
        normalized = "/";
        if (string.IsNullOrWhiteSpace(returnPath))
        {
            return true;
        }

        var candidate = Uri.UnescapeDataString(returnPath);
        if (!candidate.StartsWith('/')
            || candidate.StartsWith("//", StringComparison.Ordinal)
            || candidate.StartsWith("/\\", StringComparison.Ordinal)
            || candidate.Contains('\\', StringComparison.Ordinal)
            || candidate.Contains("//", StringComparison.Ordinal)
            || candidate.Contains(':', StringComparison.Ordinal)
            || candidate.Any(character => char.IsControl(character) || char.IsWhiteSpace(character)))
        {
            return false;
        }

        if (candidate.Contains("://", StringComparison.Ordinal))
        {
            return false;
        }

        normalized = StripDeniedCompletionQuery(candidate);
        return true;
    }

    private static string StripDeniedCompletionQuery(string candidate)
    {
        var hash = candidate.IndexOf('#');
        var fragment = hash >= 0 ? candidate[hash..] : string.Empty;
        var withoutFragment = hash >= 0 ? candidate[..hash] : candidate;
        var queryIndex = withoutFragment.IndexOf('?');
        if (queryIndex < 0)
        {
            return candidate;
        }

        var path = withoutFragment[..queryIndex];
        var kept = withoutFragment[(queryIndex + 1)..]
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part =>
                !part.Equals(SignInCompletionRecovery.QueryName, StringComparison.Ordinal)
                && !part.StartsWith(SignInCompletionRecovery.QueryName + "=", StringComparison.Ordinal))
            .ToArray();
        if (kept.Length == 0)
        {
            return path + fragment;
        }

        return path + "?" + string.Join('&', kept) + fragment;
    }
}

public static class HumanIdentityResolver
{
    public static HumanIdentityResolution Resolve(
        ExactIssuerSubject presented,
        string configuredIssuer,
        HumanIdentityBinding? binding,
        bool actorExists,
        bool actorDisabled,
        IReadOnlyCollection<Guid> eligibleOrganizationIds,
        Guid? clientSuppliedOrganizationId)
    {
        ArgumentNullException.ThrowIfNull(presented);
        ArgumentNullException.ThrowIfNull(eligibleOrganizationIds);

        if (clientSuppliedOrganizationId is not null)
        {
            return HumanIdentityResolution.Deny(
                HumanAuthenticationReasonCodes.ClientSuppliedOrganizationRejected);
        }

        if (!string.Equals(presented.Issuer, configuredIssuer, StringComparison.Ordinal))
        {
            return HumanIdentityResolution.Deny(HumanAuthenticationReasonCodes.IssuerMismatch);
        }

        if (binding is null || !binding.Identity.Matches(presented.Issuer, presented.Subject))
        {
            return HumanIdentityResolution.Deny(HumanAuthenticationReasonCodes.UnknownSubject);
        }

        if (!binding.IsEnabled || actorDisabled)
        {
            return HumanIdentityResolution.Deny(HumanAuthenticationReasonCodes.DisabledIdentity);
        }

        if (!actorExists || binding.ActorId == Guid.Empty)
        {
            return HumanIdentityResolution.Deny(HumanAuthenticationReasonCodes.UnknownSubject);
        }

        var distinctOrganizations = eligibleOrganizationIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (distinctOrganizations.Length == 0)
        {
            return HumanIdentityResolution.Deny(HumanAuthenticationReasonCodes.ZeroOrganizationContext);
        }

        if (distinctOrganizations.Length > 1)
        {
            return HumanIdentityResolution.Deny(
                HumanAuthenticationReasonCodes.AmbiguousOrganizationContext);
        }

        return HumanIdentityResolution.Permit(binding.ActorId, distinctOrganizations[0]);
    }
}

public static class ApplicationSessionPolicy
{
    public static readonly TimeSpan MaximumInactivity = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan MaximumAbsoluteLifetime = TimeSpan.FromHours(12);

    public static TimeSpan BoundInactivity(TimeSpan configured)
    {
        if (configured <= TimeSpan.Zero || configured > MaximumInactivity)
        {
            return MaximumInactivity;
        }

        return configured;
    }

    public static TimeSpan BoundAbsoluteLifetime(TimeSpan configured)
    {
        if (configured <= TimeSpan.Zero || configured > MaximumAbsoluteLifetime)
        {
            return MaximumAbsoluteLifetime;
        }

        return configured;
    }

    public static ApplicationSessionLifetime CreateLifetime(
        DateTimeOffset now,
        TimeSpan inactivity,
        TimeSpan absoluteLifetime)
    {
        var boundedInactivity = BoundInactivity(inactivity);
        var boundedAbsolute = BoundAbsoluteLifetime(absoluteLifetime);
        return new ApplicationSessionLifetime(
            now,
            now,
            now + boundedInactivity,
            now + boundedAbsolute);
    }

    public static bool IsExpired(ApplicationSessionRecord session, DateTimeOffset now) =>
        now >= session.Lifetime.IdleExpiresAt || now >= session.Lifetime.AbsoluteExpiresAt;

    public static string? AuthenticateFailureReason(ApplicationSessionRecord? session, DateTimeOffset now)
    {
        if (session is null)
        {
            return HumanAuthenticationReasonCodes.MissingSession;
        }

        if (session.RotatedAt is not null)
        {
            return HumanAuthenticationReasonCodes.RotatedSession;
        }

        if (session.RevokedAt is not null)
        {
            return HumanAuthenticationReasonCodes.RevokedSession;
        }

        if (session.CredentialDigest is null || !session.IsLive)
        {
            return HumanAuthenticationReasonCodes.RevokedSession;
        }

        if (IsExpired(session, now))
        {
            return HumanAuthenticationReasonCodes.ExpiredSession;
        }

        return null;
    }

    public static ApplicationSessionLifetime TouchActivity(
        ApplicationSessionLifetime lifetime,
        DateTimeOffset now,
        TimeSpan inactivity)
    {
        var boundedInactivity = BoundInactivity(inactivity);
        var nextIdle = now + boundedInactivity;
        if (nextIdle > lifetime.AbsoluteExpiresAt)
        {
            nextIdle = lifetime.AbsoluteExpiresAt;
        }

        return lifetime with
        {
            LastSeenAt = now,
            IdleExpiresAt = nextIdle,
        };
    }
}

public static class AuthenticationStrengthEvaluator
{
    public const string ReviewerRelationship = "reviewer";
    public const string AdministratorRelationship = "administrator";

    public static bool RequiresMfa(string? relationship, string action)
    {
        if (string.Equals(relationship, ReviewerRelationship, StringComparison.Ordinal)
            || string.Equals(relationship, AdministratorRelationship, StringComparison.Ordinal))
        {
            return true;
        }

        return action.Contains("release", StringComparison.Ordinal)
            || action.Contains("export", StringComparison.Ordinal);
    }

    public static string? Evaluate(
        AuthenticationStrength presented,
        string? relationship,
        string action,
        IReadOnlySet<string> allowedAcr,
        IReadOnlySet<string> allowedAmr)
    {
        ArgumentNullException.ThrowIfNull(presented);
        ArgumentNullException.ThrowIfNull(allowedAcr);
        ArgumentNullException.ThrowIfNull(allowedAmr);

        if (!RequiresMfa(relationship, action))
        {
            return null;
        }

        if (presented.HasRecognizedEvidence(allowedAcr, allowedAmr))
        {
            return null;
        }

        if (!string.IsNullOrEmpty(presented.Acr) || presented.Amr.Count > 0)
        {
            return HumanAuthenticationReasonCodes.UnrecognizedAuthenticationStrength;
        }

        return HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength;
    }
}

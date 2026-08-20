using System.Security.Cryptography;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.IdentityAccess.Application;

public sealed class HumanAuthenticationCoordinator(
    IHumanIdentityBindingStore bindings,
    IApplicationSessionStore sessions,
    IAuthenticationSecurityEventWriter audit,
    ILookupDigestCalculator digests,
    IDatabaseClock clock,
    HumanAuthenticationOptions options) : IHumanAuthenticationCoordinator
{
    public async Task<HumanAuthenticationResult> CompleteLoginAsync(
        ValidatedHumanLogin login,
        Guid? clientSuppliedOrganizationId,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(login);
        var now = await clock.GetUtcNowAsync(cancellationToken).ConfigureAwait(false);
        var resolution = await ResolveAsync(login.Identity, clientSuppliedOrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (!resolution.Succeeded)
        {
            await WriteAsync(
                AuthenticationSecurityEventTypes.LoginDenied,
                "deny",
                resolution.ReasonCode!,
                correlationId,
                now,
                null,
                null,
                null,
                cancellationToken).ConfigureAwait(false);
            return HumanAuthenticationResult.Deny(resolution.ReasonCode!);
        }

        var created = await CreateSessionAsync(
            resolution.ActorId!.Value,
            resolution.OrganizationId!.Value,
            login,
            predecessorSessionId: null,
            now,
            cancellationToken).ConfigureAwait(false);
        await WriteAsync(
            AuthenticationSecurityEventTypes.LoginCompleted,
            "permit",
            "authn.login_completed",
            correlationId,
            now,
            created.ActorId,
            created.OrganizationId,
            created.ApplicationSessionId,
            cancellationToken).ConfigureAwait(false);
        return created;
    }

    public async Task<AuthenticatedApplicationSession?> AuthenticateAsync(
        string rawCredential,
        bool advanceActivity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawCredential) || rawCredential.Length > 256)
        {
            return null;
        }

        var now = await clock.GetUtcNowAsync(cancellationToken).ConfigureAwait(false);
        var digest = digests.Compute(rawCredential);
        var session = await sessions.FindLiveByCredentialDigestAsync(digest, cancellationToken)
            .ConfigureAwait(false);
        var failure = ApplicationSessionPolicy.AuthenticateFailureReason(session, now);
        if (failure is not null || session is null)
        {
            return null;
        }

        var actor = await bindings.GetActorStateAsync(session.ActorId, cancellationToken).ConfigureAwait(false);
        if (!actor.Exists || actor.Disabled)
        {
            return null;
        }

        if (advanceActivity)
        {
            var touched = ApplicationSessionPolicy.TouchActivity(
                session.Lifetime,
                now,
                options.Inactivity);
            await sessions.TouchActivityAsync(session.ApplicationSessionId, touched, cancellationToken)
                .ConfigureAwait(false);
        }

        return new AuthenticatedApplicationSession(
            session.ApplicationSessionId,
            session.ActorId,
            session.OrganizationId,
            session.Strength,
            session.Identity);
    }

    public async Task<HumanAuthenticationResult> RotateAsync(
        Guid applicationSessionId,
        string terminalReason,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(terminalReason);
        var now = await clock.GetUtcNowAsync(cancellationToken).ConfigureAwait(false);
        var current = await sessions.GetByIdAsync(applicationSessionId, cancellationToken).ConfigureAwait(false);
        if (current is null || ApplicationSessionPolicy.AuthenticateFailureReason(current, now) is not null)
        {
            return HumanAuthenticationResult.Deny(HumanAuthenticationReasonCodes.MissingSession);
        }

        await sessions.TerminateLiveAsync(
            current.ApplicationSessionId,
            now,
            terminalReason,
            rotated: true,
            cancellationToken).ConfigureAwait(false);
        var rotated = await CreateSessionAsync(
            current.ActorId,
            current.OrganizationId,
            new ValidatedHumanLogin(current.Identity, current.Strength, null),
            current.ApplicationSessionId,
            now,
            cancellationToken,
            current.ProviderSessionDigest).ConfigureAwait(false);
        await WriteAsync(
            AuthenticationSecurityEventTypes.SessionRotated,
            "permit",
            terminalReason,
            correlationId,
            now,
            rotated.ActorId,
            rotated.OrganizationId,
            rotated.ApplicationSessionId,
            cancellationToken).ConfigureAwait(false);
        return rotated;
    }

    public async Task<bool> LogoutAsync(
        string rawCredential,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var now = await clock.GetUtcNowAsync(cancellationToken).ConfigureAwait(false);
        var digest = digests.Compute(rawCredential);
        var session = await sessions.FindLiveByCredentialDigestAsync(digest, cancellationToken)
            .ConfigureAwait(false);
        if (session is null)
        {
            return false;
        }

        await sessions.TerminateLiveAsync(
            session.ApplicationSessionId,
            now,
            ApplicationSessionTerminalReasons.Logout,
            rotated: false,
            cancellationToken).ConfigureAwait(false);
        await WriteAsync(
            AuthenticationSecurityEventTypes.LogoutCompleted,
            "permit",
            ApplicationSessionTerminalReasons.Logout,
            correlationId,
            now,
            session.ActorId,
            session.OrganizationId,
            session.ApplicationSessionId,
            cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<int> ApplyProviderForcedLogoutAsync(
        string providerSessionId,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSessionId);
        var now = await clock.GetUtcNowAsync(cancellationToken).ConfigureAwait(false);
        var count = await sessions.RevokeLiveByProviderSessionDigestAsync(
            digests.Compute(providerSessionId),
            now,
            ApplicationSessionTerminalReasons.ProviderForcedLogout,
            cancellationToken).ConfigureAwait(false);
        await WriteAsync(
            AuthenticationSecurityEventTypes.ProviderLifecycleApplied,
            "permit",
            ApplicationSessionTerminalReasons.ProviderForcedLogout,
            correlationId,
            now,
            null,
            null,
            null,
            cancellationToken).ConfigureAwait(false);
        return count;
    }

    public async Task<int> ApplyAccountDisablementAsync(
        ExactIssuerSubject identity,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var now = await clock.GetUtcNowAsync(cancellationToken).ConfigureAwait(false);
        await bindings.DisableByIdentityAsync(identity, now, cancellationToken).ConfigureAwait(false);
        var count = await sessions.RevokeLiveByIdentityAsync(
            identity,
            now,
            ApplicationSessionTerminalReasons.AccountDisabled,
            cancellationToken).ConfigureAwait(false);
        await WriteAsync(
            AuthenticationSecurityEventTypes.ProviderLifecycleApplied,
            "permit",
            ApplicationSessionTerminalReasons.AccountDisabled,
            correlationId,
            now,
            null,
            null,
            null,
            cancellationToken).ConfigureAwait(false);
        return count;
    }

    private async Task<HumanIdentityResolution> ResolveAsync(
        ExactIssuerSubject identity,
        Guid? clientSuppliedOrganizationId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(identity.Issuer, options.Issuer, StringComparison.Ordinal))
        {
            return HumanIdentityResolution.Deny(HumanAuthenticationReasonCodes.IssuerMismatch);
        }

        var binding = await bindings.FindByIdentityAsync(identity, cancellationToken).ConfigureAwait(false);
        if (binding is null)
        {
            return HumanIdentityResolver.Resolve(
                identity,
                options.Issuer,
                null,
                false,
                false,
                [],
                clientSuppliedOrganizationId);
        }

        var actor = await bindings.GetActorStateAsync(binding.ActorId, cancellationToken).ConfigureAwait(false);
        var organizations = await bindings.ListEligibleOrganizationIdsAsync(binding.ActorId, cancellationToken)
            .ConfigureAwait(false);
        return HumanIdentityResolver.Resolve(
            identity,
            options.Issuer,
            binding,
            actor.Exists,
            actor.Disabled,
            organizations,
            clientSuppliedOrganizationId);
    }

    private async Task<HumanAuthenticationResult> CreateSessionAsync(
        Guid actorId,
        Guid organizationId,
        ValidatedHumanLogin login,
        Guid? predecessorSessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken,
        string? inheritedProviderSessionDigest = null)
    {
        var rawCredential = OpaqueSessionCredential.Create();
        var providerSessionDigest = inheritedProviderSessionDigest
            ?? (string.IsNullOrWhiteSpace(login.ProviderSessionId)
                ? null
                : digests.Compute(login.ProviderSessionId));
        var session = new ApplicationSessionRecord(
            Guid.NewGuid(),
            actorId,
            organizationId,
            login.Identity,
            digests.Compute(rawCredential),
            login.Strength,
            providerSessionDigest,
            ApplicationSessionPolicy.CreateLifetime(now, options.Inactivity, options.AbsoluteLifetime),
            null,
            null,
            predecessorSessionId,
            null);
        await sessions.InsertAsync(session, cancellationToken).ConfigureAwait(false);
        return HumanAuthenticationResult.Permit(
            session.ApplicationSessionId,
            actorId,
            organizationId,
            rawCredential);
    }

    private Task WriteAsync(
        string eventType,
        string outcome,
        string reasonCode,
        Guid correlationId,
        DateTimeOffset now,
        Guid? actorId,
        Guid? organizationId,
        Guid? applicationSessionId,
        CancellationToken cancellationToken) =>
        audit.WriteAsync(
            new AuthenticationSecurityEvent(
                Guid.NewGuid(),
                now,
                eventType,
                outcome,
                reasonCode,
                correlationId,
                actorId,
                organizationId,
                applicationSessionId),
            cancellationToken);
}

public sealed class HumanAuthenticationOptions
{
    public string Issuer { get; init; } = string.Empty;

    public TimeSpan Inactivity { get; init; } = ApplicationSessionPolicy.MaximumInactivity;

    public TimeSpan AbsoluteLifetime { get; init; } = ApplicationSessionPolicy.MaximumAbsoluteLifetime;
}

public static class OpaqueSessionCredential
{
    public static string Create()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    public static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

public sealed class HmacLookupDigestCalculator(byte[] key) : ILookupDigestCalculator
{
    public string Compute(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var hash = HMACSHA256.HashData(key, System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

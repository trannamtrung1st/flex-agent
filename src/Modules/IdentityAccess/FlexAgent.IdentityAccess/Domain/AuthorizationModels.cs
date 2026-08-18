namespace FlexAgent.IdentityAccess.Domain;

public static class AuthorizationActions
{
    public const string RegisterConfigurationSourceVersion = "configuration_source_version.register";
    public const string SubscribeSessionEvents = "session.events.subscribe";
    public const string FireSessionTimerLane = "session.timer_lane.fire";
    public const string IssueServiceDelegation = "service_delegation.issue";
    public const string RevokeServiceDelegation = "service_delegation.revoke";
}

public static class AuthorizationResourceTypes
{
    public const string ConfigurationSource = "configuration_source";
    public const string ConfigurationSourceVersion = "configuration_source_version";
    public const string Session = "session";
    public const string ServiceDelegation = "service_delegation";
}

public static class AuthorizationReferenceTypes
{
    public const string ServiceDelegation = "service_delegation";
    public const string ActorOrganizationGrant = "actor_organization_grant";
}

public static class AuthorizationReasonCodes
{
    public const string MissingActor = "auth.missing_actor";
    public const string UnknownActor = "auth.unknown_actor";
    public const string DeniedNoGrant = "auth.denied_no_grant";
    public const string RevokedGrant = "auth.revoked_grant";
    public const string ScopeMismatch = "auth.scope_mismatch";
    public const string ParentNotFound = "auth.parent_not_found";
    public const string Unavailable = "auth.unavailable";
    public const string MissingDelegation = "auth.missing_delegation";
    public const string RevokedDelegation = "auth.revoked_delegation";
    public const string ExpiredDelegation = "auth.expired_delegation";
    public const string DelegationNotEffective = "auth.delegation_not_effective";
    public const string DelegationActorMismatch = "auth.delegation_actor_mismatch";
    public const string DelegationActionMismatch = "auth.delegation_action_mismatch";
}

public static class AuthorizationOutcomes
{
    public const string Permit = "permit";
    public const string Deny = "deny";
}

public sealed record OrganizationScope(Guid OrganizationId);

public sealed record ResourceScope(
    OrganizationScope Organization,
    string ResourceType,
    Guid ResourceId);

public sealed record TrustedActor(Guid ActorId, string ActorType);

public sealed record AuthorizationRequest(
    TrustedActor? Actor,
    OrganizationScope Organization,
    string Action,
    ResourceScope Resource,
    string SourceChannel,
    Guid CorrelationId,
    Guid? DelegationId = null,
    Guid? ActivityId = null,
    Guid? ParticipantId = null,
    Guid? AttemptId = null);

public sealed record ServiceDelegationIssue(
    Guid DelegationId,
    Guid ServiceActorId,
    string AllowedAction,
    string SystemPurpose,
    string InitiatingAuthority,
    DateTimeOffset EffectiveAt,
    DateTimeOffset? ExpiresAt = null);

public sealed record ServiceDelegationMutationContext(
    TrustedActor Initiator,
    Guid CorrelationId,
    string SourceChannel,
    string Reason);

public sealed record AuthorizedServiceDelegationIssue(
    ServiceDelegationIssue Issue,
    ServiceDelegationMutationContext Mutation);

public sealed class AuthorizationDeniedException : InvalidOperationException
{
    public AuthorizationDeniedException(string reasonCode)
        : base($"Authorization denied ({reasonCode}).")
    {
        ReasonCode = reasonCode;
    }

    public string ReasonCode { get; }
}

public sealed record AuthorizationDecision(
    bool IsPermitted,
    string Outcome,
    string ReasonCode,
    long? RelationshipVersion,
    string PolicyVersion,
    string? AuthorizationReferenceType = null,
    Guid? AuthorizationReferenceId = null)
{
    public static AuthorizationDecision Permit(
        long relationshipVersion,
        string policyVersion = "policy.v1",
        string? authorizationReferenceType = null,
        Guid? authorizationReferenceId = null) =>
        new(
            true,
            AuthorizationOutcomes.Permit,
            "auth.permitted",
            relationshipVersion,
            policyVersion,
            authorizationReferenceType,
            authorizationReferenceId);

    public static AuthorizationDecision Deny(string reasonCode, string policyVersion = "policy.v1") =>
        new(false, AuthorizationOutcomes.Deny, reasonCode, null, policyVersion);
}

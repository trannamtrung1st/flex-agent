namespace FlexAgent.Contracts.Audit;

public sealed record AuditEventV1(
    string EventSchema,
    string EventId,
    AuditActorV1 Actor,
    string OrganizationId,
    string Action,
    AuditResourceRefV1 ResourceRef,
    string Outcome,
    string? ReasonCode,
    string OccurredAt,
    string CorrelationId,
    string? DurabilityClass);

public sealed record AuditActorV1(string ActorType, string ActorId);

public sealed record AuditResourceRefV1(string ResourceType, string ResourceId);

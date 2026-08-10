namespace FlexAgent.Contracts.Browser;

public static class BrowserSchemaVersion
{
    public const string V1 = "v1";
}

/// <summary>Opaque one-time grant created by the test harness; exchanged for an HttpOnly application session.</summary>
public sealed record ScenarioGrantRequestV1(
    string ScenarioId,
    string ActorStage);

public sealed record ScenarioGrantResponseV1(
    string SchemaVersion,
    string GrantToken,
    DateTimeOffset ExpiresAt);

public sealed record ScenarioGrantExchangeRequestV1(string GrantToken);

public sealed record ScenarioGrantExchangeResponseV1(
    string SchemaVersion,
    DateTimeOffset ExpiresAt);

public sealed record ScenarioGrantExchangeResultV1(
    string SessionId,
    ScenarioGrantExchangeResponseV1 Response);

public sealed record ActorContextV1(
    string SchemaVersion,
    string ActorId,
    string DisplayName,
    string OrganizationId,
    string OrganizationName,
    IReadOnlyList<string> Capabilities,
    string ActorStage,
    bool IsSynthetic);

public sealed record PermittedActionV1(
    string ActionId,
    string Label,
    string? Description,
    bool IsDestructive);

public sealed record NavigationDestinationV1(
    string DestinationId,
    string Label,
    string Route,
    string Tier,
    bool IsAvailable,
    string? UnavailableReason);

public sealed record NavigationProjectionV1(
    string SchemaVersion,
    IReadOnlyList<NavigationDestinationV1> Destinations);

public sealed record HomeWorkItemV1(
    string ItemId,
    string Title,
    string StatusLabel,
    string PriorityBand,
    string? Route,
    string? NextActionLabel);

public sealed record HomeProjectionV1(
    string SchemaVersion,
    string Greeting,
    IReadOnlyList<HomeWorkItemV1> WorkItems,
    IReadOnlyList<PermittedActionV1> PermittedActions);

public sealed record ActivitySummaryV1(
    string ActivityId,
    string Title,
    string Form,
    string Type,
    string StatusLabel,
    string? Route);

public sealed record ActivitiesListProjectionV1(
    string SchemaVersion,
    IReadOnlyList<ActivitySummaryV1> Activities,
    IReadOnlyList<PermittedActionV1> PermittedActions);

public sealed record ReadinessCategoryV1(
    string CategoryId,
    string Label,
    string Status,
    bool IsBlocking,
    string? Detail);

public sealed record ActivityDetailProjectionV1(
    string SchemaVersion,
    string ActivityId,
    string Title,
    string Form,
    string Type,
    string LifecycleState,
    int ExpectedVersion,
    IReadOnlyList<ReadinessCategoryV1> ReadinessCategories,
    IReadOnlyList<PermittedActionV1> PermittedActions,
    string? BaselineSummary);

public sealed record EnrollmentSummaryV1(
    string EnrollmentId,
    string ParticipantLabel,
    string StatusLabel);

public sealed record ParticipantChoiceV1(
    string ParticipantId,
    string DisplayLabel);

public sealed record EnrollmentProjectionV1(
    string SchemaVersion,
    string ActivityId,
    string LifecycleState,
    IReadOnlyList<EnrollmentSummaryV1> Enrollments,
    IReadOnlyList<ParticipantChoiceV1> PermittedParticipants,
    IReadOnlyList<PermittedActionV1> PermittedActions);

public sealed record SubmissionVersionV1(
    string VersionId,
    string Label,
    string StatusLabel,
    string? ContentPreview);

public sealed record AssignmentProjectionV1(
    string SchemaVersion,
    string EnrollmentId,
    string ActivityTitle,
    string TaskSummary,
    string Timezone,
    string? Deadline,
    string AttemptStatus,
    IReadOnlyList<SubmissionVersionV1> SubmissionVersions,
    IReadOnlyList<PermittedActionV1> PermittedActions,
    string LifecycleState);

public sealed record SessionTranscriptItemV1(
    string ItemId,
    string Role,
    string Content,
    string Status,
    string? OccurredAt);

public sealed record SessionProjectionV1(
    string SchemaVersion,
    string SessionId,
    string LifecycleState,
    string? RemainingTime,
    IReadOnlyList<SessionTranscriptItemV1> Transcript,
    IReadOnlyList<PermittedActionV1> PermittedActions,
    string? BoundSubmissionSummary,
    int SessionVersion,
    string? LastSequence);

public sealed record ReviewCaseSummaryV1(
    string CaseId,
    string Title,
    string StatusLabel,
    string? Route);

public sealed record ReviewWorkProjectionV1(
    string SchemaVersion,
    IReadOnlyList<ReviewCaseSummaryV1> Cases,
    IReadOnlyList<PermittedActionV1> PermittedActions);

public sealed record EvidenceItemV1(
    string EvidenceId,
    string Label,
    string LocatorSummary,
    string? ContentPreview);

public sealed record CriterionResultV1(
    string CriterionId,
    string Label,
    string Outcome,
    IReadOnlyList<EvidenceItemV1> Evidence);

public sealed record ReviewCaseDetailProjectionV1(
    string SchemaVersion,
    string CaseId,
    string StatusLabel,
    string CandidateLineage,
    IReadOnlyList<CriterionResultV1> Criteria,
    IReadOnlyList<PermittedActionV1> PermittedActions,
    string? HumanRevisionDraft,
    string LifecycleState,
    int ExpectedVersion);

public sealed record ReleaseItemSummaryV1(
    string ReleaseId,
    string Title,
    string StatusLabel,
    string? Route);

public sealed record ReleaseWorkProjectionV1(
    string SchemaVersion,
    IReadOnlyList<ReleaseItemSummaryV1> Items,
    IReadOnlyList<PermittedActionV1> PermittedActions);

public sealed record ReleaseDetailProjectionV1(
    string SchemaVersion,
    string ReleaseId,
    string StatusLabel,
    string ResultPreview,
    string AudiencePolicy,
    IReadOnlyList<PermittedActionV1> PermittedActions,
    int ExpectedVersion,
    string LifecycleState);

public sealed record ResultItemV1(
    string ResultId,
    string ActivityTitle,
    string StatusLabel,
    string? Route);

public sealed record ResultsProjectionV1(
    string SchemaVersion,
    IReadOnlyList<ResultItemV1> Results,
    IReadOnlyList<PermittedActionV1> PermittedActions);

public sealed record ResultDetailProjectionV1(
    string SchemaVersion,
    string ResultId,
    string StatusLabel,
    string? Content,
    string LifecycleState,
    string? CorrectionNote);

public sealed record GovernanceEntryV1(
    string EntryId,
    string Action,
    string ActorLabel,
    string OccurredAt,
    string Outcome);

public sealed record GovernanceProjectionV1(
    string SchemaVersion,
    IReadOnlyList<GovernanceEntryV1> Entries,
    IReadOnlyList<PermittedActionV1> PermittedActions,
    bool IsPartial);

public sealed record PlannedTierProjectionV1(
    string SchemaVersion,
    string ModuleName,
    string Tier,
    string Message,
    IReadOnlyList<PermittedActionV1> PermittedActions);

public sealed record BrowserCommandEnvelopeV1(
    string SchemaVersion,
    string CommandId,
    string IdempotencyKey,
    string CommandType,
    string? ResourceId,
    int? ExpectedVersion,
    IReadOnlyDictionary<string, string>? Payload);

public sealed record BrowserCommandResultV1(
    string SchemaVersion,
    string Outcome,
    string? CorrelationId,
    int? NewVersion,
    string? LifecycleState,
    string? PermittedRecoveryAction,
    string? SafeMessage);

public sealed record ProtectedContentResponseV1(
    string SchemaVersion,
    string Outcome,
    string? SafeMessage);

public sealed record AccessChangedResponseV1(
    string SchemaVersion,
    string Outcome,
    string SafeMessage);

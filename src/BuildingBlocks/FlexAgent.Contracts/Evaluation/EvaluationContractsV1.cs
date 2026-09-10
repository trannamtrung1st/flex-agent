using FlexAgent.Contracts.Evidence;
using FlexAgent.Contracts.Manifest;

namespace FlexAgent.Contracts.Evaluation;

public sealed record EvaluationSourceRefV1(string SourceId, string SourceVersion, string ContentDigest);

public sealed record EvaluationRequestV1(
    string SchemaVersion,
    string RequestId,
    string RequestKind,
    SessionOwnershipRefV1 Ownership,
    string HandoffId,
    string ManifestSealProcedureId,
    string FrozenInputDigest,
    EvaluationSourceRefV1 ProcedureRef,
    string IdempotencyKey,
    string DelegationRef,
    string State,
    string? PredecessorEvaluationId,
    string? ReplacementReason);

public sealed record EvaluationWorkV1(
    string SchemaVersion,
    string WorkId,
    string RequestId,
    string RequestKind,
    SessionOwnershipRefV1 Ownership,
    string FrozenInputDigest,
    EvaluationSourceRefV1 ProcedureRef,
    string EvaluatorRegistryVersion,
    string DelegationRef,
    string State,
    int AttemptOrdinal,
    string AvailableAt,
    string Timeout,
    int MaxAttempts,
    string? LeaseExpiresAt,
    string? FailureCategory);

public sealed record DeterministicInvocationV1(
    string SchemaVersion,
    string InvocationId,
    string RequestId,
    string CriterionId,
    string CriterionVersion,
    SessionOwnershipRefV1 Ownership,
    string EvaluatorId,
    string EvaluatorVersion,
    string EvaluatorDigest,
    string ConfigurationDigest,
    string DependencyDigest,
    string CanonicalInputDigest,
    string CpuTimeLimit,
    string ElapsedTimeLimit,
    int MemoryLimitBytes,
    int OutputLimitBytes,
    string StartedAt,
    string FinishedAt,
    string Outcome,
    ProtectedPayloadRefV1? InputRef,
    ProtectedPayloadRefV1? OutputRef,
    string? FailureCategory);

public sealed record EvaluationProviderArtifactV1(
    string SchemaVersion,
    string ArtifactId,
    string RequestId,
    int AttemptOrdinal,
    SessionOwnershipRefV1 Ownership,
    string ModelProfileId,
    string ModelProfileVersion,
    string ModelProfileDigest,
    string CredentialBindingReference,
    ProtectedPayloadRefV1 RequestRef,
    string Outcome,
    ProtectedPayloadRefV1? ResponseRef,
    string? FailureCategory);

public sealed record EvaluationModelPermittedEvidenceV1(
    string EvidenceId,
    string SourceType,
    string ContentDigest);

public sealed record EvaluationModelDeterministicFactV1(
    string SourceId,
    string ContentDigest,
    ProtectedPayloadRefV1 ProtectedRef);

public sealed record EvaluationModelRequestV1(
    string SchemaVersion,
    string RequestId,
    string InvocationAttemptId,
    string CriterionId,
    string CriterionVersion,
    string EvaluatorMode,
    SessionOwnershipRefV1 Ownership,
    string InputSchemaId,
    string OutputSchemaId,
    string InstructionVersion,
    string ModelProfileId,
    string ModelProfileVersion,
    string ModelProfileDigest,
    string CredentialBindingReference,
    IReadOnlyList<EvaluationModelPermittedEvidenceV1> PermittedEvidence,
    IReadOnlyList<EvaluationModelDeterministicFactV1>? DeterministicFacts,
    int MaxContextUnicodeScalars);

public sealed record EvaluationModelResponseV1(
    string SchemaVersion,
    string OutputSchemaId,
    string CriterionId,
    string CriterionVersion,
    string EvaluatorMode,
    string Status,
    string Confidence,
    IReadOnlyList<string> Uncertainty,
    string Rationale,
    IReadOnlyList<string> EvidenceIds,
    ProtectedPayloadRefV1 ResponseRef,
    object? Score,
    string? ProvisionalFeedback,
    string? DeterministicInvocationId);

public sealed record CriterionJudgmentV1(
    string SchemaVersion,
    string JudgmentId,
    string EvaluationId,
    string CriterionId,
    string CriterionVersion,
    string EvaluatorMode,
    string Status,
    string Confidence,
    IReadOnlyList<string> Uncertainty,
    string Rationale,
    IReadOnlyList<string> EvidenceIds,
    object? Score,
    string? ProvisionalFeedback,
    string? DeterministicInvocationId);

public sealed record EvaluationArtifactV1(
    string SchemaVersion,
    string EvaluationId,
    string RequestId,
    SessionOwnershipRefV1 Ownership,
    EvaluationSourceRefV1 ProcedureRef,
    string ConfigurationDigest,
    string ManifestDigest,
    string ManifestSealProcedureId,
    string EvidenceSetId,
    string EvidenceSetDigest,
    IReadOnlyList<string> CriterionIds,
    string AggregateStatus,
    string CompletedAt,
    string CreationServiceId,
    string? PredecessorEvaluationId);

public sealed record EvaluationAnnotationV1(
    string SchemaVersion,
    string AnnotationId,
    string EvaluationId,
    string Kind,
    string Disposition,
    string Reason,
    string ActorType,
    string ActorId,
    string OccurredAt,
    string? EvidenceId);

public sealed record EvaluationReplacementV1(
    string SchemaVersion,
    string LineageId,
    string PredecessorEvaluationId,
    string SuccessorRequestId,
    string SuccessorEvaluationId,
    string Reason,
    string ActorType,
    string ActorId,
    string OccurredAt);

public sealed record EvaluationReviewHandoffV1(
    string SchemaVersion,
    string HandoffId,
    string EvaluationId,
    SessionOwnershipRefV1 Ownership,
    string CaseState,
    bool InitialCandidateRecorded,
    string OccurredAt,
    string? InitialCandidateReason);

public sealed record EvaluationProcedureV1(
    string ProcedureSchema,
    string ProcedureId,
    string ProcedureVersion,
    IReadOnlyList<EvaluationProcedureCriterionV1> Criteria,
    EvaluationAggregationV1 Aggregation,
    string InsufficiencyBehavior,
    string NotApplicableBehavior,
    string ConflictBehavior,
    EvaluationRetryBoundsV1 RetryBounds,
    EvaluationResourceBoundsV1 ResourceBounds,
    string ReviewPolicyRef,
    string ReplacementPolicyRef,
    string LifecyclePolicyRef);

public sealed record EvaluationProcedureCriterionV1(
    string CriterionId,
    string CriterionVersion,
    string DisplayLabel,
    string EvaluatorMode,
    IReadOnlyList<string> PermittedStatuses,
    object ScoreField,
    EvaluationConfidenceFieldV1 ConfidenceField,
    IReadOnlyList<string> UncertaintyCategories,
    EvaluationEvidenceRequirementsV1 EvidenceRequirements,
    int RationaleMaxUnicodeScalars,
    bool ProvisionalFeedbackPermitted,
    DeterministicEvaluatorBindingV1? DeterministicEvaluator,
    EvaluationAgentIoV1? AgentIo);

public sealed record EvaluationConfidenceFieldV1(string FieldKind, IReadOnlyList<string> PermittedValues);

public sealed record EvaluationEvidenceRequirementsV1(
    int MinimumItems,
    int MaximumItems,
    IReadOnlyList<string> PermittedSourceTypes,
    bool WholeItemFallbackPermitted);

public sealed record DeterministicEvaluatorBindingV1(
    string EvaluatorId,
    string EvaluatorVersion,
    string EvaluatorDigest,
    string Operation,
    string InputSchemaId,
    string OutputSchemaId,
    string CanonicalizationProcedure,
    string ConfigurationDigest,
    string DependencyDigest,
    string CpuTimeLimit,
    string ElapsedTimeLimit,
    int MemoryLimitBytes,
    int OutputLimitBytes,
    string NetworkEgress,
    string ExecutableSelection);

public sealed record EvaluationAgentIoV1(
    string InputSchemaId,
    string OutputSchemaId,
    int MaxContextUnicodeScalars);

public sealed record EvaluationAggregationV1(string AggregationKind);

public sealed record EvaluationRetryBoundsV1(int MaxAttempts, string Backoff, string AttemptTimeout);

public sealed record EvaluationResourceBoundsV1(
    string ElapsedTimeout,
    int MaxEvidenceItems,
    int MaxProviderOutputBytes);

public sealed record ScoreFieldNoneV1(string FieldKind);

public sealed record IntegerRangeScoreFieldV1(string FieldKind, int Minimum, int Maximum);

public sealed record EnumeratedDecisionScoreFieldV1(string FieldKind, IReadOnlyList<string> PermittedValues);

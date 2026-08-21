namespace FlexAgent.AssessmentConfiguration.Domain;

public static class AssessmentActivityForms
{
    public const string Campaign = "campaign";
}

public static class AssessmentConfiguredTypes
{
    public const string Assessment = "assessment";
}

public static class CohortStates
{
    public const string Draft = "draft";
    public const string Activated = "activated";
}

public static class MemoryReadModes
{
    public const string Disabled = "disabled";
    public const string ImmutableSnapshot = "immutable_snapshot";
}

public static class SourceLifecycleStates
{
    public const string Available = "available";
    public const string Revoked = "revoked";
    public const string Unavailable = "unavailable";
    public const string MutableAlias = "mutable_alias";
}

public static class DeploymentEnvironments
{
    public const string Development = "development";
    public const string Testing = "testing";
    public const string Production = "production";
}

public static class ReadinessSeverities
{
    public const string Ready = "ready";
    public const string Warning = "warning";
    public const string Blocked = "blocked";
}

public static class FairnessClassifications
{
    public const string Inherited = "inherited";
    public const string ActivitySupplied = "activity_supplied";
    public const string CohortSupplied = "cohort_supplied";
    public const string Derived = "derived";
    public const string MostRestrictive = "most_restrictive";
    public const string ApprovedException = "approved_exception";
}

public static class AssessmentSourceCategories
{
    public const string OrganizationPolicy = "organization_policy";
    public const string Agent = "agent";
    public const string Harness = "harness";
    public const string ActivityRevision = "activity_revision";
    public const string TaskSubmission = "task_submission";
    public const string Workflow = "workflow";
    public const string AdaptiveFollowUp = "adaptive_follow_up";
    public const string RubricEvaluation = "rubric_evaluation";
    public const string ModelDeployment = "model_deployment";
    public const string Knowledge = "knowledge";
    public const string Capability = "capability";
    public const string Memory = "memory";
    public const string ReviewRelease = "review_release";
    public const string Timing = "timing";
    public const string ExceptionReference = "exception_reference";
    public const string AuditAvailability = "audit_availability";
}

public static class AssessmentSourceKinds
{
    public const string OrganizationPolicy = "assessment.organization_policy.v1";
    public const string AgentRevision = "assessment.agent_revision.v1";
    public const string HarnessRevision = "assessment.harness_revision.v1";
    public const string WorkflowPolicy = "assessment.workflow_policy.v1";
    public const string AdaptiveFollowUp = "assessment.adaptive_follow_up.v1";
    public const string RubricEvaluation = "assessment.rubric_evaluation.v1";
    public const string ModelDeployment = "assessment.model_deployment.v1";
    public const string KnowledgeReference = "assessment.knowledge_reference.v1";
    public const string CapabilityProfile = "assessment.capability_profile.v1";
    public const string StableMemorySnapshot = "assessment.stable_memory_snapshot.v1";
    public const string ReviewRelease = "assessment.review_release.v1";
    public const string TaskRequirement = "assessment.task_requirement.v1";
}

public static class AssessmentFailureCodes
{
    public const string Denied = "assessment.denied";
    public const string InvalidField = "assessment.invalid_field";
    public const string StaleRevision = "assessment.stale_revision";
    public const string Immutable = "assessment.immutable";
    public const string NewCohortRequired = "assessment.new_cohort_required";
    public const string Widening = "assessment.widening";
    public const string MissingSource = "assessment.missing_source";
    public const string KnowledgeUnselected = "assessment.knowledge_unselected";
    public const string MutableSource = "assessment.mutable_source";
    public const string RevokedSource = "assessment.revoked_source";
    public const string UnavailableSource = "assessment.unavailable_source";
    public const string WrongScope = "assessment.wrong_scope";
    public const string DigestMismatch = "assessment.digest_mismatch";
    public const string Incompatible = "assessment.incompatible";
    public const string InvalidMemory = "assessment.invalid_memory";
    public const string InvalidTiming = "assessment.invalid_timing";
    public const string ProhibitedCapability = "assessment.prohibited_capability";
    public const string MissingException = "assessment.missing_exception";
    public const string AuditUnavailable = "assessment.audit_unavailable";
    public const string TransactionOwnerMissing = "assessment.transaction_owner_missing";
    public const string NotReady = "assessment.not_ready";
    public const string IdempotencyConflict = "assessment.idempotency_conflict";
    public const string ConcurrentActivation = "assessment.concurrent_activation";
}

public static class AssessmentActivationOutcomes
{
    public const string Activated = "assessment.activated";
    public const string Deduplicated = "assessment.activation.deduplicated";
}

public static class AssessmentRevisionChangeCategories
{
    public const string Created = "created";
    public const string Saved = "saved";
}

public static class AssessmentAuthorizationActions
{
    public const string CreateActivity = "assessment.activity.create";
    public const string ReadActivity = "assessment.activity.read";
    public const string SaveActivity = "assessment.activity.save";
    public const string CheckReadiness = "assessment.readiness.check";
    public const string ActivateCohort = "assessment.cohort.activate";
    public const string SelectSources = "assessment.source.select";
    public const string ReconcileActivation = "assessment.activation.reconcile";
    public const string ReadBaseline = "assessment.baseline.read";
    public const string ReadBaselineProvenance = "assessment.baseline.provenance.read";
}

public static class AssessmentResourceTypes
{
    public const string Activity = "assessment_activity";
    public const string Cohort = "assessment_cohort";
    public const string Baseline = "assessment_activation_baseline";
}

public static class ActivationBaselineProcedure
{
    public const string Id = "activation-baseline-jcs-sha256-v1";
    public const string SchemaVersion = "v1";
    public const string CanonicalizationVersion = "rfc8785";
}

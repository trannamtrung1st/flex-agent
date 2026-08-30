namespace FlexAgent.AssessmentConfiguration.Domain;

public sealed record ActivityDraft(
    Guid OrganizationId,
    Guid ActivityId,
    Guid RevisionId,
    long RevisionNumber,
    string Form,
    string ConfiguredType,
    AssessmentDraftContent Content,
    bool HasActivatedCohort,
    DateTimeOffset UpdatedAtUtc)
{
    public static AssessmentDecision<ActivityDraft> Create(
        Guid organizationId,
        Guid activityId,
        Guid revisionId,
        string title,
        TaskBinding task,
        TimingRules timing,
        ExactSourceRef organizationPolicy,
        ExactSourceRef agent,
        ExactSourceRef harness,
        ExactSourceRef workflow,
        ExactSourceRef adaptiveFollowUp,
        ExactSourceRef rubric,
        ExactSourceRef modelDeployment,
        IReadOnlyList<ExactSourceRef> knowledge,
        ExactSourceRef capabilityProfile,
        ExactSourceRef reviewRelease)
    {
        if (organizationId == Guid.Empty || activityId == Guid.Empty || revisionId == Guid.Empty)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.InvalidField);
        }

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.InvalidField, "title");
        }

        if (string.IsNullOrWhiteSpace(task.Title) || string.IsNullOrWhiteSpace(task.SubmissionRequirementSummary))
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.InvalidField, "task");
        }

        if (!timing.IsValid(out var timingCode))
        {
            return AssessmentDecision<ActivityDraft>.Fail(timingCode, "timing");
        }

        var content = new AssessmentDraftContent(
            Title: title.Trim(),
            OrganizationPolicy: organizationPolicy,
            Agent: agent,
            Harness: harness,
            Task: task,
            Workflow: workflow,
            AdaptiveFollowUp: adaptiveFollowUp,
            Rubric: rubric,
            ModelDeployment: modelDeployment,
            Knowledge: knowledge,
            CapabilityProfile: capabilityProfile,
            ReviewRelease: reviewRelease,
            Memory: MemoryChoice.Disabled,
            Timing: timing,
            RequestedCapabilities: CapabilityBounds.P0Assessment,
            ApprovedException: null);

        return AssessmentDecision<ActivityDraft>.Ok(
            new ActivityDraft(
                organizationId,
                activityId,
                revisionId,
                RevisionNumber: 1,
                AssessmentActivityForms.Campaign,
                AssessmentConfiguredTypes.Assessment,
                content,
                HasActivatedCohort: false,
                UpdatedAtUtc: DateTimeOffset.UtcNow));
    }

    public AssessmentDecision<ActivityDraft> Save(
        long expectedRevisionNumber,
        AssessmentDraftContent nextContent)
    {
        if (HasActivatedCohort)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.NewCohortRequired);
        }

        if (expectedRevisionNumber != RevisionNumber)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.StaleRevision);
        }

        if (string.IsNullOrWhiteSpace(nextContent.Title) || nextContent.Title.Trim().Length > 200)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.InvalidField, "title");
        }

        if (!nextContent.Timing.IsValid(out var timingCode))
        {
            return AssessmentDecision<ActivityDraft>.Fail(timingCode, "timing");
        }

        if (nextContent.Memory.Mode == MemoryReadModes.ImmutableSnapshot && nextContent.Memory.Snapshot is null)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.InvalidMemory, "memory");
        }

        if (nextContent.Memory.Mode == MemoryReadModes.Disabled && nextContent.Memory.Snapshot is not null)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.InvalidMemory, "memory");
        }

        if (nextContent.Memory.Mode is not (MemoryReadModes.Disabled or MemoryReadModes.ImmutableSnapshot))
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.InvalidMemory, "memory");
        }

        if (nextContent.RequestedCapabilities.ViolatesP0Profile())
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.ProhibitedCapability, "capabilities");
        }

        return AssessmentDecision<ActivityDraft>.Ok(
            this with
            {
                RevisionId = Guid.CreateVersion7(),
                RevisionNumber = RevisionNumber + 1,
                Content = nextContent with { Title = nextContent.Title.Trim() },
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
    }

    public AssessmentDecision<ActivityDraft> MarkActivatedCohort()
    {
        return AssessmentDecision<ActivityDraft>.Ok(this with
        {
            HasActivatedCohort = true,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    public AssessmentDecision<ActivityDraft> CreateSuccessorRevision()
    {
        if (!HasActivatedCohort)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.InvalidField);
        }

        return AssessmentDecision<ActivityDraft>.Ok(
            this with
            {
                ActivityId = Guid.CreateVersion7(),
                RevisionId = Guid.CreateVersion7(),
                RevisionNumber = 1,
                HasActivatedCohort = false,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
    }
}

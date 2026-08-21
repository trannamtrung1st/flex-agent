using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public static class AssessmentSourceSelection
{
    public static string? Validate(ActivityDraft draft, IReadOnlyList<TrustedSourceDescriptor> selectable)
    {
        if (draft.Content.ApprovedException is not null)
        {
            return AssessmentFailureCodes.MissingException;
        }

        foreach (var slot in Slots(draft))
        {
            var match = selectable.FirstOrDefault(source => source.Matches(slot.Reference));
            if (match is null)
            {
                return AssessmentFailureCodes.MissingSource;
            }

            if (!string.Equals(match.SourceKind, slot.ExpectedKind, StringComparison.Ordinal)
                || !string.Equals(match.Category, slot.ExpectedCategory, StringComparison.Ordinal))
            {
                return AssessmentFailureCodes.Incompatible;
            }
        }

        return null;
    }

    public static IReadOnlyList<(ExactSourceRef Reference, string ExpectedCategory, string ExpectedKind)> Slots(
        ActivityDraft draft)
    {
        var content = draft.Content;
        var slots = new List<(ExactSourceRef, string, string)>
        {
            (content.OrganizationPolicy, AssessmentSourceCategories.OrganizationPolicy, AssessmentSourceKinds.OrganizationPolicy),
            (content.Agent, AssessmentSourceCategories.Agent, AssessmentSourceKinds.AgentRevision),
            (content.Harness, AssessmentSourceCategories.Harness, AssessmentSourceKinds.HarnessRevision),
            (content.Workflow, AssessmentSourceCategories.Workflow, AssessmentSourceKinds.WorkflowPolicy),
            (content.AdaptiveFollowUp, AssessmentSourceCategories.AdaptiveFollowUp, AssessmentSourceKinds.AdaptiveFollowUp),
            (content.Rubric, AssessmentSourceCategories.RubricEvaluation, AssessmentSourceKinds.RubricEvaluation),
            (content.ModelDeployment, AssessmentSourceCategories.ModelDeployment, AssessmentSourceKinds.ModelDeployment),
            (content.CapabilityProfile, AssessmentSourceCategories.Capability, AssessmentSourceKinds.CapabilityProfile),
            (content.ReviewRelease, AssessmentSourceCategories.ReviewRelease, AssessmentSourceKinds.ReviewRelease),
            (content.Task.RequirementSource, AssessmentSourceCategories.TaskSubmission, AssessmentSourceKinds.TaskRequirement),
        };
        slots.AddRange(content.Knowledge.Select(item =>
            (item, AssessmentSourceCategories.Knowledge, AssessmentSourceKinds.KnowledgeReference)));
        if (content.Memory.Snapshot is { } snapshot)
        {
            slots.Add((snapshot, AssessmentSourceCategories.Memory, AssessmentSourceKinds.StableMemorySnapshot));
        }

        return slots;
    }
}

using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class ActivationBaselineDocumentTests
{
    [Fact]
    public void Ready_draft_produces_versioned_fairness_document_without_binding_metadata()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var result = ActivationBaselineDocument.FromReadyDraft(draft, AssessmentFixtures.PermittedSources());

        Assert.True(result.Succeeded);
        var document = result.Value!;
        Assert.Equal(ActivationBaselineProcedure.Id, document.ProcedureId);
        Assert.Equal(ActivationBaselineProcedure.SchemaVersion, document.SchemaVersion);
        Assert.DoesNotContain(document.FairnessDomains, domain => domain.DomainKey is "cohort_id" or "baseline_id");
        Assert.Contains(document.FairnessDomains, domain => domain.DomainKey == AssessmentSourceCategories.Memory);
        Assert.Contains(document.ResolutionDecisions, decision => decision.DecisionKey == "empty_cohort_permitted");
        Assert.Empty(document.ApprovedExceptionRefs);
        Assert.True(ActivationBaselineDocument.Validate(document).Succeeded);
    }

    [Fact]
    public void One_field_change_is_visible_in_the_corresponding_domain()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var original = ActivationBaselineDocument.FromReadyDraft(draft, AssessmentFixtures.PermittedSources()).Value!;
        var retitled = draft.Save(draft.RevisionNumber, draft.Content with { Title = "Changed" }).Value!;
        var changed = ActivationBaselineDocument.FromReadyDraft(retitled, AssessmentFixtures.PermittedSources()).Value!;

        var originalTitle = original.FairnessDomains.Single(domain => domain.DomainKey == AssessmentSourceCategories.ActivityRevision)
            .EffectiveValue["title"];
        var changedTitle = changed.FairnessDomains.Single(domain => domain.DomainKey == AssessmentSourceCategories.ActivityRevision)
            .EffectiveValue["title"];

        Assert.NotEqual(originalTitle, changedTitle);
    }

    [Fact]
    public void Task_requirement_reference_comes_from_the_trusted_descriptor()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var sources = AssessmentFixtures.PermittedSources();
        var trusted = sources.Single(source => source.Category == AssessmentSourceCategories.TaskSubmission);

        var document = ActivationBaselineDocument.FromReadyDraft(draft, sources).Value!;
        var reference = document.SourceReferences.Single(item => item.SourceKey == AssessmentSourceCategories.TaskSubmission);
        var domain = document.FairnessDomains.Single(item => item.DomainKey == AssessmentSourceCategories.TaskSubmission);

        Assert.Equal(trusted.SourceId, reference.SourceId);
        Assert.Equal(trusted.VersionId, reference.SourceVersion);
        Assert.Equal(trusted.ContentDigest, reference.ContentDigest);
        Assert.Equal(trusted.ContentDigest, domain.EffectiveValue["requirement_digest"]);
    }

    [Fact]
    public void Blocked_readiness_cannot_produce_a_baseline()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var sources = AssessmentFixtures.PermittedSources();
        sources.RemoveAt(1);

        var result = ActivationBaselineDocument.FromReadyDraft(draft, sources);

        Assert.False(result.Succeeded);
        Assert.Equal(AssessmentFailureCodes.NotReady, result.OutcomeCode);
    }

    [Fact]
    public void Unknown_domain_or_procedure_is_rejected()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var valid = ActivationBaselineDocument.FromReadyDraft(draft, AssessmentFixtures.PermittedSources()).Value!;
        var unknownDomain = valid with
        {
            FairnessDomains = [..valid.FairnessDomains, new FairnessDomainValue("unknown_domain", new Dictionary<string, string> { ["x"] = "1" }, FairnessClassifications.Derived)],
        };
        var wrongProcedure = valid with { ProcedureId = "rsc-jcs-sha256-v1" };

        Assert.False(ActivationBaselineDocument.Validate(unknownDomain).Succeeded);
        Assert.Equal(AssessmentFailureCodes.Incompatible, ActivationBaselineDocument.Validate(wrongProcedure).OutcomeCode);
    }

    [Fact]
    public void Equivalent_content_can_be_built_for_a_second_activity()
    {
        var first = AssessmentFixtures.CreateDraft().Value!;
        var second = ActivityDraft.Create(
            AssessmentFixtures.OrganizationId,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            first.Content.Title,
            first.Content.Task,
            first.Content.Timing,
            first.Content.OrganizationPolicy,
            first.Content.Agent,
            first.Content.Harness,
            first.Content.Workflow,
            first.Content.AdaptiveFollowUp,
            first.Content.Rubric,
            first.Content.ModelDeployment,
            first.Content.Knowledge,
            first.Content.CapabilityProfile,
            first.Content.ReviewRelease).Value!;

        var left = ActivationBaselineDocument.FromReadyDraft(first, AssessmentFixtures.PermittedSources()).Value!;
        var right = ActivationBaselineDocument.FromReadyDraft(second, AssessmentFixtures.PermittedSources()).Value!;

        Assert.Equal(
            left.FairnessDomains.Single(domain => domain.DomainKey == AssessmentSourceCategories.Agent).EffectiveValue,
            right.FairnessDomains.Single(domain => domain.DomainKey == AssessmentSourceCategories.Agent).EffectiveValue);
        Assert.NotEqual(
            left.FairnessDomains.Single(domain => domain.DomainKey == AssessmentSourceCategories.ActivityRevision).EffectiveValue["activity_id"],
            right.FairnessDomains.Single(domain => domain.DomainKey == AssessmentSourceCategories.ActivityRevision).EffectiveValue["activity_id"]);
    }
}

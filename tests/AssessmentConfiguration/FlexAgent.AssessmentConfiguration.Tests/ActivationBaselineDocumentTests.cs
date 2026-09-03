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
    public void Empty_knowledge_is_recorded_and_does_not_block_the_baseline()
    {
        var draft = ActivityDraft.Create(
            AssessmentFixtures.OrganizationId,
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "P0 Assessment",
            AssessmentFixtures.ValidTask(),
            AssessmentFixtures.ValidTiming(),
            AssessmentFixtures.Ref(1),
            AssessmentFixtures.Ref(2),
            AssessmentFixtures.Ref(3),
            AssessmentFixtures.Ref(4),
            AssessmentFixtures.Ref(5),
            AssessmentFixtures.Ref(6),
            AssessmentFixtures.Ref(7),
            [],
            AssessmentFixtures.Ref(10),
            AssessmentFixtures.Ref(11)).Value!;

        var result = ActivationBaselineDocument.FromReadyDraft(draft, AssessmentFixtures.PermittedSources());

        Assert.True(result.Succeeded);
        var knowledge = result.Value!.FairnessDomains.Single(domain => domain.DomainKey == AssessmentSourceCategories.Knowledge);
        Assert.Equal("none", knowledge.EffectiveValue["selected"]);
        Assert.DoesNotContain(result.Value.SourceReferences, reference => reference.SourceKey == AssessmentSourceCategories.Knowledge);
        Assert.True(ActivationBaselineDocument.Validate(result.Value).Succeeded);
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
    public void Per_attempt_duration_is_frozen_in_the_timing_domain()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var original = ActivationBaselineDocument.FromReadyDraft(draft, AssessmentFixtures.PermittedSources()).Value!;
        var shortened = draft.Save(
            draft.RevisionNumber,
            draft.Content with { Timing = draft.Content.Timing with { PerAttemptDurationSeconds = 1800 } }).Value!;
        var changed = ActivationBaselineDocument.FromReadyDraft(shortened, AssessmentFixtures.PermittedSources()).Value!;

        var originalTiming = original.FairnessDomains.Single(domain => domain.DomainKey == AssessmentSourceCategories.Timing);
        var changedTiming = changed.FairnessDomains.Single(domain => domain.DomainKey == AssessmentSourceCategories.Timing);

        Assert.Equal("3600", originalTiming.EffectiveValue["per_attempt_duration_seconds"]);
        Assert.Equal("1800", changedTiming.EffectiveValue["per_attempt_duration_seconds"]);
        Assert.NotEqual(originalTiming.EffectiveValue, changedTiming.EffectiveValue);
    }

    [Fact]
    public void Configured_warning_thresholds_are_frozen_only_when_authored()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var withoutWarnings = ActivationBaselineDocument.FromReadyDraft(
            draft,
            AssessmentFixtures.PermittedSources()).Value!;
        var withWarnings = ActivationBaselineDocument.FromReadyDraft(
            draft.Save(
                draft.RevisionNumber,
                draft.Content with
                {
                    Timing = draft.Content.Timing with
                    {
                        WarningApproachingRemainingSeconds = 900,
                        WarningImminentRemainingSeconds = 300,
                    },
                }).Value!,
            AssessmentFixtures.PermittedSources()).Value!;

        var omitted = withoutWarnings.FairnessDomains
            .Single(domain => domain.DomainKey == AssessmentSourceCategories.Timing)
            .EffectiveValue;
        var authored = withWarnings.FairnessDomains
            .Single(domain => domain.DomainKey == AssessmentSourceCategories.Timing)
            .EffectiveValue;

        Assert.False(omitted.ContainsKey("warning_approaching_remaining_seconds"));
        Assert.False(omitted.ContainsKey("warning_imminent_remaining_seconds"));
        Assert.Equal("900", authored["warning_approaching_remaining_seconds"]);
        Assert.Equal("300", authored["warning_imminent_remaining_seconds"]);
    }

    [Fact]
    public void Activation_provenance_is_frozen_into_resolution_decisions()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var actorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var correlationId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var occurredAt = new DateTimeOffset(2026, 8, 21, 4, 0, 0, TimeSpan.Zero);
        var document = ActivationBaselineDocument.FromReadyDraft(
            draft,
            AssessmentFixtures.PermittedSources(),
            new ActivationProvenance(actorId, "human.interactive", correlationId, occurredAt)).Value!;

        Assert.Contains(document.ResolutionDecisions, decision =>
            decision.DecisionKey == "activation_actor_id" && decision.Outcome == actorId.ToString("D"));
        Assert.Contains(document.ResolutionDecisions, decision =>
            decision.DecisionKey == "activation_correlation_id" && decision.Outcome == correlationId.ToString("D"));
        Assert.Contains(document.ResolutionDecisions, decision =>
            decision.DecisionKey == "activation_occurred_at" && decision.Outcome == "2026-08-21T04:00:00.000Z");
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

using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class ActivityDraftTests
{
    [Fact]
    public void Create_defaults_to_campaign_assessment_and_disabled_memory()
    {
        var result = AssessmentFixtures.CreateDraft();

        Assert.True(result.Succeeded);
        Assert.Equal(AssessmentActivityForms.Campaign, result.Value!.Form);
        Assert.Equal(AssessmentConfiguredTypes.Assessment, result.Value.ConfiguredType);
        Assert.Equal(1, result.Value.RevisionNumber);
        Assert.Equal(MemoryReadModes.Disabled, result.Value.Content.Memory.Mode);
        Assert.Null(result.Value.Content.Memory.Snapshot);
        Assert.False(result.Value.HasActivatedCohort);
    }

    [Fact]
    public void Create_rejects_empty_title()
    {
        var result = ActivityDraft.Create(
            AssessmentFixtures.OrganizationId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "  ",
            AssessmentFixtures.ValidTask(),
            AssessmentFixtures.ValidTiming(),
            AssessmentFixtures.Ref(1),
            AssessmentFixtures.Ref(2),
            AssessmentFixtures.Ref(3),
            AssessmentFixtures.Ref(4),
            AssessmentFixtures.Ref(5),
            AssessmentFixtures.Ref(6),
            AssessmentFixtures.Ref(7),
            [AssessmentFixtures.Ref(8)],
            AssessmentFixtures.Ref(10),
            AssessmentFixtures.Ref(11));

        Assert.False(result.Succeeded);
        Assert.Equal(AssessmentFailureCodes.InvalidField, result.OutcomeCode);
        Assert.Equal("title", result.Field);
    }

    [Fact]
    public void Save_rejects_stale_expected_revision()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var result = draft.Save(expectedRevisionNumber: 99, draft.Content);

        Assert.False(result.Succeeded);
        Assert.Equal(AssessmentFailureCodes.StaleRevision, result.OutcomeCode);
    }

    [Fact]
    public void Save_increments_revision_for_matching_expected_version()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var next = draft.Content with { Title = "Updated assessment" };

        var result = draft.Save(draft.RevisionNumber, next);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.RevisionNumber);
        Assert.NotEqual(draft.RevisionId, result.Value.RevisionId);
        Assert.Equal("Updated assessment", result.Value.Content.Title);
    }

    [Fact]
    public void Save_rejects_voice_or_tool_widening()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var widened = draft.Content with
        {
            RequestedCapabilities = CapabilityBounds.P0Assessment with { VoiceEnabled = true },
        };

        var result = draft.Save(draft.RevisionNumber, widened);

        Assert.False(result.Succeeded);
        Assert.Equal(AssessmentFailureCodes.ProhibitedCapability, result.OutcomeCode);
    }

    [Fact]
    public void Save_rejects_snapshot_memory_without_exact_reference()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var invalid = draft.Content with
        {
            Memory = new MemoryChoice(MemoryReadModes.ImmutableSnapshot, null),
        };

        var result = draft.Save(draft.RevisionNumber, invalid);

        Assert.False(result.Succeeded);
        Assert.Equal(AssessmentFailureCodes.InvalidMemory, result.OutcomeCode);
    }

    [Fact]
    public void Activated_draft_cannot_be_edited_and_requires_a_new_cohort()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!.MarkActivatedCohort().Value!;

        var save = draft.Save(draft.RevisionNumber, draft.Content);
        Assert.Equal(AssessmentFailureCodes.NewCohortRequired, save.OutcomeCode);

        var successor = draft.CreateSuccessorRevision();
        Assert.True(successor.Succeeded);
        Assert.False(successor.Value!.HasActivatedCohort);
        Assert.NotEqual(draft.ActivityId, successor.Value.ActivityId);
        Assert.Equal(1, successor.Value.RevisionNumber);
    }

    [Fact]
    public void Timing_rejects_non_utc_offset_and_inverted_window()
    {
        var offsetTiming = AssessmentFixtures.ValidTiming() with
        {
            StartsAtUtc = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.FromHours(7)),
        };
        Assert.False(offsetTiming.IsValid(out var offsetCode));
        Assert.Equal(AssessmentFailureCodes.InvalidTiming, offsetCode);

        var inverted = AssessmentFixtures.ValidTiming() with
        {
            EndsAtUtc = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
        };
        Assert.False(inverted.IsValid(out var invertedCode));
        Assert.Equal(AssessmentFailureCodes.InvalidTiming, invertedCode);
    }
}

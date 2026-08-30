using System.Text.Json;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Canonicalization;
using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.Runtime.Tests;

public sealed class DemoWorkSeedFixtureTests
{
    public static readonly Guid DemoOrganizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    public static readonly Guid DemoAdminActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    public static readonly Guid DemoParticipantActorId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab");

    public static readonly Guid ActivatedActivityId = Guid.Parse("a1000000-0000-4000-8000-000000000025");
    public static readonly Guid ActivatedRevisionId = Guid.Parse("b1000000-0000-4000-8000-000000000025");
    public static readonly Guid ActivatedCohortId = Guid.Parse("c1000000-0000-4000-8000-000000000025");
    public static readonly Guid ActivatedBaselineId = Guid.Parse("d1000000-0000-4000-8000-000000000025");
    public static readonly Guid ActivatedTaskId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public const string ActivatedCampaignTitle = "Q3 Safety Compliance — Pilot Cohort";
    public const string ActivatedTaskTitle = "Hazard identification response";

    public const string ActivatedBaselineDigest =
        "7df34f545bf192ca09bd7c6177d6314febed518e1799b533b4c745c872592452";

    [Fact]
    public void Activated_campaign_baseline_digest_matches_demo_work_seed_contract()
    {
        var draft = BuildActivatedDraft();
        var sources = BuildDemoSources();
        var document = ActivationBaselineDocument.FromReadyDraft(draft, sources);
        Assert.True(document.Succeeded, document.OutcomeCode);

        var digest = new ActivationBaselineDigester().Digest(document.Value!);
        Assert.True(digest.Succeeded, digest.OutcomeCode);

        Assert.Equal(ActivatedBaselineDigest, digest.Value);
    }

    [Fact]
    public void Activated_campaign_fixture_json_contract_is_stable()
    {
        var draft = BuildActivatedDraft();
        var sources = BuildDemoSources();
        var document = ActivationBaselineDocument.FromReadyDraft(draft, sources);
        Assert.True(document.Succeeded, document.OutcomeCode);

        var contentJson = JsonSerializer.Serialize(draft.Content, JsonOptions);
        var documentJson = JsonSerializer.Serialize(document.Value, JsonOptions);
        Assert.Contains("Q3 Safety Compliance", contentJson);
        Assert.Contains("activation-baseline-jcs-sha256-v1", documentJson);
    }

    internal static ActivityDraft BuildActivatedDraft() =>
        ActivityDraft.Create(
            DemoOrganizationId,
            ActivatedActivityId,
            ActivatedRevisionId,
            ActivatedCampaignTitle,
            new TaskBinding(
                ActivatedTaskId,
                ActivatedTaskTitle,
                "Submit one written response",
                AssessmentDevelopmentSources.TaskRequirement),
            new TimingRules(
                new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 30, 17, 0, 0, TimeSpan.Zero),
                "UTC",
                2,
                3600),
            AssessmentDevelopmentSources.OrganizationPolicy,
            AssessmentDevelopmentSources.Agent,
            AssessmentDevelopmentSources.Harness,
            AssessmentDevelopmentSources.Workflow,
            AssessmentDevelopmentSources.AdaptiveFollowUp,
            AssessmentDevelopmentSources.Rubric,
            AssessmentDevelopmentSources.ModelDeployment,
            [AssessmentDevelopmentSources.Knowledge],
            AssessmentDevelopmentSources.Capability,
            AssessmentDevelopmentSources.ReviewRelease).Value! with
        {
            HasActivatedCohort = true,
        };

    internal static ActivityDraft BuildDraftCampaign(int index)
    {
        var suffix = index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        var activityId = Guid.Parse($"a1000000-0000-4000-8000-{index:D12}");
        var revisionId = Guid.Parse($"b1000000-0000-4000-8000-{index:D12}");
        return ActivityDraft.Create(
            DemoOrganizationId,
            activityId,
            revisionId,
            $"Demo Campaign {suffix}",
            new TaskBinding(
                Guid.Parse($"44444444-4444-4444-4444-{index:D12}"),
                $"Task {suffix}",
                "Submit one written response",
                AssessmentDevelopmentSources.TaskRequirement),
            new TimingRules(
                new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 9, 30, 17, 0, 0, TimeSpan.Zero),
                "UTC",
                2,
                3600),
            AssessmentDevelopmentSources.OrganizationPolicy,
            AssessmentDevelopmentSources.Agent,
            AssessmentDevelopmentSources.Harness,
            AssessmentDevelopmentSources.Workflow,
            AssessmentDevelopmentSources.AdaptiveFollowUp,
            AssessmentDevelopmentSources.Rubric,
            AssessmentDevelopmentSources.ModelDeployment,
            [AssessmentDevelopmentSources.Knowledge],
            AssessmentDevelopmentSources.Capability,
            AssessmentDevelopmentSources.ReviewRelease).Value!;
    }

    [Fact]
    public void Emit_demo_work_fixture_artifacts_when_requested()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("DEMO_WORK_EMIT"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var draft = BuildActivatedDraft();
        var sources = BuildDemoSources();
        var document = ActivationBaselineDocument.FromReadyDraft(draft, sources).Value!;
        var root = FindRepositoryRoot();
        var directory = Path.Combine(root, "deploy", "compose", "authenticated-browser", ".generated");
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "demo-work-activated-content.json"),
            JsonSerializer.Serialize(draft.Content, JsonOptions));
        File.WriteAllText(
            Path.Combine(directory, "demo-work-activated-baseline.json"),
            JsonSerializer.Serialize(document, JsonOptions));
        File.WriteAllText(
            Path.Combine(directory, "demo-work-draft-content.json"),
            JsonSerializer.Serialize(BuildDraftCampaign(1).Content, JsonOptions));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FlexAgent.slnx"))
                || File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    internal static IReadOnlyList<TrustedSourceDescriptor> BuildDemoSources() =>
        AssessmentDevelopmentSources.ForOrganization(DemoOrganizationId);
}

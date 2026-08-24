using FlexAgent.Api;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.Submissions.Domain;
using FlexAgent.Submissions.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace FlexAgent.Runtime.Tests;

public sealed class OwnerMaterialPolicyPortTests
{
    private static readonly Guid OrganizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    private static readonly Guid ActivityId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid CohortId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2");
    private static readonly Guid TaskSourceId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1");
    private static readonly Guid TaskVersionId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2");
    private static readonly string TaskDigest = new('a', 64);

    [Fact]
    public async Task Frozen_requirement_returns_mvp_defaults_for_verified_activated_task()
    {
        var reader = new StubActivatedCohortBindingReader { Snapshot = Snapshot(verificationDegraded: false) };
        var port = new AssessmentFrozenSubmissionRequirementPort(reader);

        var policy = await port.ResolveFrozenAsync(
            OrganizationId,
            ActivityId,
            CohortId,
            TaskSourceId,
            TaskVersionId,
            TaskDigest,
            null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(policy);
        Assert.Equal(TaskSourceId, policy.FrozenRequirement.SourceId);
        Assert.Equal(MaterialPolicyContract.Version, policy.ContractVersion);
        Assert.Contains(policy.Categories, category => category.Category == MaterialCategories.DirectText && category.Available);
    }

    [Fact]
    public async Task Frozen_requirement_fails_closed_on_task_substitution()
    {
        var reader = new StubActivatedCohortBindingReader { Snapshot = Snapshot(verificationDegraded: false) };
        var port = new AssessmentFrozenSubmissionRequirementPort(reader);

        var policy = await port.ResolveFrozenAsync(
            OrganizationId,
            ActivityId,
            CohortId,
            Guid.CreateVersion7(),
            TaskVersionId,
            TaskDigest,
            null,
            TestContext.Current.CancellationToken);

        Assert.Null(policy);
    }

    [Fact]
    public async Task Frozen_requirement_fails_closed_when_baseline_verification_is_degraded()
    {
        var reader = new StubActivatedCohortBindingReader { Snapshot = Snapshot(verificationDegraded: true) };
        var port = new AssessmentFrozenSubmissionRequirementPort(reader);

        var policy = await port.ResolveFrozenAsync(
            OrganizationId,
            ActivityId,
            CohortId,
            TaskSourceId,
            TaskVersionId,
            TaskDigest,
            null,
            TestContext.Current.CancellationToken);

        Assert.Null(policy);
    }

    [Fact]
    public async Task Current_material_policy_is_available_outside_production()
    {
        var port = new EnvironmentMaterialPolicyPort(new StubHostEnvironment("Testing"));
        var policy = await port.ResolveCurrentAsync(
            OrganizationId,
            new PolicySourceRef(Guid.CreateVersion7(), Guid.CreateVersion7(), new string('b', 64)),
            DateTimeOffset.UtcNow,
            null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(policy);
        Assert.True(policy.EnvironmentEligible);
    }

    [Fact]
    public async Task Current_material_policy_fails_closed_in_production()
    {
        var port = new EnvironmentMaterialPolicyPort(new StubHostEnvironment("Production"));
        var policy = await port.ResolveCurrentAsync(
            OrganizationId,
            new PolicySourceRef(Guid.CreateVersion7(), Guid.CreateVersion7(), new string('b', 64)),
            DateTimeOffset.UtcNow,
            null,
            TestContext.Current.CancellationToken);

        Assert.Null(policy);
    }

    private static ActivatedCohortBindingSnapshot Snapshot(bool verificationDegraded) =>
        new(
            OrganizationId,
            ActivityId,
            CohortId,
            Guid.CreateVersion7(),
            new string('c', 64),
            "activated",
            TaskSourceId,
            TaskVersionId,
            TaskDigest,
            "Campaign",
            "Task",
            "UTC",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(14),
            DateTimeOffset.UtcNow.AddDays(7),
            verificationDegraded);

    private sealed class StubActivatedCohortBindingReader : IActivatedCohortBindingReader
    {
        public ActivatedCohortBindingSnapshot? Snapshot { get; init; }

        public Task<ActivatedCohortBindingSnapshot?> GetActivatedAsync(
            Guid organizationId,
            Guid activityId,
            Guid cohortId,
            object? commitTransaction,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);
    }

    private sealed class StubHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "tests";

        public string ContentRootPath { get; set; } = "/tmp";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

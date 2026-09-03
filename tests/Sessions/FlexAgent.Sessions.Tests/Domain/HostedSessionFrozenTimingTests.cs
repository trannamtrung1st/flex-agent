using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class HostedSessionFrozenTimingTests
{
    [Fact]
    public void Unbounded_duration_is_not_a_synthetic_45_minute_budget()
    {
        var policy = HostedSessionFrozenTiming.Resolve("unbounded");

        Assert.Equal(HostedTimingReconstruction.Unbounded, policy.Reconstruction);
        Assert.Null(policy.BudgetSeconds);
        Assert.Empty(policy.WarningSchedule);
    }

    [Fact]
    public void Persisted_camelCase_activation_baseline_is_read_as_unbounded()
    {
        var policy = HostedSessionFrozenTiming.FromActivationBaselineDocument(
            """
            {
              "procedureId": "activation-baseline-jcs-sha256-v1",
              "schemaVersion": "v1",
              "fairnessDomains": [
                {
                  "domainKey": "timing",
                  "classification": "cohort_supplied",
                  "effectiveValue": {
                    "starts_at": "2026-09-01T00:00:00.000Z",
                    "ends_at": "2026-09-30T23:59:00.000Z",
                    "deadline_at": "2026-09-30T17:00:00.000Z",
                    "time_zone_id": "UTC",
                    "attempt_limit": "2",
                    "per_attempt_duration_seconds": "unbounded"
                  }
                }
              ]
            }
            """);

        Assert.Equal(HostedTimingReconstruction.Unbounded, policy.Reconstruction);
        Assert.Null(policy.BudgetSeconds);
    }

    [Fact]
    public void Real_activation_baseline_unbounded_timing_stays_unbounded()
    {
        var policy = HostedSessionFrozenTiming.FromActivationBaselineDocument(
            """
            {
              "fairness_domains": [
                {
                  "domain_key": "timing",
                  "effective_value": {
                    "starts_at": "2026-09-01T09:00:00.000Z",
                    "ends_at": "2026-09-12T17:00:00.000Z",
                    "deadline_at": "2026-09-10T17:00:00.000Z",
                    "time_zone_id": "UTC",
                    "attempt_limit": "1",
                    "per_attempt_duration_seconds": "unbounded"
                  }
                }
              ]
            }
            """);

        Assert.Equal(HostedTimingReconstruction.Unbounded, policy.Reconstruction);
        Assert.Null(policy.BudgetSeconds);
    }

    [Fact]
    public void Persisted_camelCase_timed_baseline_keeps_the_budget_without_invented_warnings()
    {
        var policy = HostedSessionFrozenTiming.FromActivationBaselineDocument(
            """
            {
              "fairnessDomains": [
                {
                  "domainKey": "timing",
                  "effectiveValue": {
                    "per_attempt_duration_seconds": "3600"
                  }
                }
              ]
            }
            """);

        Assert.Equal(HostedTimingReconstruction.Timed, policy.Reconstruction);
        Assert.Equal(3600, policy.BudgetSeconds);
        Assert.Empty(policy.WarningSchedule);
    }

    [Fact]
    public void Real_activation_baseline_timed_shape_without_warning_keys_keeps_the_budget()
    {
        var policy = HostedSessionFrozenTiming.FromActivationBaselineDocument(
            """
            {
              "fairness_domains": [
                {
                  "domain_key": "timing",
                  "effective_value": {
                    "starts_at": "2026-09-01T09:00:00.000Z",
                    "ends_at": "2026-09-12T17:00:00.000Z",
                    "deadline_at": "2026-09-10T17:00:00.000Z",
                    "time_zone_id": "UTC",
                    "attempt_limit": "1",
                    "per_attempt_duration_seconds": "3600"
                  }
                }
              ]
            }
            """);

        Assert.Equal(HostedTimingReconstruction.Timed, policy.Reconstruction);
        Assert.Equal(3600, policy.BudgetSeconds);
        Assert.Empty(policy.WarningSchedule);
    }

    [Fact]
    public void Missing_or_corrupt_baseline_is_unavailable_not_a_45_minute_timer()
    {
        Assert.Equal(
            HostedTimingReconstruction.Unavailable,
            HostedSessionFrozenTiming.FromActivationBaselineDocument(null).Reconstruction);
        Assert.Equal(
            HostedTimingReconstruction.Unavailable,
            HostedSessionFrozenTiming.FromActivationBaselineDocument("{").Reconstruction);
        Assert.Equal(
            HostedTimingReconstruction.Unavailable,
            HostedSessionFrozenTiming.FromActivationBaselineDocument("""{"fairness_domains":[]}""").Reconstruction);
        Assert.Equal(
            HostedTimingReconstruction.Unavailable,
            HostedSessionFrozenTiming.Resolve("not-a-duration").Reconstruction);
    }

    [Fact]
    public void Effective_duration_replaces_the_cohort_baseline()
    {
        var policy = HostedSessionFrozenTiming.Resolve("3600")
            .WithEffectiveDuration(5400);

        Assert.Equal(HostedTimingReconstruction.Timed, policy.Reconstruction);
        Assert.Equal(5400, policy.BudgetSeconds);
    }

    [Fact]
    public void Effective_duration_does_not_repair_corrupt_timing()
    {
        var policy = HostedFrozenTimingPolicy.UnavailablePolicy.WithEffectiveDuration(5400);

        Assert.Equal(HostedTimingReconstruction.Unavailable, policy.Reconstruction);
        Assert.Null(policy.BudgetSeconds);
    }

    [Fact]
    public void Optional_warning_keys_are_consumed_only_when_present_on_the_timing_domain()
    {
        var policy = HostedSessionFrozenTiming.FromActivationBaselineDocument(
            """
            {
              "fairness_domains": [
                {
                  "domain_key": "timing",
                  "effective_value": {
                    "per_attempt_duration_seconds": "3600",
                    "warning_approaching_remaining_seconds": "900",
                    "warning_imminent_remaining_seconds": "300"
                  }
                }
              ]
            }
            """);

        Assert.Equal(3600, policy.BudgetSeconds);
        Assert.Contains(policy.WarningSchedule, item => item.Code == "approaching" && item.RemainingSecondsThreshold == 900);
        Assert.Contains(policy.WarningSchedule, item => item.Code == "imminent" && item.RemainingSecondsThreshold == 300);
    }

    [Fact]
    public void Frozen_document_round_trip_keeps_effective_budget_and_configured_warnings()
    {
        var captured = HostedSessionFrozenTiming.Compose(
            """
            {
              "fairness_domains": [
                {
                  "domain_key": "timing",
                  "effective_value": {
                    "per_attempt_duration_seconds": "3600",
                    "warning_approaching_remaining_seconds": "900",
                    "warning_imminent_remaining_seconds": "300"
                  }
                }
              ]
            }
            """,
            5400,
            applyEffectiveDuration: true);

        var reloaded = HostedSessionFrozenTiming.FromDocumentJson(
            HostedSessionFrozenTiming.ToDocumentJson(captured));

        Assert.Equal(HostedTimingReconstruction.Timed, reloaded.Reconstruction);
        Assert.Equal(5400, reloaded.BudgetSeconds);
        Assert.Contains(reloaded.WarningSchedule, item => item.Code == "approaching" && item.RemainingSecondsThreshold == 900);
        Assert.Contains(reloaded.WarningSchedule, item => item.Code == "imminent" && item.RemainingSecondsThreshold == 300);
    }

    [Fact]
    public void Missing_frozen_document_is_unavailable()
    {
        Assert.Equal(
            HostedTimingReconstruction.Unavailable,
            HostedSessionFrozenTiming.FromDocumentJson(null).Reconstruction);
        Assert.Equal(
            HostedTimingReconstruction.Unavailable,
            HostedSessionFrozenTiming.FromDocumentJson("{").Reconstruction);
    }
}

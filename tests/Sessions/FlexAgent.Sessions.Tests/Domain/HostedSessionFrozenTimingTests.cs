using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class HostedSessionFrozenTimingTests
{
    [Fact]
    public void Missing_duration_uses_the_labeled_synthetic_development_budget()
    {
        var policy = HostedSessionFrozenTiming.Resolve("unbounded");

        Assert.Equal(HostedSessionTiming.SyntheticDevelopmentActiveDurationSeconds, policy.BudgetSeconds);
        Assert.Empty(policy.WarningSchedule);
    }

    [Fact]
    public void Activation_baseline_document_supplies_duration_and_configured_warnings()
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
}

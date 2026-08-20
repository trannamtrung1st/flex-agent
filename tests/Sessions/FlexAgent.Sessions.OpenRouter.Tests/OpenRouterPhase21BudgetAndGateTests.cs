using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterPhase21BudgetAndGateTests
{
    [Fact]
    public void Gpt_oss_phase_authorizes_only_consumed_zero_through_seven_on_its_own_ledger()
    {
        Assert.Equal("gpt-oss-darkbloom-matrix", OpenRouterLiveQualification.GptOssDarkbloomPhase);
        Assert.Equal(0, OpenRouterLiveQualification.GptOssDarkbloomStartsAtConsumed);
        Assert.Equal(8, OpenRouterLiveQualification.Phase21MaxInferenceRequests);
        Assert.Equal(24, OpenRouterLiveQualification.MaxInferenceRequests);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 0,
                expectedConsumedText: "0",
                out var authorized));
        Assert.Equal(string.Empty, authorized);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 1,
                expectedConsumedText: "1",
                out var retry));
        Assert.Equal(string.Empty, retry);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 2,
                expectedConsumedText: "2",
                out var secondRetry));
        Assert.Equal(string.Empty, secondRetry);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 3,
                expectedConsumedText: "3",
                out var finalRetry));
        Assert.Equal(string.Empty, finalRetry);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 4,
                expectedConsumedText: "4",
                out var ownerRetry));
        Assert.Equal(string.Empty, ownerRetry);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 5,
                expectedConsumedText: "5",
                out var ownerFinal));
        Assert.Equal(string.Empty, ownerFinal);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 6,
                expectedConsumedText: "6",
                out var tokenRetry));
        Assert.Equal(string.Empty, tokenRetry);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 7,
                expectedConsumedText: "7",
                out var tokenFinal));
        Assert.Equal(string.Empty, tokenFinal);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 8,
                expectedConsumedText: "8",
                out var stale));
        Assert.Equal("gpt_oss_darkbloom_requires_consumed_0_to_7", stale);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                "gemma-darkbloom-matrix",
                currentConsumed: 0,
                expectedConsumedText: "0",
                out var wrongPhase));
        Assert.Equal("phase_mismatch", wrongPhase);
    }

    [Fact]
    public void Historical_counts_cannot_reopen_retired_candidates_or_exhausted_gpt_oss()
    {
        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 11,
                expectedConsumedText: "11",
                out var phase21));
        Assert.Equal("gpt_oss_darkbloom_requires_consumed_0_to_7", phase21);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                "pinned-matrix",
                "pinned-matrix",
                currentConsumed: 11,
                expectedConsumedText: "11",
                out var lightning));
        Assert.Equal("retired_candidate", lightning);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.DiscoveryPhase,
                OpenRouterLiveQualification.DiscoveryPhase,
                currentConsumed: 11,
                expectedConsumedText: "11",
                out var discovery));
        Assert.Equal("discovery_retired", discovery);
    }

    [Fact]
    public void Phase21_budget_is_a_distinct_ledger_and_cannot_mutate_historical_state()
    {
        using var directory = new TemporaryDirectory();
        var historicalPath = Path.Combine(directory.Path, "historical");
        var phase21Path = Path.Combine(directory.Path, "phase21");
        File.WriteAllText(historicalPath, "openrouter_qualification_budget.v1\n11\n24\n");
        File.WriteAllText(phase21Path, "openrouter_qualification_budget.v1\n5\n12\n");
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            File.SetUnixFileMode(historicalPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.SetUnixFileMode(phase21Path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        var historicalBytes = File.ReadAllBytes(historicalPath);
        var fiveOfTwelve = File.ReadAllBytes(phase21Path);

        var historical = new OpenRouterQualificationBudget(historicalPath);
        Assert.True(historical.TryRead(out var eleven));
        Assert.Equal(11, eleven);
        Assert.Equal(historicalBytes, File.ReadAllBytes(historicalPath));

        var phase21AgainstHistorical = OpenRouterQualificationBudget.CreatePhase21(historicalPath);
        Assert.False(phase21AgainstHistorical.TryRead(out var rejectedRead));
        Assert.Equal(0, rejectedRead);
        Assert.False(phase21AgainstHistorical.TryReserveExpected(0, out var rejectedReserve));
        Assert.Equal(0, rejectedReserve);
        Assert.Equal(historicalBytes, File.ReadAllBytes(historicalPath));

        var phase21AgainstStrictHistory = OpenRouterQualificationBudget.CreatePhase21(phase21Path);
        Assert.False(phase21AgainstStrictHistory.TryRead(out var rejectedFive));
        Assert.Equal(0, rejectedFive);
        Assert.Equal(fiveOfTwelve, File.ReadAllBytes(phase21Path));

        var fresh = OpenRouterQualificationBudget.CreatePhase21(Path.Combine(directory.Path, "fresh"));
        Assert.True(fresh.TryReserveExpected(0, out var first));
        Assert.Equal(1, first);
        Assert.True(fresh.TryReserveExpected(1, out var second));
        Assert.Equal(2, second);
        Assert.True(fresh.TryReserve(out var third));
        Assert.Equal(3, third);
        Assert.True(fresh.TryReserve(out var fourth));
        Assert.Equal(4, fourth);
        Assert.True(fresh.TryReserve(out var fifth));
        Assert.Equal(5, fifth);
        Assert.True(fresh.TryReserve(out var sixth));
        Assert.Equal(6, sixth);
        Assert.True(fresh.TryReserve(out var seventh));
        Assert.Equal(7, seventh);
        Assert.True(fresh.TryReserve(out var eighth));
        Assert.Equal(8, eighth);
        Assert.False(fresh.TryReserve(out var exhausted));
        Assert.Equal(8, exhausted);
        Assert.Equal(
            "openrouter_qualification_budget.phase21.v1\n8\n8\n",
            File.ReadAllText(Path.Combine(directory.Path, "fresh")));
        Assert.Equal(historicalBytes, File.ReadAllBytes(historicalPath));
        Assert.Equal(fiveOfTwelve, File.ReadAllBytes(phase21Path));
    }

    [Fact]
    public void Historical_budget_cannot_increment_a_phase21_ledger()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "phase21");
        var phase21 = OpenRouterQualificationBudget.CreatePhase21(path);
        Assert.True(phase21.TryReserveExpected(0, out var first));
        Assert.Equal(1, first);
        var original = File.ReadAllBytes(path);

        var historical = new OpenRouterQualificationBudget(path);
        Assert.False(historical.TryRead(out var unread));
        Assert.Equal(0, unread);
        Assert.False(historical.TryReserveExpected(1, out var reserved));
        Assert.Equal(0, reserved);
        Assert.Equal(original, File.ReadAllBytes(path));
    }

    [Fact]
    public void Visible_content_at_the_acceptance_ceiling_is_truncated()
    {
        var control = new ModelExecutionStructuredControl(Admission());
        var truncated = new ModelContentCompleted
        {
            Provenance = Provenance(OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens),
        };
        Assert.False(
            OpenRouterLiveMatrixQualification.TryQualify(
                control,
                [new ModelContentTextDelta("Hi"), truncated],
                OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens,
                out var denial));
        Assert.Equal("length_truncated", denial);

        var accepted = new ModelContentCompleted { Provenance = Provenance(255) };
        Assert.True(
            OpenRouterLiveMatrixQualification.TryQualify(
                control,
                [new ModelContentTextDelta("Hi"), accepted],
                OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens,
                out var ok));
        Assert.Equal(string.Empty, ok);
    }

    private static ValidatedAgentDecisionEnvelope Admission()
    {
        var utf8 =
            """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"no_action","outputs":[],"requested_actions":[],"no_action":{"reason_category":"intentional_silence"}}
            """u8.ToArray();
        Assert.True(ValidatedAgentDecisionEnvelope.TryAdmit(utf8, out var admitted, out _) && admitted is not null);
        return admitted!;
    }

    private static ModelProviderAttemptProvenance Provenance(int outputTokens) =>
        new(
            ModelDeploymentAdapterKinds.OpenRouter,
            OpenRouterAdapterContracts.AdapterContractVersion,
            OpenRouterLiveQualification.GptOssDarkbloomProfileId,
            "1",
            OpenRouterLiveQualification.GptOssDarkbloomProfileDigest,
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            OpenRouterLiveQualification.GptOssDarkbloomModel,
            ExecutionAttemptOutcomeCategories.ContentProduced,
            10,
            outputTokens,
            "pref.prat.phase21.content",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            ModelProviderRequestPhases.Content,
            "prat.phase21.content");

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            var directory = Directory.CreateTempSubdirectory("flex-agent-openrouter-phase21-budget-");
            Path = directory.FullName;
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(
                    Path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}

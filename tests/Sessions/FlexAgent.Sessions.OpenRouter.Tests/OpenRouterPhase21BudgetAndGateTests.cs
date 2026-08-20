using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterPhase21BudgetAndGateTests
{
    [Fact]
    public void Gpt_oss_phase_authorizes_only_at_consumed_zero_on_its_own_ledger()
    {
        Assert.Equal("gpt-oss-darkbloom-matrix", OpenRouterLiveQualification.GptOssDarkbloomPhase);
        Assert.Equal(0, OpenRouterLiveQualification.GptOssDarkbloomStartsAtConsumed);
        Assert.Equal(4, OpenRouterLiveQualification.Phase21MaxInferenceRequests);
        Assert.Equal(12, OpenRouterLiveQualification.MaxInferenceRequests);

        Assert.True(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 0,
                expectedConsumedText: "0",
                out var authorized));
        Assert.Equal(string.Empty, authorized);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 1,
                expectedConsumedText: "1",
                out var stale));
        Assert.Equal("gpt_oss_darkbloom_requires_consumed_0", stale);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                currentConsumed: 0,
                expectedConsumedText: "0",
                out var wrongPhase));
        Assert.Equal("phase_mismatch", wrongPhase);
    }

    [Fact]
    public void Recorded_historical_eleven_of_twelve_refuses_every_live_phase_including_phase21()
    {
        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                OpenRouterLiveQualification.GptOssDarkbloomPhase,
                currentConsumed: 11,
                expectedConsumedText: "11",
                out var phase21));
        Assert.Equal("gpt_oss_darkbloom_requires_consumed_0", phase21);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                OpenRouterLiveQualification.GemmaDarkbloomPhase,
                currentConsumed: 11,
                expectedConsumedText: "11",
                out var gemma));
        Assert.Equal("gemma_darkbloom_requires_consumed_9", gemma);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.NemotronNanoBackupPhase,
                OpenRouterLiveQualification.NemotronNanoBackupPhase,
                currentConsumed: 11,
                expectedConsumedText: "11",
                out var nano));
        Assert.Equal("nemotron_nano_backup_requires_consumed_10", nano);

        Assert.False(
            OpenRouterLiveQualification.TryAuthorizeReservation(
                OpenRouterLiveQualification.PinnedMatrixPhase,
                OpenRouterLiveQualification.PinnedMatrixPhase,
                currentConsumed: 11,
                expectedConsumedText: "11",
                out var lightning));
        Assert.Equal("pinned_matrix_already_recorded", lightning);

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
    public void Phase21_budget_is_a_distinct_0_of_4_ledger_and_cannot_mutate_historical_state()
    {
        using var directory = new TemporaryDirectory();
        var historicalPath = Path.Combine(directory.Path, "historical");
        var phase21Path = Path.Combine(directory.Path, "phase21");
        File.WriteAllText(historicalPath, "openrouter_qualification_budget.v1\n11\n12\n");
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
        Assert.False(fresh.TryReserve(out var exhausted));
        Assert.Equal(4, exhausted);
        Assert.Equal(
            "openrouter_qualification_budget.phase21.v1\n4\n4\n",
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
    public void Visible_content_acceptance_stays_below_256_even_when_the_request_ceiling_is_1024()
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

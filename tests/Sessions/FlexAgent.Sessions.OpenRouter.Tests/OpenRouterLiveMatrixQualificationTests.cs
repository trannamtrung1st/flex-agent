using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterLiveMatrixQualificationTests
{
    [Fact]
    public void Empty_completed_stream_does_not_qualify()
    {
        var control = new ModelExecutionStructuredControl(Admission());
        var completed = new ModelContentCompleted
        {
            Provenance = Provenance(outputTokens: 4),
        };

        Assert.False(
            OpenRouterLiveMatrixQualification.TryQualify(
                control,
                [completed],
                OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens,
                "stop",
                out var denial));
        Assert.Equal("missing_visible_content", denial);
    }

    [Fact]
    public void Length_truncated_output_does_not_qualify()
    {
        var control = new ModelExecutionStructuredControl(Admission());
        var completed = new ModelContentCompleted
        {
            Provenance = Provenance(OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens),
        };

        Assert.False(
            OpenRouterLiveMatrixQualification.TryQualify(
                control,
                [new ModelContentTextDelta("Hi"), completed],
                OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens,
                "stop",
                out var denial));
        Assert.Equal("length_truncated", denial);
    }

    [Fact]
    public void Failed_content_does_not_qualify_even_with_a_delta()
    {
        var control = new ModelExecutionStructuredControl(Admission());

        Assert.False(
            OpenRouterLiveMatrixQualification.TryQualify(
                control,
                [new ModelContentTextDelta("Hi"), new ModelContentFailed(ExecutionFailureReasons.ProviderUnavailable)],
                OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens,
                "stop",
                out var denial));
        Assert.Equal("content_failed", denial);
    }

    [Fact]
    public void Structured_control_plus_visible_non_truncated_content_qualifies()
    {
        var control = new ModelExecutionStructuredControl(Admission());
        var completed = new ModelContentCompleted
        {
            Provenance = Provenance(outputTokens: 8),
        };

        Assert.True(
            OpenRouterLiveMatrixQualification.TryQualify(
                control,
                [new ModelContentTextDelta("Hello"), completed],
                OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens,
                "stop",
                out var denial));
        Assert.Equal(string.Empty, denial);
    }

    [Fact]
    public void Content_reservation_requires_an_admitted_structured_decision()
    {
        Assert.True(
            OpenRouterLiveMatrixQualification.TryAuthorizeContentAfterControl(
                new ModelExecutionStructuredControl(Admission()),
                out var admitted));
        Assert.Equal(string.Empty, admitted);

        Assert.False(
            OpenRouterLiveMatrixQualification.TryAuthorizeContentAfterControl(
                new ModelExecutionFailed(ExecutionFailureReasons.ProviderUnavailable),
                out var rejected));
        Assert.Equal("control_not_admitted", rejected);
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
            "openrouter.synthetic.local.nemotron-3.5-lightning",
            "1",
            "52b47fe8a81ec93aad637d3d81fee665ee9a8230762ecad3204ad6963ca038ac",
            "nvidia/nemotron-3.5-lightning:free",
            "nvidia/nemotron-3.5-lightning:free",
            ExecutionAttemptOutcomeCategories.ContentProduced,
            10,
            outputTokens,
            "pref.prat.phase9.content",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            ModelProviderRequestPhases.Content,
            "prat.phase9.content");
}
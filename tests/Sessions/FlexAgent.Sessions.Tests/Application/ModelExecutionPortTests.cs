using System.Text;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class ModelExecutionPortTests
{
    [Fact]
    public async Task Fake_adapter_returns_structured_control_without_message_content()
    {
        var adapter = new DeterministicFakeModelExecutionAdapter();
        var envelope = SessionRuntimeTestFixtures.Envelope(
            "ainv.00000001",
            outputs: [SessionRuntimeTestFixtures.MessageOutput()]);
        adapter.EnqueueEnvelope(envelope);

        var result = await adapter.ExecuteAsync(CreateRequest("ainv.00000001"), CancellationToken.None);

        var control = Assert.IsType<ModelExecutionStructuredControl>(result);
        Assert.Equal(DecisionDispositions.Respond, control.Envelope.Disposition);
        Assert.All(
            control.Envelope.Outputs,
            output => Assert.True(string.IsNullOrWhiteSpace(output.ModelAgentOutputId)));
    }

    [Fact]
    public async Task Oversized_or_malformed_control_is_an_execution_failure_not_a_decision()
    {
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueControlJson(Encoding.UTF8.GetBytes(new string('x', 128)));
        var oversized = await adapter.ExecuteAsync(
            CreateRequest("ainv.00000001", maxControlUtf8Bytes: 16),
            CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(oversized).ReasonCategory);

        adapter.EnqueueControlJson("{ not json"u8.ToArray());
        var malformed = await adapter.ExecuteAsync(CreateRequest("ainv.00000001"), CancellationToken.None);
        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(malformed).ReasonCategory);
    }

    [Fact]
    public async Task Cancellation_does_not_fabricate_a_decision()
    {
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueEnvelope(SessionRuntimeTestFixtures.Envelope("ainv.00000001"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await adapter.ExecuteAsync(CreateRequest("ainv.00000001"), cts.Token);

        Assert.Equal(
            ExecutionAttemptOutcomeCategories.Cancelled,
            Assert.IsType<ModelExecutionFailed>(result).ReasonCategory);
    }

    [Fact]
    public void Missing_credential_binding_fails_closed_without_calling_the_port()
    {
        var missing = new ModelDeploymentCredentialBindingResult(
            false,
            ModelDeploymentCredentialBindingOutcomeCodes.BindingMissing,
            null);

        var rejected = ModelExecutionPreflight.RejectIfBindingUnavailable(missing);

        Assert.NotNull(rejected);
        Assert.Equal(ExecutionFailureReasons.CredentialBindingFailed, rejected!.ReasonCategory);
    }

    [Fact]
    public void Provider_mismatch_does_not_fall_back_to_another_binding()
    {
        var mismatched = ModelDeploymentCredentialBindingResolver.Resolve(
            new ModelDeploymentCredentialBindingRequest(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "synthetic.provider",
                OrganizationBindingReference: "bind.opaque.0001",
                OrganizationBindingVersion: "bind.v1",
                DeploymentDefaultBindingReference: "bind.default.0001",
                DeploymentDefaultBindingVersion: "bind.v1",
                OrganizationBindingRevoked: false,
                OrganizationBindingProviderMismatch: true,
                OrganizationBindingWrongOrganization: false));

        var rejected = ModelExecutionPreflight.RejectIfBindingUnavailable(mismatched);

        Assert.False(mismatched.Succeeded);
        Assert.Equal(ModelDeploymentCredentialBindingOutcomeCodes.BindingProviderMismatch, mismatched.OutcomeCode);
        Assert.NotNull(rejected);
        Assert.Equal(ExecutionFailureReasons.CredentialBindingFailed, rejected!.ReasonCategory);
    }

    [Fact]
    public async Task Parsed_control_json_yields_one_envelope_or_an_execution_failure()
    {
        var adapter = new DeterministicFakeModelExecutionAdapter();
        var json = """
            {"schema_version":"v2","agent_decision_id":"adec.00000001","agent_invocation_id":"ainv.00000001","produced_at":"2026-08-14T00:00:00Z","disposition":"no_action","outputs":[],"requested_actions":[],"no_action":{"reason_category":"intentional_silence"}}
            """;
        adapter.EnqueueControlJson(Encoding.UTF8.GetBytes(json));

        var result = await adapter.ExecuteAsync(CreateRequest("ainv.00000001"), CancellationToken.None);

        var control = Assert.IsType<ModelExecutionStructuredControl>(result);
        Assert.Equal(DecisionDispositions.NoAction, control.Envelope.Disposition);
        Assert.Empty(control.Envelope.Outputs);
    }

    private static ModelExecutionAttemptRequest CreateRequest(
        string invocationId,
        int maxControlUtf8Bytes = 65_536)
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var context = new InvocationContext(
            session.Ownership,
            session.Binding.ConfigurationDigest,
            session.Policy.PolicyDigest,
            session.Binding.PermittedSubmissionRefs,
            session.Binding.PermittedKnowledgeRefs,
            session.Binding.PermittedMemoryReadRefs,
            [],
            [InvocationContextFactCategories.SubmissionRef]);
        return new ModelExecutionAttemptRequest(
            session.Ownership,
            invocationId,
            "synthetic.provider",
            "bind.opaque.0001",
            "bind.v1",
            context,
            AttemptOrdinal: 1,
            maxControlUtf8Bytes);
    }
}

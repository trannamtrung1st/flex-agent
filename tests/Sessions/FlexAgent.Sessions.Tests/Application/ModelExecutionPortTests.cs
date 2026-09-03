using System.Reflection;
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
            decisionId: "adec.port.00000001",
            outputs:
            [
                SessionRuntimeTestFixtures.MessageOutput(
                    turnId: "turn.00000001",
                    responseSlotId: "slot.00000001"),
            ]);
        adapter.EnqueueEnvelope(envelope);

        var result = await adapter.ExecuteAsync(CreateRequest("ainv.00000001"), CancellationToken.None);

        var control = Assert.IsType<ModelExecutionStructuredControl>(result);
        Assert.Equal(DecisionDispositions.Respond, control.Envelope.Disposition);
        Assert.All(
            control.Envelope.Outputs,
            output => Assert.True(string.IsNullOrWhiteSpace(output.ModelAgentOutputId)));
    }

    [Fact]
    public async Task Typed_message_payload_ref_cannot_become_structured_control()
    {
        var adapter = new DeterministicFakeModelExecutionAdapter();
        var envelope = SessionRuntimeTestFixtures.Envelope(
            "ainv.00000001",
            decisionId: "adec.port.00000001",
            outputs:
            [
                SessionRuntimeTestFixtures.MessageOutput(
                    turnId: "turn.00000001",
                    responseSlotId: "slot.00000001",
                    payloadRef: new ProtectedContentRef(
                        "prot.message.invalid.0001",
                        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")),
            ]);
        adapter.EnqueueEnvelope(envelope);

        var result = await adapter.ExecuteAsync(CreateRequest("ainv.00000001"), CancellationToken.None);

        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(result).ReasonCategory);
        Assert.IsNotType<ModelExecutionStructuredControl>(result);
    }

    [Fact]
    public void Structured_control_can_only_be_constructed_from_a_schema_admitted_envelope()
    {
        var parameter = Assert.Single(typeof(ModelExecutionStructuredControl).GetConstructors().Single().GetParameters());
        Assert.Equal(typeof(ValidatedAgentDecisionEnvelope), parameter.ParameterType);
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
    public async Task Synthetic_development_adapter_repeats_a_respond_envelope_and_visible_reply()
    {
        var adapter = new SyntheticDevelopmentModelExecutionAdapter();
        var first = await adapter.ExecuteAsync(CreateRequest("ainv.synth.0001"), CancellationToken.None);
        var second = await adapter.ExecuteAsync(CreateRequest("ainv.synth.0002"), CancellationToken.None);

        var firstControl = Assert.IsType<ModelExecutionStructuredControl>(first);
        var secondControl = Assert.IsType<ModelExecutionStructuredControl>(second);
        Assert.Equal(DecisionDispositions.Respond, firstControl.Envelope.Disposition);
        Assert.Equal("ainv.synth.0001", firstControl.Envelope.InvocationId);
        Assert.Equal("ainv.synth.0002", secondControl.Envelope.InvocationId);
        Assert.NotEqual(firstControl.Envelope.DecisionId, secondControl.Envelope.DecisionId);

        var streamed = new List<ModelContentEvent>();
        await foreach (var item in adapter.StreamParticipantVisibleContentAsync(
            new ModelContentStreamRequest(
                SessionRuntimeTestFixtures.CreateOwnership(),
                "ainv.synth.0001",
                "agen.synth.0001"),
            CancellationToken.None))
        {
            streamed.Add(item);
        }

        Assert.Equal(
            SyntheticDevelopmentModelExecutionAdapter.ParticipantVisibleReply,
            Assert.IsType<ModelContentTextDelta>(streamed[0]).ExactUtf8Text);
        Assert.IsType<ModelContentCompleted>(streamed[1]);
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

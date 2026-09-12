using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class ProtectedModelResponseWireBytesDigestTests
{
    [Fact]
    public void Digest_procedure_id_is_versioned()
    {
        Assert.Equal(
            "evaluation-model-response-wire-content-digest-sha256-v1",
            ProtectedModelResponseWireBytesDigest.DigestProcedureId);
    }

    [Fact]
    public void Wire_json_contains_response_ref_content_digest_marker()
    {
        var response = new EvaluationModelResponseV1(
            "v1",
            "eval.agent.assisted.output.v1",
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            EvaluatorModes.AgentAssisted,
            CriterionStatuses.Satisfied,
            "high",
            ["ambiguous_language"],
            "Structure is complete.",
            ["evid.synthetic.0001"],
            new ProtectedPayloadRefV1("prot.eval.res.example", new string('0', 64)),
            "pass",
            null,
            "dinv.synthetic.0002");
        var wireText = System.Text.Encoding.UTF8.GetString(
            EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(response));

        Assert.Contains("\"content_digest\":\"", wireText, StringComparison.Ordinal);
    }

    [Fact]
    public void Bound_wire_bytes_match_adapter_response_ref_digest()
    {
        var response = new EvaluationModelResponseV1(
            "v1",
            "eval.agent.assisted.output.v1",
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            EvaluatorModes.AgentAssisted,
            CriterionStatuses.Satisfied,
            "high",
            ["ambiguous_language"],
            "Structure is complete.",
            ["evid.synthetic.0001"],
            new ProtectedPayloadRefV1("prot.eval.res.example", new string('0', 64)),
            "pass",
            null,
            "dinv.synthetic.0002");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var verified = ProtectedModelResponseWireBytesDigest.TryVerify(
            bound.WireUtf8,
            bound.ResponseRef.ContentDigest);

        Assert.True(verified.Succeeded, verified.OutcomeCode);
        Assert.Equal(bound.ResponseRef.ContentDigest, verified.Value);
    }

    [Fact]
    public void Mutated_wire_bytes_fail_digest_verification()
    {
        var response = new EvaluationModelResponseV1(
            "v1",
            "eval.agent.assisted.output.v1",
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            EvaluatorModes.AgentAssisted,
            CriterionStatuses.Satisfied,
            "high",
            ["ambiguous_language"],
            "Structure is complete.",
            ["evid.synthetic.0001"],
            new ProtectedPayloadRefV1("prot.eval.res.example", new string('0', 64)),
            "pass",
            null,
            "dinv.synthetic.0002");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);
        var mutated = response with
        {
            Rationale = "Forged rationale after binding.",
            ResponseRef = bound.ResponseRef,
        };
        var mutatedWireUtf8 = EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(mutated);

        var verified = ProtectedModelResponseWireBytesDigest.TryVerify(
            mutatedWireUtf8,
            bound.ResponseRef.ContentDigest);

        Assert.False(verified.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, verified.OutcomeCode);
        Assert.Equal("response_ref", verified.Field);
    }
}

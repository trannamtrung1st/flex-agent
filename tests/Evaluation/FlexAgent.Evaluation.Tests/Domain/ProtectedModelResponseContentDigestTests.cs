using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class ProtectedModelResponseContentDigestTests
{
    [Fact]
    public void Bound_wire_bytes_match_document_response_ref_digest()
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

        var verified = ProtectedModelResponseContentDigest.TryVerify(
            bound.WireUtf8,
            bound.ResponseRef.ContentDigest);

        Assert.True(verified.Succeeded, verified.OutcomeCode);
        Assert.Equal(bound.ResponseRef.ContentDigest, verified.Value);
    }

    [Fact]
    public void Forged_content_digest_is_rejected()
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

        var verified = ProtectedModelResponseContentDigest.TryVerify(
            bound.WireUtf8,
            new string('f', 64));

        Assert.False(verified.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, verified.OutcomeCode);
        Assert.Equal("response_ref", verified.Field);
    }
}

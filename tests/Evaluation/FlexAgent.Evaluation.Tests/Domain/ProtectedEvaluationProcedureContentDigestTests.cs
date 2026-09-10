using System.Text;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class ProtectedEvaluationProcedureContentDigestTests
{
    [Fact]
    public void Canonical_synthetic_procedure_bytes_match_frozen_digest()
    {
        var utf8 = EvaluationFixtures.LoadSyntheticProcedureCanonicalUtf8();

        var result = ProtectedEvaluationProcedureContentDigest.TryVerify(
            utf8,
            EvaluationFixtures.SyntheticProcedureRef().ContentDigest);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(EvaluationFixtures.SyntheticProcedureRef().ContentDigest, result.Value);
    }

    [Fact]
    public void Tampered_valid_procedure_bytes_with_unchanged_digest_metadata_are_rejected()
    {
        var utf8 = EvaluationFixtures.LoadSyntheticProcedureCanonicalUtf8();
        var tampered = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(utf8).Replace(
                "evalproc.p0.text.synthetic.v1",
                "evalproc.p0.text.synthetic.v2",
                StringComparison.Ordinal));

        var result = ProtectedEvaluationProcedureContentDigest.TryVerify(
            tampered,
            EvaluationFixtures.SyntheticProcedureRef().ContentDigest);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("procedure_content_digest", result.Field);
    }
}

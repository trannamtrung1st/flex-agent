using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests.Domain;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationCompletionDeterministicProvenanceVerifierTests
{
    [Fact]
    public void Single_succeeded_attempt_requires_matching_canonical_input_digest()
    {
        var invocationAttemptId = Guid.CreateVersion7();
        var attemptId = Guid.CreateVersion7();
        var criterion = Criterion();
        var attempts = new[]
        {
            CreateAttempt(
                attemptId,
                invocationAttemptId,
                criterion,
                new string('a', 64),
                "succeeded"),
        };
        var judgment = CreateJudgment(attemptId, criterion, Guid.CreateVersion7());

        var valid = EvaluationCompletionDeterministicProvenanceVerifier.IsJudgmentProvenanceValid(
            judgment,
            criterion,
            invocationAttemptId,
            attempts,
            [],
            out var field);

        Assert.True(valid);
        Assert.Null(field);
    }

    [Fact]
    public void Conflicting_canonical_input_digests_require_evidence_binding()
    {
        var invocationAttemptId = Guid.CreateVersion7();
        var correctAttemptId = Guid.CreateVersion7();
        var wrongAttemptId = Guid.CreateVersion7();
        var criterion = Criterion();
        var attempts = new[]
        {
            CreateAttempt(
                correctAttemptId,
                invocationAttemptId,
                criterion,
                new string('a', 64),
                "succeeded"),
            CreateAttempt(
                wrongAttemptId,
                invocationAttemptId,
                criterion,
                new string('b', 64),
                "succeeded"),
        };
        var judgment = CreateJudgment(wrongAttemptId, criterion, Guid.CreateVersion7());

        var valid = EvaluationCompletionDeterministicProvenanceVerifier.IsJudgmentProvenanceValid(
            judgment,
            criterion,
            invocationAttemptId,
            attempts,
            [],
            out var field);

        Assert.False(valid);
        Assert.Equal("canonical_input_digest", field);
    }

    [Fact]
    public void Deterministic_fact_evidence_binds_expected_canonical_input_digest()
    {
        var invocationAttemptId = Guid.CreateVersion7();
        var correctAttemptId = Guid.CreateVersion7();
        var wrongAttemptId = Guid.CreateVersion7();
        var evidenceId = Guid.CreateVersion7();
        var criterion = Criterion();
        var attempts = new[]
        {
            CreateAttempt(
                correctAttemptId,
                invocationAttemptId,
                criterion,
                new string('a', 64),
                "succeeded"),
            CreateAttempt(
                wrongAttemptId,
                invocationAttemptId,
                criterion,
                new string('b', 64),
                "succeeded"),
        };
        var evidence = new EvaluationEvidenceLocatorRecord(
            evidenceId,
            "deterministic.fact",
            correctAttemptId,
            Guid.CreateVersion7(),
            new string('c', 64),
            "evidence-locator.v1",
            new string('d', 64),
            "exact_range",
            "verified");
        var judgment = CreateJudgment(correctAttemptId, criterion, evidenceId);

        var valid = EvaluationCompletionDeterministicProvenanceVerifier.IsJudgmentProvenanceValid(
            judgment,
            criterion,
            invocationAttemptId,
            attempts,
            [evidence],
            out var field);

        Assert.True(valid);
        Assert.Null(field);
    }

    private static EvaluationProcedureCriterionV1 Criterion() =>
        EvaluationProcedureTestFixtures.LoadP0TextSynthetic().Criteria
            .Single(item => item.CriterionId == "crit.objective.word-count");

    private static DeterministicAttemptProvenanceRow CreateAttempt(
        Guid attemptId,
        Guid invocationAttemptId,
        EvaluationProcedureCriterionV1 criterion,
        string canonicalInputDigest,
        string outcome) =>
        new(
            attemptId,
            invocationAttemptId,
            criterion.CriterionId,
            criterion.CriterionVersion,
            criterion.DeterministicEvaluator!.EvaluatorId,
            criterion.DeterministicEvaluator.EvaluatorVersion,
            criterion.DeterministicEvaluator.EvaluatorDigest,
            criterion.DeterministicEvaluator.DependencyDigest,
            criterion.DeterministicEvaluator.ConfigurationDigest,
            canonicalInputDigest,
            outcome);

    private static CriterionJudgment CreateJudgment(
        Guid attemptId,
        EvaluationProcedureCriterionV1 criterion,
        Guid? evidenceId = null)
    {
        var procedure = EvaluationProcedureTestFixtures.LoadP0TextSynthetic();
        var draft = new CriterionJudgmentDraft(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            criterion.CriterionId,
            criterion.CriterionVersion,
            criterion.EvaluatorMode,
            CriterionStatuses.Satisfied,
            "high",
            ["evaluator_bound"],
            "Rationale with enough detail for validation.",
            evidenceId is null ? [] : [evidenceId.Value],
            null,
            null,
            attemptId);
        var created = CriterionJudgmentValidator.TryCreate(procedure, draft);
        Assert.True(created.Succeeded, created.OutcomeCode);
        return created.Value!;
    }
}

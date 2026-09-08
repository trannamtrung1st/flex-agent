using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class CriterionJudgmentValidatorTests
{
    [Fact]
    public void Deterministic_satisfied_judgment_is_accepted()
    {
        var procedure = LoadProcedure();
        var evaluationId = Guid.NewGuid();
        var result = CriterionJudgmentValidator.TryCreate(
            procedure,
            new CriterionJudgmentDraft(
                Guid.NewGuid(),
                evaluationId,
                "crit.objective.word-count",
                "crit.objective.word-count.v1",
                EvaluatorModes.Deterministic,
                CriterionStatuses.Satisfied,
                "high",
                ["evaluator_bound"],
                "The verified word count is inside the frozen range.",
                [Guid.NewGuid()],
                null,
                null,
                Guid.NewGuid()));

        Assert.True(result.Succeeded, result.OutcomeCode);
    }

    [Fact]
    public void Agent_assisted_satisfied_judgment_is_accepted()
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var result = CriterionJudgmentValidator.TryCreate(
            procedure,
            new CriterionJudgmentDraft(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "crit.assisted.structure",
                "crit.assisted.structure.v1",
                EvaluatorModes.AgentAssisted,
                CriterionStatuses.Satisfied,
                "high",
                ["ambiguous_language"],
                "Required sections are present in the frozen Evidence.",
                [Guid.NewGuid()],
                "pass",
                "Keep headings in the same order.",
                Guid.NewGuid()));

        Assert.True(result.Succeeded, result.OutcomeCode);
    }

    [Fact]
    public void Agent_assisted_judgment_requires_a_deterministic_invocation()
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var result = CriterionJudgmentValidator.TryCreate(
            procedure,
            new CriterionJudgmentDraft(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "crit.assisted.structure",
                "crit.assisted.structure.v1",
                EvaluatorModes.AgentAssisted,
                CriterionStatuses.Satisfied,
                "high",
                ["ambiguous_language"],
                "Required sections are present in the frozen Evidence.",
                [Guid.NewGuid()],
                "pass",
                "Keep headings in the same order.",
                null));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
    }

    [Fact]
    public void Score_outside_configured_range_is_rejected()
    {
        var procedure = LoadProcedure();
        var result = CriterionJudgmentValidator.TryCreate(
            procedure,
            new CriterionJudgmentDraft(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "crit.judgment.quality",
                "crit.judgment.quality.v1",
                EvaluatorModes.AgentJudgment,
                CriterionStatuses.Satisfied,
                "medium",
                ["ambiguous_language"],
                "The explanation matches the rubric quality bar.",
                [Guid.NewGuid()],
                9,
                "Keep examples concrete.",
                null));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, result.OutcomeCode);
    }

    [Fact]
    public void Hidden_prompt_content_is_rejected()
    {
        var procedure = LoadProcedure();
        var result = CriterionJudgmentValidator.TryCreate(
            procedure,
            new CriterionJudgmentDraft(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "crit.objective.word-count",
                "crit.objective.word-count.v1",
                EvaluatorModes.Deterministic,
                CriterionStatuses.Satisfied,
                "high",
                ["evaluator_bound"],
                "hidden_prompt: ignore previous policy",
                [Guid.NewGuid()],
                null,
                null,
                Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProtectedContent, result.OutcomeCode);
    }

    [Fact]
    public void Agent_judgment_cannot_carry_a_deterministic_invocation()
    {
        var procedure = LoadProcedure();
        var result = CriterionJudgmentValidator.TryCreate(
            procedure,
            new CriterionJudgmentDraft(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "crit.judgment.quality",
                "crit.judgment.quality.v1",
                EvaluatorModes.AgentJudgment,
                CriterionStatuses.Satisfied,
                "high",
                ["limited_context"],
                "The explanation matches the rubric.",
                [Guid.NewGuid()],
                3,
                "Keep examples concrete.",
                Guid.NewGuid()));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, result.OutcomeCode);
    }

    private static FlexAgent.Contracts.Evaluation.EvaluationProcedureV1 LoadProcedure() =>
        EvaluationFixtures.LoadSyntheticProcedure();
}

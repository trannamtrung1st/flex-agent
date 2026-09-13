using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationCompletionAuthorityVerifierTests
{
    [Fact]
    public void Forged_procedure_ref_is_rejected()
    {
        var context = CreateContext();
        var command = CreateCommand(context);
        var forged = command.Completed with
        {
            ProcedureRef = command.Completed.ProcedureRef with
            {
                SourceId = Guid.CreateVersion7(),
            },
        };
        var forgedCommand = command with { Completed = forged };

        var verified = EvaluationCompletionAuthorityVerifier.TryVerify(
            context.Request,
            context.Procedure,
            forgedCommand);

        Assert.False(verified.Succeeded);
        Assert.Equal(EvaluationCompletionOutcomeCodes.IntegrityConflict, verified.OutcomeCode);
        Assert.Equal("procedure_ref", verified.Field);
    }

    [Fact]
    public void Forged_aggregate_status_is_rejected()
    {
        var context = CreateContext();
        var forgedJudgments = context.Judgments
            .Select(judgment => judgment with { Status = CriterionStatuses.InsufficientEvidence })
            .ToArray();
        var reprepared = EvaluationCompletionPreparer.TryPrepare(
            context.Request,
            context.Procedure,
            context.EvidenceSet,
            context.EvidenceItems,
            forgedJudgments,
            DateTimeOffset.UtcNow,
            "evaluation-service").Value!;
        var command = CreateCommand(context) with
        {
            Judgments = forgedJudgments,
            Completed = reprepared with
            {
                AggregateStatus = EvaluationAggregateStatuses.Complete,
            },
        };

        var verified = EvaluationCompletionAuthorityVerifier.TryVerify(
            context.Request,
            context.Procedure,
            command);

        Assert.False(verified.Succeeded);
        Assert.Equal(EvaluationCompletionOutcomeCodes.IntegrityConflict, verified.OutcomeCode);
        Assert.Equal("completed", verified.Field);
    }

    [Fact]
    public void Missing_criterion_judgment_is_rejected()
    {
        var context = CreateContext();
        var command = CreateCommand(context);
        var forgedCommand = command with { Judgments = [command.Judgments[0]] };

        var verified = EvaluationCompletionAuthorityVerifier.TryVerify(
            context.Request,
            context.Procedure,
            forgedCommand);

        Assert.False(verified.Succeeded);
        Assert.True(
            verified.OutcomeCode is EvaluationFailureCodes.IncompleteCriteria
                or EvaluationCompletionOutcomeCodes.IntegrityConflict,
            verified.OutcomeCode);
    }

    [Fact]
    public void Valid_command_passes_authoritative_verification()
    {
        var context = CreateContext();
        var command = CreateCommand(context);

        var verified = EvaluationCompletionAuthorityVerifier.TryVerify(
            context.Request,
            context.Procedure,
            command);

        Assert.True(verified.Succeeded, verified.OutcomeCode);
        Assert.Equal(command.Completed.AggregateStatus, verified.Value!.AggregateStatus);
        Assert.Equal(command.Completed.EvidenceSetDigest, verified.Value.EvidenceSetDigest);
        Assert.Equal(command.Completed.ProcedureRef, verified.Value.ProcedureRef);
    }

    private static VerificationContext CreateContext()
    {
        var procedure = EvaluationFixtures.LoadSyntheticProcedure();
        var frozen = EvaluationFixtures.CreateFrozenInput().Value!;
        var request = EvaluationRequest.TryCreate(
            Guid.NewGuid(),
            EvaluationRequestKinds.Initial,
            frozen,
            "idem-eval-authority",
            "deleg.eval.synthetic",
            EvaluationRequestStates.Completing).Value!;
        var evaluationId = Guid.NewGuid();
        var evidenceIds = procedure.Criteria.Select(_ => Guid.NewGuid()).ToArray();
        var items = evidenceIds.Select(id =>
            EvidenceItem.TryCreate(
                id,
                "submission.direct_text",
                EvaluationFixtures.Submission(),
                frozen.Ownership,
                evaluationId,
                "exact_range").Value!).ToArray();
        var evidenceSetId = Guid.NewGuid();
        var invocationAttemptId = Guid.NewGuid();
        var evidenceSetDigest = EvaluationCompletionEvidenceSeal.TryComputeExpectedDigest(
            evidenceSetId,
            invocationAttemptId,
            frozen,
            items);
        Assert.True(evidenceSetDigest.Succeeded, evidenceSetDigest.OutcomeCode);
        var set = EvidenceSet.TryCreate(
            evidenceSetId,
            evaluationId,
            frozen.Ownership,
            items,
            evidenceSetDigest.Value!).Value!;
        var judgments = procedure.Criteria.Select((criterion, index) =>
        {
            object? score = criterion.EvaluatorMode == EvaluatorModes.AgentAssisted
                ? "pass"
                : criterion.EvaluatorMode == EvaluatorModes.AgentJudgment
                    ? 3
                    : null;
            IReadOnlyList<string> uncertainty = criterion.EvaluatorMode == EvaluatorModes.Deterministic
                ? new[] { "evaluator_bound" }
                : new[] { "ambiguous_language" };
            var created = CriterionJudgmentValidator.TryCreate(
                procedure,
                new CriterionJudgmentDraft(
                    Guid.NewGuid(),
                    evaluationId,
                    criterion.CriterionId,
                    criterion.CriterionVersion,
                    criterion.EvaluatorMode,
                    CriterionStatuses.Satisfied,
                    "high",
                    uncertainty,
                    "The criterion is judged against the frozen Evidence set.",
                    [evidenceIds[index]],
                    score,
                    criterion.EvaluatorMode == EvaluatorModes.Deterministic ? null : "Keep the explanation specific.",
                    criterion.EvaluatorMode == EvaluatorModes.AgentJudgment ? null : Guid.NewGuid()));
            Assert.True(created.Succeeded, created.OutcomeCode);
            return created.Value!;
        }).ToArray();
        return new VerificationContext(
            request,
            procedure,
            set,
            items,
            judgments,
            invocationAttemptId);
    }

    private static EvaluationCompletionCommand CreateCommand(VerificationContext context)
    {
        var completed = EvaluationCompletionPreparer.TryPrepare(
            context.Request,
            context.Procedure,
            context.EvidenceSet,
            context.EvidenceItems,
            context.Judgments,
            DateTimeOffset.UtcNow,
            "evaluation-service").Value!;
        completed = completed with
        {
            EvidenceSetDigest = EvaluationCompletionEvidenceSeal.TryComputeExpectedDigest(
                context.EvidenceSet.EvidenceSetId,
                context.InvocationAttemptId,
                context.Request.FrozenInput,
                context.EvidenceItems).Value!,
        };
        return new EvaluationCompletionCommand(
            Guid.NewGuid(),
            context.Request.RequestId,
            context.InvocationAttemptId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            EvaluationActorTypes.Service,
            Guid.NewGuid(),
            "unit.test",
            completed,
            context.EvidenceItems,
            context.Judgments,
            []);
    }

    private sealed record VerificationContext(
        EvaluationRequest Request,
        EvaluationProcedureV1 Procedure,
        EvidenceSet EvidenceSet,
        IReadOnlyList<EvidenceItem> EvidenceItems,
        IReadOnlyList<CriterionJudgment> Judgments,
        Guid InvocationAttemptId);
}

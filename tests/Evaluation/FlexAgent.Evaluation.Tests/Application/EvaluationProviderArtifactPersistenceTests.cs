using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Evaluation.Tests;

namespace FlexAgent.Evaluation.Tests.Application;

public sealed class EvaluationProviderArtifactPersistenceTests
{
    private static readonly EvaluationOwnership Ownership = EvaluationFixtures.Ownership();
    private static readonly Guid RequestId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab");
    private static readonly Guid InvocationAttemptId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbc");

    [Fact]
    public async Task In_memory_store_appends_provider_artifact_and_retries_idempotently()
    {
        var store = new InMemoryEvaluationProviderArtifactStore();
        var command = CreateCommand();

        var first = await store.TryAppendAsync(command, CancellationToken.None);
        var second = await store.TryAppendAsync(command, CancellationToken.None);

        Assert.True(first.Succeeded, first.OutcomeCode);
        Assert.True(second.Succeeded, second.OutcomeCode);
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public async Task Conflicting_response_ref_on_same_artifact_id_fails_with_deterministic_conflict()
    {
        var store = new InMemoryEvaluationProviderArtifactStore();
        var command = CreateCommand();

        Assert.True((await store.TryAppendAsync(command, CancellationToken.None)).Succeeded);

        var conflicting = command with
        {
            ProtectedResponseRef = ProviderArtifactProvenance.ProtectedResponseRef(new string('f', 64)),
        };
        var result = await store.TryAppendAsync(conflicting, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DeterministicConflict, result.OutcomeCode);
    }

    [Fact]
    public void Build_append_command_maps_failed_attempt_without_response_ref()
    {
        var context = CreateContext();
        var criterion = EvaluationFixtures.LoadSyntheticProcedure().Criteria[2];
        var request = CreateRequest(criterion);
        var result = new EvaluationModelAttemptFailed(EvaluationModelExecutionOutcomeCategories.ProviderTimeout);

        var decision = EvaluationProviderArtifactPersistence.TryBuildAppendCommand(
            context,
            criterion,
            request,
            result);

        Assert.True(decision.Succeeded, decision.OutcomeCode);
        Assert.Equal(ProviderArtifactOutcomes.TimedOut, decision.Value!.Outcome);
        Assert.Null(decision.Value.ProtectedResponseRef);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.ProviderTimeout, decision.Value.FailureCategory);
    }

    [Fact]
    public void Build_append_command_maps_successful_attempt_with_response_ref()
    {
        var context = CreateContext();
        var criterion = EvaluationFixtures.LoadSyntheticProcedure().Criteria[2];
        var request = CreateRequest(criterion);
        var responseDigest = new string('d', 64);
        var result = CreateSucceededWireResult(criterion, responseDigest);

        var decision = EvaluationProviderArtifactPersistence.TryBuildAppendCommand(
            context,
            criterion,
            request,
            result);

        Assert.True(decision.Succeeded, decision.OutcomeCode);
        Assert.Equal(ProviderArtifactOutcomes.Succeeded, decision.Value!.Outcome);
        Assert.Equal(
            ProviderArtifactProvenance.ProtectedResponseRef(responseDigest),
            decision.Value.ProtectedResponseRef);
    }

    [Fact]
    public void Build_append_command_maps_post_validation_output_semantic_invalid_with_bound_response_ref()
    {
        var context = CreateContext();
        var criterion = EvaluationFixtures.LoadSyntheticProcedure().Criteria[2];
        var request = CreateRequest(criterion);
        var responseDigest = new string('d', 64);
        var result = CreateSucceededWireResult(criterion, responseDigest);
        var boundRef = new ProtectedPayloadRefV1("prot.eval.res.document", new string('e', 64));

        var decision = EvaluationProviderArtifactPersistence.TryBuildAppendCommand(
            context,
            criterion,
            request,
            result,
            EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid,
            boundRef);

        Assert.True(decision.Succeeded, decision.OutcomeCode);
        Assert.Equal(ProviderArtifactOutcomes.InvalidOutput, decision.Value!.Outcome);
        Assert.Equal(
            EvaluationModelExecutionOutcomeCategories.OutputSemanticInvalid,
            decision.Value.FailureCategory);
        Assert.Equal(
            ProviderArtifactProvenance.ProtectedResponseRef(boundRef.ContentDigest),
            decision.Value.ProtectedResponseRef);
        Assert.NotEqual(
            ProviderArtifactProvenance.ProtectedResponseRef(responseDigest),
            decision.Value.ProtectedResponseRef);
    }

    [Fact]
    public void Build_append_command_maps_post_validation_schema_invalid_with_response_ref()
    {
        var context = CreateContext();
        var criterion = EvaluationFixtures.LoadSyntheticProcedure().Criteria[2];
        var request = CreateRequest(criterion);
        var responseDigest = new string('d', 64);
        var result = CreateSucceededWireResult(criterion, responseDigest);

        var decision = EvaluationProviderArtifactPersistence.TryBuildAppendCommand(
            context,
            criterion,
            request,
            result,
            EvaluationModelExecutionOutcomeCategories.SchemaInvalid);

        Assert.True(decision.Succeeded, decision.OutcomeCode);
        Assert.Equal(ProviderArtifactOutcomes.InvalidOutput, decision.Value!.Outcome);
        Assert.Equal(EvaluationModelExecutionOutcomeCategories.SchemaInvalid, decision.Value.FailureCategory);
        Assert.Equal(
            ProviderArtifactProvenance.ProtectedResponseRef(responseDigest),
            decision.Value.ProtectedResponseRef);
    }

    private static EvaluationModelAttemptSucceeded CreateSucceededWireResult(
        EvaluationProcedureCriterionV1 criterion,
        string responseDigest)
    {
        var response = new EvaluationModelResponseV1(
            "v1",
            criterion.AgentIo!.OutputSchemaId,
            criterion.CriterionId,
            criterion.CriterionVersion,
            criterion.EvaluatorMode,
            CriterionStatuses.Satisfied,
            "high",
            ["ambiguous_language"],
            "Synthetic evaluation model response.",
            [EvaluationEvidenceSourceIdentity.StableEvidenceId(Guid.Parse("22222222-2222-4222-8222-222222222222"))],
            new ProtectedPayloadRefV1("prot.eval.res.synthetic", responseDigest),
            3,
            null,
            null);
        return new EvaluationModelAttemptSucceeded(
            new EvaluationModelWireResponse(
                EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(response),
                response.ResponseRef));
    }

    private static ProviderArtifactAppendCommand CreateCommand()
    {
        var context = CreateContext();
        var criterion = EvaluationFixtures.LoadSyntheticProcedure().Criteria[2];
        var request = CreateRequest(criterion);
        var decision = EvaluationProviderArtifactPersistence.TryBuildAppendCommand(
            context,
            criterion,
            request,
            CreateSucceededWireResult(
                criterion,
                new string('d', 64)));

        return decision.Value!;
    }

    private static EvaluationModelExecutionContext CreateContext()
    {
        var evidenceStableId = EvaluationEvidenceSourceIdentity.StableEvidenceId(
            Guid.Parse("22222222-2222-4222-8222-222222222222"));
        return new EvaluationModelExecutionContext(
            EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(Ownership),
            Ownership,
            RequestId,
            EvaluationStableOwnershipReferenceFactory.StableRequestId(RequestId),
            InvocationAttemptId,
            EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(InvocationAttemptId),
            Guid.Parse("11111111-1111-4111-8111-111111111115"),
            EvaluationFixtures.Model(),
            "eval.instructions.p0.v1",
            [
                new EvaluationModelPermittedEvidenceV1(
                    evidenceStableId,
                    "submission.direct_text",
                    new string('b', 64)),
            ],
            new Dictionary<string, Guid>(StringComparer.Ordinal)
            {
                [evidenceStableId] = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            },
            null,
            null);
    }

    private static EvaluationModelRequestV1 CreateRequest(EvaluationProcedureCriterionV1 criterion) =>
        new(
            "v1",
            EvaluationStableOwnershipReferenceFactory.StableRequestId(RequestId),
            EvaluationStableOwnershipReferenceFactory.StableInvocationAttemptId(InvocationAttemptId),
            criterion.CriterionId,
            criterion.CriterionVersion,
            criterion.EvaluatorMode,
            EvaluationStableOwnershipReferenceFactory.ToSessionOwnershipRef(Ownership),
            criterion.AgentIo!.InputSchemaId,
            criterion.AgentIo.OutputSchemaId,
            "eval.instructions.p0.v1",
            EvaluationFixtures.Model().ProfileId,
            EvaluationFixtures.Model().ProfileVersion,
            EvaluationFixtures.Model().ProfileDigest,
            EvaluationFixtures.Model().CredentialBindingReference,
            [
                new EvaluationModelPermittedEvidenceV1(
                    EvaluationEvidenceSourceIdentity.StableEvidenceId(
                        Guid.Parse("22222222-2222-4222-8222-222222222222")),
                    "submission.direct_text",
                    new string('b', 64)),
            ],
            null,
            criterion.AgentIo.MaxContextUnicodeScalars);
}

using Dapper;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;
using Npgsql;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class PostgresEvaluationProviderArtifactStore(
    PostgresConnectionAccessor connectionAccessor) : IEvaluationProviderArtifactStore
{
    public async Task<EvaluationDecision<Guid>> TryAppendAsync(
        ProviderArtifactAppendCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Ownership.OrganizationId == Guid.Empty
            || command.RequestId == Guid.Empty
            || command.InvocationAttemptId == Guid.Empty
            || command.ProviderArtifactId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.ProtectedRequestRef)
            || string.IsNullOrWhiteSpace(command.CriterionId)
            || string.IsNullOrWhiteSpace(command.CriterionVersion))
        {
            return EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.InvalidField);
        }

        await using var connection = await connectionAccessor.OpenConnectionAsync(cancellationToken);
        var inserted = await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO evaluation_provider_artifacts (
                    organization_id, provider_artifact_id, request_id, invocation_attempt_id,
                    criterion_id, criterion_version,
                    model_profile_id, model_profile_version, model_profile_digest,
                    credential_binding_reference, protected_request_ref, protected_response_ref,
                    outcome, failure_category)
                SELECT
                    @OrganizationId, @ProviderArtifactId, @RequestId, @InvocationAttemptId,
                    @CriterionId, @CriterionVersion,
                    @ModelProfileId, @ModelProfileVersion, @ModelProfileDigest,
                    @CredentialBindingReference, @ProtectedRequestRef, @ProtectedResponseRef,
                    @Outcome, @FailureCategory
                FROM evaluation_requests AS request
                INNER JOIN evaluation_invocation_attempts AS attempt
                  ON attempt.organization_id = request.organization_id
                 AND attempt.request_id = request.request_id
                 AND attempt.invocation_attempt_id = @InvocationAttemptId
                WHERE request.organization_id = @OrganizationId
                  AND request.request_id = @RequestId
                  AND request.activity_id = @ActivityId
                  AND request.participant_id = @ParticipantId
                  AND request.attempt_id = @AttemptId
                  AND request.session_id = @SessionId
                ON CONFLICT (organization_id, provider_artifact_id) DO NOTHING;
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.ProviderArtifactId,
                    command.RequestId,
                    command.InvocationAttemptId,
                    command.CriterionId,
                    command.CriterionVersion,
                    ModelProfileId = command.ModelIdentity.ProfileId,
                    ModelProfileVersion = command.ModelIdentity.ProfileVersion,
                    ModelProfileDigest = command.ModelIdentity.ProfileDigest,
                    CredentialBindingReference = command.ModelIdentity.CredentialBindingReference,
                    command.ProtectedRequestRef,
                    command.ProtectedResponseRef,
                    command.Outcome,
                    command.FailureCategory,
                    command.Ownership.ActivityId,
                    command.Ownership.ParticipantId,
                    command.Ownership.AttemptId,
                    command.Ownership.SessionId,
                },
                cancellationToken: cancellationToken));

        if (inserted == 1)
        {
            return EvaluationDecision<Guid>.Ok(command.ProviderArtifactId);
        }

        return await TryReconcileExistingAsync(connection, command, cancellationToken);
    }

    private static async Task<EvaluationDecision<Guid>> TryReconcileExistingAsync(
        NpgsqlConnection connection,
        ProviderArtifactAppendCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await connection.QuerySingleOrDefaultAsync<PersistedProviderArtifactRow>(
            new CommandDefinition(
                """
                SELECT
                    invocation_attempt_id,
                    criterion_id,
                    criterion_version,
                    model_profile_id,
                    model_profile_version,
                    model_profile_digest,
                    credential_binding_reference,
                    protected_request_ref,
                    protected_response_ref,
                    outcome,
                    failure_category
                FROM evaluation_provider_artifacts
                WHERE organization_id = @OrganizationId
                  AND provider_artifact_id = @ProviderArtifactId;
                """,
                new
                {
                    command.Ownership.OrganizationId,
                    command.ProviderArtifactId,
                },
                cancellationToken: cancellationToken));

        if (existing is null)
        {
            return EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.InvalidField);
        }

        var candidate = ProviderArtifactProvenance.FromAppendCommand(command);
        var persisted = new ProviderArtifactProvenanceSnapshot(
            existing.invocation_attempt_id,
            existing.criterion_id,
            existing.criterion_version,
            existing.model_profile_id,
            existing.model_profile_version,
            existing.model_profile_digest,
            existing.credential_binding_reference,
            existing.protected_request_ref,
            existing.protected_response_ref,
            existing.outcome,
            existing.failure_category);

        if (!ProviderArtifactProvenance.IsEquivalentRetry(persisted, candidate))
        {
            return EvaluationDecision<Guid>.Fail(EvaluationFailureCodes.DeterministicConflict);
        }

        return EvaluationDecision<Guid>.Ok(command.ProviderArtifactId);
    }

    private sealed record PersistedProviderArtifactRow(
        Guid invocation_attempt_id,
        string criterion_id,
        string criterion_version,
        string model_profile_id,
        string model_profile_version,
        string model_profile_digest,
        string credential_binding_reference,
        string protected_request_ref,
        string? protected_response_ref,
        string outcome,
        string? failure_category);
}

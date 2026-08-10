using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;
using Npgsql;

namespace FlexAgent.Configuration.Infrastructure;

public sealed class RegisterConfigurationSourceVersionHandler(
    IAuthorizationKernel authorizationKernel,
    ICommitAuthorizationKernel commitAuthorizationKernel,
    PostgresConfigurationSourceVersionRepository versionRepository,
    ConfigurationDigestVerifier digestVerifier,
    PostgresConnectionAccessor connectionAccessor,
    IAuditEventWriter auditEventWriter,
    IOutboxItemWriter outboxItemWriter) : IRegisterConfigurationSourceVersionHandler
{
    public async Task<RegisterConfigurationSourceVersionResult> HandleAsync(
        RegisterConfigurationSourceVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        var resourceScope = new ResourceScope(
            command.Organization,
            AuthorizationResourceTypes.ConfigurationSource,
            command.ConfigurationSourceId);

        var authorizationRequest = new AuthorizationRequest(
            command.Actor,
            command.Organization,
            AuthorizationActions.RegisterConfigurationSourceVersion,
            resourceScope,
            command.SourceChannel,
            command.CorrelationId);

        var admissionDecision = await authorizationKernel.AuthorizeAsync(authorizationRequest, cancellationToken);
        if (!admissionDecision.IsPermitted)
        {
            return Denied();
        }

        if (!digestVerifier.TryVerify(
                command.ProcedureId,
                command.SchemaVersion,
                command.CanonicalUtf8Content,
                command.DeclaredContentDigest,
                out var digestFailureCode))
        {
            return new RegisterConfigurationSourceVersionResult(
                false,
                digestFailureCode ?? RegisterConfigurationSourceVersionFailureCodes.InvalidDigest,
                null);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);

        try
        {
            var commitDecision = await commitAuthorizationKernel.ReauthorizeInTransactionAsync(
                authorizationRequest,
                scope.Transaction,
                cancellationToken);

            if (!commitDecision.IsPermitted)
            {
                await scope.RollbackAsync(cancellationToken);
                return Denied();
            }

            var parentExists = await versionRepository.SourceExistsInOrganizationAsync(
                command.Organization.OrganizationId,
                command.ConfigurationSourceId,
                scope.Transaction,
                cancellationToken);

            if (!parentExists)
            {
                await scope.RollbackAsync(cancellationToken);
                return Denied();
            }

            var existingByIdempotency = await versionRepository.GetByIdempotencyKeyAsync(
                command.Organization.OrganizationId,
                command.ConfigurationSourceId,
                command.IdempotencyKey,
                scope.Transaction,
                cancellationToken);

            if (existingByIdempotency is not null)
            {
                if (!MatchesPayload(existingByIdempotency, command))
                {
                    await scope.RollbackAsync(cancellationToken);
                    return new RegisterConfigurationSourceVersionResult(
                        false,
                        RegisterConfigurationSourceVersionFailureCodes.IdempotencyConflict,
                        null);
                }

                await scope.CommitAsync(cancellationToken);
                return Success(existingByIdempotency);
            }

            var existingByDigest = await versionRepository.GetByDigestAsync(
                command.Organization.OrganizationId,
                command.ConfigurationSourceId,
                command.DeclaredContentDigest,
                scope.Transaction,
                cancellationToken);

            if (existingByDigest is not null)
            {
                await scope.CommitAsync(cancellationToken);
                return Success(existingByDigest);
            }

            var versionId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var row = new ConfigurationSourceVersionRow(
                versionId,
                command.Organization.OrganizationId,
                command.ConfigurationSourceId,
                command.SchemaVersion,
                command.ProcedureId,
                command.DeclaredContentDigest,
                command.IdempotencyKey,
                createdAt);

            try
            {
                await versionRepository.InsertAsync(row, scope.Transaction, cancellationToken);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                var reconciled = await ReconcileUniqueViolationAsync(command, scope.Transaction, cancellationToken);
                if (reconciled is not null)
                {
                    await scope.CommitAsync(cancellationToken);
                    return reconciled;
                }

                throw;
            }

            await auditEventWriter.InsertAsync(
                new AuditEventWriteModel(
                    EventId: Guid.NewGuid(),
                    OrganizationId: command.Organization.OrganizationId,
                    EventSchemaVersion: "audit-event.v1",
                    OccurredAt: new DateTimeOffset(createdAt, TimeSpan.Zero),
                    CorrelationId: command.CorrelationId,
                    ActorType: command.Actor.ActorType,
                    ActorId: command.Actor.ActorId,
                    Action: AuthorizationActions.RegisterConfigurationSourceVersion,
                    ResourceType: AuthorizationResourceTypes.ConfigurationSourceVersion,
                    ResourceId: versionId,
                    Outcome: "succeeded",
                    ReasonCode: null,
                    RelationshipVersion: commitDecision.RelationshipVersion,
                    SourceChannel: command.SourceChannel,
                    PayloadDigest: command.DeclaredContentDigest),
                scope.Transaction,
                cancellationToken);

            await outboxItemWriter.InsertAsync(
                new OutboxItemWriteModel(
                    Id: Guid.NewGuid(),
                    OrganizationId: command.Organization.OrganizationId,
                    EventType: "configuration_source_version.registered",
                    AggregateType: AuthorizationResourceTypes.ConfigurationSourceVersion,
                    AggregateId: versionId,
                    CorrelationId: command.CorrelationId,
                    PayloadDigest: command.DeclaredContentDigest,
                    CreatedAt: new DateTimeOffset(createdAt, TimeSpan.Zero)),
                scope.Transaction,
                cancellationToken);

            await scope.CommitAsync(cancellationToken);
            return Success(row);
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<RegisterConfigurationSourceVersionResult?> ReconcileUniqueViolationAsync(
        RegisterConfigurationSourceVersionCommand command,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var existingByIdempotency = await versionRepository.GetByIdempotencyKeyAsync(
            command.Organization.OrganizationId,
            command.ConfigurationSourceId,
            command.IdempotencyKey,
            transaction,
            cancellationToken);

        if (existingByIdempotency is not null)
        {
            return MatchesPayload(existingByIdempotency, command)
                ? Success(existingByIdempotency)
                : new RegisterConfigurationSourceVersionResult(
                    false,
                    RegisterConfigurationSourceVersionFailureCodes.IdempotencyConflict,
                    null);
        }

        var existingByDigest = await versionRepository.GetByDigestAsync(
            command.Organization.OrganizationId,
            command.ConfigurationSourceId,
            command.DeclaredContentDigest,
            transaction,
            cancellationToken);

        return existingByDigest is not null ? Success(existingByDigest) : null;
    }

    private static bool MatchesPayload(ConfigurationSourceVersionRow existing, RegisterConfigurationSourceVersionCommand command) =>
        string.Equals(existing.ContentDigest, command.DeclaredContentDigest, StringComparison.Ordinal)
        && string.Equals(existing.ProcedureId, command.ProcedureId, StringComparison.Ordinal)
        && string.Equals(existing.SchemaVersion, command.SchemaVersion, StringComparison.Ordinal);

    private static RegisterConfigurationSourceVersionResult Success(ConfigurationSourceVersionRow row) =>
        new(
            true,
            "configuration_source_version.registered",
            new ConfigurationSourceVersionIdentity(
                row.OrganizationId,
                row.ConfigurationSourceId,
                row.Id,
                row.ContentDigest));

    private static RegisterConfigurationSourceVersionResult Denied() =>
        new(false, RegisterConfigurationSourceVersionFailureCodes.Denied, null);
}

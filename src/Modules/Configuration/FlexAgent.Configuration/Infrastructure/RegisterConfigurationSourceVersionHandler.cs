using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;

namespace FlexAgent.Configuration.Infrastructure;

public sealed class RegisterConfigurationSourceVersionHandler(
    IAuthorizationKernel authorizationKernel,
    ICommitAuthorizationKernel commitAuthorizationKernel,
    PostgresConfigurationSourceVersionRepository versionRepository,
    PostgresConfigurationSourceVersionIdempotencyRepository idempotencyRepository,
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

        var payloadFingerprint = ConfigurationPayloadFingerprint.Compute(command);

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

            var existingIdempotency = await idempotencyRepository.GetByKeyAsync(
                command.Organization.OrganizationId,
                command.ConfigurationSourceId,
                AuthorizationActions.RegisterConfigurationSourceVersion,
                command.IdempotencyKey,
                scope.Transaction,
                cancellationToken);

            if (existingIdempotency is not null)
            {
                var result = await ResolveIdempotencyRecordAsync(
                    command,
                    existingIdempotency,
                    scope.Transaction,
                    cancellationToken);

                await scope.CommitAsync(cancellationToken);
                return result;
            }

            var existingByDigest = await versionRepository.GetByDigestAsync(
                command.Organization.OrganizationId,
                command.ConfigurationSourceId,
                command.DeclaredContentDigest,
                scope.Transaction,
                cancellationToken);

            if (existingByDigest is not null)
            {
                var result = await BindIdempotencyAndReturnAsync(
                    command,
                    existingByDigest,
                    payloadFingerprint,
                    scope.Transaction,
                    cancellationToken);

                await scope.CommitAsync(cancellationToken);
                return result;
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

            var inserted = await versionRepository.TryInsertAsync(row, scope.Transaction, cancellationToken);
            var authoritativeRow = inserted ?? await versionRepository.GetByDigestAsync(
                command.Organization.OrganizationId,
                command.ConfigurationSourceId,
                command.DeclaredContentDigest,
                scope.Transaction,
                cancellationToken);

            if (authoritativeRow is null)
            {
                await scope.RollbackAsync(cancellationToken);
                throw new InvalidOperationException("Configuration source version insert did not produce an authoritative row.");
            }

            var idempotencyResult = await BindIdempotencyAndReturnAsync(
                command,
                authoritativeRow,
                payloadFingerprint,
                scope.Transaction,
                cancellationToken);

            if (!idempotencyResult.Succeeded)
            {
                await scope.RollbackAsync(cancellationToken);
                return idempotencyResult;
            }

            if (inserted is not null)
            {
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
                        ResourceId: authoritativeRow.Id,
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
                        AggregateId: authoritativeRow.Id,
                        CorrelationId: command.CorrelationId,
                        PayloadDigest: command.DeclaredContentDigest,
                        CreatedAt: new DateTimeOffset(createdAt, TimeSpan.Zero)),
                    scope.Transaction,
                    cancellationToken);
            }

            await scope.CommitAsync(cancellationToken);
            return idempotencyResult;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<RegisterConfigurationSourceVersionResult> ResolveIdempotencyRecordAsync(
        RegisterConfigurationSourceVersionCommand command,
        ConfigurationSourceVersionIdempotencyRow existingIdempotency,
        Npgsql.NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(existingIdempotency.PayloadFingerprint, ConfigurationPayloadFingerprint.Compute(command), StringComparison.Ordinal))
        {
            return new RegisterConfigurationSourceVersionResult(
                false,
                RegisterConfigurationSourceVersionFailureCodes.IdempotencyConflict,
                null);
        }

        var version = await versionRepository.GetByIdForSourceAsync(
            command.Organization.OrganizationId,
            command.ConfigurationSourceId,
            existingIdempotency.VersionId,
            transaction,
            cancellationToken);

        if (version is null
            || version.ConfigurationSourceId != command.ConfigurationSourceId)
        {
            return Denied();
        }

        return Success(version);
    }

    private async Task<RegisterConfigurationSourceVersionResult> BindIdempotencyAndReturnAsync(
        RegisterConfigurationSourceVersionCommand command,
        ConfigurationSourceVersionRow version,
        string payloadFingerprint,
        Npgsql.NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (!MatchesPayload(version, command))
        {
            return new RegisterConfigurationSourceVersionResult(
                false,
                RegisterConfigurationSourceVersionFailureCodes.IdempotencyConflict,
                null);
        }

        var insertedVersionId = await idempotencyRepository.TryInsertAsync(
            new ConfigurationSourceVersionIdempotencyRow(
                command.Organization.OrganizationId,
                command.ConfigurationSourceId,
                AuthorizationActions.RegisterConfigurationSourceVersion,
                command.IdempotencyKey,
                version.Id,
                payloadFingerprint,
                DateTime.UtcNow),
            transaction,
            cancellationToken);

        if (insertedVersionId is not null)
        {
            return Success(version);
        }

        var existingIdempotency = await idempotencyRepository.GetByKeyAsync(
            command.Organization.OrganizationId,
            command.ConfigurationSourceId,
            AuthorizationActions.RegisterConfigurationSourceVersion,
            command.IdempotencyKey,
            transaction,
            cancellationToken);

        if (existingIdempotency is null)
        {
            throw new InvalidOperationException("Idempotency record conflict could not be reconciled.");
        }

        return await ResolveIdempotencyRecordAsync(command, existingIdempotency, transaction, cancellationToken);
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

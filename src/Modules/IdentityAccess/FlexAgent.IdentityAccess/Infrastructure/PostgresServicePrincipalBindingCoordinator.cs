using System.Security.Cryptography;
using System.Text;
using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed record ServicePrincipalBindingRecord(
    Guid BindingId,
    string AuthenticationProfile,
    string AuthenticationMethod,
    string Issuer,
    string ExternalSubject,
    string? ClientIdentity,
    string ExpectedAudience,
    Guid ServiceActorId,
    string ServicePurpose,
    DateTimeOffset EffectiveAt,
    DateTimeOffset? RevokedAt,
    long BindingVersion);

public sealed record ServicePrincipalBindingProvision(
    Guid BindingId,
    string AuthenticationProfile,
    string AuthenticationMethod,
    string Issuer,
    string ExternalSubject,
    string? ClientIdentity,
    string ExpectedAudience,
    Guid ServiceActorId,
    string ServicePurpose,
    DateTimeOffset EffectiveAt);

public static class PostgresServicePrincipalBindingCoordinator
{
    public static async Task ProvisionInTransactionAsync(
        Guid auditOrganizationId,
        ServicePrincipalBindingProvision provision,
        ServiceDelegationMutationContext mutation,
        ICommitAuthorizationKernel authorizationKernel,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        IAuditEventWriter? auditEventWriter = null)
    {
        ArgumentNullException.ThrowIfNull(provision);
        ArgumentNullException.ThrowIfNull(mutation);
        var decision = await authorizationKernel.AuthorizeInTransactionAsync(
            CreateRequest(auditOrganizationId, provision.BindingId, mutation, AuthorizationActions.ProvisionServicePrincipalBinding),
            transaction,
            cancellationToken);
        if (!decision.IsPermitted)
        {
            throw new AuthorizationDeniedException(decision.ReasonCode);
        }

        var existing = await LoadCurrentAsync(
            provision.AuthenticationProfile,
            provision.Issuer,
            provision.ExternalSubject,
            provision.ExpectedAudience,
            transaction,
            cancellationToken);
        if (existing is not null)
        {
            if (existing.ServiceActorId != provision.ServiceActorId)
            {
                throw new InvalidOperationException(
                    "An active principal binding already exists for this issuer and subject; replace it instead.");
            }

            return;
        }

        var connection = transaction.Connection
            ?? throw new InvalidOperationException("Principal binding writes require an open transaction.");
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO service_principal_bindings (
                    binding_id, authentication_profile, authentication_method, issuer, external_subject,
                    client_identity, expected_audience, service_actor_id, service_purpose, effective_at,
                    revoked_at, binding_version, created_at)
                VALUES (
                    @BindingId, @AuthenticationProfile, @AuthenticationMethod, @Issuer, @ExternalSubject,
                    @ClientIdentity, @ExpectedAudience, @ServiceActorId, @ServicePurpose, @EffectiveAt,
                    NULL, 1, clock_timestamp());
                """,
                new
                {
                    provision.BindingId,
                    provision.AuthenticationProfile,
                    provision.AuthenticationMethod,
                    provision.Issuer,
                    provision.ExternalSubject,
                    provision.ClientIdentity,
                    provision.ExpectedAudience,
                    provision.ServiceActorId,
                    provision.ServicePurpose,
                    EffectiveAt = provision.EffectiveAt.UtcDateTime,
                },
                transaction,
                cancellationToken: cancellationToken));
        await WriteHistoryAndAuditAsync(
            auditEventWriter ?? new PostgresAuditEventWriter(),
            auditOrganizationId,
            provision.BindingId,
            mutation,
            decision,
            AuthorizationActions.ProvisionServicePrincipalBinding,
            "provision",
            previousActorId: null,
            provision.ServiceActorId,
            previousRevokedAt: null,
            newRevokedAt: null,
            bindingVersion: 1,
            transaction,
            cancellationToken);
        var reauth = await authorizationKernel.ReauthorizeInTransactionAsync(
            CreateRequest(auditOrganizationId, provision.BindingId, mutation, AuthorizationActions.ProvisionServicePrincipalBinding),
            transaction,
            cancellationToken);
        if (!reauth.IsPermitted)
        {
            await PostgresServiceDelegationCoordinator.AbortCallerTransactionAsync(transaction);
            throw new AuthorizationDeniedException(reauth.ReasonCode);
        }
    }

    public static async Task RevokeInTransactionAsync(
        Guid auditOrganizationId,
        Guid bindingId,
        ServiceDelegationMutationContext mutation,
        ICommitAuthorizationKernel authorizationKernel,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        IAuditEventWriter? auditEventWriter = null)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        var decision = await authorizationKernel.AuthorizeInTransactionAsync(
            CreateRequest(auditOrganizationId, bindingId, mutation, AuthorizationActions.RevokeServicePrincipalBinding),
            transaction,
            cancellationToken);
        if (!decision.IsPermitted)
        {
            throw new AuthorizationDeniedException(decision.ReasonCode);
        }

        var connection = transaction.Connection
            ?? throw new InvalidOperationException("Principal binding writes require an open transaction.");
        var current = await connection.QuerySingleOrDefaultAsync<BindingRow>(
            new CommandDefinition(
                """
                SELECT binding_id, authentication_profile, authentication_method, issuer, external_subject,
                       client_identity, expected_audience, service_actor_id, service_purpose, effective_at,
                       revoked_at, binding_version
                FROM service_principal_bindings
                WHERE binding_id = @BindingId
                FOR UPDATE;
                """,
                new { BindingId = bindingId },
                transaction,
                cancellationToken: cancellationToken));
        if (current is null)
        {
            throw new InvalidOperationException("Service principal binding was not found.");
        }

        if (current.revoked_at is not null)
        {
            return;
        }

        var nextVersion = current.binding_version + 1;
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE service_principal_bindings
                SET revoked_at = clock_timestamp(),
                    binding_version = @BindingVersion
                WHERE binding_id = @BindingId
                  AND revoked_at IS NULL;
                """,
                new { BindingId = bindingId, BindingVersion = nextVersion },
                transaction,
                cancellationToken: cancellationToken));
        await WriteHistoryAndAuditAsync(
            auditEventWriter ?? new PostgresAuditEventWriter(),
            auditOrganizationId,
            bindingId,
            mutation,
            decision,
            AuthorizationActions.RevokeServicePrincipalBinding,
            "revoke",
            current.service_actor_id,
            current.service_actor_id,
            current.revoked_at,
            DateTime.UtcNow,
            nextVersion,
            transaction,
            cancellationToken);
        var reauth = await authorizationKernel.ReauthorizeInTransactionAsync(
            CreateRequest(auditOrganizationId, bindingId, mutation, AuthorizationActions.RevokeServicePrincipalBinding),
            transaction,
            cancellationToken);
        if (!reauth.IsPermitted)
        {
            await PostgresServiceDelegationCoordinator.AbortCallerTransactionAsync(transaction);
            throw new AuthorizationDeniedException(reauth.ReasonCode);
        }
    }

    public static async Task ReplaceInTransactionAsync(
        Guid auditOrganizationId,
        Guid bindingId,
        Guid replacementServiceActorId,
        ServiceDelegationMutationContext mutation,
        ICommitAuthorizationKernel authorizationKernel,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        IAuditEventWriter? auditEventWriter = null)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        if (replacementServiceActorId == Guid.Empty)
        {
            throw new ArgumentException("Replacement service actor is required.", nameof(replacementServiceActorId));
        }

        var decision = await authorizationKernel.AuthorizeInTransactionAsync(
            CreateRequest(auditOrganizationId, bindingId, mutation, AuthorizationActions.ReplaceServicePrincipalBinding),
            transaction,
            cancellationToken);
        if (!decision.IsPermitted)
        {
            throw new AuthorizationDeniedException(decision.ReasonCode);
        }

        var connection = transaction.Connection
            ?? throw new InvalidOperationException("Principal binding writes require an open transaction.");
        var current = await connection.QuerySingleOrDefaultAsync<BindingRow>(
            new CommandDefinition(
                """
                SELECT binding_id, authentication_profile, authentication_method, issuer, external_subject,
                       client_identity, expected_audience, service_actor_id, service_purpose, effective_at,
                       revoked_at, binding_version
                FROM service_principal_bindings
                WHERE binding_id = @BindingId
                FOR UPDATE;
                """,
                new { BindingId = bindingId },
                transaction,
                cancellationToken: cancellationToken));
        if (current is null || current.revoked_at is not null)
        {
            throw new InvalidOperationException("An active service principal binding is required to replace.");
        }

        if (current.service_actor_id == replacementServiceActorId)
        {
            return;
        }

        var nextVersion = current.binding_version + 1;
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE service_principal_bindings
                SET service_actor_id = @ServiceActorId,
                    binding_version = @BindingVersion
                WHERE binding_id = @BindingId
                  AND revoked_at IS NULL;
                """,
                new
                {
                    BindingId = bindingId,
                    ServiceActorId = replacementServiceActorId,
                    BindingVersion = nextVersion,
                },
                transaction,
                cancellationToken: cancellationToken));
        await WriteHistoryAndAuditAsync(
            auditEventWriter ?? new PostgresAuditEventWriter(),
            auditOrganizationId,
            bindingId,
            mutation,
            decision,
            AuthorizationActions.ReplaceServicePrincipalBinding,
            "replace",
            current.service_actor_id,
            replacementServiceActorId,
            current.revoked_at,
            current.revoked_at,
            nextVersion,
            transaction,
            cancellationToken);
        var reauth = await authorizationKernel.ReauthorizeInTransactionAsync(
            CreateRequest(auditOrganizationId, bindingId, mutation, AuthorizationActions.ReplaceServicePrincipalBinding),
            transaction,
            cancellationToken);
        if (!reauth.IsPermitted)
        {
            await PostgresServiceDelegationCoordinator.AbortCallerTransactionAsync(transaction);
            throw new AuthorizationDeniedException(reauth.ReasonCode);
        }
    }

    public static async Task<ServicePrincipalBindingRecord?> LoadCurrentAsync(
        string authenticationProfile,
        string issuer,
        string externalSubject,
        string expectedAudience,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var connection = transaction.Connection
            ?? throw new InvalidOperationException("Principal binding reads require an open transaction.");
        var row = await connection.QuerySingleOrDefaultAsync<BindingRow>(
            new CommandDefinition(
                """
                SELECT binding_id, authentication_profile, authentication_method, issuer, external_subject,
                       client_identity, expected_audience, service_actor_id, service_purpose, effective_at,
                       revoked_at, binding_version
                FROM service_principal_bindings
                WHERE authentication_profile = @AuthenticationProfile
                  AND issuer = @Issuer
                  AND external_subject = @ExternalSubject
                  AND expected_audience = @ExpectedAudience
                  AND revoked_at IS NULL;
                """,
                new
                {
                    AuthenticationProfile = authenticationProfile,
                    Issuer = issuer,
                    ExternalSubject = externalSubject,
                    ExpectedAudience = expectedAudience,
                },
                transaction,
                cancellationToken: cancellationToken));
        return row is null
            ? null
            : new ServicePrincipalBindingRecord(
                row.binding_id,
                row.authentication_profile,
                row.authentication_method,
                row.issuer,
                row.external_subject,
                row.client_identity,
                row.expected_audience,
                row.service_actor_id,
                row.service_purpose,
                DateTime.SpecifyKind(row.effective_at, DateTimeKind.Utc),
                row.revoked_at is { } revoked ? DateTime.SpecifyKind(revoked, DateTimeKind.Utc) : null,
                row.binding_version);
    }

    public static async Task<bool> MatchesCurrentInTransactionAsync(
        Guid bindingId,
        long bindingVersion,
        Guid serviceActorId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var connection = transaction.Connection
            ?? throw new InvalidOperationException("Principal binding reads require an open transaction.");
        var matched = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(
                """
                SELECT 1
                FROM service_principal_bindings
                WHERE binding_id = @BindingId
                  AND binding_version = @BindingVersion
                  AND service_actor_id = @ServiceActorId
                  AND revoked_at IS NULL
                  AND effective_at <= clock_timestamp()
                FOR SHARE;
                """,
                new
                {
                    BindingId = bindingId,
                    BindingVersion = bindingVersion,
                    ServiceActorId = serviceActorId,
                },
                transaction,
                cancellationToken: cancellationToken));
        return matched == 1;
    }

    private static AuthorizationRequest CreateRequest(
        Guid organizationId,
        Guid bindingId,
        ServiceDelegationMutationContext mutation,
        string action) =>
        new(
            mutation.Initiator,
            new OrganizationScope(organizationId),
            action,
            new ResourceScope(
                new OrganizationScope(organizationId),
                AuthorizationResourceTypes.ServicePrincipalBinding,
                bindingId),
            mutation.SourceChannel,
            mutation.CorrelationId);

    private static async Task WriteHistoryAndAuditAsync(
        IAuditEventWriter auditEventWriter,
        Guid organizationId,
        Guid bindingId,
        ServiceDelegationMutationContext mutation,
        AuthorizationDecision decision,
        string auditAction,
        string mutationKind,
        Guid? previousActorId,
        Guid newActorId,
        DateTime? previousRevokedAt,
        DateTime? newRevokedAt,
        long bindingVersion,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = transaction.Connection
            ?? throw new InvalidOperationException("Principal binding writes require an open transaction.");
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO service_principal_binding_transitions (
                    transition_id, binding_id, mutation_kind, previous_actor_id, new_actor_id,
                    previous_revoked_at, new_revoked_at, binding_version, actor_id, actor_type,
                    reason, correlation_id, occurred_at)
                VALUES (
                    @TransitionId, @BindingId, @MutationKind, @PreviousActorId, @NewActorId,
                    @PreviousRevokedAt, @NewRevokedAt, @BindingVersion, @ActorId, @ActorType,
                    @Reason, @CorrelationId, clock_timestamp());
                """,
                new
                {
                    TransitionId = Guid.NewGuid(),
                    BindingId = bindingId,
                    MutationKind = mutationKind,
                    PreviousActorId = previousActorId,
                    NewActorId = newActorId,
                    PreviousRevokedAt = previousRevokedAt,
                    NewRevokedAt = newRevokedAt,
                    BindingVersion = bindingVersion,
                    ActorId = mutation.Initiator.ActorId,
                    ActorType = mutation.Initiator.ActorType,
                    mutation.Reason,
                    mutation.CorrelationId,
                },
                transaction,
                cancellationToken: cancellationToken));
        var digestSource = $"{mutationKind}|{bindingId:N}|{newActorId:N}|{bindingVersion}|{mutation.Reason}";
        await auditEventWriter.InsertAsync(
            new AuditEventWriteModel(
                EventId: Guid.NewGuid(),
                OrganizationId: organizationId,
                EventSchemaVersion: "audit-event.v1",
                OccurredAt: DateTimeOffset.UtcNow,
                CorrelationId: mutation.CorrelationId,
                ActorType: mutation.Initiator.ActorType,
                ActorId: mutation.Initiator.ActorId,
                Action: auditAction,
                ResourceType: AuthorizationResourceTypes.ServicePrincipalBinding,
                ResourceId: bindingId,
                Outcome: "succeeded",
                ReasonCode: null,
                RelationshipVersion: decision.RelationshipVersion,
                SourceChannel: mutation.SourceChannel,
                PayloadDigest: Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(digestSource)))
                    .ToLowerInvariant(),
                AuthorizationReferenceType: decision.AuthorizationReferenceType,
                AuthorizationReferenceId: decision.AuthorizationReferenceId),
            transaction,
            cancellationToken);
    }

    private sealed record BindingRow(
        Guid binding_id,
        string authentication_profile,
        string authentication_method,
        string issuer,
        string external_subject,
        string? client_identity,
        string expected_audience,
        Guid service_actor_id,
        string service_purpose,
        DateTime effective_at,
        DateTime? revoked_at,
        long binding_version);
}

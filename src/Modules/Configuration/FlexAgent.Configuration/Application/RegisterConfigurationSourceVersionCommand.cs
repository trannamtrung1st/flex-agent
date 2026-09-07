using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.Configuration.Application;

public sealed record RegisterConfigurationSourceVersionCommand(
    TrustedActor Actor,
    OrganizationScope Organization,
    Guid ConfigurationSourceId,
    string ProcedureId,
    string SchemaVersion,
    ReadOnlyMemory<byte> CanonicalUtf8Content,
    string DeclaredContentDigest,
    string IdempotencyKey,
    Guid CorrelationId,
    string SourceChannel);

public interface IRegisterConfigurationSourceVersionHandler
{
    Task<RegisterConfigurationSourceVersionResult> HandleAsync(
        RegisterConfigurationSourceVersionCommand command,
        CancellationToken cancellationToken = default);
}

public interface IProtectedConfigurationSourcePayloadReader
{
    Task<ConfigurationSourcePayload?> GetPayloadForVersionAsync(
        Guid organizationId,
        Guid configurationSourceId,
        Guid sourceVersionId,
        CancellationToken cancellationToken = default);
}

public sealed record ConfigurationSourcePayload(
    Guid OrganizationId,
    Guid ConfigurationSourceId,
    Guid SourceVersionId,
    string ContentDigest,
    byte[] CanonicalUtf8);

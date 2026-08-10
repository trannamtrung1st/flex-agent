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

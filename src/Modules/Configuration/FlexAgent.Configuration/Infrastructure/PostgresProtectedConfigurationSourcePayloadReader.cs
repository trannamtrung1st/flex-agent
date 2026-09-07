using FlexAgent.Configuration.Application;

namespace FlexAgent.Configuration.Infrastructure;

public sealed class PostgresProtectedConfigurationSourcePayloadReader(
    PostgresConfigurationSourceVersionRepository versions) : IProtectedConfigurationSourcePayloadReader
{
    public async Task<ConfigurationSourcePayload?> GetPayloadForVersionAsync(
        Guid organizationId,
        Guid configurationSourceId,
        Guid sourceVersionId,
        CancellationToken cancellationToken = default)
    {
        var row = await versions.GetPayloadForVersionAsync(
            organizationId,
            configurationSourceId,
            sourceVersionId,
            cancellationToken);
        return row is null
            ? null
            : new ConfigurationSourcePayload(
                row.OrganizationId,
                row.ConfigurationSourceId,
                row.SourceVersionId,
                row.ContentDigest,
                row.CanonicalUtf8);
    }
}

using System.Text.RegularExpressions;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Configuration.Infrastructure;

public sealed partial class ConfigurationDigestVerifier
{
    private static readonly CanonicalJsonLimits DefaultLimits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 64,
        maxObjectProperties: 4_096,
        maxArrayElements: 4_096);

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex LowercaseSha256Pattern();

    public bool TryVerify(
        string procedureId,
        string schemaVersion,
        ReadOnlyMemory<byte> canonicalUtf8Content,
        string declaredDigest,
        out string? failureCode)
    {
        failureCode = null;

        if (!string.Equals(procedureId, Domain.ConfigurationProcedureIds.RscJcsSha256V1, StringComparison.Ordinal))
        {
            failureCode = Domain.RegisterConfigurationSourceVersionFailureCodes.InvalidProcedure;
            return false;
        }

        if (!string.Equals(schemaVersion, Domain.ConfigurationSchemaVersions.V1, StringComparison.Ordinal))
        {
            failureCode = Domain.RegisterConfigurationSourceVersionFailureCodes.InvalidProcedure;
            return false;
        }

        if (!LowercaseSha256Pattern().IsMatch(declaredDigest))
        {
            failureCode = Domain.RegisterConfigurationSourceVersionFailureCodes.InvalidDigest;
            return false;
        }

        try
        {
            var computed = CanonicalJsonProcessor.CanonicalizeSha256Hex(canonicalUtf8Content.Span, DefaultLimits);
            if (!string.Equals(computed, declaredDigest, StringComparison.Ordinal))
            {
                failureCode = Domain.RegisterConfigurationSourceVersionFailureCodes.InvalidDigest;
                return false;
            }
        }
        catch (CanonicalJsonException)
        {
            failureCode = Domain.RegisterConfigurationSourceVersionFailureCodes.InvalidDigest;
            return false;
        }

        return true;
    }
}

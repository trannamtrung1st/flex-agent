using System.Security.Cryptography;
using System.Text;
using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;

namespace FlexAgent.Configuration.Infrastructure;

internal static class ConfigurationPayloadFingerprint
{
    public static string Compute(RegisterConfigurationSourceVersionCommand command) =>
        Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        $"{command.ProcedureId}|{command.SchemaVersion}|{command.DeclaredContentDigest}")))
            .ToLowerInvariant();
}

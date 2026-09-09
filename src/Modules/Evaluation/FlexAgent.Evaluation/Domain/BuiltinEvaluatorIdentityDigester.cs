using System.Text;
using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Evaluation.Domain;

public static class BuiltinEvaluatorIdentityDigester
{
    public const string ImplementationKind = "flex-agent.evaluation.builtin.in-process.v1";

    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 64,
        maxObjectProperties: 4_096,
        maxArrayElements: 4_096);

    public static (string EvaluatorDigest, string ConfigurationDigest, string DependencyDigest) ComputeDigests(
        EvaluatorRegistryEntry entry,
        BuiltinEvaluatorImplementationIdentity implementation)
    {
        var evaluatorDigest = Digest(BuildEvaluatorIdentityPayload(entry, implementation));
        var configurationDigest = Digest(BuildConfigurationPayload(entry, implementation));
        var dependencyDigest = Digest(BuildDependencyPayload(entry, implementation));
        return (evaluatorDigest, configurationDigest, dependencyDigest);
    }

    public static EvaluatorRegistryEntry WithComputedDigests(
        EvaluatorRegistryEntry entry,
        BuiltinEvaluatorImplementationIdentity implementation)
    {
        var digests = ComputeDigests(entry, implementation);
        return entry with
        {
            EvaluatorDigest = digests.EvaluatorDigest,
            ConfigurationDigest = digests.ConfigurationDigest,
            DependencyDigest = digests.DependencyDigest,
        };
    }

    private static string Digest(string canonicalJson) =>
        CanonicalJsonProcessor.CanonicalizeSha256Hex(Encoding.UTF8.GetBytes(canonicalJson), Limits);

    private static string BuildEvaluatorIdentityPayload(
        EvaluatorRegistryEntry entry,
        BuiltinEvaluatorImplementationIdentity implementation)
    {
        if (!implementation.OperationArtifactDigests.TryGetValue(entry.Operation, out var operationArtifactDigest))
        {
            throw new InvalidOperationException($"Missing implementation artifact for operation '{entry.Operation}'.");
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("canonicalization_procedure", entry.CanonicalizationProcedure);
            writer.WriteString("cpu_time_limit", entry.CpuTimeLimit);
            writer.WriteString("elapsed_time_limit", entry.ElapsedTimeLimit);
            writer.WriteString("enforcement_profile", InProcessDeterministicExecutionContract.EnforcementProfile);
            writer.WriteString("evaluator_id", entry.EvaluatorId);
            writer.WriteString("evaluator_version", entry.EvaluatorVersion);
            writer.WriteString("executable_selection", entry.ExecutableSelection);
            writer.WriteString("implementation_closure_digest", implementation.ImplementationClosureDigest);
            writer.WriteString("implementation_kind", ImplementationKind);
            writer.WriteString("implementation_manifest_version", implementation.ManifestVersion);
            writer.WriteString("input_schema_id", entry.InputSchemaId);
            writer.WriteNumber("memory_limit_bytes", entry.MemoryLimitBytes);
            writer.WriteString("network_egress", entry.NetworkEgress);
            writer.WriteString("operation", entry.Operation);
            writer.WriteString("operation_artifact_digest", operationArtifactDigest);
            writer.WriteNumber("output_limit_bytes", entry.OutputLimitBytes);
            writer.WriteString("output_schema_id", entry.OutputSchemaId);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string BuildConfigurationPayload(
        EvaluatorRegistryEntry entry,
        BuiltinEvaluatorImplementationIdentity implementation)
    {
        if (!implementation.OperationArtifactDigests.TryGetValue(entry.Operation, out var operationArtifactDigest))
        {
            throw new InvalidOperationException($"Missing implementation artifact for operation '{entry.Operation}'.");
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("evaluator_id", entry.EvaluatorId);
            writer.WriteString("evaluator_version", entry.EvaluatorVersion);
            writer.WriteString("implementation_closure_digest", implementation.ImplementationClosureDigest);
            writer.WriteString("implementation_manifest_version", implementation.ManifestVersion);
            writer.WriteString("operation", entry.Operation);
            writer.WriteString("operation_artifact_digest", operationArtifactDigest);
            writer.WriteStartObject("operation_configuration");
            writer.WriteString(
                "enforcement_profile",
                InProcessDeterministicExecutionContract.EnforcementProfile);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string BuildDependencyPayload(
        EvaluatorRegistryEntry entry,
        BuiltinEvaluatorImplementationIdentity implementation)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("bundle_digest", BuiltinEvaluatorImplementationManifest.ComputeBundleDigest(implementation));
            writer.WriteString("dependency_closure", ImplementationKind);
            writer.WriteString("evaluator_id", entry.EvaluatorId);
            writer.WriteString("evaluator_version", entry.EvaluatorVersion);
            writer.WriteString("implementation_closure_digest", implementation.ImplementationClosureDigest);
            writer.WriteString("implementation_manifest_version", implementation.ManifestVersion);
            writer.WritePropertyName("modules");
            writer.WriteStartArray();
            writer.WriteStringValue("FlexAgent.Evaluation.Infrastructure");
            writer.WriteEndArray();
            writer.WritePropertyName("operation_artifact_digests");
            writer.WriteStartObject();
            foreach (var pair in implementation.OperationArtifactDigests.OrderBy(static p => p.Key, StringComparer.Ordinal))
            {
                writer.WriteString(pair.Key, pair.Value);
            }

            writer.WriteEndObject();
            writer.WritePropertyName("source_artifact_digests");
            writer.WriteStartObject();
            foreach (var pair in implementation.SourceArtifactDigests.OrderBy(static p => p.Key, StringComparer.Ordinal))
            {
                writer.WriteString(pair.Key, pair.Value);
            }

            writer.WriteEndObject();
            writer.WriteString("runtime", "net10.0");
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

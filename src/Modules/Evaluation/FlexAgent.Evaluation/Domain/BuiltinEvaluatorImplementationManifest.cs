using System.Text;
using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Evaluation.Domain;

public sealed record BuiltinEvaluatorImplementationIdentity(
    string ManifestVersion,
    string RunnerSourceArtifactDigest,
    IReadOnlyDictionary<string, string> OperationArtifactDigests);

public static class BuiltinEvaluatorImplementationManifest
{
    public const string ManifestVersion = "eval.builtin.impl-manifest.v1";

    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 64,
        maxObjectProperties: 4_096,
        maxArrayElements: 4_096);

    private static readonly IReadOnlyDictionary<string, string> OperationArtifacts =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [EvaluatorOperations.BoundedCalculation] = DigestOperation(
                EvaluatorOperations.BoundedCalculation,
                "word_count",
                DeterministicExecutionBounds.MaxJsonStringUtf8Bytes,
                DeterministicExecutionBounds.MaxJsonPropertyCount),
            [EvaluatorOperations.ExactCompare] = DigestOperation(
                EvaluatorOperations.ExactCompare,
                "digest_equality",
                128,
                4),
            [EvaluatorOperations.SchemaValidate] = DigestOperation(
                EvaluatorOperations.SchemaValidate,
                "object_payload_presence",
                DeterministicExecutionBounds.MaxJsonPropertyCount,
                DeterministicExecutionBounds.MaxJsonDepth),
            [EvaluatorOperations.CitationValidate] = DigestOperation(
                EvaluatorOperations.CitationValidate,
                "stable_id_citations",
                DeterministicExecutionBounds.MaxJsonArrayLength,
                128),
            [EvaluatorOperations.RubricAggregate] = DigestOperation(
                EvaluatorOperations.RubricAggregate,
                "bounded_score_total",
                32,
                100),
        };

    public static BuiltinEvaluatorImplementationIdentity CreateIdentity(string runnerSourceArtifactDigest) =>
        new(
            ManifestVersion,
            runnerSourceArtifactDigest,
            OperationArtifacts);

    public static string ComputeBundleDigest(BuiltinEvaluatorImplementationIdentity identity)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("manifest_version", identity.ManifestVersion);
            writer.WriteString("runner_source_artifact_digest", identity.RunnerSourceArtifactDigest);
            writer.WritePropertyName("operation_artifact_digests");
            writer.WriteStartObject();
            foreach (var pair in identity.OperationArtifactDigests.OrderBy(static p => p.Key, StringComparer.Ordinal))
            {
                writer.WriteString(pair.Key, pair.Value);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return CanonicalJsonProcessor.CanonicalizeSha256Hex(stream.ToArray(), Limits);
    }

    private static string DigestOperation(
        string operation,
        string algorithm,
        int maxCollectionSize,
        int maxScalarMagnitude)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("algorithm", algorithm);
            writer.WriteString("enforcement_profile", InProcessDeterministicExecutionContract.EnforcementProfile);
            writer.WriteNumber("max_collection_size", maxCollectionSize);
            writer.WriteNumber("max_scalar_magnitude", maxScalarMagnitude);
            writer.WriteString("operation", operation);
            writer.WriteEndObject();
        }

        return CanonicalJsonProcessor.CanonicalizeSha256Hex(stream.ToArray(), Limits);
    }
}

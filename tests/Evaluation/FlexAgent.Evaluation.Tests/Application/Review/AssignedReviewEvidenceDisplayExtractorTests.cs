using System.Text;
using System.Text.Json;
using FlexAgent.Evaluation.Application.Review;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Application.Review;

public sealed class AssignedReviewEvidenceDisplayExtractorTests
{
    [Fact]
    public void Json_pointer_discloses_only_the_resolved_leaf_node()
    {
        const string projectionJson =
            """
            {
              "score":{"value":42,"label":"pass"},
              "other":{"secret":"hidden"}
            }
            """;
        var context = BuildFactContext("configuration.fact", "cfg.1", "rev.1", projectionJson);

        var result = ExtractPointer(context, "/score/value");

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("42", result.Value);
        Assert.DoesNotContain("hidden", result.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("pass", result.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_pointer_supports_array_indexes()
    {
        const string projectionJson =
            """
            {
              "items":[{"value":"one"},{"value":"two"}],
              "secret":"hidden"
            }
            """;
        var context = BuildFactContext("configuration.fact", "cfg.1", "rev.1", projectionJson);

        var result = ExtractPointer(context, "/items/1/value");

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("\"two\"", result.Value);
        Assert.DoesNotContain("hidden", result.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Json_pointer_supports_escaped_object_property_names()
    {
        const string projectionJson =
            """
            {
              "facts":{
                "a/b":{"value":"permitted"}
              },
              "other":"hidden"
            }
            """;
        var context = BuildFactContext("configuration.fact", "cfg.1", "rev.1", projectionJson);

        var result = ExtractPointer(context, "/facts/a~1b/value");

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("\"permitted\"", result.Value);
        Assert.DoesNotContain("hidden", result.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_json_pointer_is_unavailable()
    {
        const string projectionJson = """{"score":{"value":42}}""";
        var context = BuildFactContext("configuration.fact", "cfg.1", "rev.1", projectionJson);

        var result = ExtractPointer(context, "/score/missing");

        Assert.False(result.Succeeded);
        Assert.Equal(ReviewFailureCodes.Unavailable, result.OutcomeCode);
    }

    private static EvaluationDecision<string> ExtractPointer(
        EvidenceLocatorVerificationContext context,
        string jsonPointer)
    {
        using var locatorDocument = JsonDocument.Parse(
            $$"""
            {
              "locator_schema":"evidence-locator.v1",
              "source_type":"configuration.fact",
              "source_ref":{"source_id":"cfg.1","source_version":"rev.1"},
              "ownership_ref":{
                "organization_id":"org.1","activity_id":"act.1","participant_id":"part.1",
                "attempt_id":"att.1","session_id":"sess.1","evaluation_id":"eval.1"
              },
              "location":{"location_type":"json_pointer","json_pointer":"{{jsonPointer}}"},
              "precision":"exact_range",
              "integrity":{
                "source_digest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv.1"}
            }
            """);

        return AssignedReviewEvidenceDisplayExtractor.TryExtractDisplayText(
            locatorDocument.RootElement,
            context);
    }

    private static EvidenceLocatorVerificationContext BuildFactContext(
        string sourceType,
        string sourceId,
        string sourceVersion,
        string projectionJson)
    {
        var digest = new string('c', 64);
        var projectionUtf8 = Encoding.UTF8.GetBytes(projectionJson);
        var fact = new EvaluationSafeFactProjection(sourceId, sourceVersion, digest, projectionUtf8);
        var facts = new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal)
        {
            [sourceId] = fact,
        };

        return new EvidenceLocatorVerificationContext(
            new EvaluationStableOwnershipReference(
                "org.1",
                "act.1",
                "part.1",
                "att.1",
                "sess.1",
                "eval.1"),
            42,
            new Dictionary<string, EvaluationSessionTranscriptMaterial>(StringComparer.Ordinal),
            new Dictionary<string, EvaluationSessionTranscriptMaterial>(StringComparer.Ordinal),
            new Dictionary<string, EvaluationSubmissionMaterial>(StringComparer.Ordinal),
            facts,
            new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal),
            new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal),
            false);
    }
}

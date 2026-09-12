using System.Text;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class ProtectedModelResponseWireBytesDigestTests
{
    private static readonly string PlaceholderDigest = new('0', 64);
    [Fact]
    public void Digest_procedure_id_is_versioned()
    {
        Assert.Equal(
            "evaluation-model-response-wire-content-digest-sha256-v1",
            ProtectedModelResponseWireBytesDigest.DigestProcedureId);
    }

    [Fact]
    public void Wire_json_contains_response_ref_content_digest_marker()
    {
        var response = new EvaluationModelResponseV1(
            "v1",
            "eval.agent.assisted.output.v1",
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            EvaluatorModes.AgentAssisted,
            CriterionStatuses.Satisfied,
            "high",
            ["ambiguous_language"],
            "Structure is complete.",
            ["evid.synthetic.0001"],
            new ProtectedPayloadRefV1("prot.eval.res.example", new string('0', 64)),
            "pass",
            null,
            "dinv.synthetic.0002");
        var wireText = System.Text.Encoding.UTF8.GetString(
            EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(response));

        Assert.Contains("\"content_digest\":\"", wireText, StringComparison.Ordinal);
    }

    [Fact]
    public void Bound_wire_bytes_match_adapter_response_ref_digest()
    {
        var response = new EvaluationModelResponseV1(
            "v1",
            "eval.agent.assisted.output.v1",
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            EvaluatorModes.AgentAssisted,
            CriterionStatuses.Satisfied,
            "high",
            ["ambiguous_language"],
            "Structure is complete.",
            ["evid.synthetic.0001"],
            new ProtectedPayloadRefV1("prot.eval.res.example", new string('0', 64)),
            "pass",
            null,
            "dinv.synthetic.0002");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);

        var verified = ProtectedModelResponseWireBytesDigest.TryVerify(
            bound.WireUtf8,
            bound.ResponseRef.ContentDigest);

        Assert.True(verified.Succeeded, verified.OutcomeCode);
        Assert.Equal(bound.ResponseRef.ContentDigest, verified.Value);
    }

    [Fact]
    public void Mutated_wire_bytes_fail_digest_verification()
    {
        var response = new EvaluationModelResponseV1(
            "v1",
            "eval.agent.assisted.output.v1",
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            EvaluatorModes.AgentAssisted,
            CriterionStatuses.Satisfied,
            "high",
            ["ambiguous_language"],
            "Structure is complete.",
            ["evid.synthetic.0001"],
            new ProtectedPayloadRefV1("prot.eval.res.example", new string('0', 64)),
            "pass",
            null,
            "dinv.synthetic.0002");
        var bound = EvaluationModelResponseDocumentBinder.Bind(response);
        var mutated = response with
        {
            Rationale = "Forged rationale after binding.",
            ResponseRef = bound.ResponseRef,
        };
        var mutatedWireUtf8 = EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(mutated);

        var verified = ProtectedModelResponseWireBytesDigest.TryVerify(
            mutatedWireUtf8,
            bound.ResponseRef.ContentDigest);

        Assert.False(verified.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, verified.OutcomeCode);
        Assert.Equal("response_ref", verified.Field);
    }

    [Fact]
    public void TryVerify_accepts_whitespace_formatted_provider_wire_bytes()
    {
        var placeholderWireUtf8 = CreatePlaceholderWireUtf8();
        var formatted = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(placeholderWireUtf8)
                .Replace("\"content_digest\":\"", "\"content_digest\": \"", StringComparison.Ordinal)
                .Replace(",\"score\"", ",\n  \"score\"", StringComparison.Ordinal));

        Assert.True(ProtectedModelResponseWireBytesDigest.TryCompute(formatted, out var digest));
        Assert.True(
            EvaluationModelResponseContentDigestWireLocator.TryReplaceDigestValue(
                formatted,
                PlaceholderDigest,
                digest,
                out var boundFormatted));

        var verified = ProtectedModelResponseWireBytesDigest.TryVerify(boundFormatted, digest);

        Assert.True(verified.Succeeded, verified.OutcomeCode);
        Assert.Equal(digest, verified.Value);
    }

    [Fact]
    public void TryVerify_accepts_reordered_top_level_properties()
    {
        var placeholderWireUtf8 = CreatePlaceholderWireUtf8();
        var canonicalText = Encoding.UTF8.GetString(placeholderWireUtf8);
        var responseRefJson = ExtractJsonObject(canonicalText, "\"response_ref\"");
        var withoutResponseRef = RemoveJsonProperty(canonicalText, "\"response_ref\"");
        var reorderedText = "{" + responseRefJson + "," + withoutResponseRef[1..];
        var reordered = Encoding.UTF8.GetBytes(reorderedText);

        Assert.True(ProtectedModelResponseWireBytesDigest.TryCompute(reordered, out var digest));
        Assert.True(
            EvaluationModelResponseContentDigestWireLocator.TryReplaceDigestValue(
                reordered,
                PlaceholderDigest,
                digest,
                out var boundReordered));

        var verified = ProtectedModelResponseWireBytesDigest.TryVerify(boundReordered, digest);

        Assert.True(verified.Succeeded, verified.OutcomeCode);
        Assert.Equal(digest, verified.Value);
    }

    [Fact]
    public void TryVerify_fails_closed_when_content_digest_is_missing()
    {
        var bound = BindSampleResponse();
        var missingDigest = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(bound.WireUtf8)
                .Replace(
                    $"\"content_digest\":\"{bound.ResponseRef.ContentDigest}\"",
                    "\"content_digest\":\"\"",
                    StringComparison.Ordinal));

        var verified = ProtectedModelResponseWireBytesDigest.TryVerify(
            missingDigest,
            bound.ResponseRef.ContentDigest);

        Assert.False(verified.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, verified.OutcomeCode);
        Assert.Equal("response_ref", verified.Field);
    }

    [Fact]
    public void TryVerify_fails_closed_when_response_ref_is_missing()
    {
        var bound = BindSampleResponse();
        var withoutResponseRef = Encoding.UTF8.GetBytes(
            RemoveJsonProperty(Encoding.UTF8.GetString(bound.WireUtf8), "\"response_ref\""));

        var verified = ProtectedModelResponseWireBytesDigest.TryVerify(
            withoutResponseRef,
            bound.ResponseRef.ContentDigest);

        Assert.False(verified.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, verified.OutcomeCode);
        Assert.Equal("response_ref", verified.Field);
    }

    [Fact]
    public void TryVerify_does_not_throw_on_malformed_json()
    {
        var verified = ProtectedModelResponseWireBytesDigest.TryVerify(
            "{not-json"u8,
            new string('a', 64));

        Assert.False(verified.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidJudgment, verified.OutcomeCode);
        Assert.Equal("response_ref", verified.Field);
    }

    private static (byte[] WireUtf8, ProtectedPayloadRefV1 ResponseRef) BindSampleResponse() =>
        EvaluationModelResponseDocumentBinder.Bind(CreateSampleResponse());

    private static byte[] CreatePlaceholderWireUtf8() =>
        EvaluationModelResponseDocumentWriter.WriteCanonicalUtf8(
            CreateSampleResponse() with
            {
                ResponseRef = new ProtectedPayloadRefV1("prot.eval.res.example", PlaceholderDigest),
            });

    private static EvaluationModelResponseV1 CreateSampleResponse() =>
        new(
            "v1",
            "eval.agent.assisted.output.v1",
            "crit.assisted.structure",
            "crit.assisted.structure.v1",
            EvaluatorModes.AgentAssisted,
            CriterionStatuses.Satisfied,
            "high",
            ["ambiguous_language"],
            "Structure is complete.",
            ["evid.synthetic.0001"],
            new ProtectedPayloadRefV1("prot.eval.res.example", PlaceholderDigest),
            "pass",
            null,
            "dinv.synthetic.0002");

    private static string ExtractJsonObject(string json, string propertyName)
    {
        var propertyIndex = json.IndexOf(propertyName, StringComparison.Ordinal);
        Assert.True(propertyIndex >= 0);

        var objectStart = json.IndexOf('{', propertyIndex);
        var depth = 0;
        for (var index = objectStart; index < json.Length; index++)
        {
            switch (json[index])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return json[propertyIndex..(index + 1)];
                    }

                    break;
            }
        }

        throw new InvalidOperationException("JSON object was not found.");
    }

    private static string RemoveJsonProperty(string json, string propertyName)
    {
        var propertyIndex = json.IndexOf(propertyName, StringComparison.Ordinal);
        Assert.True(propertyIndex >= 0);

        var removeStart = propertyIndex;
        while (removeStart > 0 && json[removeStart - 1] != '{' && json[removeStart - 1] != ',')
        {
            removeStart--;
        }

        if (removeStart > 0 && json[removeStart] == ',')
        {
            removeStart++;
        }

        var objectStart = json.IndexOf('{', propertyIndex);
        var depth = 0;
        var removeEnd = objectStart;
        for (var index = objectStart; index < json.Length; index++)
        {
            switch (json[index])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        removeEnd = index + 1;
                        break;
                    }

                    break;
            }

            if (removeEnd > objectStart)
            {
                break;
            }
        }

        var trailingComma = removeEnd < json.Length && json[removeEnd] == ',' ? 1 : 0;
        return string.Concat(json.AsSpan(0, removeStart), json.AsSpan(removeEnd + trailingComma));
    }
}

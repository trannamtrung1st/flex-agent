using System.Text.Json;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceLocatorVerifierTests
{
    private static readonly string ContractsRoot = FindContractsRoot();

    [Theory]
    [InlineData("valid-transcript-whole-item.json")]
    [InlineData("valid-configuration-json-pointer.json")]
    public void Valid_locator_fixtures_verify_against_matching_material(string fixtureName)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(LocatorFixturePath(fixtureName)));
        var locator = document.RootElement;
        var context = BuildContextForFixture(locator);

        var result = EvidenceLocatorVerifier.TryVerify(locator, context);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("verified", result.Value!.VerificationState);
    }

    [Fact]
    public void Cross_scope_ownership_is_rejected()
    {
        using var document = JsonDocument.Parse(
            File.ReadAllBytes(LocatorFixturePath("valid-transcript-whole-item.json")));
        var locator = document.RootElement;
        var baseContext = BuildContextForFixture(locator);
        var context = baseContext with
        {
            TrustedOwnership = baseContext.TrustedOwnership with
            {
                EvaluationId = "eval.synthetic.other",
            },
        };

        var result = EvidenceLocatorVerifier.TryVerify(locator, context);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.IncompleteOwnership, result.OutcomeCode);
    }

    [Fact]
    public void Wrong_source_digest_is_rejected()
    {
        using var document = JsonDocument.Parse(
            File.ReadAllBytes(LocatorFixturePath("valid-transcript-whole-item.json")));
        var locator = MutateIntegrity(document.RootElement, sourceDigest: new string('0', 64));
        var context = BuildContextForFixture(document.RootElement);

        var result = EvidenceLocatorVerifier.TryVerify(locator, context);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
    }

    [Fact]
    public void Post_cutoff_transcript_item_is_rejected()
    {
        using var document = JsonDocument.Parse(
            File.ReadAllBytes(LocatorFixturePath("valid-transcript-whole-item.json")));
        var locator = document.RootElement;
        var baseContext = BuildContextForFixture(locator);
        var transcript = baseContext.TranscriptItemsByMessageId["msg.synthetic.0001"] with
        {
            PublishedSequence = 99,
        };
        var context = baseContext with
        {
            TranscriptItemsByMessageId = new Dictionary<string, EvaluationSessionTranscriptMaterial>(
                baseContext.TranscriptItemsByMessageId,
                StringComparer.Ordinal)
            {
                ["msg.synthetic.0001"] = transcript,
            },
        };

        var result = EvidenceLocatorVerifier.TryVerify(locator, context);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.ProtectedContent, result.OutcomeCode);
    }

    [Fact]
    public void Submission_byte_range_verifies_with_exact_bytes()
    {
        var content = new byte[256];
        Array.Fill(content, (byte)'a');
        var excerptDigest = EvidenceTextSourceNormalizer.DigestUtf8(content.AsSpan(0, 128));
        var sourceDigest = EvidenceTextSourceNormalizer.DigestUtf8(content);
        using var document = JsonDocument.Parse(
            File.ReadAllBytes(LocatorFixturePath("valid-submission-byte-range.json")));
        var locator = MutateIntegrity(
            MutateExcerptDigest(document.RootElement, excerptDigest),
            sourceDigest);
        var context = BuildSubmissionContext(locator, content, sourceDigest);

        var result = EvidenceLocatorVerifier.TryVerify(locator, context);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("verified", result.Value!.VerificationState);
    }

    [Fact]
    public void Whole_item_fallback_records_lower_precision()
    {
        var content = new byte[256];
        Array.Fill(content, (byte)'a');
        var sourceDigest = EvidenceTextSourceNormalizer.DigestUtf8(content);
        using var document = JsonDocument.Parse(
            File.ReadAllBytes(LocatorFixturePath("valid-submission-byte-range.json")));
        var locator = MutateIntegrity(document.RootElement, sourceDigest);
        var context = BuildSubmissionContext(locator, content, sourceDigest, permitWholeItemFallback: true);

        var result = EvidenceLocatorVerifier.TryVerify(locator, context);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("lower_precision", result.Value!.VerificationState);
    }

    private static EvidenceLocatorVerificationContext BuildContextForFixture(
        JsonElement locator,
        bool permitWholeItemFallback = false)
    {
        var ownership = locator.GetProperty("ownership_ref");
        var trustedOwnership = new EvaluationStableOwnershipReference(
            ownership.GetProperty("organization_id").GetString()!,
            ownership.GetProperty("activity_id").GetString()!,
            ownership.GetProperty("participant_id").GetString()!,
            ownership.GetProperty("attempt_id").GetString()!,
            ownership.GetProperty("session_id").GetString()!,
            ownership.GetProperty("evaluation_id").GetString()!);

        var sourceType = locator.GetProperty("source_type").GetString()!;
        var sourceRef = locator.GetProperty("source_ref");
        var sourceId = sourceRef.GetProperty("source_id").GetString()!;
        var sourceVersion = sourceRef.GetProperty("source_version").GetString()!;
        var sourceDigest = locator.GetProperty("integrity").GetProperty("source_digest").GetString()!;
        var cutoff = sourceRef.TryGetProperty("terminal_cutoff_sequence", out var cutoffElement)
                     && cutoffElement.ValueKind == JsonValueKind.String
            ? long.Parse(cutoffElement.GetString()!, System.Globalization.CultureInfo.InvariantCulture)
            : 42L;

        var submissionItems = new Dictionary<string, EvaluationSubmissionMaterial>(StringComparer.Ordinal);
        var transcriptItems = new Dictionary<string, EvaluationSessionTranscriptMaterial>(StringComparer.Ordinal);
        var configurationFacts = new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal);
        var manifestFacts = new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal);

        switch (sourceType)
        {
            case "submission.direct_text":
            case "submission.text_attachment":
                submissionItems[sourceId] = new EvaluationSubmissionMaterial(
                    sourceId,
                    sourceVersion,
                    sourceType,
                    sourceDigest,
                    BuildSubmissionBytes(sourceType, sourceDigest));
                break;
            case "session.transcript_item":
            case "session.work_trace":
                transcriptItems[sourceId] = new EvaluationSessionTranscriptMaterial(
                    sourceId,
                    sourceVersion,
                    PublishedSequence: cutoff,
                    sourceDigest,
                    "fixture transcript"u8.ToArray());
                break;
            case "configuration.fact":
                configurationFacts[sourceId] = new EvaluationSafeFactProjection(
                    sourceId,
                    sourceVersion,
                    sourceDigest,
                    """{"facts":[{"value":"allowed"}]}"""u8.ToArray());
                break;
        }

        return new EvidenceLocatorVerificationContext(
            trustedOwnership,
            cutoff,
            transcriptItems,
            submissionItems,
            configurationFacts,
            manifestFacts,
            permitWholeItemFallback);
    }

    private static EvidenceLocatorVerificationContext BuildSubmissionContext(
        JsonElement locator,
        ReadOnlyMemory<byte> content,
        string sourceDigest,
        bool permitWholeItemFallback = false)
    {
        var ownership = locator.GetProperty("ownership_ref");
        var sourceRef = locator.GetProperty("source_ref");
        return new EvidenceLocatorVerificationContext(
            new EvaluationStableOwnershipReference(
                ownership.GetProperty("organization_id").GetString()!,
                ownership.GetProperty("activity_id").GetString()!,
                ownership.GetProperty("participant_id").GetString()!,
                ownership.GetProperty("attempt_id").GetString()!,
                ownership.GetProperty("session_id").GetString()!,
                ownership.GetProperty("evaluation_id").GetString()!),
            42,
            new Dictionary<string, EvaluationSessionTranscriptMaterial>(StringComparer.Ordinal),
            new Dictionary<string, EvaluationSubmissionMaterial>(StringComparer.Ordinal)
            {
                [sourceRef.GetProperty("source_id").GetString()!] = new EvaluationSubmissionMaterial(
                    sourceRef.GetProperty("source_id").GetString()!,
                    sourceRef.GetProperty("source_version").GetString()!,
                    locator.GetProperty("source_type").GetString()!,
                    sourceDigest,
                    content),
            },
            new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal),
            new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal),
            permitWholeItemFallback);
    }

    private static ReadOnlyMemory<byte> BuildSubmissionBytes(string sourceType, string sourceDigest)
    {
        if (sourceType == "submission.text_attachment")
        {
            return "line1\nline2\nline3\nline4\nline5\nline6\nline7\nline8\n"u8.ToArray();
        }

        _ = sourceDigest;
        return new byte[256];
    }

    private static JsonElement MutateExcerptDigest(JsonElement locator, string excerptDigest)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in locator.EnumerateObject())
            {
                if (property.NameEquals("location"))
                {
                    writer.WritePropertyName("location");
                    writer.WriteStartObject();
                    foreach (var locationProperty in property.Value.EnumerateObject())
                    {
                        if (locationProperty.NameEquals("excerpt_digest"))
                        {
                            writer.WriteString("excerpt_digest", excerptDigest);
                            continue;
                        }

                        locationProperty.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    private static JsonElement MutateIntegrity(JsonElement locator, string sourceDigest)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in locator.EnumerateObject())
            {
                if (property.NameEquals("integrity"))
                {
                    writer.WritePropertyName("integrity");
                    writer.WriteStartObject();
                    writer.WriteString("source_digest", sourceDigest);
                    writer.WriteString(
                        "adapter_version",
                        property.Value.GetProperty("adapter_version").GetString());
                    writer.WriteString(
                        "verification_state",
                        property.Value.GetProperty("verification_state").GetString());
                    writer.WriteEndObject();
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    private static string LocatorFixturePath(string fixtureName) =>
        Path.Combine(
            ContractsRoot,
            "fixtures",
            "schema",
            "v1",
            "evidence",
            "evidence-locator",
            fixtureName);

    private static string FindContractsRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "contracts", "catalog.manifest.json");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate contracts root.");
    }
}

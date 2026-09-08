using System.Text.Json;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceLocatorMetadataProjectorTests
{
    [Fact]
    public void Submission_locator_projects_protected_submission_references()
    {
        var evidenceId = Guid.CreateVersion7();
        var itemRecordId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var acceptedVersionId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var sourceDigest = new string('c', 64);
        using var locatorDocument = JsonDocument.Parse(
            """
            {
              "locator_schema":"evidence-locator.v1",
              "source_type":"submission.direct_text",
              "source_ref":{"source_id":"item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","source_version":"rev.0001"},
              "ownership_ref":{
                "organization_id":"org.synthetic.0001",
                "activity_id":"act.synthetic.0001",
                "participant_id":"part.synthetic.0001",
                "attempt_id":"att.synthetic.0001",
                "session_id":"sess.synthetic.0001",
                "evaluation_id":"eval.synthetic.0001"
              },
              "location":{"location_type":"whole_item","item_id":"item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"},
              "precision":"whole_item",
              "integrity":{
                "source_digest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv.metadata.0001"}
            }
            """);
        var handoff = new EvaluationHandoffSnapshot(
            "handoff.metadata.0001",
            new EvaluationOwnership(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7()),
            "completed",
            Guid.CreateVersion7(),
            42,
            "manifest-jcs-sha256-v2",
            new string('f', 64),
            Guid.CreateVersion7(),
            new string('c', 64),
            Guid.CreateVersion7(),
            new string('d', 64));
        var submissionBundle = new EvaluationSubmissionEvidenceBundle(
        [
            new EvaluationSubmissionMaterial(
                "item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "rev.0001",
                "submission.direct_text",
                sourceDigest,
                new byte[256],
                acceptedVersionId,
                itemRecordId),
        ]);
        var verified = new VerifiedEvidenceLocator(
            "submission.direct_text",
            new string('a', 64),
            new string('b', 64),
            "verified",
            sourceDigest,
            "whole_item",
            new string('e', 64));

        var result = EvidenceLocatorMetadataProjector.TryCreate(
            evidenceId,
            locatorDocument.RootElement,
            verified,
            handoff,
            submissionBundle);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(evidenceId, result.Value!.EvidenceId);
        Assert.Equal(itemRecordId, result.Value.SourceId);
        Assert.Equal(acceptedVersionId, result.Value.SourceVersionId);
        Assert.Equal(sourceDigest, result.Value.SourceContentDigest);
        Assert.Equal("evidence-locator.v1", result.Value.LocatorSchema);
        Assert.Equal(64, result.Value.LocatorDigest.Length);
    }

    [Fact]
    public void Work_trace_locator_projects_deterministic_source_references()
    {
        var evidenceId = Guid.CreateVersion7();
        const string sourceId = "trace.synthetic.0001";
        const string sourceVersion = "rev.trace.0001";
        using var locatorDocument = JsonDocument.Parse(
            $$"""
            {
              "locator_schema":"evidence-locator.v1",
              "source_type":"session.work_trace",
              "source_ref":{
                "source_id":"{{sourceId}}",
                "source_version":"{{sourceVersion}}",
                "terminal_cutoff_sequence":"42"
              },
              "ownership_ref":{
                "organization_id":"org.synthetic.0001",
                "activity_id":"act.synthetic.0001",
                "participant_id":"part.synthetic.0001",
                "attempt_id":"att.synthetic.0001",
                "session_id":"sess.synthetic.0001",
                "evaluation_id":"eval.synthetic.0001"
              },
              "location":{"location_type":"whole_item","item_id":"{{sourceId}}"},
              "precision":"whole_item",
              "integrity":{
                "source_digest":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv.metadata.0002"}
            }
            """);
        var verified = new VerifiedEvidenceLocator(
            "session.work_trace",
            new string('a', 64),
            new string('b', 64),
            "verified",
            new string('d', 64),
            "whole_item",
            new string('e', 64));
        var handoff = new EvaluationHandoffSnapshot(
            "handoff.metadata.0002",
            new EvaluationOwnership(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7()),
            "completed",
            Guid.CreateVersion7(),
            42,
            "manifest-jcs-sha256-v2",
            new string('f', 64),
            Guid.CreateVersion7(),
            new string('c', 64),
            Guid.CreateVersion7(),
            new string('d', 64));

        var result = EvidenceLocatorMetadataProjector.TryCreate(
            evidenceId,
            locatorDocument.RootElement,
            verified,
            handoff,
            null);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(
            EvaluationDeterministicGuid.CreateVersion5(EvaluationSourceNamespaces.WorkTraceItem, sourceId),
            result.Value!.SourceId);
        Assert.Equal(
            EvaluationDeterministicGuid.CreateVersion5(EvaluationSourceNamespaces.WorkTraceVersion, sourceVersion),
            result.Value.SourceVersionId);
    }

    [Fact]
    public void Fallback_locator_metadata_persists_effective_whole_item_projection()
    {
        var content = new byte[256];
        Array.Fill(content, (byte)'a');
        var sourceDigest = EvidenceTextSourceNormalizer.DigestUtf8(content);
        using var locatorDocument = JsonDocument.Parse(
            File.ReadAllBytes(LocatorFixturePath("valid-submission-byte-range.json")));
        var locator = MutateIntegrity(locatorDocument.RootElement, sourceDigest);
        var context = BuildSubmissionContext(locator, content, sourceDigest, permitWholeItemFallback: true);
        var attemptedLocatorDigest = EvidenceLocatorDigestComputer.TryComputeLocatorDigest(locator);

        var verifiedResult = EvidenceLocatorVerifier.TryVerify(locator, context);
        Assert.True(verifiedResult.Succeeded, verifiedResult.OutcomeCode);

        var result = EvidenceLocatorMetadataProjector.TryCreate(
            Guid.CreateVersion7(),
            locator,
            verifiedResult.Value!,
            new EvaluationHandoffSnapshot(
                "handoff.metadata.fallback",
                new EvaluationOwnership(
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7()),
                "completed",
                Guid.CreateVersion7(),
                42,
                "manifest-jcs-sha256-v2",
                new string('f', 64),
                Guid.CreateVersion7(),
                new string('c', 64),
                Guid.CreateVersion7(),
                new string('d', 64)),
            new EvaluationSubmissionEvidenceBundle(
            [
                new EvaluationSubmissionMaterial(
                    locator.GetProperty("source_ref").GetProperty("source_id").GetString()!,
                    locator.GetProperty("source_ref").GetProperty("source_version").GetString()!,
                    locator.GetProperty("source_type").GetString()!,
                    sourceDigest,
                    content,
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7()),
            ]));

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("whole_item", result.Value!.Precision);
        Assert.Equal("lower_precision", result.Value.IntegrityState);
        Assert.Equal(verifiedResult.Value!.VerifiedLocatorDigest, result.Value.LocatorDigest);
        Assert.NotEqual(attemptedLocatorDigest.Value, result.Value.LocatorDigest);
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

    private static EvidenceLocatorVerificationContext BuildSubmissionContext(
        JsonElement locator,
        ReadOnlyMemory<byte> content,
        string sourceDigest,
        bool permitWholeItemFallback)
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
                    content,
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7()),
            },
            new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal),
            new Dictionary<string, EvaluationSafeFactProjection>(StringComparer.Ordinal),
            permitWholeItemFallback);
    }

    private static string LocatorFixturePath(string fixtureName) =>
        Path.Combine(
            FindContractsRoot(),
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

using System.Text.Json;
using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceLocatorCompletionVerifierTests
{
    private static readonly EvaluationProcedureV1 Procedure =
        EvaluationProcedureTestFixtures.LoadP0TextSynthetic();

    [Fact]
    public void Completion_verifier_seals_verified_locators_from_authoritative_handoff_ownership()
    {
        var evaluationId = Guid.CreateVersion7();
        var requestId = Guid.CreateVersion7();
        var ownership = new EvaluationOwnership(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"));
        var trustedOwnership = EvaluationStableOwnershipReferenceFactory.From(ownership, evaluationId);
        var evidenceId = Guid.CreateVersion7();
        var content = new byte[256];
        var sourceDigest = EvidenceTextSourceNormalizer.DigestUtf8(content);
        var submissionBundle = new EvaluationSubmissionEvidenceBundle(
        [
            new EvaluationSubmissionMaterial(
                "item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "rev.0001",
                "submission.direct_text",
                sourceDigest,
                content,
                Guid.CreateVersion7(),
                Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")),
        ]);
        using var locatorDocument = JsonDocument.Parse(
            $$"""
            {
              "locator_schema":"evidence-locator.v1",
              "source_type":"submission.direct_text",
              "source_ref":{"source_id":"item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","source_version":"rev.0001"},
              "ownership_ref":{
                "organization_id":"{{trustedOwnership.OrganizationId}}",
                "activity_id":"{{trustedOwnership.ActivityId}}",
                "participant_id":"{{trustedOwnership.ParticipantId}}",
                "attempt_id":"{{trustedOwnership.AttemptId}}",
                "session_id":"{{trustedOwnership.SessionId}}",
                "evaluation_id":"{{trustedOwnership.EvaluationId}}"
              },
              "location":{"location_type":"whole_item","item_id":"item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"},
              "precision":"whole_item",
              "integrity":{
                "source_digest":"{{sourceDigest}}",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv.completion.0001"}
            }
            """);
        var handoff = new EvaluationHandoffSnapshot(
            "handoff.completion.0001",
            ownership,
            "completed",
            Guid.CreateVersion7(),
            42,
            "manifest-jcs-sha256-v2",
            new string('f', 64),
            Guid.CreateVersion7(),
            new string('c', 64),
            Guid.CreateVersion7(),
            new string('d', 64));
        var request = new EvidenceLocatorCompletionRequest(
            evaluationId,
            requestId,
            handoff.HandoffId,
            [
                new EvidenceLocatorVerificationEntry(
                    evidenceId,
                    "crit.objective.word-count",
                    locatorDocument.RootElement.Clone()),
            ]);

        var result = EvidenceLocatorCompletionVerifier.TryVerify(
            ownership.OrganizationId,
            ownership.SessionId,
            Procedure,
            request,
            new EvaluationSessionEvidenceBundle(handoff, []),
            submissionBundle);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Single(result.Value!.VerifiedLocators);
        Assert.Single(result.Value.SealedItems);
        Assert.Equal(
            EvaluationEvidenceSourceIdentity.StableEvidenceId(evidenceId),
            result.Value.SealedItems[0].EvidenceId);
        Assert.Equal("verified", result.Value.SealedItems[0].VerificationState);
        Assert.Single(result.Value.LocatorRecords);
        Assert.Equal("evidence-locator.v1", result.Value.LocatorRecords[0].LocatorSchema);
    }

    [Fact]
    public void Whole_item_fallback_is_denied_when_procedure_disallows_it()
    {
        var procedure = Procedure with
        {
            Criteria = Procedure.Criteria
                .Select(item => item.CriterionId == "crit.objective.word-count"
                    ? item with
                    {
                        EvidenceRequirements = item.EvidenceRequirements with
                        {
                            WholeItemFallbackPermitted = false,
                        },
                    }
                    : item)
                .ToArray(),
        };
        var evaluationId = Guid.CreateVersion7();
        var ownership = new EvaluationOwnership(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        var trustedOwnership = EvaluationStableOwnershipReferenceFactory.From(ownership, evaluationId);
        using var locatorDocument = JsonDocument.Parse(
            File.ReadAllBytes(LocatorFixturePath("valid-submission-byte-range.json")));
        var content = "line1\nline2\nline3\nline4\nline5\nline6\nline7\nline8\n"u8.ToArray();
        var sourceDigest = EvidenceTextSourceNormalizer.DigestUtf8(content);
        var locator = MutateOwnership(
            MutateIntegrity(locatorDocument.RootElement, sourceDigest),
            trustedOwnership);
        var handoff = new EvaluationHandoffSnapshot(
            "handoff.fallback.denied",
            ownership,
            "completed",
            Guid.CreateVersion7(),
            42,
            "manifest-jcs-sha256-v2",
            new string('f', 64),
            Guid.CreateVersion7(),
            new string('c', 64),
            Guid.CreateVersion7(),
            new string('d', 64));
        var request = new EvidenceLocatorCompletionRequest(
            evaluationId,
            Guid.CreateVersion7(),
            handoff.HandoffId,
            [
                new EvidenceLocatorVerificationEntry(
                    Guid.CreateVersion7(),
                    "crit.objective.word-count",
                    locator),
            ]);

        var result = EvidenceLocatorCompletionVerifier.TryVerify(
            ownership.OrganizationId,
            ownership.SessionId,
            procedure,
            request,
            new EvaluationSessionEvidenceBundle(handoff, []),
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

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        _ = trustedOwnership;
    }

    [Theory]
    [InlineData("organization_id")]
    [InlineData("activity_id")]
    [InlineData("participant_id")]
    [InlineData("attempt_id")]
    [InlineData("session_id")]
    public void Locator_ownership_mismatch_against_authoritative_handoff_is_rejected(string fieldName)
    {
        var evaluationId = Guid.CreateVersion7();
        var ownership = new EvaluationOwnership(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        var trustedOwnership = EvaluationStableOwnershipReferenceFactory.From(ownership, evaluationId);
        using var locatorDocument = JsonDocument.Parse(
            $$"""
            {
              "locator_schema":"evidence-locator.v1",
              "source_type":"submission.direct_text",
              "source_ref":{"source_id":"item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","source_version":"rev.0001"},
              "ownership_ref":{
                "organization_id":"{{trustedOwnership.OrganizationId}}",
                "activity_id":"{{trustedOwnership.ActivityId}}",
                "participant_id":"{{trustedOwnership.ParticipantId}}",
                "attempt_id":"{{trustedOwnership.AttemptId}}",
                "session_id":"{{trustedOwnership.SessionId}}",
                "evaluation_id":"{{trustedOwnership.EvaluationId}}"
              },
              "location":{"location_type":"whole_item","item_id":"item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"},
              "precision":"whole_item",
              "integrity":{
                "source_digest":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv.completion.0001"}
            }
            """);
        var locator = MutateOwnershipField(locatorDocument.RootElement, fieldName, "scope.mismatch.0001");
        var handoff = new EvaluationHandoffSnapshot(
            "handoff.completion.scope",
            ownership,
            "completed",
            Guid.CreateVersion7(),
            42,
            "manifest-jcs-sha256-v2",
            new string('f', 64),
            Guid.CreateVersion7(),
            new string('c', 64),
            Guid.CreateVersion7(),
            new string('d', 64));
        var request = new EvidenceLocatorCompletionRequest(
            evaluationId,
            Guid.CreateVersion7(),
            handoff.HandoffId,
            [new EvidenceLocatorVerificationEntry(Guid.CreateVersion7(), "crit.objective.word-count", locator)]);

        var result = EvidenceLocatorCompletionVerifier.TryVerify(
            ownership.OrganizationId,
            ownership.SessionId,
            Procedure,
            request,
            new EvaluationSessionEvidenceBundle(handoff, []),
            new EvaluationSubmissionEvidenceBundle(
            [
                new EvaluationSubmissionMaterial(
                    "item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    "rev.0001",
                    "submission.direct_text",
                    new string('a', 64),
                    new byte[256],
                    Guid.CreateVersion7(),
                    Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")),
            ]));

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.IncompleteOwnership, result.OutcomeCode);
    }

    [Theory]
    [InlineData("organization")]
    [InlineData("session")]
    public void External_scope_parameters_must_match_authoritative_handoff(string scopeField)
    {
        var evaluationId = Guid.CreateVersion7();
        var ownership = new EvaluationOwnership(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        var handoff = new EvaluationHandoffSnapshot(
            "handoff.completion.external",
            ownership,
            "completed",
            Guid.CreateVersion7(),
            42,
            "manifest-jcs-sha256-v2",
            new string('f', 64),
            Guid.CreateVersion7(),
            new string('c', 64),
            Guid.CreateVersion7(),
            new string('d', 64));
        var request = new EvidenceLocatorCompletionRequest(
            evaluationId,
            Guid.CreateVersion7(),
            handoff.HandoffId,
            []);

        var organizationId = scopeField == "organization"
            ? Guid.CreateVersion7()
            : ownership.OrganizationId;
        var sessionId = scopeField == "session"
            ? Guid.CreateVersion7()
            : ownership.SessionId;

        var binding = EvidenceLocatorCompletionVerifier.TryBindTrustedOwnership(
            organizationId,
            sessionId,
            request,
            new EvaluationSessionEvidenceBundle(handoff, []),
            out _);

        Assert.NotNull(binding);
        Assert.Equal(EvaluationFailureCodes.IncompleteOwnership, binding!.OutcomeCode);
    }

    [Fact]
    public void Handoff_id_mismatch_is_rejected()
    {
        var ownership = new EvaluationOwnership(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        var request = new EvidenceLocatorCompletionRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "handoff.requested",
            []);

        var binding = EvidenceLocatorCompletionVerifier.TryBindTrustedOwnership(
            ownership.OrganizationId,
            ownership.SessionId,
            request,
            new EvaluationSessionEvidenceBundle(
                new EvaluationHandoffSnapshot(
                    "handoff.authoritative",
                    ownership,
                    "completed",
                    Guid.CreateVersion7(),
                    1,
                    "manifest-jcs-sha256-v2",
                    new string('f', 64),
                    Guid.CreateVersion7(),
                    new string('c', 64),
                    Guid.CreateVersion7(),
                    new string('d', 64)),
                []),
            out _);

        Assert.NotNull(binding);
        Assert.Equal(EvaluationFailureCodes.IncompleteOwnership, binding!.OutcomeCode);
        Assert.Equal("handoff_id", binding.Field);
    }

    [Fact]
    public void Duplicate_evidence_ids_fail_before_verification()
    {
        using var locatorDocument = JsonDocument.Parse("{}");
        var ownership = new EvaluationOwnership(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        var evidenceId = Guid.CreateVersion7();
        var request = new EvidenceLocatorCompletionRequest(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "handoff.duplicate",
            [
                new EvidenceLocatorVerificationEntry(
                    evidenceId,
                    "crit.objective.word-count",
                    locatorDocument.RootElement.Clone()),
                new EvidenceLocatorVerificationEntry(
                    evidenceId,
                    "crit.objective.word-count",
                    locatorDocument.RootElement.Clone()),
            ]);

        var result = EvidenceLocatorCompletionVerifier.TryVerify(
            ownership.OrganizationId,
            ownership.SessionId,
            Procedure,
            request,
            new EvaluationSessionEvidenceBundle(
                new EvaluationHandoffSnapshot(
                    "handoff.duplicate",
                    ownership,
                    "completed",
                    Guid.CreateVersion7(),
                    1,
                    "manifest-jcs-sha256-v2",
                    new string('f', 64),
                    Guid.CreateVersion7(),
                    new string('c', 64),
                    Guid.CreateVersion7(),
                    new string('d', 64)),
                []),
            null);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DuplicateIdentity, result.OutcomeCode);
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

    private static JsonElement MutateOwnership(
        JsonElement locator,
        EvaluationStableOwnershipReference ownership)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in locator.EnumerateObject())
            {
                if (property.NameEquals("ownership_ref"))
                {
                    writer.WritePropertyName("ownership_ref");
                    writer.WriteStartObject();
                    writer.WriteString("organization_id", ownership.OrganizationId);
                    writer.WriteString("activity_id", ownership.ActivityId);
                    writer.WriteString("participant_id", ownership.ParticipantId);
                    writer.WriteString("attempt_id", ownership.AttemptId);
                    writer.WriteString("session_id", ownership.SessionId);
                    writer.WriteString("evaluation_id", ownership.EvaluationId);
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

    private static JsonElement MutateOwnershipField(JsonElement locator, string fieldName, string value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in locator.EnumerateObject())
            {
                if (property.NameEquals("ownership_ref"))
                {
                    writer.WritePropertyName("ownership_ref");
                    writer.WriteStartObject();
                    foreach (var ownershipProperty in property.Value.EnumerateObject())
                    {
                        if (ownershipProperty.NameEquals(fieldName))
                        {
                            writer.WriteString(fieldName, value);
                            continue;
                        }

                        ownershipProperty.WriteTo(writer);
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

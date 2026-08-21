using System.Text;
using System.Text.Json;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.CanonicalJson;

namespace FlexAgent.AssessmentConfiguration.Canonicalization;

public sealed class ActivationBaselineDigester : IActivationBaselineDigester
{
    public static CanonicalJsonLimits ProductionLimits { get; } = new(
        ActivationBaselineDocument.MaxUtf8Bytes,
        ActivationBaselineDocument.MaxNestingDepth,
        ActivationBaselineDocument.MaxObjectProperties,
        ActivationBaselineDocument.MaxArrayElements);

    public AssessmentDecision<string> Digest(ActivationBaselineDocument document)
    {
        var validated = ActivationBaselineDocument.Validate(document);
        if (!validated.Succeeded)
        {
            return AssessmentDecision<string>.Fail(validated.OutcomeCode);
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { SkipValidation = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("procedure_id", document.ProcedureId);
            writer.WriteString("schema_version", document.SchemaVersion);
            writer.WriteString("canonicalization_version", document.CanonicalizationVersion);
            writer.WritePropertyName("fairness_domains");
            writer.WriteStartArray();
            foreach (var domain in document.FairnessDomains.OrderBy(item => item.DomainKey, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("classification", domain.Classification);
                writer.WriteString("domain_key", domain.DomainKey);
                writer.WritePropertyName("effective_value");
                writer.WriteStartObject();
                foreach (var pair in domain.EffectiveValue.OrderBy(item => item.Key, StringComparer.Ordinal))
                {
                    if (pair.Key.Length > 64 || pair.Value.Length > 512)
                    {
                        return AssessmentDecision<string>.Fail(AssessmentFailureCodes.InvalidField, domain.DomainKey);
                    }

                    writer.WriteString(pair.Key, pair.Value);
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WritePropertyName("resolution_decisions");
            writer.WriteStartArray();
            foreach (var decision in document.ResolutionDecisions.OrderBy(item => item.DecisionKey, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("decision_key", decision.DecisionKey);
                writer.WriteString("outcome", decision.Outcome);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WritePropertyName("source_references");
            writer.WriteStartArray();
            foreach (var reference in document.SourceReferences
                         .OrderBy(item => item.SourceKey, StringComparer.Ordinal)
                         .ThenBy(item => item.SourceId))
            {
                writer.WriteStartObject();
                writer.WriteString("content_digest", reference.ContentDigest);
                writer.WriteString("source_id", reference.SourceId.ToString("D"));
                writer.WriteString("source_key", reference.SourceKey);
                writer.WriteString("source_version", reference.SourceVersion.ToString("D"));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WritePropertyName("approved_exception_refs");
            writer.WriteStartArray();
            foreach (var exception in document.ApprovedExceptionRefs.OrderBy(item => item))
            {
                writer.WriteStringValue(exception.ToString("D"));
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        try
        {
            var digest = CanonicalJsonProcessor.CanonicalizeSha256Hex(stream.ToArray(), ProductionLimits);
            return AssessmentDecision<string>.Ok(digest);
        }
        catch (CanonicalJsonException)
        {
            return AssessmentDecision<string>.Fail(AssessmentFailureCodes.InvalidField);
        }
    }
}

public sealed class AssessmentCommandDigest : IAssessmentCommandDigest
{
    public string Compute(ActivateCohortCommand command)
    {
        var payload = string.Join(
            '|',
            command.Actor.Organization.OrganizationId.ToString("D"),
            command.ActivityId.ToString("D"),
            command.CohortId.ToString("D"),
            command.ExpectedRevisionId.ToString("D"),
            command.ExpectedRevisionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.IdempotencyKey,
            command.Environment);
        var utf8 = Encoding.UTF8.GetBytes(payload);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(utf8)).ToLowerInvariant();
    }
}

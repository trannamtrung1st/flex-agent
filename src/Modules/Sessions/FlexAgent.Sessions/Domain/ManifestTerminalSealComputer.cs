using System.Text;
using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Sessions.Domain;

public static class ManifestTerminalSealComputer
{
    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 64,
        maxObjectProperties: 4_096,
        maxArrayElements: 4_096);

    public static string ComputeDigest(ManifestSealDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var json = BuildCanonicalPayload(document);
        return CanonicalJsonProcessor.CanonicalizeSha256Hex(Encoding.UTF8.GetBytes(json), Limits);
    }

    public static bool Verify(ManifestSealDocument document, string expectedDigest) =>
        string.Equals(ComputeDigest(document), expectedDigest, StringComparison.Ordinal);

    private static string BuildCanonicalPayload(ManifestSealDocument document)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("canonicalization_version", document.CanonicalizationVersion);
            writer.WriteStartObject("configuration_ref");
            writer.WriteString("configuration_digest", document.ConfigurationDigest);
            writer.WriteString("configuration_id", document.ConfigurationId);
            writer.WriteEndObject();
            writer.WriteString("manifest_schema_version", document.ManifestSchemaVersion);
            writer.WriteStartObject("ownership");
            writer.WriteString("activity_id", document.ActivityId);
            writer.WriteString("attempt_id", document.AttemptId);
            writer.WriteString("organization_id", document.OrganizationId);
            writer.WriteString("participant_id", document.ParticipantId);
            writer.WriteString("session_id", document.SessionId);
            writer.WriteEndObject();
            writer.WriteString("procedure_id", document.ProcedureId);
            writer.WritePropertyName("runtime_records");
            writer.WriteStartArray();
            foreach (var record in document.RuntimeRecords)
            {
                writer.WriteStartObject();
                writer.WriteString("payload_digest", record.PayloadDigest);
                writer.WriteString("record_type", record.RecordType);
                writer.WriteNumber("sequence", record.Sequence);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteString("schema_version", document.SchemaVersion);
            writer.WriteString("terminal_reason", document.TerminalReason);
            writer.WriteString("terminal_state", document.TerminalState);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

internal static class SessionOwnershipStableIds
{
    public static string Organization(Guid id) => Prefix("org", id);

    public static string Activity(Guid id) => Prefix("act", id);

    public static string Participant(Guid id) => Prefix("part", id);

    public static string Attempt(Guid id) => Prefix("att", id);

    public static string Session(Guid id) => Prefix("sess", id);

    private static string Prefix(string prefix, Guid id) =>
        $"{prefix}.{id.ToString("N").ToLowerInvariant()}";
}

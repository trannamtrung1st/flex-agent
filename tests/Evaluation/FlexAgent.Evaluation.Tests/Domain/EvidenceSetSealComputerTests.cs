using System.Text.Json;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceSetSealComputerTests
{
    private static readonly string ContractsRoot = FindContractsRoot();

    [Fact]
    public void Sorted_evidence_items_fixture_matches_expected_sha256()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(FixturePath("sorted-evidence-items")));
        var digestDocument = document.RootElement.GetProperty("digest_document");
        var request = new EvidenceSetSealRequest(
            digestDocument.GetProperty("evidence_set_id").GetString()!,
            digestDocument.GetProperty("evaluation_invocation_id").GetString()!,
            ReadOwnership(digestDocument.GetProperty("ownership")),
            digestDocument.GetProperty("handoff_digest").GetString()!,
            digestDocument.GetProperty("frozen_input_digest").GetString()!,
            digestDocument.GetProperty("evidence_items").EnumerateArray()
                .Select(ReadItem)
                .ToArray());

        var result = EvidenceSetSealComputer.TryComputeDigest(request);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(
            document.RootElement.GetProperty("expected_sha256_hex").GetString(),
            result.Value);
    }

    [Fact]
    public void Tampered_item_digest_fails_the_expected_digest()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(FixturePath("sorted-evidence-items")));
        var digestDocument = document.RootElement.GetProperty("digest_document");
        var items = digestDocument.GetProperty("evidence_items").EnumerateArray()
            .Select(ReadItem)
            .Select((item, index) => index == 0
                ? item with { LocationDigest = new string('0', 64) }
                : item)
            .ToArray();
        var request = new EvidenceSetSealRequest(
            digestDocument.GetProperty("evidence_set_id").GetString()!,
            digestDocument.GetProperty("evaluation_invocation_id").GetString()!,
            ReadOwnership(digestDocument.GetProperty("ownership")),
            digestDocument.GetProperty("handoff_digest").GetString()!,
            digestDocument.GetProperty("frozen_input_digest").GetString()!,
            items);

        var result = EvidenceSetSealComputer.TryComputeDigest(request);

        Assert.True(result.Succeeded);
        Assert.NotEqual(
            document.RootElement.GetProperty("expected_sha256_hex").GetString(),
            result.Value);
    }

    [Fact]
    public void Duplicate_evidence_ids_are_rejected()
    {
        var item = new SealedEvidenceItemReference(
            "evid.synthetic.0001",
            "submission.direct_text",
            new string('a', 64),
            new string('b', 64),
            "verified");
        var request = new EvidenceSetSealRequest(
            "evset.synthetic.0001",
            "inv.synthetic.0001",
            new EvidenceSetOwnershipReference(
                "org.synthetic.0001",
                "act.synthetic.0001",
                "part.synthetic.0001",
                "att.synthetic.0001",
                "sess.synthetic.0001"),
            new string('c', 64),
            new string('d', 64),
            [item, item]);

        var result = EvidenceSetSealComputer.TryComputeDigest(request);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DuplicateIdentity, result.OutcomeCode);
    }

    private static SealedEvidenceItemReference ReadItem(JsonElement element) =>
        new(
            element.GetProperty("evidence_id").GetString()!,
            element.GetProperty("source_type").GetString()!,
            element.GetProperty("source_ref_digest").GetString()!,
            element.GetProperty("location_digest").GetString()!,
            element.GetProperty("verification_state").GetString()!);

    private static EvidenceSetOwnershipReference ReadOwnership(JsonElement element) =>
        new(
            element.GetProperty("organization_id").GetString()!,
            element.GetProperty("activity_id").GetString()!,
            element.GetProperty("participant_id").GetString()!,
            element.GetProperty("attempt_id").GetString()!,
            element.GetProperty("session_id").GetString()!);

    private static string FixturePath(string caseId) =>
        Path.Combine(
            ContractsRoot,
            "fixtures",
            "jcs",
            "evidence-set-jcs-sha256-v1",
            caseId,
            "fixture.json");

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

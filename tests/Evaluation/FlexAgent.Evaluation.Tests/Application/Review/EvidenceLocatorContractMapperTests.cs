using System.Text.Json;
using FlexAgent.Contracts.Evidence;
using FlexAgent.Evaluation.Application.Review;

namespace FlexAgent.Evaluation.Tests.Application.Review;

public sealed class EvidenceLocatorContractMapperTests
{
    private static readonly string ContractsRoot = FindContractsRoot();

    [Theory]
    [InlineData("valid-transcript-whole-item.json", typeof(WholeItemLocationV1))]
    [InlineData("valid-configuration-json-pointer.json", typeof(JsonPointerLocationV1))]
    public void Canonical_locator_round_trips_to_contract_v1(string fixtureName, Type expectedLocationType)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(LocatorFixturePath(fixtureName)));
        var mapped = EvidenceLocatorContractMapper.TryMapV1(document.RootElement);

        Assert.True(mapped.Succeeded, mapped.OutcomeCode);
        Assert.Equal("evidence-locator.v1", mapped.Value!.LocatorSchema);
        Assert.IsType(expectedLocationType, mapped.Value.Location);
    }

    [Fact]
    public void Utf8_byte_range_location_preserves_range_fields()
    {
        const string locatorJson =
            """
            {
              "locator_schema":"evidence-locator.v1",
              "source_type":"session.transcript_item",
              "source_ref":{"source_id":"msg.1","source_version":"rev.1","terminal_cutoff_sequence":"1"},
              "ownership_ref":{
                "organization_id":"org.1","activity_id":"act.1","participant_id":"part.1",
                "attempt_id":"att.1","session_id":"sess.1","evaluation_id":"eval.1"
              },
              "location":{
                "location_type":"utf8_byte_range",
                "item_id":"msg.1",
                "start_inclusive":0,
                "end_exclusive":4,
                "excerpt_digest":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
              },
              "precision":"exact_range",
              "integrity":{
                "source_digest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv.1"}
            }
            """;
        using var document = JsonDocument.Parse(locatorJson);
        var mapped = EvidenceLocatorContractMapper.TryMapV1(document.RootElement);

        Assert.True(mapped.Succeeded, mapped.OutcomeCode);
        var location = Assert.IsType<Utf8ByteRangeLocationV1>(mapped.Value!.Location);
        Assert.Equal(0, location.StartInclusive);
        Assert.Equal(4, location.EndExclusive);
        Assert.Equal("exact_range", mapped.Value.Precision);
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

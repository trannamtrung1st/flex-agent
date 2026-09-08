using System.Text.Json;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceLocatorStructuralValidatorTests
{
    private static readonly string ContractsRoot = FindContractsRoot();

    [Theory]
    [InlineData("valid-configuration-json-pointer.json")]
    [InlineData("valid-submission-byte-range.json")]
    [InlineData("valid-submission-line-range.json")]
    [InlineData("valid-transcript-whole-item.json")]
    public void Valid_locator_fixtures_pass_structural_validation(string fixtureName)
    {
        var bytes = File.ReadAllBytes(LocatorFixturePath(fixtureName));
        var result = EvidenceLocatorStructuralValidator.ValidateJson(bytes);
        Assert.True(result.Succeeded, result.OutcomeCode);
    }

    [Theory]
    [InlineData("invalid-byte-range-missing-excerpt-digest.json")]
    [InlineData("invalid-configuration-whole-item.json")]
    [InlineData("invalid-line-range-missing-split-version.json")]
    public void Invalid_locator_fixtures_fail_structural_validation(string fixtureName)
    {
        var bytes = File.ReadAllBytes(LocatorFixturePath(fixtureName));
        var result = EvidenceLocatorStructuralValidator.ValidateJson(bytes);
        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.InvalidField, result.OutcomeCode);
    }

    [Fact]
    public void Session_transcript_locator_requires_terminal_cutoff_sequence()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(LocatorFixturePath("valid-transcript-whole-item.json")));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.NameEquals("source_ref"))
                {
                    writer.WritePropertyName("source_ref");
                    writer.WriteStartObject();
                    foreach (var sourceProperty in property.Value.EnumerateObject())
                    {
                        if (sourceProperty.NameEquals("terminal_cutoff_sequence"))
                        {
                            continue;
                        }

                        sourceProperty.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        var result = EvidenceLocatorStructuralValidator.ValidateJson(stream.ToArray());

        Assert.False(result.Succeeded);
        Assert.Equal("source_ref.terminal_cutoff_sequence", result.Field);
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

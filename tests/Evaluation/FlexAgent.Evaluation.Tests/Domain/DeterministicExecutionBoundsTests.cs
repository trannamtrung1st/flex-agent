using System.Text;
using System.Text.Json;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class DeterministicExecutionBoundsTests
{
    [Fact]
    public void Canonical_input_exceeding_memory_limit_is_rejected()
    {
        var input = Encoding.UTF8.GetBytes("""{"schema":"x"}""");

        Assert.False(DeterministicExecutionBounds.TryValidateCanonicalInputSize(input, 8, out _));
    }

    [Fact]
    public void Excessive_json_nesting_is_rejected()
    {
        var builder = new StringBuilder("{\"a\":");
        for (var i = 0; i < DeterministicExecutionBounds.MaxJsonDepth + 2; i++)
        {
            builder.Append('{');
        }

        builder.Append("\"x\":1");
        for (var i = 0; i < DeterministicExecutionBounds.MaxJsonDepth + 2; i++)
        {
            builder.Append('}');
        }

        builder.Append('}');
        var input = Encoding.UTF8.GetBytes(builder.ToString());

        Assert.False(DeterministicExecutionBounds.TryParseBoundedJson(input, out _, out _));
    }

    [Fact]
    public void Excessive_json_property_count_is_rejected()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            for (var i = 0; i <= DeterministicExecutionBounds.MaxJsonPropertyCount; i++)
            {
                writer.WriteNumber($"p{i}", i);
            }

            writer.WriteEndObject();
        }

        Assert.False(DeterministicExecutionBounds.TryParseBoundedJson(stream.ToArray(), out _, out _));
    }

    [Fact]
    public void Elapsed_limit_violation_is_detected()
    {
        var startedAt = DateTimeOffset.UtcNow.AddSeconds(-2);

        Assert.False(DeterministicExecutionBounds.TryValidateElapsedLimit(startedAt, "PT1S", out _));
    }
}

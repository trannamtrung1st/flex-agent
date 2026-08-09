using System.Text;
using FlexAgent.CanonicalJson;

namespace FlexAgent.CanonicalJson.Tests;

public sealed class CanonicalJsonProcessorTests
{
    private static readonly CanonicalJsonLimits TestLimits = new(
        maxUtf8Bytes: 4_096,
        maxNestingDepth: 16,
        maxObjectProperties: 32,
        maxArrayElements: 32);

    [Fact]
    public void CanonicalizeSha256Hex_returns_lowercase_hex()
    {
        var input = Encoding.UTF8.GetBytes("""{"b":2,"a":1}""");
        var digest = CanonicalJsonProcessor.CanonicalizeSha256Hex(input, TestLimits);
        Assert.Matches("^[0-9a-f]{64}$", digest);
        Assert.Equal(digest, digest.ToLowerInvariant());
    }

    [Fact]
    public void Top_level_array_is_rejected()
    {
        var input = Encoding.UTF8.GetBytes("""[1,2,3]""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, TestLimits));
        Assert.Equal(CanonicalJsonFailureCategory.TopLevelNotObject, ex.Category);
    }

    [Fact]
    public void Duplicate_property_is_rejected()
    {
        var input = Encoding.UTF8.GetBytes("""{"a":1,"a":2}""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, TestLimits));
        Assert.Equal(CanonicalJsonFailureCategory.DuplicateProperty, ex.Category);
    }

    [Fact]
    public void Invalid_utf8_is_rejected()
    {
        var input = new byte[] { 0x7b, 0x22, 0x61, 0x22, 0x3a, 0xff, 0x7d };
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, TestLimits));
        Assert.Equal(CanonicalJsonFailureCategory.InvalidUtf8, ex.Category);
    }

    [Fact]
    public void Lone_surrogate_escape_is_rejected()
    {
        var input = Encoding.UTF8.GetBytes("""{"x":"\uD800"}""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, TestLimits));
        Assert.Equal(CanonicalJsonFailureCategory.InvalidUnicode, ex.Category);
    }

    [Fact]
    public void Negative_zero_is_rejected()
    {
        var input = Encoding.UTF8.GetBytes("""{"n":-0}""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, TestLimits));
        Assert.Equal(CanonicalJsonFailureCategory.NegativeZero, ex.Category);
    }

    [Fact]
    public void Input_byte_limit_is_enforced()
    {
        var limits = new CanonicalJsonLimits(maxUtf8Bytes: 8, maxNestingDepth: 8, maxObjectProperties: 8, maxArrayElements: 8);
        var input = Encoding.UTF8.GetBytes("""{"value":"too-long"}""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, limits));
        Assert.Equal(CanonicalJsonFailureCategory.InputTooLarge, ex.Category);
    }

    [Fact]
    public void Nesting_depth_limit_is_enforced()
    {
        var limits = new CanonicalJsonLimits(maxUtf8Bytes: 256, maxNestingDepth: 2, maxObjectProperties: 8, maxArrayElements: 8);
        var input = Encoding.UTF8.GetBytes("""{"a":{"b":{"c":1}}}""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, limits));
        Assert.Equal(CanonicalJsonFailureCategory.NestingDepthExceeded, ex.Category);
    }

    [Fact]
    public void Property_count_limit_is_enforced()
    {
        var limits = new CanonicalJsonLimits(maxUtf8Bytes: 512, maxNestingDepth: 8, maxObjectProperties: 1, maxArrayElements: 8);
        var input = Encoding.UTF8.GetBytes("""{"a":1,"b":2}""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, limits));
        Assert.Equal(CanonicalJsonFailureCategory.PropertyCountExceeded, ex.Category);
    }

    [Fact]
    public void Array_length_limit_is_enforced()
    {
        var limits = new CanonicalJsonLimits(maxUtf8Bytes: 512, maxNestingDepth: 8, maxObjectProperties: 8, maxArrayElements: 1);
        var input = Encoding.UTF8.GetBytes("""{"items":[1,2]}""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, limits));
        Assert.Equal(CanonicalJsonFailureCategory.ArrayLengthExceeded, ex.Category);
    }

    [Fact]
    public void Array_length_limit_counts_object_elements_in_arrays()
    {
        var limits = new CanonicalJsonLimits(maxUtf8Bytes: 512, maxNestingDepth: 8, maxObjectProperties: 8, maxArrayElements: 2);
        var input = Encoding.UTF8.GetBytes("""{"items":[{"a":1},{"a":2},{"a":3}]}""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, limits));
        Assert.Equal(CanonicalJsonFailureCategory.ArrayLengthExceeded, ex.Category);
    }

    [Fact]
    public void Failure_messages_do_not_echo_sensitive_marker()
    {
        const string secretMarker = "SYNTHETIC_SECRET_MARKER_7f3c";
        var input = Encoding.UTF8.GetBytes($$"""{"{{secretMarker}}":"value","{{secretMarker}}":2}""");
        var ex = Assert.Throws<CanonicalJsonException>(() => CanonicalJsonProcessor.CanonicalizeUtf8(input, TestLimits));
        Assert.DoesNotContain(secretMarker, ex.Message, StringComparison.Ordinal);
    }
}

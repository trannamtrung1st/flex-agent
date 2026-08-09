namespace FlexAgent.CanonicalJson;

/// <summary>
/// Explicit, caller-supplied resource limits for canonicalization.
/// No production defaults are defined by this artifact.
/// </summary>
public readonly struct CanonicalJsonLimits : IEquatable<CanonicalJsonLimits>
{
    public CanonicalJsonLimits(
        int maxUtf8Bytes,
        int maxNestingDepth,
        int maxObjectProperties,
        int maxArrayElements)
    {
        if (maxUtf8Bytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUtf8Bytes));
        }

        if (maxNestingDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxNestingDepth));
        }

        if (maxObjectProperties <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxObjectProperties));
        }

        if (maxArrayElements <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxArrayElements));
        }

        MaxUtf8Bytes = maxUtf8Bytes;
        MaxNestingDepth = maxNestingDepth;
        MaxObjectProperties = maxObjectProperties;
        MaxArrayElements = maxArrayElements;
    }

    public int MaxUtf8Bytes { get; }

    public int MaxNestingDepth { get; }

    public int MaxObjectProperties { get; }

    public int MaxArrayElements { get; }

    public bool Equals(CanonicalJsonLimits other) =>
        MaxUtf8Bytes == other.MaxUtf8Bytes
        && MaxNestingDepth == other.MaxNestingDepth
        && MaxObjectProperties == other.MaxObjectProperties
        && MaxArrayElements == other.MaxArrayElements;

    public override bool Equals(object obj) => obj is CanonicalJsonLimits other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(
        MaxUtf8Bytes,
        MaxNestingDepth,
        MaxObjectProperties,
        MaxArrayElements);
}

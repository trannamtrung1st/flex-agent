namespace FlexAgent.CanonicalJson;

/// <summary>
/// Canonicalization failure with a stable category and no protected input content.
/// </summary>
public sealed class CanonicalJsonException : Exception
{
    public CanonicalJsonException(CanonicalJsonFailureCategory category)
        : base(category.ToString())
    {
        Category = category;
    }

    public CanonicalJsonFailureCategory Category { get; }
}

namespace FlexAgent.CanonicalJson;

/// <summary>
/// Stable, non-content-bearing canonicalization failure categories.
/// </summary>
public enum CanonicalJsonFailureCategory
{
    InputTooLarge,
    InvalidUtf8,
    InvalidUnicode,
    TopLevelNotObject,
    DuplicateProperty,
    NestingDepthExceeded,
    PropertyCountExceeded,
    ArrayLengthExceeded,
    NonFiniteNumber,
    NegativeZero,
    MalformedJson,
    UpstreamCanonicalizationFailed,
}

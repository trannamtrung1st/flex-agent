namespace FlexAgent.Contract.Tests.Harness;

internal enum SchemaCompatibilityFailure
{
    MissingDialect,
    UnexpectedDialect,
    UnsupportedKeyword,
    SchemaBuildFailed,
    ValidationFailed,
}

internal sealed class SchemaCompatibilityException : Exception
{
    public SchemaCompatibilityException(SchemaCompatibilityFailure failure)
        : base(failure.ToString())
    {
        Failure = failure;
    }

    public SchemaCompatibilityFailure Failure { get; }
}

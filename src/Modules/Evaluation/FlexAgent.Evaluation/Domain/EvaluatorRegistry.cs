namespace FlexAgent.Evaluation.Domain;

public static class EvaluatorRegistryVersions
{
    public const string P0 = "evalreg.p0.v1";

    public const string BuiltinAlias = "evalreg.builtin.v1";
}

public static class EvaluatorQualificationStates
{
    public const string Qualified = "qualified";

    public const string Unqualified = "unqualified";
}

public static class EvaluatorOperations
{
    public const string ExactCompare = "exact_compare";

    public const string SchemaValidate = "schema_validate";

    public const string CitationValidate = "citation_validate";

    public const string BoundedCalculation = "bounded_calculation";

    public const string RubricAggregate = "rubric_aggregate";
}

public sealed record EvaluatorRegistryEntry(
    string EvaluatorId,
    string EvaluatorVersion,
    string EvaluatorDigest,
    string Operation,
    string InputSchemaId,
    string OutputSchemaId,
    string CanonicalizationProcedure,
    string ConfigurationDigest,
    string DependencyDigest,
    string CpuTimeLimit,
    string ElapsedTimeLimit,
    int MemoryLimitBytes,
    int OutputLimitBytes,
    string NetworkEgress,
    string ExecutableSelection,
    string QualificationState);

public sealed record EvaluatorRegistrySnapshot(
    string RegistryVersion,
    IReadOnlyDictionary<string, EvaluatorRegistryEntry> Entries);

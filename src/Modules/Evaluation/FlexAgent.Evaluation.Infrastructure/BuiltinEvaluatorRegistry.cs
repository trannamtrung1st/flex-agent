using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class BuiltinEvaluatorRegistry : IEvaluatorRegistry
{
    public const string BoundedCalcEvaluatorDigest =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    public const string BoundedCalcConfigurationDigest =
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    public const string BoundedCalcDependencyDigest =
        "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

    public const string SchemaValidateEvaluatorDigest =
        "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";

    public const string SchemaValidateConfigurationDigest =
        "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";

    public const string SchemaValidateDependencyDigest =
        "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";

    private static readonly EvaluatorRegistryEntry BoundedCalcEntry = CreateEntry(
        "eval.builtin.bounded-calc",
        EvaluatorOperations.BoundedCalculation,
        "eval.builtin.bounded-calc.input.v1",
        "eval.builtin.bounded-calc.output.v1",
        BoundedCalcEvaluatorDigest,
        BoundedCalcConfigurationDigest,
        BoundedCalcDependencyDigest);

    private static readonly EvaluatorRegistryEntry SchemaValidateEntry = CreateEntry(
        "eval.builtin.schema-validate",
        EvaluatorOperations.SchemaValidate,
        "eval.builtin.schema-validate.input.v1",
        "eval.builtin.schema-validate.output.v1",
        SchemaValidateEvaluatorDigest,
        SchemaValidateConfigurationDigest,
        SchemaValidateDependencyDigest);

    private static readonly EvaluatorRegistryEntry ExactCompareEntry = CreateEntry(
        "eval.builtin.exact-compare",
        EvaluatorOperations.ExactCompare,
        "eval.builtin.exact-compare.input.v1",
        "eval.builtin.exact-compare.output.v1",
        "1111111111111111111111111111111111111111111111111111111111111111",
        "2222222222222222222222222222222222222222222222222222222222222222",
        "3333333333333333333333333333333333333333333333333333333333333333");

    private static readonly EvaluatorRegistryEntry CitationValidateEntry = CreateEntry(
        "eval.builtin.citation-validate",
        EvaluatorOperations.CitationValidate,
        "eval.builtin.citation-validate.input.v1",
        "eval.builtin.citation-validate.output.v1",
        "4444444444444444444444444444444444444444444444444444444444444444",
        "5555555555555555555555555555555555555555555555555555555555555555",
        "6666666666666666666666666666666666666666666666666666666666666666");

    private static readonly EvaluatorRegistryEntry RubricAggregateEntry = CreateEntry(
        "eval.builtin.rubric-aggregate",
        EvaluatorOperations.RubricAggregate,
        "eval.builtin.rubric-aggregate.input.v1",
        "eval.builtin.rubric-aggregate.output.v1",
        "7777777777777777777777777777777777777777777777777777777777777777",
        "8888888888888888888888888888888888888888888888888888888888888888",
        "9999999999999999999999999999999999999999999999999999999999999999");

    private static readonly EvaluatorRegistrySnapshot P0Snapshot = CreateSnapshot(EvaluatorRegistryVersions.P0);

    private static readonly EvaluatorRegistrySnapshot BuiltinAliasSnapshot =
        CreateSnapshot(EvaluatorRegistryVersions.BuiltinAlias);

    public EvaluationDecision<EvaluatorRegistrySnapshot> TryGetRegistry(string registryVersion)
    {
        if (EvaluationIdentity.ContainsMutableAlias(registryVersion))
        {
            return EvaluationDecision<EvaluatorRegistrySnapshot>.Fail(
                EvaluationFailureCodes.MutableAlias,
                "evaluator_registry_version");
        }

        if (!EvaluationIdentity.IsStableId(registryVersion))
        {
            return EvaluationDecision<EvaluatorRegistrySnapshot>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "evaluator_registry_version");
        }

        return registryVersion switch
        {
            EvaluatorRegistryVersions.P0 => EvaluationDecision<EvaluatorRegistrySnapshot>.Ok(P0Snapshot),
            EvaluatorRegistryVersions.BuiltinAlias =>
                EvaluationDecision<EvaluatorRegistrySnapshot>.Ok(BuiltinAliasSnapshot),
            _ => EvaluationDecision<EvaluatorRegistrySnapshot>.Fail(
                EvaluationFailureCodes.UnqualifiedEvaluator,
                "evaluator_registry_version"),
        };
    }

    private static EvaluatorRegistrySnapshot CreateSnapshot(string registryVersion)
    {
        var entries = new Dictionary<string, EvaluatorRegistryEntry>(StringComparer.Ordinal)
        {
            [BoundedCalcEntry.EvaluatorId] = BoundedCalcEntry,
            [SchemaValidateEntry.EvaluatorId] = SchemaValidateEntry,
            [ExactCompareEntry.EvaluatorId] = ExactCompareEntry,
            [CitationValidateEntry.EvaluatorId] = CitationValidateEntry,
            [RubricAggregateEntry.EvaluatorId] = RubricAggregateEntry,
        };

        return new EvaluatorRegistrySnapshot(registryVersion, entries);
    }

    private static EvaluatorRegistryEntry CreateEntry(
        string evaluatorId,
        string operation,
        string inputSchemaId,
        string outputSchemaId,
        string evaluatorDigest,
        string configurationDigest,
        string dependencyDigest) =>
        new(
            evaluatorId,
            $"{evaluatorId}.v1",
            evaluatorDigest,
            operation,
            inputSchemaId,
            outputSchemaId,
            "jcs-sha256-v1",
            configurationDigest,
            dependencyDigest,
            "PT5S",
            "PT10S",
            16_777_216,
            4096,
            "prohibited",
            "prohibited",
            EvaluatorQualificationStates.Qualified);
}

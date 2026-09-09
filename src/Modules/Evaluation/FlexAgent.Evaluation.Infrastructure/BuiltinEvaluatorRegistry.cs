using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Infrastructure;

public sealed class BuiltinEvaluatorRegistry : IEvaluatorRegistry
{
    private static readonly EvaluatorRegistryEntry BoundedCalcEntry = CreateEntry(
        "eval.builtin.bounded-calc",
        EvaluatorOperations.BoundedCalculation,
        "eval.builtin.bounded-calc.input.v1",
        "eval.builtin.bounded-calc.output.v1");

    private static readonly EvaluatorRegistryEntry SchemaValidateEntry = CreateEntry(
        "eval.builtin.schema-validate",
        EvaluatorOperations.SchemaValidate,
        "eval.builtin.schema-validate.input.v1",
        "eval.builtin.schema-validate.output.v1");

    private static readonly EvaluatorRegistryEntry ExactCompareEntry = CreateEntry(
        "eval.builtin.exact-compare",
        EvaluatorOperations.ExactCompare,
        "eval.builtin.exact-compare.input.v1",
        "eval.builtin.exact-compare.output.v1");

    private static readonly EvaluatorRegistryEntry CitationValidateEntry = CreateEntry(
        "eval.builtin.citation-validate",
        EvaluatorOperations.CitationValidate,
        "eval.builtin.citation-validate.input.v1",
        "eval.builtin.citation-validate.output.v1");

    private static readonly EvaluatorRegistryEntry RubricAggregateEntry = CreateEntry(
        "eval.builtin.rubric-aggregate",
        EvaluatorOperations.RubricAggregate,
        "eval.builtin.rubric-aggregate.input.v1",
        "eval.builtin.rubric-aggregate.output.v1");

    public static string BoundedCalcEvaluatorDigest => BoundedCalcEntry.EvaluatorDigest;

    public static string BoundedCalcConfigurationDigest => BoundedCalcEntry.ConfigurationDigest;

    public static string BoundedCalcDependencyDigest => BoundedCalcEntry.DependencyDigest;

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
        string outputSchemaId)
    {
        var entry = new EvaluatorRegistryEntry(
            evaluatorId,
            $"{evaluatorId}.v1",
            string.Empty,
            operation,
            inputSchemaId,
            outputSchemaId,
            "jcs-sha256-v1",
            string.Empty,
            string.Empty,
            "PT5S",
            "PT10S",
            16_777_216,
            4096,
            "prohibited",
            "prohibited",
            EvaluatorQualificationStates.Qualified);

        return BuiltinEvaluatorIdentityDigester.WithComputedDigests(
            entry,
            BuiltinEvaluatorImplementationBinding.Identity);
    }
}

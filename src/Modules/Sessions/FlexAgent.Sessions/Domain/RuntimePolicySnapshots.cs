using System.Collections.Immutable;

namespace FlexAgent.Sessions.Domain;

internal static class RuntimePolicySnapshots
{
    internal static ImmutableArray<RuntimeTriggerDescriptor> CopyTriggers(
        IReadOnlyList<RuntimeTriggerDescriptor> triggers) =>
        triggers
            .Select(static trigger => new RuntimeTriggerDescriptor(trigger.TriggerFamily, trigger.TriggerType))
            .ToImmutableArray();

    internal static ImmutableArray<string> CopyStrings(IReadOnlyList<string> values) =>
        values.ToImmutableArray();

    internal static ImmutableArray<DecisionTypeSchemaBinding> CopySchemaBindings(
        IReadOnlyList<DecisionTypeSchemaBinding> bindings) =>
        bindings
            .Select(static binding => new DecisionTypeSchemaBinding(binding.DecisionType, binding.SchemaVersion))
            .ToImmutableArray();
}

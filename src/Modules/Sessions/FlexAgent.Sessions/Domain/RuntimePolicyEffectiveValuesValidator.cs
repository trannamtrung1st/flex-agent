namespace FlexAgent.Sessions.Domain;

internal static class RuntimePolicyEffectiveValuesValidator
{
    internal static bool HasRequiredCommunicationPolicy(RuntimePolicyEffectiveValues values) =>
        values.AgentInitiatedOpeningPermitted is not null
        && values.AgentInitiatedClosingPermitted is not null
        && values.NoActionPermitted is not null;

    internal static bool HasRequiredStreamingPublicationBounds(RuntimePolicyEffectiveValues values) =>
        values.StreamingPublicationBounds is not null;

    internal static bool HasRequiredFreezeInputs(RuntimePolicyEffectiveValues values) =>
        HasRequiredCommunicationPolicy(values)
        && HasRequiredStreamingPublicationBounds(values);
}

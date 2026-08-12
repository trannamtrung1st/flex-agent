namespace FlexAgent.Sessions.Domain;

internal static class RuntimePolicyEffectiveValuesValidator
{
    internal static bool HasRequiredCommunicationPolicy(RuntimePolicyEffectiveValues values) =>
        values.AgentInitiatedOpeningPermitted is not null
        && values.AgentInitiatedClosingPermitted is not null
        && values.NoActionPermitted is not null;
}

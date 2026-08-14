namespace FlexAgent.Sessions.Domain;

internal static class P0DecisionProfileValidator
{
    internal static P0ProfileValidation Validate(
        EnvelopeRecommendation envelope,
        FrozenTextSessionRuntimePolicy policy,
        Func<string, bool> isDecisionTypeSupportedByP0)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(policy);

        var outputs = new List<OutputItemValidation>(envelope.Outputs.Count);
        var acceptedMessageCount = 0;
        var isNoAction = string.Equals(envelope.Disposition, DecisionDispositions.NoAction, StringComparison.Ordinal);
        foreach (var output in envelope.Outputs)
        {
            if (isNoAction)
            {
                outputs.Add(RejectOutput(output, RejectionReasonCategories.PolicyProhibited));
                continue;
            }

            outputs.Add(ValidateOutput(output, envelope.Outputs, ref acceptedMessageCount));
        }

        var actions = new List<RequestedActionItemValidation>(envelope.RequestedActions.Count);
        var acceptedTimerCount = 0;
        foreach (var action in envelope.RequestedActions)
        {
            actions.Add(ValidateAction(action, policy, isDecisionTypeSupportedByP0, ref acceptedTimerCount));
        }

        var timerOutcome = acceptedTimerCount > 0
            ? TimerValidationOutcomes.Accepted
            : envelope.RequestedActions.Any(action =>
                string.Equals(action.Kind, AgentRequestedActionKinds.NextTimerRequest, StringComparison.Ordinal))
                ? TimerValidationOutcomes.Rejected
                : TimerValidationOutcomes.NotPresent;

        if (isNoAction && !policy.NoActionPermitted)
        {
            return new P0ProfileValidation(
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.PolicyProhibited,
                outputs,
                actions,
                timerOutcome);
        }

        if (!isNoAction && acceptedMessageCount == 0)
        {
            return new P0ProfileValidation(
                DecisionValidationOutcomes.Rejected,
                RejectionReasonCategories.PayloadInvalid,
                outputs,
                actions,
                timerOutcome);
        }

        return new P0ProfileValidation(
            DecisionValidationOutcomes.Accepted,
            null,
            outputs,
            actions,
            timerOutcome);
    }

    private static OutputItemValidation ValidateOutput(
        OutputRecommendation output,
        IReadOnlyList<OutputRecommendation> siblings,
        ref int acceptedMessageCount)
    {
        if (string.Equals(output.Kind, AgentOutputKinds.Voice, StringComparison.Ordinal))
        {
            return RejectOutput(output, RejectionReasonCategories.CapabilityDisabled);
        }

        if (!string.Equals(output.Kind, AgentOutputKinds.Message, StringComparison.Ordinal))
        {
            return RejectOutput(output, RejectionReasonCategories.CapabilityDisabled);
        }

        if (!string.IsNullOrWhiteSpace(output.ModelAgentOutputId))
        {
            return RejectOutput(output, RejectionReasonCategories.PayloadInvalid);
        }

        if (!string.IsNullOrWhiteSpace(output.ModelAudience)
            && !string.Equals(output.ModelAudience, AgentOutputAudiences.Participant, StringComparison.Ordinal))
        {
            return RejectOutput(output, RejectionReasonCategories.PolicyProhibited);
        }

        if (string.IsNullOrWhiteSpace(output.CommunicationPurpose))
        {
            return RejectOutput(output, RejectionReasonCategories.PayloadInvalid);
        }

        var referenceRejection = ResolveLocalReferences(output, siblings);
        if (referenceRejection is not null)
        {
            return RejectOutput(output, referenceRejection);
        }

        if (acceptedMessageCount > 0)
        {
            return RejectOutput(output, RejectionReasonCategories.PolicyProhibited);
        }

        acceptedMessageCount++;
        return new OutputItemValidation(
            output.LocalRef,
            output.Kind,
            DecisionValidationOutcomes.Accepted,
            null,
            AllocateOutputId());
    }

    private static string? ResolveLocalReferences(
        OutputRecommendation output,
        IReadOnlyList<OutputRecommendation> siblings)
    {
        if (output.References is null || output.References.Count == 0)
        {
            return null;
        }

        foreach (var reference in output.References)
        {
            if (string.Equals(reference.LocalRef, output.LocalRef, StringComparison.Ordinal))
            {
                return RejectionReasonCategories.PayloadInvalid;
            }

            var matches = 0;
            foreach (var sibling in siblings)
            {
                if (string.Equals(sibling.LocalRef, reference.LocalRef, StringComparison.Ordinal))
                {
                    matches++;
                }
            }

            if (matches != 1)
            {
                return RejectionReasonCategories.PayloadInvalid;
            }
        }

        return RejectionReasonCategories.PolicyProhibited;
    }

    private static RequestedActionItemValidation ValidateAction(
        RequestedActionRecommendation action,
        FrozenTextSessionRuntimePolicy policy,
        Func<string, bool> isDecisionTypeSupportedByP0,
        ref int acceptedTimerCount)
    {
        if (string.Equals(action.Kind, AgentRequestedActionKinds.NextTimerRequest, StringComparison.Ordinal))
        {
            if (acceptedTimerCount > 0
                || policy.TimerLane is not { IsEnabled: true }
                || action.RelativeDelay is null
                || action.ExpectedScheduleRevision is null
                || !Iso8601PositiveDuration.TryParse(action.RelativeDelay, out var delay)
                || delay.CompareTo(policy.TimerLane.MinRequestedDelay) < 0
                || delay.CompareTo(policy.TimerLane.MaxRequestedDelay) > 0)
            {
                return RejectAction(action, RejectionReasonCategories.PolicyProhibited);
            }

            acceptedTimerCount++;
            return new RequestedActionItemValidation(
                action.LocalRef,
                action.Kind,
                DecisionValidationOutcomes.Accepted,
                null);
        }

        if (!isDecisionTypeSupportedByP0(action.Kind))
        {
            return RejectAction(action, RejectionReasonCategories.CapabilityDisabled);
        }

        return RejectAction(action, RejectionReasonCategories.PolicyProhibited);
    }

    private static OutputItemValidation RejectOutput(OutputRecommendation output, string reason) =>
        new(output.LocalRef, output.Kind, DecisionValidationOutcomes.Rejected, reason, null);

    private static RequestedActionItemValidation RejectAction(RequestedActionRecommendation action, string reason) =>
        new(action.LocalRef, action.Kind, DecisionValidationOutcomes.Rejected, reason);

    private static string AllocateOutputId() => $"aout.{Guid.NewGuid():N}"[..21];
}

internal sealed record P0ProfileValidation(
    string CommunicationOutcome,
    string? CommunicationRejectionReason,
    IReadOnlyList<OutputItemValidation> Outputs,
    IReadOnlyList<RequestedActionItemValidation> RequestedActions,
    string TimerValidationOutcome);

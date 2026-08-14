namespace FlexAgent.Sessions.Domain;

internal static class HistoricalDecisionEnvelopeMapper
{
    internal const string HistoricalMessageLocalRef = "out.message.primary";
    internal const string HistoricalTimerLocalRef = "act.timer.primary";
    internal const string HistoricalDeferredActionLocalRef = "act.deferred.primary";

    internal static EnvelopeRecommendation ToEnvelope(DecisionRecommendation recommendation)
    {
        ArgumentNullException.ThrowIfNull(recommendation);
        if (recommendation is EnvelopeRecommendation envelope)
        {
            return envelope;
        }

        var timerActions = MapTimer(recommendation.NextTimer);
        return recommendation switch
        {
            EmitMessageRecommendation emit => new EnvelopeRecommendation(
                emit.DecisionId,
                emit.InvocationId,
                emit.ProducedAt,
                DecisionDispositions.Respond,
                [
                    new OutputRecommendation(
                        AgentOutputKinds.Message,
                        HistoricalMessageLocalRef,
                        emit.CommunicationPurpose,
                        emit.TurnId,
                        emit.ResponseSlotId),
                ],
                timerActions),
            NoActionRecommendation noAction => new EnvelopeRecommendation(
                noAction.DecisionId,
                noAction.InvocationId,
                noAction.ProducedAt,
                DecisionDispositions.NoAction,
                [],
                timerActions,
                noAction.ReasonCategory),
            ProhibitedDecisionRecommendation prohibited => new EnvelopeRecommendation(
                prohibited.DecisionId,
                prohibited.InvocationId,
                prohibited.ProducedAt,
                DecisionDispositions.Respond,
                [],
                ConcatAction(
                    new RequestedActionRecommendation(
                        prohibited.DecisionType,
                        HistoricalDeferredActionLocalRef),
                    timerActions)),
            _ => throw new InvalidOperationException($"Unsupported Decision recommendation '{recommendation.DecisionType}'."),
        };
    }

    private static IReadOnlyList<RequestedActionRecommendation> MapTimer(NextTimerRecommendation? nextTimer)
    {
        if (nextTimer is null)
        {
            return [];
        }

        return
        [
            new RequestedActionRecommendation(
                AgentRequestedActionKinds.NextTimerRequest,
                HistoricalTimerLocalRef,
                nextTimer.RelativeDelay,
                nextTimer.ExpectedScheduleRevision),
        ];
    }

    private static IReadOnlyList<RequestedActionRecommendation> ConcatAction(
        RequestedActionRecommendation first,
        IReadOnlyList<RequestedActionRecommendation> rest)
    {
        var actions = new List<RequestedActionRecommendation>(rest.Count + 1) { first };
        actions.AddRange(rest);
        return actions;
    }
}

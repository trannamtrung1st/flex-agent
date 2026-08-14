namespace FlexAgent.Contracts.Session;

public static class AgentDecisionEnvelopeMapper
{
    public const string EnvelopeSchemaVersion = "v2";
    public const string HistoricalMessageLocalRef = "out.message.primary";
    public const string HistoricalTimerLocalRef = "act.timer.primary";
    public const string HistoricalDeferredActionLocalRef = "act.deferred.primary";

    public static AgentDecisionEnvelopeV2 FromV1(IAgentDecisionV1 decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        var requestedActions = MapRequestedActions(decision);
        return decision switch
        {
            EmitMessageAgentDecisionV1 emit => new AgentDecisionEnvelopeV2(
                EnvelopeSchemaVersion,
                emit.AgentDecisionId,
                emit.AgentInvocationId,
                emit.ProducedAt,
                DecisionDispositionV2.Respond,
                [
                    new AgentOutputRecommendationV2(
                        AgentOutputKindV2.Message,
                        HistoricalMessageLocalRef,
                        emit.EmitMessage.CommunicationPurpose,
                        emit.EmitMessage.TurnId,
                        emit.EmitMessage.ResponseSlotId),
                ],
                requestedActions,
                PayloadRef: emit.PayloadRef),
            NoActionAgentDecisionV1 noAction => new AgentDecisionEnvelopeV2(
                EnvelopeSchemaVersion,
                noAction.AgentDecisionId,
                noAction.AgentInvocationId,
                noAction.ProducedAt,
                DecisionDispositionV2.NoAction,
                [],
                requestedActions,
                noAction.NoAction,
                noAction.PayloadRef),
            RequestToolAgentDecisionV1 requestTool => MapDeferred(
                requestTool,
                AgentRequestedActionKindV2.RequestTool,
                requestedActions),
            ProposeTransitionAgentDecisionV1 propose => MapDeferred(
                propose,
                AgentRequestedActionKindV2.ProposeTransition,
                requestedActions),
            EscalateAgentDecisionV1 escalate => MapDeferred(
                escalate,
                AgentRequestedActionKindV2.Escalate,
                requestedActions),
            _ => throw new InvalidOperationException($"Unsupported historical Decision type '{decision.DecisionType}'."),
        };
    }

    private static AgentDecisionEnvelopeV2 MapDeferred(
        IAgentDecisionV1 decision,
        AgentRequestedActionKindV2 kind,
        IReadOnlyList<AgentRequestedActionV2> timerActions)
    {
        var actions = new List<AgentRequestedActionV2>(timerActions.Count + 1)
        {
            new(kind, HistoricalDeferredActionLocalRef),
        };
        actions.AddRange(timerActions);
        return new AgentDecisionEnvelopeV2(
            EnvelopeSchemaVersion,
            decision.AgentDecisionId,
            decision.AgentInvocationId,
            decision.ProducedAt,
            DecisionDispositionV2.Respond,
            [],
            actions,
            PayloadRef: decision.PayloadRef);
    }

    private static IReadOnlyList<AgentRequestedActionV2> MapRequestedActions(IAgentDecisionV1 decision)
    {
        if (decision.NextTimerRequest is null)
        {
            return [];
        }

        return
        [
            new AgentRequestedActionV2(
                AgentRequestedActionKindV2.NextTimerRequest,
                HistoricalTimerLocalRef,
                decision.NextTimerRequest.RelativeDelay,
                decision.NextTimerRequest.ExpectedScheduleRevision),
        ];
    }
}

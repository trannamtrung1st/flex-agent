namespace FlexAgent.Sessions.Domain;

public static class DecisionDispositions
{
    public const string Respond = "respond";
    public const string NoAction = "no_action";
}

public static class AgentOutputKinds
{
    public const string Message = "message";
    public const string Voice = "voice";
}

public static class AgentRequestedActionKinds
{
    public const string NextTimerRequest = "next_timer_request";
    public const string RequestTool = "request_tool";
    public const string ProposeTransition = "propose_transition";
    public const string Escalate = "escalate";
}

public static class AgentOutputAudiences
{
    public const string Participant = "participant";
    public const string Reviewer = "reviewer";
    public const string Administrator = "administrator";
    public const string RuntimeOnly = "runtime_only";
}

public static class EnvelopeParseOutcomeCodes
{
    public const string Succeeded = "envelope_parse.succeeded";
    public const string MalformedControl = "envelope_parse.malformed_control";
    public const string IncompleteControl = "envelope_parse.incomplete_control";
}

public sealed record OutputLocalReference(string Relation, string LocalRef);

public sealed record OutputRecommendation(
    string Kind,
    string LocalRef,
    string? CommunicationPurpose = null,
    string? TurnId = null,
    string? ResponseSlotId = null,
    string? ModelAgentOutputId = null,
    string? ModelAudience = null,
    IReadOnlyList<OutputLocalReference>? References = null,
    ProtectedContentRef? PayloadRef = null);

public sealed record RequestedActionRecommendation(
    string Kind,
    string LocalRef,
    string? RelativeDelay = null,
    string? ExpectedScheduleRevision = null);

public sealed record EnvelopeRecommendation(
    string DecisionId,
    string InvocationId,
    DateTimeOffset ProducedAt,
    string Disposition,
    IReadOnlyList<OutputRecommendation> Outputs,
    IReadOnlyList<RequestedActionRecommendation> RequestedActions,
    string? NoActionReasonCategory = null)
    : DecisionRecommendation(
        DecisionId,
        InvocationId,
        ProducedAt,
        EnvelopeRecommendationMapping.DeriveNextTimer(RequestedActions),
        EnvelopeRecommendationMapping.DeriveDecisionType(Disposition));

public static class EnvelopeRecommendationMapping
{
    public static NextTimerRecommendation? DeriveNextTimer(
        IReadOnlyList<RequestedActionRecommendation> requestedActions)
    {
        var timer = requestedActions.FirstOrDefault(action =>
            string.Equals(action.Kind, AgentRequestedActionKinds.NextTimerRequest, StringComparison.Ordinal));
        if (timer?.RelativeDelay is null || timer.ExpectedScheduleRevision is null)
        {
            return null;
        }

        return new NextTimerRecommendation(timer.RelativeDelay, timer.ExpectedScheduleRevision);
    }

    public static string DeriveDecisionType(string disposition) =>
        string.Equals(disposition, DecisionDispositions.NoAction, StringComparison.Ordinal)
            ? RuntimeDecisionTypes.NoAction
            : RuntimeDecisionTypes.EmitMessage;
}

public sealed record OutputItemValidation(
    string LocalRef,
    string Kind,
    string ValidationOutcome,
    string? RejectionReasonCategory,
    string? AgentOutputId);

public sealed record RequestedActionItemValidation(
    string LocalRef,
    string Kind,
    string ValidationOutcome,
    string? RejectionReasonCategory);

public sealed record EnvelopeParseResult(
    bool Succeeded,
    string OutcomeCode,
    string? FailureReasonCategory,
    EnvelopeRecommendation? Envelope);

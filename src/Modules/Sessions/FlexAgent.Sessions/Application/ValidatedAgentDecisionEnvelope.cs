using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed class ValidatedAgentDecisionEnvelope
{
    private ValidatedAgentDecisionEnvelope(EnvelopeRecommendation envelope)
    {
        Envelope = envelope;
    }

    public EnvelopeRecommendation Envelope { get; }

    public static bool TryAdmit(
        ReadOnlySpan<byte> utf8Json,
        out ValidatedAgentDecisionEnvelope? admitted,
        out string failureReasonCategory)
    {
        var parsed = AgentDecisionEnvelopeReader.Read(utf8Json);
        if (!parsed.Succeeded || parsed.Envelope is null)
        {
            admitted = null;
            failureReasonCategory = parsed.FailureReasonCategory ?? ExecutionFailureReasons.MalformedControl;
            return false;
        }

        admitted = new ValidatedAgentDecisionEnvelope(parsed.Envelope);
        failureReasonCategory = string.Empty;
        return true;
    }
}

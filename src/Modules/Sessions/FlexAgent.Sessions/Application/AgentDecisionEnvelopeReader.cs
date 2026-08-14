using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public static class AgentDecisionEnvelopeReader
{
    public static EnvelopeParseResult Read(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
        {
            return new EnvelopeParseResult(
                false,
                EnvelopeParseOutcomeCodes.IncompleteControl,
                ExecutionFailureReasons.IncompleteControl,
                null);
        }

        if (!AgentDecisionV2SchemaValidator.IsSchemaValid(utf8Json))
        {
            return new EnvelopeParseResult(
                false,
                EnvelopeParseOutcomeCodes.MalformedControl,
                ExecutionFailureReasons.MalformedControl,
                null);
        }

        var parsed = AgentDecisionEnvelopeParser.Parse(utf8Json);
        if (!parsed.Succeeded || parsed.Envelope is null)
        {
            return new EnvelopeParseResult(
                false,
                EnvelopeParseOutcomeCodes.MalformedControl,
                ExecutionFailureReasons.MalformedControl,
                null);
        }

        return parsed;
    }
}

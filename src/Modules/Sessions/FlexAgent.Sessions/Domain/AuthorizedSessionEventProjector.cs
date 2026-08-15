using System.Globalization;

namespace FlexAgent.Sessions.Domain;

public static class AuthorizedSessionEventProjector
{
    public static AuthorizedSessionEventReplayResult Project(SessionRuntime session, long afterSequence)
    {
        ArgumentNullException.ThrowIfNull(session);

        foreach (var message in session.AgentMessages)
        {
            if (message.IsTerminal && message.SealedSessionSequence is null)
            {
                return new AuthorizedSessionEventReplayResult(
                    false,
                    SessionEventReplayOutcomeCodes.Reconcile,
                    []);
            }
        }

        var events = new List<AuthorizedSessionProjectionEvent>();
        var sessionId = session.Ownership.SessionId.ToString("D");
        foreach (var message in session.AgentMessages)
        {
            foreach (var fragment in message.Fragments)
            {
                if (fragment.SessionSequence <= afterSequence)
                {
                    continue;
                }

                events.Add(new AuthorizedSessionProjectionEvent(
                    AuthorizedSessionEventTypes.AgentFragment,
                    sessionId,
                    fragment.SessionSequence.ToString(CultureInfo.InvariantCulture),
                    FormatUtc(fragment.CommittedAt),
                    "Agent response fragment published.",
                    fragment.FragmentOrdinal,
                    message.MessageId,
                    fragment.ExactUtf8Text));
            }

            if (message.IsTerminal
                && message.SealedSessionSequence is { } sealSequence
                && sealSequence > afterSequence)
            {
                events.Add(new AuthorizedSessionProjectionEvent(
                    AuthorizedSessionEventTypes.AgentComplete,
                    sessionId,
                    sealSequence.ToString(CultureInfo.InvariantCulture),
                    FormatUtc(message.SealedAt ?? session.LastCommittedAt),
                    message.CompletionState == AgentMessageCompletionStates.Complete
                        ? "Agent response complete."
                        : "Agent response incomplete.",
                    AgentMessageId: message.MessageId,
                    AssembledContentDigest: message.AssembledContentDigest,
                    FragmentCount: message.Fragments.Count));
            }
        }

        events.Sort(static (left, right) =>
            long.Parse(left.SessionSequence, CultureInfo.InvariantCulture)
                .CompareTo(long.Parse(right.SessionSequence, CultureInfo.InvariantCulture)));

        var pageSize = session.Binding.Policy.StreamingPublicationBounds.MaxFragmentCountPerMessage
            * Math.Max(1, session.Binding.Policy.StreamingPublicationBounds.MaxInFlightStreamsPerSession);
        var hasMore = events.Count > pageSize;
        if (hasMore)
        {
            events = events.Take(pageSize).ToList();
        }

        return new AuthorizedSessionEventReplayResult(
            true,
            SessionEventReplayOutcomeCodes.Succeeded,
            events,
            hasMore);
    }

    private static string FormatUtc(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        if (utc.Ticks % TimeSpan.TicksPerSecond == 0)
        {
            return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture);
    }
}

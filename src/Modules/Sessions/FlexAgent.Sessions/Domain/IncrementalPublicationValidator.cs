using System.Text;

namespace FlexAgent.Sessions.Domain;

internal static class IncrementalPublicationValidator
{
    private static readonly string[] ProhibitedMarkup =
    [
        "<script",
        "javascript:",
        "onerror=",
        "onload=",
        "onclick=",
        "<iframe",
        "<object",
        "<embed",
    ];

    internal static string? RejectDelta(
        string exactUtf8Text,
        string assembledPrefix,
        StreamingPublicationBounds bounds,
        IReadOnlyList<AgentResponseMessage> messages,
        AgentResponseMessage? existing,
        DateTimeOffset authoritativeUtc)
    {
        if (!IsRecordableText(exactUtf8Text))
        {
            return FragmentCommitOutcomeCodes.ValidationFailed;
        }

        var assembled = assembledPrefix + exactUtf8Text;
        if (ContainsProhibitedMarkup(assembled))
        {
            return FragmentCommitOutcomeCodes.ValidationFailed;
        }

        var fragmentBytes = Encoding.UTF8.GetByteCount(exactUtf8Text);
        if (fragmentBytes > bounds.MaxFragmentUtf8Bytes)
        {
            return FragmentCommitOutcomeCodes.FragmentTooLarge;
        }

        var nextCount = (existing?.Fragments.Count ?? 0) + 1;
        if (nextCount > bounds.MaxFragmentCountPerMessage)
        {
            return FragmentCommitOutcomeCodes.FragmentCountExceeded;
        }

        if (Encoding.UTF8.GetByteCount(assembled) > bounds.MaxAssembledResponseUtf8Bytes)
        {
            return FragmentCommitOutcomeCodes.AssembledSizeExceeded;
        }

        if (existing is null)
        {
            var openStreams = 0;
            foreach (var message in messages)
            {
                if (message.CompletionState == AgentMessageCompletionStates.Open)
                {
                    openStreams++;
                }
            }

            if (openStreams >= bounds.MaxInFlightStreamsPerSession)
            {
                return FragmentCommitOutcomeCodes.InFlightExceeded;
            }
        }

        var windowStart = authoritativeUtc - TimeSpan.FromSeconds(1);
        var recent = 0;
        foreach (var message in messages)
        {
            foreach (var fragment in message.Fragments)
            {
                if (fragment.CommittedAt > windowStart && fragment.CommittedAt <= authoritativeUtc)
                {
                    recent++;
                }
            }
        }

        if (recent + 1 > bounds.MaxFragmentsPerSecond)
        {
            return FragmentCommitOutcomeCodes.RateExceeded;
        }

        return null;
    }

    internal static bool IsRecordableAssembled(string assembled) =>
        IsRecordableText(assembled) && !ContainsProhibitedMarkup(assembled);

    private static bool IsRecordableText(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            if (char.IsSurrogate(ch))
            {
                if (!char.IsHighSurrogate(ch)
                    || index + 1 >= text.Length
                    || !char.IsLowSurrogate(text[index + 1]))
                {
                    return false;
                }

                index++;
                continue;
            }

            if (ch is '\t' or '\n' or '\r')
            {
                continue;
            }

            if (char.IsControl(ch) || ch == '\u007f')
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsProhibitedMarkup(string assembled)
    {
        foreach (var token in ProhibitedMarkup)
        {
            if (assembled.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

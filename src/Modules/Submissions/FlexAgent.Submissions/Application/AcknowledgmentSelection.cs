namespace FlexAgent.Submissions.Application;

public static class AcknowledgmentSelection
{
    public static IReadOnlyList<CurrentAcknowledgmentFact> CurrentExact(
        IReadOnlyList<CurrentAcknowledgmentFact> records,
        IReadOnlyList<RequiredNoticeProjection> notices)
    {
        if (notices.Count == 0)
        {
            return [];
        }

        var selected = new List<CurrentAcknowledgmentFact>(notices.Count);
        foreach (var notice in notices)
        {
            var match = records
                .Where(item =>
                    item.NoticeId == notice.NoticeId
                    && item.SourceVersionId == notice.SourceVersionId
                    && item.BoundAttemptId is null
                    && string.Equals(item.ContentDigest, notice.ContentDigest, StringComparison.Ordinal))
                .OrderByDescending(item => item.RecordedAtUtc)
                .ThenByDescending(item => item.RecordId)
                .FirstOrDefault();
            if (match is not null)
            {
                selected.Add(match);
            }
        }

        return selected;
    }

    public static IReadOnlyList<CurrentAcknowledgmentFact> CurrentBindable(
        IReadOnlyList<CurrentAcknowledgmentFact> records,
        IReadOnlyList<RequiredNoticeProjection> notices)
    {
        if (notices.Count == 0)
        {
            return [];
        }

        var selected = new List<CurrentAcknowledgmentFact>(notices.Count);
        foreach (var notice in notices)
        {
            var match = records
                .Where(item =>
                    item.NoticeId == notice.NoticeId
                    && item.SourceVersionId == notice.SourceVersionId
                    && item.BoundAttemptId is null
                    && string.Equals(item.ContentDigest, notice.ContentDigest, StringComparison.Ordinal)
                    && string.Equals(item.Outcome, notice.RequiredOutcome, StringComparison.Ordinal))
                .OrderByDescending(item => item.RecordedAtUtc)
                .ThenByDescending(item => item.RecordId)
                .FirstOrDefault();
            if (match is not null)
            {
                selected.Add(match);
            }
        }

        return selected;
    }
}

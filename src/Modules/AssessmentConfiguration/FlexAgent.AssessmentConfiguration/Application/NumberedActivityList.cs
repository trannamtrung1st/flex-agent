using FlexAgent.AssessmentConfiguration.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public enum ActivityListSortField
{
    Title,
    Activation,
    Updated,
    Revision,
}

public enum ActivityListSortDirection
{
    Asc,
    Desc,
}

public sealed record ActivityListSortTerm(string Field, string Direction);

public sealed record ActivityListSortEntry(ActivityListSortField Field, ActivityListSortDirection Direction);

public sealed record NumberedActivityListRequest(
    int? Page,
    int? PageSize,
    string? Search,
    IReadOnlyList<ActivityListSortTerm>? Sort);

public sealed record NumberedActivityListQuery(
    int Page,
    int PageSize,
    string Search,
    IReadOnlyList<ActivityListSortEntry> Sort)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 16;
    public const int MaximumPageSize = 50;
    public const int MaximumSearchLength = 200;
    public const int MaximumSortEntries = 4;
}

public sealed record NumberedActivityListPage(
    IReadOnlyList<ActivityDraft> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public static class NumberedActivityListSpecification
{
    public static AssessmentDecision<NumberedActivityListQuery> TryCreate(NumberedActivityListRequest request)
    {
        var page = request.Page ?? NumberedActivityListQuery.DefaultPage;
        var pageSize = request.PageSize ?? NumberedActivityListQuery.DefaultPageSize;
        if (page < 1 || pageSize < 1 || pageSize > NumberedActivityListQuery.MaximumPageSize)
        {
            return AssessmentDecision<NumberedActivityListQuery>.Fail(AssessmentFailureCodes.InvalidField);
        }

        var search = request.Search?.Trim() ?? string.Empty;
        if (search.Length > NumberedActivityListQuery.MaximumSearchLength)
        {
            return AssessmentDecision<NumberedActivityListQuery>.Fail(AssessmentFailureCodes.InvalidField);
        }

        if (!TryParseSort(request.Sort, out var sort))
        {
            return AssessmentDecision<NumberedActivityListQuery>.Fail(AssessmentFailureCodes.InvalidField);
        }

        return AssessmentDecision<NumberedActivityListQuery>.Ok(
            new NumberedActivityListQuery(page, pageSize, search, sort));
    }

    private static bool TryParseSort(
        IReadOnlyList<ActivityListSortTerm>? terms,
        out IReadOnlyList<ActivityListSortEntry> sort)
    {
        sort = [new ActivityListSortEntry(ActivityListSortField.Title, ActivityListSortDirection.Asc)];
        if (terms is null || terms.Count == 0)
        {
            return true;
        }

        if (terms.Count > NumberedActivityListQuery.MaximumSortEntries)
        {
            return false;
        }

        var seen = new HashSet<ActivityListSortField>();
        var entries = new List<ActivityListSortEntry>(terms.Count);
        foreach (var term in terms)
        {
            if (!TryParseField(term.Field, out var field)
                || !TryParseDirection(term.Direction, out var direction)
                || !seen.Add(field))
            {
                return false;
            }

            entries.Add(new ActivityListSortEntry(field, direction));
        }

        sort = entries;
        return true;
    }

    private static bool TryParseField(string? value, out ActivityListSortField field)
    {
        field = default;
        if (string.Equals(value, "title", StringComparison.Ordinal))
        {
            field = ActivityListSortField.Title;
            return true;
        }

        if (string.Equals(value, "activation", StringComparison.Ordinal))
        {
            field = ActivityListSortField.Activation;
            return true;
        }

        if (string.Equals(value, "updated", StringComparison.Ordinal))
        {
            field = ActivityListSortField.Updated;
            return true;
        }

        if (string.Equals(value, "revision", StringComparison.Ordinal))
        {
            field = ActivityListSortField.Revision;
            return true;
        }

        return false;
    }

    private static bool TryParseDirection(string? value, out ActivityListSortDirection direction)
    {
        direction = default;
        if (string.Equals(value, "asc", StringComparison.Ordinal))
        {
            direction = ActivityListSortDirection.Asc;
            return true;
        }

        if (string.Equals(value, "desc", StringComparison.Ordinal))
        {
            direction = ActivityListSortDirection.Desc;
            return true;
        }

        return false;
    }
}

public static class NumberedActivityListQuerying
{
    public static NumberedActivityListPage Page(IEnumerable<ActivityDraft> drafts, NumberedActivityListQuery query)
    {
        var matching = drafts
            .Where(draft => Matches(draft, query.Search))
            .Order(new ActivityListComparer(query.Sort))
            .ToArray();
        var totalItems = matching.Length;
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)query.PageSize);
        var offset = (query.Page - 1) * query.PageSize;
        var items = offset >= matching.Length
            ? Array.Empty<ActivityDraft>()
            : matching.Skip(offset).Take(query.PageSize).ToArray();
        return new NumberedActivityListPage(items, query.Page, query.PageSize, totalItems, totalPages);
    }

    public static bool Matches(ActivityDraft draft, string search)
    {
        if (search.Length == 0)
        {
            return true;
        }

        return draft.Content.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
            || draft.ActivityId.ToString("D").Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    public static string LiteralContainsPattern(string search)
    {
        var escaped = search
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return "%" + escaped + "%";
    }

    private sealed class ActivityListComparer(IReadOnlyList<ActivityListSortEntry> sort) : IComparer<ActivityDraft>
    {
        public int Compare(ActivityDraft? left, ActivityDraft? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            foreach (var entry in sort)
            {
                var comparison = CompareField(left, right, entry.Field);
                if (comparison != 0)
                {
                    return entry.Direction == ActivityListSortDirection.Desc ? -comparison : comparison;
                }
            }

            return left.ActivityId.CompareTo(right.ActivityId);
        }

        private static int CompareField(ActivityDraft left, ActivityDraft right, ActivityListSortField field) =>
            field switch
            {
                ActivityListSortField.Title => string.Compare(
                    left.Content.Title,
                    right.Content.Title,
                    StringComparison.OrdinalIgnoreCase),
                ActivityListSortField.Activation => left.HasActivatedCohort.CompareTo(right.HasActivatedCohort),
                ActivityListSortField.Updated => left.UpdatedAtUtc.CompareTo(right.UpdatedAtUtc),
                ActivityListSortField.Revision => left.RevisionNumber.CompareTo(right.RevisionNumber),
                _ => 0,
            };
    }
}

namespace FlexAgent.Sessions.Domain;

public readonly record struct DurableWorkClaimPartitionKey(Guid OrganizationId, Guid ActivityId);

public sealed record DurableWorkClaimCandidate(
    DurableWorkClaimPartitionKey Partition,
    Guid WorkId,
    DateTimeOffset WaitingSince);

public static class DurableWorkFairClaimSelector
{
    public static DurableWorkClaimCandidate? SelectHead(
        IReadOnlyList<DurableWorkClaimCandidate> claimable,
        IReadOnlyDictionary<DurableWorkClaimPartitionKey, DateTimeOffset> lastServedByPartition)
    {
        ArgumentNullException.ThrowIfNull(claimable);
        ArgumentNullException.ThrowIfNull(lastServedByPartition);
        if (claimable.Count == 0)
        {
            return null;
        }

        return claimable
            .GroupBy(candidate => candidate.Partition)
            .Select(group => group
                .OrderBy(candidate => candidate.WaitingSince)
                .ThenBy(candidate => candidate.WorkId)
                .First())
            .OrderBy(head => lastServedByPartition.TryGetValue(head.Partition, out var served)
                ? served
                : DateTimeOffset.MinValue)
            .ThenBy(head => head.WaitingSince)
            .ThenBy(head => head.WorkId)
            .First();
    }
}

using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class DurableWorkFairClaimSelectorTests
{
    [Fact]
    public void After_completing_the_oldest_partition_the_next_claim_serves_another_waiting_partition()
    {
        var orgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var orgB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var activityA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var activityB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var a1 = new DurableWorkClaimCandidate(
            new DurableWorkClaimPartitionKey(orgA, activityA),
            Guid.Parse("00000000-0000-0000-0000-0000000000a1"),
            DateTimeOffset.UnixEpoch.AddSeconds(1));
        var a2 = new DurableWorkClaimCandidate(
            new DurableWorkClaimPartitionKey(orgA, activityA),
            Guid.Parse("00000000-0000-0000-0000-0000000000a2"),
            DateTimeOffset.UnixEpoch.AddSeconds(2));
        var b1 = new DurableWorkClaimCandidate(
            new DurableWorkClaimPartitionKey(orgB, activityB),
            Guid.Parse("00000000-0000-0000-0000-0000000000b1"),
            DateTimeOffset.UnixEpoch.AddSeconds(3));

        var first = DurableWorkFairClaimSelector.SelectHead(
            [a1, a2, b1],
            new Dictionary<DurableWorkClaimPartitionKey, DateTimeOffset>());
        Assert.Equal(a1.WorkId, first!.WorkId);

        var second = DurableWorkFairClaimSelector.SelectHead(
            [a2, b1],
            new Dictionary<DurableWorkClaimPartitionKey, DateTimeOffset>
            {
                [a1.Partition] = DateTimeOffset.UnixEpoch.AddSeconds(10),
            });

        Assert.Equal(b1.WorkId, second!.WorkId);
    }

    [Fact]
    public void Same_partition_keeps_oldest_waiting_work_first()
    {
        var partition = new DurableWorkClaimPartitionKey(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var older = new DurableWorkClaimCandidate(partition, Guid.Parse("00000000-0000-0000-0000-000000000001"), DateTimeOffset.UnixEpoch);
        var newer = new DurableWorkClaimCandidate(partition, Guid.Parse("00000000-0000-0000-0000-000000000002"), DateTimeOffset.UnixEpoch.AddSeconds(1));

        var selected = DurableWorkFairClaimSelector.SelectHead([newer, older], new Dictionary<DurableWorkClaimPartitionKey, DateTimeOffset>());

        Assert.Equal(older.WorkId, selected!.WorkId);
    }
}

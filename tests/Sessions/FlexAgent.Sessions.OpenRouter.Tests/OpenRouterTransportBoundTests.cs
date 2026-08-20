using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterTransportBoundTests
{
    [Fact]
    public async Task Bounded_read_allows_exactly_the_limit()
    {
        var exact = new byte[] { 1, 2, 3, 4 };
        await using var allowed = new BoundedReadStream(new MemoryStream(exact), maxUtf8Bytes: 4);
        var buffer = new byte[16];
        var total = 0;
        int read;
        while ((read = await allowed.ReadAsync(buffer, TestContext.Current.CancellationToken)) > 0)
        {
            total += read;
        }

        Assert.Equal(4, total);
    }

    [Fact]
    public async Task Bounded_read_throws_when_the_stream_exceeds_the_limit()
    {
        await using var overflow = new BoundedReadStream(new MemoryStream([1, 2, 3, 4, 5]), maxUtf8Bytes: 4);
        var buffer = new byte[16];
        await Assert.ThrowsAsync<OpenRouterTransportLimitExceededException>(async () =>
        {
            int read;
            while ((read = await overflow.ReadAsync(buffer, TestContext.Current.CancellationToken)) > 0)
            {
            }
        });
    }
}

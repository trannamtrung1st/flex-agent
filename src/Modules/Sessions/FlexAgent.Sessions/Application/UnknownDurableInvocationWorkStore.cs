namespace FlexAgent.Sessions.Application;

public sealed class UnknownDurableInvocationWorkStore : IDurableInvocationWorkStore
{
    public static UnknownDurableInvocationWorkStore Instance { get; } = new();

    public Task<DurableInvocationWorkItem?> TryClaimExecuteInvocationAsync(
        TimeSpan lease,
        CancellationToken cancellationToken) =>
        Task.FromResult<DurableInvocationWorkItem?>(null);

    public Task ReleaseToPendingAsync(
        DurableInvocationWorkItem work,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task MarkCompletedAsync(
        DurableInvocationWorkItem work,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

namespace FlexAgent.Sessions.Application;

internal sealed class ClaimLeaseHeartbeat : IAsyncDisposable
{
    private readonly IDurableInvocationWorkStore _workStore;
    private readonly DurableInvocationWorkItem _work;
    private readonly TimeSpan _period;
    private readonly TimeSpan _lease;
    private readonly CancellationTokenSource _workCancellation;
    private readonly CancellationTokenSource _loopCancellation = new();
    private readonly Task _run;

    private ClaimLeaseHeartbeat(
        IDurableInvocationWorkStore workStore,
        DurableInvocationWorkItem work,
        TimeSpan period,
        TimeSpan lease,
        CancellationTokenSource workCancellation)
    {
        _workStore = workStore;
        _work = work;
        _period = period;
        _lease = lease;
        _workCancellation = workCancellation;
        _run = RunAsync();
    }

    public static ClaimLeaseHeartbeat Start(
        IDurableInvocationWorkStore workStore,
        DurableInvocationWorkItem work,
        TimeSpan period,
        TimeSpan lease,
        CancellationTokenSource workCancellation)
    {
        ArgumentNullException.ThrowIfNull(workStore);
        ArgumentNullException.ThrowIfNull(work);
        ArgumentNullException.ThrowIfNull(workCancellation);
        return new ClaimLeaseHeartbeat(workStore, work, period, lease, workCancellation);
    }

    private async Task RunAsync()
    {
        try
        {
            using var timer = new PeriodicTimer(_period);
            while (await timer.WaitForNextTickAsync(_loopCancellation.Token))
            {
                if (!await RenewAsync())
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (_loopCancellation.IsCancellationRequested)
        {
        }
    }

    private async Task<bool> RenewAsync()
    {
        try
        {
            var renewed = await _workStore.TryRenewClaimLeaseAsync(_work, _lease, _loopCancellation.Token);
            if (renewed is null)
            {
                _workCancellation.Cancel();
                return false;
            }

            _work.ClaimLeaseUntil = renewed;
            return true;
        }
        catch (OperationCanceledException) when (_loopCancellation.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception)
        {
            _workCancellation.Cancel();
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _loopCancellation.CancelAsync();
        try
        {
            await _run.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _loopCancellation.Dispose();
    }
}

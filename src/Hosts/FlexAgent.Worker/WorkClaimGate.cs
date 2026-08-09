namespace FlexAgent.Worker;

public sealed class WorkClaimGate
{
    private int _acceptingClaims = 1;

    public bool TryClaimWork()
    {
        return Volatile.Read(ref _acceptingClaims) == 1;
    }

    public void StopAcceptingWork()
    {
        Interlocked.Exchange(ref _acceptingClaims, 0);
    }
}

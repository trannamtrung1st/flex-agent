using System.Net;
using System.Net.Sockets;

namespace FlexAgent.Sessions.OpenAiCompatible;

public static class OpenAiCompatibleApprovedAddressConnector
{
    public static HttpRequestOptionsKey<IReadOnlyList<IPAddress>> ApprovedAddressesKey { get; } =
        new("flexagent.openai_compatible.approved_addresses");

    public static TimeSpan DefaultAddressAttemptTimeout { get; } = TimeSpan.FromSeconds(2);
    public static TimeSpan DefaultFallbackStagger { get; } = TimeSpan.FromMilliseconds(250);

    public static IReadOnlyList<IPAddress> OrderForFallback(IReadOnlyList<IPAddress> approvedAddresses)
    {
        ArgumentNullException.ThrowIfNull(approvedAddresses);
        return
        [
            .. approvedAddresses
                .Select(OpenAiCompatibleAddressClassification.Canonicalize)
                .Distinct()
                .OrderBy(address => address.AddressFamily == AddressFamily.InterNetworkV6 ? 0 : 1)
                .ThenBy(address => address.ToString(), StringComparer.Ordinal),
        ];
    }

    public static Task<NetworkStream> ConnectAsync(
        IReadOnlyList<IPAddress> approvedAddresses,
        int port,
        CancellationToken cancellationToken) =>
        ConnectAsync(approvedAddresses, port, cancellationToken, null, null, null);

    public static async Task<NetworkStream> ConnectAsync(
        IReadOnlyList<IPAddress> approvedAddresses,
        int port,
        CancellationToken cancellationToken,
        TimeSpan? addressAttemptTimeout,
        TimeSpan? fallbackStagger,
        Func<IPAddress, int, CancellationToken, Task<NetworkStream>>? connectAsync)
    {
        ArgumentNullException.ThrowIfNull(approvedAddresses);
        var ordered = OrderForFallback(approvedAddresses);
        if (ordered.Count == 0)
        {
            throw new HttpRequestException("origin_denied");
        }

        var attemptTimeout = addressAttemptTimeout ?? DefaultAddressAttemptTimeout;
        var stagger = fallbackStagger ?? DefaultFallbackStagger;
        var connect = connectAsync ?? ConnectSocketAsync;
        var pending = new List<Attempt>();
        Exception? last = null;
        var next = 0;

        try
        {
            StartNext();
            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var wait = pending.Select(attempt => (Task)attempt.Task).ToList();
                if (next < ordered.Count)
                {
                    wait.Add(Task.Delay(stagger, cancellationToken));
                }

                var completed = await Task.WhenAny(wait);
                if (next < ordered.Count && completed == wait[^1] && !pending.Any(attempt => attempt.Task.IsCompleted))
                {
                    StartNext();
                    continue;
                }

                var winner = await TakeCompletedAsync();
                if (winner is not null)
                {
                    return winner;
                }

                if (pending.Count == 0 && next < ordered.Count)
                {
                    StartNext();
                }
            }
        }
        finally
        {
            await CancelPendingAsync();
        }

        throw last ?? new HttpRequestException("origin_denied");

        void StartNext()
        {
            var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (attemptTimeout > TimeSpan.Zero)
            {
                attemptCts.CancelAfter(attemptTimeout);
            }

            var address = ordered[next++];
            pending.Add(new Attempt(connect(address, port, attemptCts.Token), attemptCts));
        }

        async Task<NetworkStream?> TakeCompletedAsync()
        {
            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var attempt = pending[i];
                if (!attempt.Task.IsCompleted)
                {
                    continue;
                }

                pending.RemoveAt(i);
                try
                {
                    var stream = await attempt.Task;
                    attempt.Cts.Dispose();
                    return stream;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    attempt.Cts.Dispose();
                    throw;
                }
                catch (Exception ex) when (ex is SocketException or OperationCanceledException or HttpRequestException)
                {
                    last = ex;
                    attempt.Cts.Dispose();
                }
            }

            return null;
        }

        async Task CancelPendingAsync()
        {
            foreach (var attempt in pending)
            {
                attempt.Cts.Cancel();
                try
                {
                    (await attempt.Task).Dispose();
                }
                catch (Exception)
                {
                    // Losing attempts are cancelled or already failed.
                }

                attempt.Cts.Dispose();
            }

            pending.Clear();
        }
    }

    private static async Task<NetworkStream> ConnectSocketAsync(
        IPAddress address,
        int port,
        CancellationToken cancellationToken)
    {
        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(address, port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private sealed record Attempt(Task<NetworkStream> Task, CancellationTokenSource Cts);
}

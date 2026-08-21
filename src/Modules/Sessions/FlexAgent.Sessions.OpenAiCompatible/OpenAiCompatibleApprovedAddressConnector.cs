using System.Net;
using System.Net.Sockets;

namespace FlexAgent.Sessions.OpenAiCompatible;

public static class OpenAiCompatibleApprovedAddressConnector
{
    public static HttpRequestOptionsKey<IReadOnlyList<IPAddress>> ApprovedAddressesKey { get; } =
        new("flexagent.openai_compatible.approved_addresses");

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

    public static async Task<NetworkStream> ConnectAsync(
        IReadOnlyList<IPAddress> approvedAddresses,
        int port,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(approvedAddresses);
        Exception? last = null;
        foreach (var address in OrderForFallback(approvedAddresses))
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(address, port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                last = ex;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        throw last ?? new HttpRequestException("origin_denied");
    }
}

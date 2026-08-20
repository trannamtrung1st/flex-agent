using System.Net;
using System.Net.Sockets;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenAiCompatible;

internal sealed class OpenAiCompatibleDestinationHandler(
    OpenAiCompatibleInstalledConfiguration configuration,
    HttpMessageHandler inner,
    IEndpointAddressResolver? resolver = null) : DelegatingHandler(inner)
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is null
            || request.Headers.ProxyAuthorization is not null
            || request.Options.TryGetValue(new HttpRequestOptionsKey<Uri>("Proxy"), out _))
        {
            return Denied();
        }

        IReadOnlyList<IPAddress> addresses;
        if (IPAddress.TryParse(request.RequestUri.Host, out var literal))
        {
            addresses = [literal];
        }
        else
        {
            addresses = resolver is null
                ? []
                : await resolver.ResolveAsync(request.RequestUri.IdnHost, cancellationToken);
        }

        var decision = OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
            request.RequestUri,
            configuration.Profile.ApprovedHttpsOrigin,
            configuration.ApiBasePath,
            configuration.DestinationPolicy,
            addresses);
        if (!decision.Allowed)
        {
            return Denied();
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static HttpResponseMessage Denied() =>
        new(HttpStatusCode.Forbidden)
        {
            ReasonPhrase = "origin_denied",
        };
}

internal static class OpenAiCompatibleTransportFactory
{
    public static HttpMessageHandler Create(
        OpenAiCompatibleInstalledConfiguration configuration,
        HttpMessageHandler? transport,
        IEndpointAddressResolver? resolver)
    {
        if (transport is not null)
        {
            return new OpenAiCompatibleDestinationHandler(configuration, transport, resolver);
        }

        var sockets = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = resolver is null
                    ? await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken)
                    : await resolver.ResolveAsync(context.DnsEndPoint.Host, cancellationToken);
                var requestUri = new UriBuilder(
                    Uri.UriSchemeHttps,
                    context.DnsEndPoint.Host,
                    context.DnsEndPoint.Port,
                    configuration.ChatCompletionsPath).Uri;
                var decision = OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
                    requestUri,
                    configuration.Profile.ApprovedHttpsOrigin,
                    configuration.ApiBasePath,
                    configuration.DestinationPolicy,
                    addresses);
                if (!decision.Allowed || decision.PinnedAddress is null)
                {
                    throw new HttpRequestException("origin_denied");
                }

                var socket = new Socket(decision.PinnedAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(decision.PinnedAddress, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };
        return new OpenAiCompatibleDestinationHandler(
            configuration,
            sockets,
            resolver ?? new SystemEndpointAddressResolver());
    }
}

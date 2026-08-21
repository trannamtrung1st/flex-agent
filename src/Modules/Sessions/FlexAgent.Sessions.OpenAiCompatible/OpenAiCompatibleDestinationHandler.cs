using System.Net;

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
        if (!decision.Allowed || decision.ApprovedAddresses.Count == 0)
        {
            return Denied();
        }

        request.Options.Set(
            OpenAiCompatibleApprovedAddressConnector.ApprovedAddressesKey,
            decision.ApprovedAddresses);
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
                if (context.InitialRequestMessage is null
                    || !context.InitialRequestMessage.Options.TryGetValue(
                        OpenAiCompatibleApprovedAddressConnector.ApprovedAddressesKey,
                        out var approved)
                    || approved is null
                    || approved.Count == 0)
                {
                    throw new HttpRequestException("origin_denied");
                }

                return await OpenAiCompatibleApprovedAddressConnector.ConnectAsync(
                    approved,
                    context.DnsEndPoint.Port,
                    cancellationToken);
            },
        };
        return new OpenAiCompatibleDestinationHandler(
            configuration,
            sockets,
            resolver ?? new SystemEndpointAddressResolver());
    }
}

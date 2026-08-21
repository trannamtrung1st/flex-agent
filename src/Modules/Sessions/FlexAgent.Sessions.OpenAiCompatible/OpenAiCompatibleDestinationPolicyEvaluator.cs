using System.Net;
using System.Net.Sockets;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenAiCompatible;

public interface IEndpointAddressResolver
{
    Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken);
}

public sealed class SystemEndpointAddressResolver : IEndpointAddressResolver
{
    public async Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        return addresses;
    }
}

public sealed record EndpointDestinationDecision(
    bool Allowed,
    IReadOnlyList<IPAddress> ApprovedAddresses,
    string ReasonCode)
{
    public IPAddress? PinnedAddress => ApprovedAddresses.Count == 0 ? null : ApprovedAddresses[0];
}

public static class OpenAiCompatibleAddressClassification
{
    public static IPAddress Canonicalize(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    public static bool IsAlwaysDenied(IPAddress address) =>
        !IsGloballyRoutable(address) && !IsPrivateUnicast(address);

    public static bool IsGloballyRoutable(IPAddress address)
    {
        var canonical = Canonicalize(address);
        if (canonical.AddressFamily == AddressFamily.InterNetwork)
        {
            return !NonGlobalIPv4Prefixes.Any(prefix => OpenAiCompatibleCidr.Contains(prefix, canonical));
        }

        return canonical.AddressFamily == AddressFamily.InterNetworkV6
            && OpenAiCompatibleCidr.Contains(Ipv6GlobalUnicast, canonical)
            && !NonGlobalIpv6InsideGlobalUnicast.Any(prefix => OpenAiCompatibleCidr.Contains(prefix, canonical));
    }

    public static bool IsPrivateUnicast(IPAddress address)
    {
        var canonical = Canonicalize(address);
        if (canonical.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = canonical.GetAddressBytes();
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168);
        }

        return canonical.AddressFamily == AddressFamily.InterNetworkV6 && canonical.IsIPv6UniqueLocal;
    }

    private const string Ipv6GlobalUnicast = "2000::/3";

    private static readonly string[] NonGlobalIPv4Prefixes =
    [
        "0.0.0.0/8",
        "10.0.0.0/8",
        "100.64.0.0/10",
        "127.0.0.0/8",
        "169.254.0.0/16",
        "172.16.0.0/12",
        "192.0.0.0/24",
        "192.0.2.0/24",
        "192.31.196.0/24",
        "192.52.193.0/24",
        "192.88.99.0/24",
        "192.168.0.0/16",
        "192.175.48.0/24",
        "198.18.0.0/15",
        "198.51.100.0/24",
        "203.0.113.0/24",
        "224.0.0.0/4",
        "240.0.0.0/4",
    ];

    private static readonly string[] NonGlobalIpv6InsideGlobalUnicast =
    [
        "2001::/23",
        "2001:db8::/32",
        "2002::/16",
        "3fff::/20",
        "2620:4f:8000::/48",
    ];
}

public static class OpenAiCompatibleDestinationPolicyEvaluator
{
    public static EndpointDestinationDecision Evaluate(
        Uri destination,
        Uri approvedOrigin,
        string apiBasePath,
        OpenAiCompatibleDestinationPolicy policy,
        IReadOnlyList<IPAddress> resolvedAddresses)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(approvedOrigin);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(resolvedAddresses);

        if (!UriMatchesApprovedEndpoint(destination, approvedOrigin, apiBasePath))
        {
            return new EndpointDestinationDecision(false, [], "origin_or_path_denied");
        }

        if (resolvedAddresses.Count == 0)
        {
            return new EndpointDestinationDecision(false, [], "resolution_empty");
        }

        var approved = new List<IPAddress>(resolvedAddresses.Count);
        foreach (var address in resolvedAddresses)
        {
            var canonical = OpenAiCompatibleAddressClassification.Canonicalize(address);
            if (!IsAddressAllowed(canonical, policy))
            {
                return new EndpointDestinationDecision(false, [], "address_denied");
            }

            approved.Add(canonical);
        }

        return new EndpointDestinationDecision(true, approved, "allowed");
    }

    public static bool UriMatchesApprovedEndpoint(Uri destination, Uri approvedOrigin, string apiBasePath)
    {
        var origin = ApprovedHttpsOrigin.Canonicalize(approvedOrigin);
        var expectedPath = OpenAiCompatibleInstalledConfiguration.NormalizeApiBasePath(apiBasePath)
            .TrimEnd('/') + OpenAiCompatibleAdapterContracts.ChatCompletionsSuffix;
        if (!string.Equals(destination.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(destination.UserInfo)
            || !string.IsNullOrEmpty(destination.Query)
            || !string.IsNullOrEmpty(destination.Fragment)
            || !string.Equals(destination.Host, origin.Host, StringComparison.OrdinalIgnoreCase)
            || EffectivePort(destination) != EffectivePort(origin)
            || !string.Equals(destination.AbsolutePath, expectedPath, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static bool IsAddressAllowed(IPAddress address, OpenAiCompatibleDestinationPolicy policy)
    {
        if (OpenAiCompatibleAddressClassification.IsAlwaysDenied(address))
        {
            return false;
        }

        if (string.Equals(policy.Kind, OpenAiCompatibleAdapterContracts.DestinationPolicyPublicOnly, StringComparison.Ordinal))
        {
            return OpenAiCompatibleAddressClassification.IsGloballyRoutable(address);
        }

        if (!string.Equals(policy.Kind, OpenAiCompatibleAdapterContracts.DestinationPolicyPrivateAllowlist, StringComparison.Ordinal)
            || !OpenAiCompatibleAddressClassification.IsPrivateUnicast(address))
        {
            return false;
        }

        return policy.AllowedPrivateCidrs.Any(cidr => OpenAiCompatibleCidr.Contains(cidr, address));
    }

    private static int EffectivePort(Uri uri) => uri.IsDefaultPort ? 443 : uri.Port;
}

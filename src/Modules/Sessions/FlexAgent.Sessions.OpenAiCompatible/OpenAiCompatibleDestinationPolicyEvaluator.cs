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

public sealed record EndpointDestinationDecision(bool Allowed, IPAddress? PinnedAddress, string ReasonCode);

public static class OpenAiCompatibleAddressClassification
{
    public static IPAddress Canonicalize(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
    }

    public static bool IsAlwaysDenied(IPAddress address)
    {
        var canonical = Canonicalize(address);
        if (IPAddress.IsLoopback(canonical)
            || IPAddress.Any.Equals(canonical)
            || IPAddress.IPv6Any.Equals(canonical)
            || canonical.IsIPv6LinkLocal
            || canonical.IsIPv6Multicast
            || IsMulticast(canonical)
            || IsLinkLocal(canonical)
            || IsUnspecified(canonical)
            || IsReservedIPv4(canonical)
            || IsCarrierGradeNat(canonical))
        {
            return true;
        }

        return false;
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

    private static bool IsUnspecified(IPAddress address) =>
        IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address);

    private static bool IsMulticast(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return address.GetAddressBytes()[0] >= 224 && address.GetAddressBytes()[0] <= 239;
        }

        return address.IsIPv6Multicast;
    }

    private static bool IsLinkLocal(IPAddress address)
    {
        if (address.IsIPv6LinkLocal)
        {
            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }

    private static bool IsReservedIPv4(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var first = address.GetAddressBytes()[0];
        return first >= 240;
    }

    private static bool IsCarrierGradeNat(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127;
    }
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
            return new EndpointDestinationDecision(false, null, "origin_or_path_denied");
        }

        if (resolvedAddresses.Count == 0)
        {
            return new EndpointDestinationDecision(false, null, "resolution_empty");
        }

        IPAddress? pinned = null;
        foreach (var address in resolvedAddresses)
        {
            var canonical = OpenAiCompatibleAddressClassification.Canonicalize(address);
            if (!IsAddressAllowed(canonical, policy))
            {
                return new EndpointDestinationDecision(false, null, "address_denied");
            }

            pinned ??= canonical;
        }

        return new EndpointDestinationDecision(true, pinned, "allowed");
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
            return !OpenAiCompatibleAddressClassification.IsPrivateUnicast(address);
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

using System.Net;
using System.Net.Sockets;
using FlexAgent.Sessions.OpenAiCompatible;

namespace FlexAgent.Sessions.OpenAiCompatible.Tests;

public sealed class OpenAiCompatibleDestinationPolicyTests
{
    private static readonly Uri Origin = new("https://models.organization.example/");
    private static readonly Uri AllowedUri = new("https://models.organization.example/v1/chat/completions");

    [Fact]
    public void Public_only_allows_public_addresses_and_denies_special_and_private_ranges()
    {
        Assert.True(Evaluate(OpenAiCompatibleDestinationPolicy.PublicOnly, IPAddress.Parse("1.2.3.4")).Allowed);
        Assert.True(Evaluate(OpenAiCompatibleDestinationPolicy.PublicOnly, IPAddress.Parse("2001:4860:4860::8888")).Allowed);
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "2001:db8::1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "3fff::1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "2620:4f:8000::1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "0.0.0.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "192.0.0.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "192.0.2.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "198.18.0.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "198.51.100.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "203.0.113.10");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "192.88.99.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "127.0.0.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "::1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "0.0.0.0");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "::");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "169.254.1.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "169.254.169.254");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "10.0.0.8");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "172.16.1.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "192.168.1.10");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "224.0.0.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "240.0.0.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "100.64.0.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "fe80::1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "ff02::1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "fc00::1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "::ffff:127.0.0.1");
        AssertDenied(OpenAiCompatibleDestinationPolicy.PublicOnly, "::ffff:10.0.0.1");
    }

    [Fact]
    public void Private_allowlist_permits_only_listed_private_unicast_and_still_denies_specials()
    {
        var policy = OpenAiCompatibleDestinationPolicy.PrivateAllowlist("10.0.0.0/8", "fd00::/8");
        Assert.True(Evaluate(policy, IPAddress.Parse("10.1.2.3")).Allowed);
        Assert.True(Evaluate(policy, IPAddress.Parse("fd00::1")).Allowed);
        Assert.False(Evaluate(policy, IPAddress.Parse("10.0.0.1"), IPAddress.Parse("172.16.0.1")).Allowed);
        AssertDenied(policy, "192.168.0.1");
        AssertDenied(policy, "1.2.3.4");
        AssertDenied(policy, "127.0.0.1");
        AssertDenied(policy, "169.254.169.254");
        AssertDenied(policy, "0.0.0.0");
        AssertDenied(policy, "224.0.0.1");
    }

    [Fact]
    public void Mixed_dns_answers_fail_closed_and_empty_resolution_is_denied()
    {
        var publicOnly = Evaluate(
            OpenAiCompatibleDestinationPolicy.PublicOnly,
            IPAddress.Parse("1.2.3.4"),
            IPAddress.Parse("10.0.0.1"));
        Assert.False(publicOnly.Allowed);
        Assert.Equal("address_denied", publicOnly.ReasonCode);

        var empty = OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
            AllowedUri,
            Origin,
            "/v1",
            OpenAiCompatibleDestinationPolicy.PublicOnly,
            []);
        Assert.False(empty.Allowed);
        Assert.Equal("resolution_empty", empty.ReasonCode);
    }

    [Fact]
    public void Alternate_scheme_port_origin_path_query_and_userinfo_are_denied()
    {
        var policy = OpenAiCompatibleDestinationPolicy.PublicOnly;
        var publicAddress = new[] { IPAddress.Parse("1.2.3.4") };
        Assert.False(OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
            new Uri("http://models.organization.example/v1/chat/completions"),
            Origin,
            "/v1",
            policy,
            publicAddress).Allowed);
        Assert.False(OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
            new Uri("https://models.organization.example:8443/v1/chat/completions"),
            Origin,
            "/v1",
            policy,
            publicAddress).Allowed);
        Assert.False(OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
            new Uri("https://other.example/v1/chat/completions"),
            Origin,
            "/v1",
            policy,
            publicAddress).Allowed);
        Assert.False(OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
            new Uri("https://models.organization.example/v1/models"),
            Origin,
            "/v1",
            policy,
            publicAddress).Allowed);
        Assert.False(OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
            new Uri("https://models.organization.example/openai/v1/chat/completions"),
            Origin,
            "/v1",
            policy,
            publicAddress).Allowed);
        Assert.False(OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
            new Uri("https://models.organization.example/v1/chat/completions?x=1"),
            Origin,
            "/v1",
            policy,
            publicAddress).Allowed);
        Assert.False(OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(
            new Uri("https://user:pass@models.organization.example/v1/chat/completions"),
            Origin,
            "/v1",
            policy,
            publicAddress).Allowed);
    }

    [Fact]
    public void Adapter_configuration_digest_covers_base_path_and_destination_policy()
    {
        var origin = new Uri("https://models.organization.example/");
        var publicDigest = OpenAiCompatibleInstalledConfiguration.ComputeAdapterConfigurationDigest(
            origin,
            "/v1",
            OpenAiCompatibleDestinationPolicy.PublicOnly);
        var otherPath = OpenAiCompatibleInstalledConfiguration.ComputeAdapterConfigurationDigest(
            origin,
            "/openai/v1",
            OpenAiCompatibleDestinationPolicy.PublicOnly);
        var privateDigest = OpenAiCompatibleInstalledConfiguration.ComputeAdapterConfigurationDigest(
            origin,
            "/v1",
            OpenAiCompatibleDestinationPolicy.PrivateAllowlist("10.0.0.0/8"));
        Assert.NotEqual(publicDigest, otherPath);
        Assert.NotEqual(publicDigest, privateDigest);
        Assert.Equal(64, publicDigest.Length);
    }

    [Fact]
    public void Private_allowlist_rejects_non_private_and_overwide_cidrs_and_canonicalizes_network_bits()
    {
        Assert.Throws<ArgumentException>(() => OpenAiCompatibleDestinationPolicy.PrivateAllowlist("0.0.0.0/0"));
        Assert.Throws<ArgumentException>(() => OpenAiCompatibleDestinationPolicy.PrivateAllowlist("::/0"));
        Assert.Throws<ArgumentException>(() => OpenAiCompatibleDestinationPolicy.PrivateAllowlist("11.0.0.0/8"));
        Assert.Throws<ArgumentException>(() => OpenAiCompatibleDestinationPolicy.PrivateAllowlist("172.16.0.0/11"));
        Assert.Throws<ArgumentException>(() => OpenAiCompatibleDestinationPolicy.PrivateAllowlist("2001:db8::/32"));
        Assert.Throws<ArgumentException>(() => OpenAiCompatibleDestinationPolicy.PrivateAllowlist("fe80::/10"));

        var hostBits = OpenAiCompatibleDestinationPolicy.PrivateAllowlist("10.1.2.3/8");
        Assert.Equal(["10.0.0.0/8"], hostBits.AllowedPrivateCidrs);
        var origin = new Uri("https://models.organization.example/");
        Assert.Equal(
            OpenAiCompatibleInstalledConfiguration.ComputeAdapterConfigurationDigest(
                origin,
                "/v1",
                OpenAiCompatibleDestinationPolicy.PrivateAllowlist("10.0.0.0/8")),
            OpenAiCompatibleInstalledConfiguration.ComputeAdapterConfigurationDigest(
                origin,
                "/v1",
                hostBits));
        Assert.Throws<ArgumentException>(() =>
            OpenAiCompatibleDestinationPolicy.PrivateAllowlist("10.1.2.3/8", "10.0.0.0/8"));
    }

    [Fact]
    public void Allowed_decision_keeps_every_validated_address_and_orders_ipv6_before_ipv4()
    {
        var ipv4 = IPAddress.Parse("1.2.3.4");
        var ipv6 = IPAddress.Parse("2001:4860:4860::8888");
        var decision = Evaluate(OpenAiCompatibleDestinationPolicy.PublicOnly, ipv4, ipv6);
        Assert.True(decision.Allowed);
        Assert.Equal([ipv4, ipv6], decision.ApprovedAddresses);
        Assert.Equal(
            [ipv6, ipv4],
            OpenAiCompatibleApprovedAddressConnector.OrderForFallback(decision.ApprovedAddresses));
    }

    [Fact]
    public async Task Connector_tries_remaining_approved_addresses_after_the_first_is_unreachable()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var accept = listener.AcceptTcpClientAsync(TestContext.Current.CancellationToken);
        await using var stream = await OpenAiCompatibleApprovedAddressConnector.ConnectAsync(
            [IPAddress.IPv6Loopback, IPAddress.Loopback],
            port,
            TestContext.Current.CancellationToken);
        using var client = await accept;
        Assert.True(stream.CanWrite);
    }

    [Fact]
    public async Task Connector_falls_back_when_the_first_approved_address_hangs()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var accept = listener.AcceptTcpClientAsync(TestContext.Current.CancellationToken);
        var firstCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var stream = await OpenAiCompatibleApprovedAddressConnector.ConnectAsync(
            [IPAddress.IPv6Loopback, IPAddress.Loopback],
            port,
            TestContext.Current.CancellationToken,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(20),
            async (address, destinationPort, cancellationToken) =>
            {
                if (IPAddress.IPv6Loopback.Equals(address))
                {
                    try
                    {
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        firstCancelled.TrySetResult();
                        throw;
                    }
                }

                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(address, destinationPort, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }).WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        using var client = await accept;
        await firstCancelled.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(stream.CanWrite);
    }

    private static EndpointDestinationDecision Evaluate(
        OpenAiCompatibleDestinationPolicy policy,
        params IPAddress[] addresses) =>
        OpenAiCompatibleDestinationPolicyEvaluator.Evaluate(AllowedUri, Origin, "/v1", policy, addresses);

    private static void AssertDenied(OpenAiCompatibleDestinationPolicy policy, params string[] addresses)
    {
        foreach (var address in addresses)
        {
            var decision = Evaluate(policy, IPAddress.Parse(address));
            Assert.False(decision.Allowed);
        }
    }
}

using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Infrastructure;

namespace FlexAgent.Runtime.Tests;

public sealed class CachedJwksKeySourceTests
{
    [Fact]
    public async Task Unknown_kid_refreshes_cached_jwks_once()
    {
        using var first = RSA.Create(2048);
        using var second = RSA.Create(2048);
        var handler = new ScriptedHandler(
            JsonSerializer.Serialize(new { keys = new[] { ToJwk(first, "old") } }),
            JsonSerializer.Serialize(new { keys = new[] { ToJwk(first, "old"), ToJwk(second, "new") } }));
        using var http = new HttpClient(handler);
        var source = new CachedJwksKeySource(http, TimeProvider.System, TimeSpan.FromMinutes(5));

        using var cached = await source.TryGetKeysAsync("https://issuer.example/jwks", TestContext.Current.CancellationToken);
        using var refreshed = await source.TryGetKeysAsync(
            "https://issuer.example/jwks",
            "new",
            TestContext.Current.CancellationToken);

        Assert.NotNull(cached);
        Assert.False(cached!.ContainsKey("new"));
        Assert.NotNull(refreshed);
        Assert.True(refreshed!.ContainsKey("new"));
        Assert.Equal(2, handler.Requests);
    }

    [Fact]
    public async Task Repeated_unknown_kids_do_not_refresh_during_the_forced_refresh_cooldown()
    {
        using var first = RSA.Create(2048);
        var handler = new ScriptedHandler(JsonSerializer.Serialize(new { keys = new[] { ToJwk(first, "old") } }));
        using var http = new HttpClient(handler);
        var source = new CachedJwksKeySource(
            http,
            TimeProvider.System,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(1));

        await source.TryGetKeysAsync("https://issuer.example/jwks", TestContext.Current.CancellationToken);
        await Task.WhenAll(
            source.TryGetKeysAsync("https://issuer.example/jwks", "missing-a", TestContext.Current.CancellationToken),
            source.TryGetKeysAsync("https://issuer.example/jwks", "missing-b", TestContext.Current.CancellationToken));
        await source.TryGetKeysAsync("https://issuer.example/jwks", "missing-c", TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.Requests);
    }

    [Fact]
    public async Task Refresh_does_not_dispose_rsa_keys_already_handed_to_callers()
    {
        using var first = RSA.Create(2048);
        using var second = RSA.Create(2048);
        var payload = "verify-me"u8.ToArray();
        var signature = first.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var handler = new ScriptedHandler(
            JsonSerializer.Serialize(new { keys = new[] { ToJwk(first, "old") } }),
            JsonSerializer.Serialize(new { keys = new[] { ToJwk(first, "old"), ToJwk(second, "new") } }));
        using var http = new HttpClient(handler);
        var source = new CachedJwksKeySource(http, TimeProvider.System, TimeSpan.FromMinutes(5));

        using var cached = await source.TryGetKeysAsync("https://issuer.example/jwks", TestContext.Current.CancellationToken);
        using var refreshed = await source.TryGetKeysAsync(
            "https://issuer.example/jwks",
            "new",
            TestContext.Current.CancellationToken);

        Assert.True(cached!.Keys["old"].VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        Assert.True(refreshed!.ContainsKey("new"));
    }

    [Fact]
    public async Task Malformed_rsa_parameters_fail_closed_without_throwing()
    {
        var handler = new ScriptedHandler(JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new { kty = "RSA", kid = "bad", n = "AA", e = "AQAB" },
            },
        }));
        using var http = new HttpClient(handler);
        var source = new CachedJwksKeySource(http, TimeProvider.System, TimeSpan.FromMinutes(5));

        var snapshot = await source.TryGetKeysAsync("https://issuer.example/jwks", TestContext.Current.CancellationToken);

        Assert.Null(snapshot);
        Assert.Null(JwksKeySnapshot.TryFromParameters(new Dictionary<string, RSAParameters>(StringComparer.Ordinal)
        {
            ["bad"] = new() { Modulus = [0x00], Exponent = [0x01, 0x00, 0x01] },
        }));
    }

    private static object ToJwk(RSA rsa, string kid)
    {
        var parameters = rsa.ExportParameters(false);
        return new
        {
            kty = "RSA",
            kid,
            n = Encode(parameters.Modulus!),
            e = Encode(parameters.Exponent!),
        };
    }

    private static string Encode(byte[] bytes)
    {
        var buffer = new byte[Base64Url.GetEncodedLength(bytes.Length)];
        Base64Url.EncodeToUtf8(bytes, buffer, out _, out var written);
        return Encoding.ASCII.GetString(buffer.AsSpan(0, written));
    }

    private sealed class ScriptedHandler(params string[] bodies) : HttpMessageHandler
    {
        private int _index;

        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            var body = bodies[Math.Min(_index, bodies.Length - 1)];
            _index++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }
}

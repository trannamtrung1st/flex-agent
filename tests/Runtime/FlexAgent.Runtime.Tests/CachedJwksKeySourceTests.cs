using System.Buffers.Text;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

        var cached = await source.TryGetKeysAsync("https://issuer.example/jwks", TestContext.Current.CancellationToken);
        var refreshed = await source.TryGetKeysAsync(
            "https://issuer.example/jwks",
            "new",
            TestContext.Current.CancellationToken);

        Assert.NotNull(cached);
        Assert.False(cached!.ContainsKey("new"));
        Assert.NotNull(refreshed);
        Assert.True(refreshed!.ContainsKey("new"));
        Assert.Equal(2, handler.Requests);
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

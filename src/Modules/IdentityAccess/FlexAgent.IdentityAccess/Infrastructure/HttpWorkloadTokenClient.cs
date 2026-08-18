using System.Net.Http.Headers;
using System.Text.Json;
using FlexAgent.IdentityAccess.Application;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class HttpWorkloadTokenClient(HttpClient httpClient) : IWorkloadTokenClient
{
    public async Task<string?> RequestClientCredentialsTokenAsync(
        string tokenEndpoint,
        string clientId,
        string clientSecret,
        string audience,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("audience", audience),
            ]),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("access_token", out var tokenElement)
            || tokenElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var token = tokenElement.GetString();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}

using System.Net;
using System.Text.Json;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.Api;

public sealed record OidcTokenExchangeResult(string? IdToken, string? ErrorReason);

public interface IOidcAuthorizationClient
{
    Task<OidcTokenExchangeResult> ExchangeAuthorizationCodeAsync(
        string code,
        string codeVerifier,
        CancellationToken cancellationToken = default);
}

public sealed class HttpOidcAuthorizationClient(
    HttpClient httpClient,
    HumanAuthenticationHostOptions options,
    ISecretSource secrets) : IOidcAuthorizationClient
{
    public async Task<OidcTokenExchangeResult> ExchangeAuthorizationCodeAsync(
        string code,
        string codeVerifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);
        if (code.Length > 2048 || codeVerifier.Length > 256)
        {
            return new OidcTokenExchangeResult(null, HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        var clientSecret = await secrets.TryReadAsync("oidc-client-secret", cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            return new OidcTokenExchangeResult(null, HumanAuthenticationReasonCodes.ConfigurationUnavailable);
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = options.RedirectUri,
            ["client_id"] = options.ClientId,
            ["client_secret"] = clientSecret,
            ["code_verifier"] = codeVerifier,
        });

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync(options.TokenEndpoint, content, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            return new OidcTokenExchangeResult(null, HumanAuthenticationReasonCodes.ProviderUnavailable);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new OidcTokenExchangeResult(
                null,
                response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.ServiceUnavailable
                    ? HumanAuthenticationReasonCodes.ProviderUnavailable
                    : HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("id_token", out var idToken)
            || idToken.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(idToken.GetString()))
        {
            return new OidcTokenExchangeResult(null, HumanAuthenticationReasonCodes.InvalidProviderResponse);
        }

        return new OidcTokenExchangeResult(idToken.GetString(), null);
    }
}

using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

internal sealed record OpenRouterSanitizedLiveObservation(
    int? StatusCode,
    string CacheClassification,
    string StatusClassification,
    bool ResponseReceived)
{
    public static OpenRouterSanitizedLiveObservation NoResponse { get; } =
        new(null, "none", "no_response", false);
}

internal sealed class OpenRouterSanitizedLiveObserver(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    private readonly List<OpenRouterSanitizedLiveObservation> _observations = [];

    public int Requests { get; private set; }

    public IReadOnlyList<OpenRouterSanitizedLiveObservation> Observations => _observations;

    public OpenRouterSanitizedLiveObservation Current { get; private set; } =
        OpenRouterSanitizedLiveObservation.NoResponse;

    public int? StatusCode => Current.StatusCode;

    public string CacheClassification => Current.CacheClassification;

    public string StatusClassification => Current.StatusClassification;

    public bool IsHttpHardStop =>
        Current.ResponseReceived
        && (string.Equals(StatusClassification, OpenRouterDiscoveryFailureReasons.RateLimited, StringComparison.Ordinal)
            || string.Equals(StatusClassification, OpenRouterDiscoveryFailureReasons.Authentication, StringComparison.Ordinal)
            || string.Equals(StatusClassification, OpenRouterDiscoveryFailureReasons.PaymentRequired, StringComparison.Ordinal)
            || string.Equals(StatusClassification, OpenRouterDiscoveryFailureReasons.PolicyDenied, StringComparison.Ordinal)
            || string.Equals(StatusClassification, OpenRouterDiscoveryFailureReasons.Timeout, StringComparison.Ordinal)
            || string.Equals(StatusClassification, OpenRouterDiscoveryFailureReasons.ProviderUnavailable, StringComparison.Ordinal)
            || string.Equals(StatusClassification, OpenRouterDiscoveryFailureReasons.ResponseCacheHit, StringComparison.Ordinal));

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests++;
        Current = OpenRouterSanitizedLiveObservation.NoResponse;
        _observations.Add(Current);
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            Current = new OpenRouterSanitizedLiveObservation(
                (int)response.StatusCode,
                ClassifyCache(response),
                Classify((int)response.StatusCode, ClassifyCache(response)),
                true);
            _observations[^1] = Current;
            return response;
        }
        catch
        {
            Current = OpenRouterSanitizedLiveObservation.NoResponse;
            _observations[^1] = Current;
            throw;
        }
    }

    public static string Classify(int status, string cacheClassification)
    {
        if (string.Equals(cacheClassification, "hit", StringComparison.Ordinal))
        {
            return OpenRouterDiscoveryFailureReasons.ResponseCacheHit;
        }

        return status switch
        {
            200 => "ok",
            401 => OpenRouterDiscoveryFailureReasons.Authentication,
            402 => OpenRouterDiscoveryFailureReasons.PaymentRequired,
            403 => OpenRouterDiscoveryFailureReasons.PolicyDenied,
            408 or 504 => OpenRouterDiscoveryFailureReasons.Timeout,
            429 => OpenRouterDiscoveryFailureReasons.RateLimited,
            >= 500 => OpenRouterDiscoveryFailureReasons.ProviderUnavailable,
            _ => OpenRouterDiscoveryFailureReasons.RequestRejected,
        };
    }

    public static string ClassifyCache(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(OpenRouterAdapterContracts.ResponseCacheStatusHeader, out var values)
            || response.Content.Headers.TryGetValues(OpenRouterAdapterContracts.ResponseCacheStatusHeader, out values))
        {
            var value = values.FirstOrDefault();
            if (string.Equals(value, "HIT", StringComparison.OrdinalIgnoreCase))
            {
                return "hit";
            }

            if (string.Equals(value, "MISS", StringComparison.OrdinalIgnoreCase))
            {
                return "miss";
            }

            return "other";
        }

        return "absent";
    }
}
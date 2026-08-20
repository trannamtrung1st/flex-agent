using System.Net;
using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterSanitizedLiveObserverTests
{
    [Fact]
    public async Task Observer_records_status_and_cache_class_without_retaining_the_body()
    {
        using var observer = new OpenRouterSanitizedLiveObserver(new FixedHandler(
            HttpStatusCode.TooManyRequests,
            "sk-or-canary-secret-do-not-leak",
            "HIT"));
        using var client = new HttpClient(observer, disposeHandler: false);
        using var request = new HttpRequestMessage(HttpMethod.Post, OpenRouterDestination.ChatCompletionsUri);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(429, observer.StatusCode);
        Assert.Equal("hit", observer.CacheClassification);
        Assert.Equal(OpenRouterDiscoveryFailureReasons.ResponseCacheHit, observer.StatusClassification);
        Assert.True(observer.IsHttpHardStop);
        Assert.Equal(1, observer.Requests);
        Assert.Equal(
            "sk-or-canary-secret-do-not-leak",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.DoesNotContain("sk-or-", observer.StatusClassification, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-or-", observer.CacheClassification, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(404, null, OpenRouterDiscoveryFailureReasons.RequestRejected, false)]
    [InlineData(200, "MISS", "ok", false)]
    [InlineData(401, null, OpenRouterDiscoveryFailureReasons.Authentication, true)]
    [InlineData(500, null, OpenRouterDiscoveryFailureReasons.ProviderUnavailable, true)]
    public void Status_and_cache_classes_are_stable(
        int status,
        string? cache,
        string expected,
        bool hardStop)
    {
        var cacheClass = cache is null
            ? "absent"
            : string.Equals(cache, "HIT", StringComparison.OrdinalIgnoreCase)
                ? "hit"
                : "miss";
        var classified = OpenRouterSanitizedLiveObserver.Classify(status, cacheClass);
        Assert.Equal(expected, classified);
        Assert.Equal(hardStop, classified is OpenRouterDiscoveryFailureReasons.RateLimited
            or OpenRouterDiscoveryFailureReasons.Authentication
            or OpenRouterDiscoveryFailureReasons.PaymentRequired
            or OpenRouterDiscoveryFailureReasons.PolicyDenied
            or OpenRouterDiscoveryFailureReasons.Timeout
            or OpenRouterDiscoveryFailureReasons.ProviderUnavailable
            or OpenRouterDiscoveryFailureReasons.ResponseCacheHit);
    }

    private sealed class FixedHandler(HttpStatusCode status, string body, string? cacheStatus) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body),
            };
            if (!string.IsNullOrWhiteSpace(cacheStatus))
            {
                response.Headers.TryAddWithoutValidation(
                    OpenRouterAdapterContracts.ResponseCacheStatusHeader,
                    cacheStatus);
            }

            return Task.FromResult(response);
        }
    }
}
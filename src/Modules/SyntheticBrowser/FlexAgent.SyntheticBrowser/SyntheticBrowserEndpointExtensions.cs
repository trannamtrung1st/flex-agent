using System.Text.Json;
using System.Text.Json.Serialization;
using FlexAgent.Contracts.Browser;
using FlexAgent.Contracts.Transport;
using FlexAgent.SyntheticBrowser.Application;
using FlexAgent.SyntheticBrowser.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FlexAgent.SyntheticBrowser;

public static class SyntheticBrowserEndpointExtensions
{
    public const string SessionCookieName = "flex_agent_synthetic_session";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static IEndpointRouteBuilder MapSyntheticBrowserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var browser = endpoints.MapGroup("/browser");

        browser.MapPost("/test/scenario-grants", CreateScenarioGrant);
        browser.MapPost("/auth/exchange", ExchangeGrant);
        browser.MapPost("/auth/logout", Logout);
        browser.MapGet("/actor-context", GetActorContext);
        browser.MapGet("/navigation", GetNavigation);
        browser.MapGet("/home", GetHome);
        browser.MapGet("/activities", GetActivities);
        browser.MapGet("/activities/{activityId}", GetActivityDetail);
        browser.MapGet("/activities/{activityId}/enrollment", GetEnrollment);
        browser.MapGet("/my-work", GetMyWork);
        browser.MapGet("/sessions/{sessionId}", GetSession);
        browser.MapPost("/commands", ExecuteCommand);
        browser.MapGet("/sessions/{sessionId}/events", GetSessionEvents);
        browser.MapGet("/review-work", GetReviewWork);
        browser.MapGet("/review-work/{caseId}", GetReviewCase);
        browser.MapGet("/release-work", GetReleaseWork);
        browser.MapGet("/release-work/{releaseId}", GetReleaseDetail);
        browser.MapGet("/results", GetResults);
        browser.MapGet("/results/{resultId}", GetResultDetail);
        browser.MapGet("/governance", GetGovernance);
        browser.MapGet("/planned-tier/{moduleName}", GetPlannedTier);

        return endpoints;
    }

    private static ISyntheticBrowserService GetService(HttpContext context) =>
        context.RequestServices.GetRequiredService<ISyntheticBrowserService>();

    private static IResult Disabled() =>
        Results.Json(new SafeErrorResponseV1(
            BrowserSchemaVersion.V1,
            "unavailable",
            Guid.NewGuid().ToString("N"),
            "none",
            null,
            null), JsonOptions, statusCode: StatusCodes.Status404NotFound);

    private static IResult Denied(string message) =>
        Results.Json(new AccessChangedResponseV1(BrowserSchemaVersion.V1, "denied", message), JsonOptions, statusCode: StatusCodes.Status403Forbidden);

    private static IResult Protected() =>
        Results.Json(new ProtectedContentResponseV1(BrowserSchemaVersion.V1, "unavailable", "Content is not available."), JsonOptions, statusCode: StatusCodes.Status404NotFound);

    private static SyntheticSessionRecord? ResolveSession(HttpContext context, ISyntheticBrowserService service)
    {
        if (!context.Request.Cookies.TryGetValue(SessionCookieName, out var sessionId) || string.IsNullOrEmpty(sessionId))
        {
            return null;
        }

        return service.ResolveSession(sessionId);
    }

    private static IResult CreateScenarioGrant(HttpContext context, ScenarioGrantRequestV1 request)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        try
        {
            var response = service.CreateScenarioGrant(request);
            return Results.Json(response, JsonOptions);
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new SafeErrorResponseV1(
                BrowserSchemaVersion.V1,
                "invalid_request",
                Guid.NewGuid().ToString("N"),
                "none",
                null,
                null));
        }
    }

    private static IResult ExchangeGrant(HttpContext context, ScenarioGrantExchangeRequestV1 request)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var response = service.ExchangeGrant(request.GrantToken);
        if (response is null)
        {
            return Results.Json(new SafeErrorResponseV1(
                BrowserSchemaVersion.V1,
                "grant_invalid_or_expired",
                Guid.NewGuid().ToString("N"),
                "none",
                null,
                null), JsonOptions, statusCode: StatusCodes.Status401Unauthorized);
        }

        context.Response.Cookies.Append(SessionCookieName, response.SessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = response.ExpiresAt,
        });

        return Results.Json(response, JsonOptions);
    }

    private static IResult Logout(HttpContext context)
    {
        context.Response.Cookies.Delete(SessionCookieName, new CookieOptions { Path = "/" });
        return Results.NoContent();
    }

    private static IResult GetActorContext(HttpContext context)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        var actor = service.GetActorContext(session);
        return actor is null ? Denied("Access has changed.") : Results.Json(actor, JsonOptions);
    }

    private static IResult GetNavigation(HttpContext context)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        return Results.Json(service.GetNavigation(session), JsonOptions);
    }

    private static IResult GetHome(HttpContext context)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        return Results.Json(service.GetHome(session), JsonOptions);
    }

    private static IResult GetActivities(HttpContext context)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Json(service.GetActivities(session), JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Activity administration access.");
        }
    }

    private static IResult GetActivityDetail(HttpContext context, string activityId)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var detail = service.GetActivityDetail(session, activityId);
            return detail is null ? Protected() : Results.Json(detail, JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Activity administration access.");
        }
    }

    private static IResult GetEnrollment(HttpContext context, string activityId)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var enrollment = service.GetEnrollment(session, activityId);
            return enrollment is null ? Protected() : Results.Json(enrollment, JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Activity administration access.");
        }
    }

    private static IResult GetMyWork(HttpContext context)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var assignment = service.GetMyWorkAssignment(session, null);
            return assignment is null ? Protected() : Results.Json(assignment, JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Participant work access.");
        }
    }

    private static IResult GetSession(HttpContext context, string sessionId)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        var projection = service.GetSession(session, sessionId);
        return projection is null ? Protected() : Results.Json(projection, JsonOptions);
    }

    private static IResult ExecuteCommand(HttpContext context, BrowserCommandEnvelopeV1 command)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        var result = service.ExecuteCommand(session, command);
        var statusCode = result.Outcome switch
        {
            "denied" => StatusCodes.Status403Forbidden,
            "conflict" => StatusCodes.Status409Conflict,
            "uncertain" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status200OK,
        };

        return Results.Json(result, JsonOptions, statusCode: statusCode);
    }

    private static async Task GetSessionEvents(HttpContext context, string sessionId)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";

        await foreach (var evt in service.GetSessionEvents(session, sessionId).ToAsyncEnumerable())
        {
            var json = JsonSerializer.Serialize(evt, JsonOptions);
            await context.Response.WriteAsync($"data: {json}\n\n");
            await context.Response.Body.FlushAsync();
        }
    }

    private static IResult GetReviewWork(HttpContext context)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Json(service.GetReviewWork(session), JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Review work access.");
        }
    }

    private static IResult GetReviewCase(HttpContext context, string caseId)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var detail = service.GetReviewCase(session, caseId);
            return detail is null ? Protected() : Results.Json(detail, JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Review work access.");
        }
    }

    private static IResult GetReleaseWork(HttpContext context)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Json(service.GetReleaseWork(session), JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Release work access.");
        }
    }

    private static IResult GetReleaseDetail(HttpContext context, string releaseId)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var detail = service.GetReleaseDetail(session, releaseId);
            return detail is null ? Protected() : Results.Json(detail, JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Release work access.");
        }
    }

    private static IResult GetResults(HttpContext context)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Json(service.GetResults(session), JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Results access.");
        }
    }

    private static IResult GetResultDetail(HttpContext context, string resultId)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var detail = service.GetResultDetail(session, resultId);
            return detail is null ? Protected() : Results.Json(detail, JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Results access.");
        }
    }

    private static IResult GetGovernance(HttpContext context)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Json(service.GetGovernance(session), JsonOptions);
        }
        catch (SyntheticAccessDeniedException)
        {
            return Denied("You do not have Governance access.");
        }
    }

    private static IResult GetPlannedTier(HttpContext context, string moduleName)
    {
        var service = GetService(context);
        if (!service.IsEnabled)
        {
            return Disabled();
        }

        var session = ResolveSession(context, service);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        return Results.Json(service.GetPlannedTier(session, moduleName), JsonOptions);
    }
}

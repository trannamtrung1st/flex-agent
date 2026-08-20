using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.OpenRouter;

public sealed class OpenRouterModelExecutionAdapter(
    IInstalledModelDeploymentProfileRegistry profiles,
    IModelDeploymentCredentialCatalog catalog,
    IProviderCredentialSecretSource secrets,
    IOpenRouterInstalledConfigurationRegistry configurations,
    HttpMessageHandler? transport = null,
    bool privacyPreflightConfirmed = false) : IModelExecutionPort
{
    public const string AdapterContractVersion = OpenRouterAdapterContracts.AdapterContractVersion;

    public async Task<ModelExecutionAttemptResult> ExecuteAsync(
        ModelExecutionAttemptRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var startedAt = DateTimeOffset.UtcNow;
        if (!privacyPreflightConfirmed)
        {
            return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, null);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Fail(ExecutionAttemptOutcomeCategories.Cancelled, startedAt, request, null);
        }

        var resolved = TryResolve(request.FrozenDeployment, request.Ownership, out var failure);
        if (resolved is null)
        {
            return Fail(failure!, startedAt, request, null);
        }

        using var secret = await secrets.TryReadAsync(resolved.Value.SecretName, cancellationToken);
        if (secret is null)
        {
            return Fail(ExecutionFailureReasons.CredentialBindingFailed, startedAt, request, resolved.Value.Profile);
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(resolved.Value.Profile.ControlTimeout);
            var operation = timeoutCts.Token;
            using var lifetime = CreateClient();
            using var httpRequest = OpenRouterRequestFactory.CreateControl(
                resolved.Value.Profile,
                resolved.Value.Configuration,
                request.AgentInvocationId,
                request.Context,
                secret.Reveal());
            using var response = await lifetime.Client.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                operation);
            if (!response.IsSuccessStatusCode)
            {
                return Fail(MapStatus(response.StatusCode), startedAt, request, resolved.Value.Profile);
            }

            if (OpenRouterResponseParser.IsResponseCacheHit(response))
            {
                return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Value.Profile);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(operation);
            await using var bounded = new BoundedReadStream(stream, OpenRouterAdapterContracts.MaxControlEnvelopeUtf8Bytes);
            using var document = await JsonDocument.ParseAsync(bounded, cancellationToken: operation);
            if (!OpenRouterResponseParser.TryReadTerminalFacts(
                    document.RootElement,
                    resolved.Value.Profile.ResolvedModelVersion,
                    resolved.Value.Configuration.ExpectedReturnedProviderIdentity,
                    out var facts,
                    out _)
                || facts is null)
            {
                return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Value.Profile);
            }

            if (!OpenRouterResponseParser.TryReadControlContent(document.RootElement, out var content)
                || string.IsNullOrEmpty(content))
            {
                return Fail(ExecutionFailureReasons.MalformedControl, startedAt, request, resolved.Value.Profile);
            }

            var utf8 = System.Text.Encoding.UTF8.GetBytes(content);
            var outcome = AdmitControl(request, utf8);
            return outcome with
            {
                Provenance = CreateProvenance(
                    resolved.Value.Profile,
                    Outcome(outcome),
                    facts.InputTokens,
                    facts.OutputTokens,
                    request.ProviderAttemptId,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    ModelProviderRequestPhases.Control),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Fail(ExecutionAttemptOutcomeCategories.Cancelled, startedAt, request, resolved.Value.Profile);
        }
        catch (OperationCanceledException)
        {
            return Fail(ExecutionFailureReasons.ProviderTimeout, startedAt, request, resolved.Value.Profile);
        }
        catch (OpenRouterTransportLimitExceededException)
        {
            return Fail(ExecutionFailureReasons.MalformedControl, startedAt, request, resolved.Value.Profile);
        }
        catch (HttpRequestException)
        {
            return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Value.Profile);
        }
        catch (JsonException)
        {
            return Fail(ExecutionFailureReasons.MalformedControl, startedAt, request, resolved.Value.Profile);
        }
    }

    public async IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
        ModelContentStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!privacyPreflightConfirmed)
        {
            yield break;
        }

        var resolved = TryResolve(request.FrozenDeployment, request.Ownership, out _);
        if (resolved is null)
        {
            yield break;
        }

        using var secret = await secrets.TryReadAsync(resolved.Value.SecretName, cancellationToken);
        if (secret is null)
        {
            yield break;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(resolved.Value.Profile.ContentTimeout);
        var operation = timeoutCts.Token;
        var startedAt = DateTimeOffset.UtcNow;
        var publishedFragment = false;
        var lifetime = CreateClient();
        HttpResponseMessage? response = null;
        Stream? stream = null;
        string? startupFailure = null;
        try
        {
            using var httpRequest = OpenRouterRequestFactory.CreateContent(
                resolved.Value.Profile,
                resolved.Value.Configuration,
                request.AgentInvocationId,
                request.Context,
                secret.Reveal());
            response = await lifetime.Client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, operation);
            if (!response.IsSuccessStatusCode)
            {
                startupFailure = MapStatus(response.StatusCode);
            }
            else if (OpenRouterResponseParser.IsResponseCacheHit(response))
            {
                startupFailure = ExecutionFailureReasons.ProviderUnavailable;
            }
            else
            {
                stream = await response.Content.ReadAsStreamAsync(operation);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            startupFailure = ExecutionAttemptOutcomeCategories.Cancelled;
        }
        catch (OperationCanceledException)
        {
            startupFailure = ExecutionFailureReasons.ProviderTimeout;
        }
        catch (HttpRequestException)
        {
            startupFailure = ExecutionFailureReasons.ProviderUnavailable;
        }

        if (startupFailure is not null)
        {
            lifetime.Dispose();
            response?.Dispose();
            yield return ContentFailure(resolved.Value.Profile, startupFailure, request.ProviderAttemptId, startedAt);
            if (string.Equals(startupFailure, ExecutionAttemptOutcomeCategories.Cancelled, StringComparison.Ordinal))
            {
                throw new OperationCanceledException(cancellationToken);
            }

            yield break;
        }

        OpenRouterTerminalFacts? facts = null;
        var sawTerminal = false;
        var sawDone = false;
        var visibleUtf8Bytes = 0;
        await using var enumerator = OpenRouterSseParser.ReadDataPayloadsAsync(stream!, operation)
            .GetAsyncEnumerator(operation);
        while (true)
        {
            bool moved;
            string? failureReason = null;
            try
            {
                moved = await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                failureReason = ExecutionAttemptOutcomeCategories.Cancelled;
                moved = false;
            }
            catch (OperationCanceledException)
            {
                failureReason = ExecutionFailureReasons.ProviderTimeout;
                moved = false;
            }
            catch (OpenRouterTransportLimitExceededException)
            {
                failureReason = publishedFragment
                    ? ExecutionFailureReasons.ProviderUnavailable
                    : ExecutionFailureReasons.MalformedControl;
                moved = false;
            }
            catch (HttpRequestException)
            {
                failureReason = ExecutionFailureReasons.ProviderUnavailable;
                moved = false;
            }

            if (failureReason is not null)
            {
                yield return ContentFailure(resolved.Value.Profile, failureReason, request.ProviderAttemptId, startedAt);
                await enumerator.DisposeAsync();
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }

                response?.Dispose();
                lifetime.Dispose();
                if (string.Equals(failureReason, ExecutionAttemptOutcomeCategories.Cancelled, StringComparison.Ordinal))
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                yield break;
            }

            if (!moved)
            {
                break;
            }

            var payload = enumerator.Current;
            if (sawDone)
            {
                yield return ContentFailure(
                    resolved.Value.Profile,
                    ExecutionFailureReasons.ProviderUnavailable,
                    request.ProviderAttemptId,
                    startedAt);
                await enumerator.DisposeAsync();
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }

                response?.Dispose();
                lifetime.Dispose();
                yield break;
            }

            if (string.Equals(payload, "[DONE]", StringComparison.Ordinal))
            {
                if (!sawTerminal)
                {
                    yield return ContentFailure(
                        resolved.Value.Profile,
                        ExecutionFailureReasons.ProviderUnavailable,
                        request.ProviderAttemptId,
                        startedAt);
                    await enumerator.DisposeAsync();
                    if (stream is not null)
                    {
                        await stream.DisposeAsync();
                    }

                    response?.Dispose();
                    lifetime.Dispose();
                    yield break;
                }

                sawDone = true;
                continue;
            }

            if (sawTerminal)
            {
                yield return ContentFailure(
                    resolved.Value.Profile,
                    ExecutionFailureReasons.ProviderUnavailable,
                    request.ProviderAttemptId,
                    startedAt);
                await enumerator.DisposeAsync();
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }

                response?.Dispose();
                lifetime.Dispose();
                yield break;
            }

            JsonDocument? document = null;
            string? parseFailure = null;
            try
            {
                document = JsonDocument.Parse(payload);
            }
            catch (JsonException)
            {
                parseFailure = publishedFragment
                    ? ExecutionFailureReasons.ProviderUnavailable
                    : ExecutionFailureReasons.MalformedControl;
            }

            if (parseFailure is not null)
            {
                yield return ContentFailure(
                    resolved.Value.Profile,
                    parseFailure,
                    request.ProviderAttemptId,
                    startedAt);
                await enumerator.DisposeAsync();
                if (stream is not null)
                {
                    await stream.DisposeAsync();
                }

                response?.Dispose();
                lifetime.Dispose();
                yield break;
            }

            var parsed = document ?? throw new InvalidOperationException("OpenRouter SSE JSON parse produced no document.");
            using (parsed)
            {
                if (OpenRouterResponseParser.TryReadDelta(document.RootElement, out var delta) && !string.IsNullOrEmpty(delta))
                {
                    visibleUtf8Bytes += System.Text.Encoding.UTF8.GetByteCount(delta);
                    if (visibleUtf8Bytes > OpenRouterAdapterContracts.MaxVisibleContentUtf8Bytes)
                    {
                        yield return ContentFailure(
                            resolved.Value.Profile,
                            publishedFragment
                                ? ExecutionFailureReasons.ProviderUnavailable
                                : ExecutionFailureReasons.MalformedControl,
                            request.ProviderAttemptId,
                            startedAt);
                        await enumerator.DisposeAsync();
                        if (stream is not null)
                        {
                            await stream.DisposeAsync();
                        }

                        response?.Dispose();
                        lifetime.Dispose();
                        yield break;
                    }

                    publishedFragment = true;
                    yield return new ModelContentTextDelta(delta);
                }

                if (document.RootElement.TryGetProperty("openrouter_metadata", out _))
                {
                    if (sawTerminal
                        || !OpenRouterResponseParser.TryReadTerminalFacts(
                            document.RootElement,
                            resolved.Value.Profile.ResolvedModelVersion,
                            resolved.Value.Configuration.ExpectedReturnedProviderIdentity,
                            out facts,
                            out _))
                    {
                        yield return ContentFailure(
                            resolved.Value.Profile,
                            ExecutionFailureReasons.ProviderUnavailable,
                            request.ProviderAttemptId,
                            startedAt);
                        await enumerator.DisposeAsync();
                        if (stream is not null)
                        {
                            await stream.DisposeAsync();
                        }

                        response?.Dispose();
                        lifetime.Dispose();
                        yield break;
                    }

                    sawTerminal = true;
                }
            }
        }

        await enumerator.DisposeAsync();
        if (stream is not null)
        {
            await stream.DisposeAsync();
        }

        response?.Dispose();
        lifetime.Dispose();
        if (!sawDone || facts is null)
        {
            yield return ContentFailure(
                resolved.Value.Profile,
                ExecutionFailureReasons.ProviderUnavailable,
                request.ProviderAttemptId,
                startedAt);
            yield break;
        }

        yield return new ModelContentCompleted
        {
            Provenance = CreateProvenance(
                resolved.Value.Profile,
                ExecutionAttemptOutcomeCategories.ContentProduced,
                facts.InputTokens,
                facts.OutputTokens,
                request.ProviderAttemptId,
                startedAt,
                DateTimeOffset.UtcNow,
                ModelProviderRequestPhases.Content),
        };
    }

    private (InstalledModelDeploymentProfile Profile, OpenRouterInstalledConfiguration Configuration, string SecretName)? TryResolve(
        FrozenModelDeploymentBinding? frozen,
        SessionOwnership ownership,
        out string? failure)
    {
        failure = ExecutionFailureReasons.CredentialBindingFailed;
        if (frozen is null)
        {
            return null;
        }

        var resolution = FrozenModelDeploymentResolver.Resolve(ownership, frozen, profiles, catalog);
        if (!resolution.Succeeded || resolution.Profile is null || resolution.SecretName is null)
        {
            return null;
        }

        if (!string.Equals(resolution.Profile.AdapterKind, ModelDeploymentAdapterKinds.OpenRouter, StringComparison.Ordinal)
            || !string.Equals(resolution.Profile.AdapterContractVersion, AdapterContractVersion, StringComparison.Ordinal))
        {
            return null;
        }

        var configuration = configurations.TryGet(
            resolution.Profile.ProfileId,
            resolution.Profile.ProfileVersion,
            resolution.Profile.ProfileDigest);
        if (configuration is null
            || !string.Equals(configuration.AdapterConfigurationDigest, resolution.Profile.AdapterConfigurationDigest, StringComparison.Ordinal)
            || !string.Equals(configuration.Profile.RequestedModel, resolution.Profile.RequestedModel, StringComparison.Ordinal)
            || !string.Equals(configuration.Profile.ResolvedModelVersion, resolution.Profile.ResolvedModelVersion, StringComparison.Ordinal))
        {
            return null;
        }

        failure = null;
        return (resolution.Profile, configuration, resolution.SecretName);
    }

    private ClientLifetime CreateClient()
    {
        var inner = transport ?? new HttpClientHandler { AllowAutoRedirect = false };
        var bounded = new OpenRouterDestinationHandler(inner);
        var http = new HttpClient(bounded, disposeHandler: transport is null)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        return new ClientLifetime(http);
    }

    private static ModelExecutionAttemptResult AdmitControl(ModelExecutionAttemptRequest request, byte[] utf8Json)
    {
        if (utf8Json.Length > request.MaxControlUtf8Bytes)
        {
            return new ModelExecutionFailed(ExecutionFailureReasons.MalformedControl);
        }

        if (!ValidatedAgentDecisionEnvelope.TryAdmit(utf8Json, out var admitted, out var failureReasonCategory)
            || admitted is null)
        {
            return new ModelExecutionFailed(failureReasonCategory);
        }

        if (!string.Equals(admitted.Envelope.InvocationId, request.AgentInvocationId, StringComparison.Ordinal))
        {
            return new ModelExecutionFailed(ExecutionFailureReasons.MalformedControl);
        }

        return new ModelExecutionStructuredControl(admitted);
    }

    private static string MapStatus(HttpStatusCode status) =>
        (int)status switch
        {
            408 or 504 => ExecutionFailureReasons.ProviderTimeout,
            _ => ExecutionFailureReasons.ProviderUnavailable,
        };

    private static string Outcome(ModelExecutionAttemptResult result) =>
        result is ModelExecutionStructuredControl
            ? ExecutionAttemptOutcomeCategories.DecisionProduced
            : result is ModelExecutionFailed failed
                ? failed.ReasonCategory
                : ExecutionFailureReasons.MalformedControl;

    private static ModelExecutionFailed Fail(
        string reason,
        DateTimeOffset startedAt,
        ModelExecutionAttemptRequest request,
        InstalledModelDeploymentProfile? profile) =>
        new ModelExecutionFailed(reason)
        {
            Provenance = profile is null
                ? null
                : CreateProvenance(
                    profile,
                    reason,
                    null,
                    null,
                    request.ProviderAttemptId,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    ModelProviderRequestPhases.Control),
        };

    private static ModelProviderAttemptProvenance CreateProvenance(
        InstalledModelDeploymentProfile profile,
        string outcome,
        int? inputTokens,
        int? outputTokens,
        string? providerAttemptId,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        string phase) =>
        new(
            profile.AdapterKind,
            AdapterContractVersion,
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.RequestedModel,
            profile.ResolvedModelVersion,
            outcome,
            inputTokens,
            outputTokens,
            string.IsNullOrWhiteSpace(providerAttemptId) ? null : $"pref.{providerAttemptId}",
            startedAt,
            completedAt,
            phase,
            providerAttemptId);

    private static ModelContentFailed ContentFailure(
        InstalledModelDeploymentProfile profile,
        string reason,
        string? providerAttemptId,
        DateTimeOffset startedAt) =>
        new(reason)
        {
            Provenance = CreateProvenance(
                profile,
                reason,
                null,
                null,
                providerAttemptId,
                startedAt,
                DateTimeOffset.UtcNow,
                ModelProviderRequestPhases.Content),
        };

    private sealed class ClientLifetime(HttpClient client) : IDisposable
    {
        public HttpClient Client { get; } = client;

        public void Dispose() => Client.Dispose();
    }
}

public static class OpenRouterLiveQualification
{
    public const string EnableEnvironmentVariable = "FLEXAGENT_LIVE_OPENROUTER_QUALIFICATION";
    public const string PrivacyEnvironmentVariable = "FLEXAGENT_OPENROUTER_PRIVACY_PREFLIGHT";
    public const string BudgetPathEnvironmentVariable = "FLEXAGENT_OPENROUTER_QUALIFICATION_BUDGET_PATH";
    public const int MaxInferenceRequests = 12;

    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable(EnableEnvironmentVariable), "1", StringComparison.Ordinal);

    public static bool PrivacyPreflightConfirmed =>
        string.Equals(Environment.GetEnvironmentVariable(PrivacyEnvironmentVariable), "1", StringComparison.Ordinal);
}

public sealed class OpenRouterDiscoveryClient(HttpMessageHandler? transport = null)
{
    public async Task<OpenRouterDiscoveryCandidate?> DiscoverAsync(
        string secret,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        if (transport is null
            && (!OpenRouterLiveQualification.IsEnabled || !OpenRouterLiveQualification.PrivacyPreflightConfirmed))
        {
            return null;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(OpenRouterAdapterContracts.ControlTimeout);
        var operation = timeoutCts.Token;
        var inner = transport ?? new HttpClientHandler { AllowAutoRedirect = false };
        using var bounded = new OpenRouterDestinationHandler(inner);
        using var client = new HttpClient(bounded, disposeHandler: transport is null)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        using var request = OpenRouterRequestFactory.CreateDiscovery(secret);
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, operation);
            if (!response.IsSuccessStatusCode || OpenRouterResponseParser.IsResponseCacheHit(response))
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(operation);
            await using var boundedBody = new BoundedReadStream(stream, OpenRouterAdapterContracts.MaxControlEnvelopeUtf8Bytes);
            using var document = await JsonDocument.ParseAsync(boundedBody, cancellationToken: operation);
            if (!document.RootElement.TryGetProperty("model", out var model)
                || model.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(model.GetString())
                || string.Equals(model.GetString(), OpenRouterAdapterContracts.DiscoveryModel, StringComparison.Ordinal)
                || !model.GetString()!.EndsWith(":free", StringComparison.Ordinal))
            {
                return null;
            }

            if (!OpenRouterResponseParser.TryReadSelectedProvider(
                    document.RootElement,
                    out var selectedProvider)
                || string.IsNullOrWhiteSpace(selectedProvider))
            {
                return null;
            }

            if (!OpenRouterResponseParser.TryReadTerminalFacts(
                    document.RootElement,
                    model.GetString()!,
                    selectedProvider,
                    out var facts,
                    out _)
                || facts is null)
            {
                return null;
            }

            return new OpenRouterDiscoveryCandidate(facts.ReturnedModel, facts.SelectedProvider);
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException or JsonException or OpenRouterTransportLimitExceededException)
        {
            return null;
        }
    }
}

public sealed record OpenRouterDiscoveryCandidate(string Model, string ProviderIdentity);

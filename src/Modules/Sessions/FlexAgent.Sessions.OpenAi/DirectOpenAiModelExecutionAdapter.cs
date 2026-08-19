using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using OpenAI;
using OpenAI.Chat;

namespace FlexAgent.Sessions.OpenAi;

public sealed class DirectOpenAiModelExecutionAdapter(
    IInstalledModelDeploymentProfileRegistry profiles,
    IModelDeploymentCredentialCatalog catalog,
    IProviderCredentialSecretSource secrets,
    HttpMessageHandler? transport = null) : IModelExecutionPort
{
    public const string AdapterContractVersion = "sessions.openai.v1";

    public async Task<ModelExecutionAttemptResult> ExecuteAsync(
        ModelExecutionAttemptRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var startedAt = DateTimeOffset.UtcNow;
        var resolved = TryResolve(request.FrozenDeployment, request.Ownership, out var failure);
        if (resolved is null)
        {
            return Fail(failure!, startedAt, request, profile: null, usage: null);
        }

        using var secret = await secrets.TryReadAsync(resolved.Value.SecretName, cancellationToken);
        if (secret is null)
        {
            return Fail(
                ExecutionFailureReasons.CredentialBindingFailed,
                startedAt,
                request,
                resolved.Value.Profile,
                null);
        }

        try
        {
            using var lifetime = CreateClient(resolved.Value.Profile, secret, resolved.Value.Profile.ControlTimeout);
            var chat = lifetime.Client.GetChatClient(resolved.Value.Profile.ResolvedModelVersion);
            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = resolved.Value.Profile.MaxOutputTokens,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            };
            var completion = await chat.CompleteChatAsync(
                BuildMinimizedMessages(request.AgentInvocationId, request.Context, control: true),
                options,
                cancellationToken);
            if (completion.Value.Content.Count == 0 || string.IsNullOrEmpty(completion.Value.Content[0].Text))
            {
                return Fail(ExecutionFailureReasons.MalformedControl, startedAt, request, resolved.Value.Profile, null);
            }

            if (string.IsNullOrWhiteSpace(completion.Value.Model)
                || !ModelIdentityMatches(resolved.Value.Profile.ResolvedModelVersion, completion.Value.Model))
            {
                return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Value.Profile, null);
            }

            var utf8 = System.Text.Encoding.UTF8.GetBytes(completion.Value.Content[0].Text);
            var outcome = DeterministicControl(request, utf8);
            return outcome with
            {
                Provenance = CreateProvenance(
                    resolved.Value.Profile,
                    Outcome(outcome),
                    completion.Value.Usage?.InputTokenCount,
                    completion.Value.Usage?.OutputTokenCount,
                    request.ProviderAttemptId,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    resolved.Value.Profile.ResolvedModelVersion,
                    ModelProviderRequestPhases.Control),
            };
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            return Fail(ExecutionAttemptOutcomeCategories.Cancelled, startedAt, request, resolved.Value.Profile, null);
        }
        catch (TaskCanceledException)
        {
            return Fail(ExecutionFailureReasons.ProviderTimeout, startedAt, request, resolved.Value.Profile, null);
        }
        catch (ClientResultException exception)
        {
            return Fail(MapStatus(exception.Status), startedAt, request, resolved.Value.Profile, null);
        }
        catch (HttpRequestException)
        {
            return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Value.Profile, null);
        }
    }

    public async IAsyncEnumerable<ModelContentEvent> StreamParticipantVisibleContentAsync(
        ModelContentStreamRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
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

        using var lifetime = CreateClient(resolved.Value.Profile, secret, resolved.Value.Profile.ContentTimeout);
        var chat = lifetime.Client.GetChatClient(resolved.Value.Profile.ResolvedModelVersion);
        ChatCompletionOptions options = new()
        {
            MaxOutputTokenCount = resolved.Value.Profile.MaxOutputTokens,
        };
        var startedAt = DateTimeOffset.UtcNow;
        var observedResolvedModel = false;

        await using var enumerator = chat.CompleteChatStreamingAsync(
            BuildMinimizedMessages(request.AgentInvocationId, request.Context, control: false),
            options,
            cancellationToken).GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool moved;
            try
            {
                moved = await enumerator.MoveNextAsync();
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            catch (TaskCanceledException)
            {
                yield break;
            }
            catch (ClientResultException)
            {
                yield break;
            }
            catch (HttpRequestException)
            {
                yield break;
            }

            if (!moved)
            {
                if (!observedResolvedModel)
                {
                    yield break;
                }

                yield return new ModelContentCompleted
                {
                    Provenance = CreateProvenance(
                        resolved.Value.Profile,
                        ExecutionAttemptOutcomeCategories.ContentProduced,
                        null,
                        null,
                        request.ProviderAttemptId,
                        startedAt,
                        DateTimeOffset.UtcNow,
                        resolved.Value.Profile.ResolvedModelVersion,
                        ModelProviderRequestPhases.Content),
                };
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(enumerator.Current.Model))
            {
                if (!ModelIdentityMatches(resolved.Value.Profile.ResolvedModelVersion, enumerator.Current.Model))
                {
                    yield break;
                }

                observedResolvedModel = true;
            }

            foreach (var part in enumerator.Current.ContentUpdate)
            {
                if (part.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(part.Text))
                {
                    yield return new ModelContentTextDelta(part.Text);
                }
            }
        }
    }

    private (InstalledModelDeploymentProfile Profile, string SecretName)? TryResolve(
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

        if (!string.Equals(resolution.Profile.AdapterKind, ModelDeploymentAdapterKinds.DirectOpenAi, StringComparison.Ordinal)
            || !string.Equals(resolution.Profile.AdapterContractVersion, AdapterContractVersion, StringComparison.Ordinal))
        {
            return null;
        }

        failure = null;
        return (resolution.Profile, resolution.SecretName);
    }

    private ClientLifetime CreateClient(InstalledModelDeploymentProfile profile, ProviderSecret secret, TimeSpan timeout)
    {
        var inner = transport ?? new HttpClientHandler { AllowAutoRedirect = false };
        var bounded = new ApprovedOriginHandler(profile.ApprovedHttpsOrigin, inner);
        var http = new HttpClient(bounded, disposeHandler: transport is null)
        {
            Timeout = timeout,
        };
        var options = new OpenAIClientOptions
        {
            Endpoint = ApprovedHttpsOrigin.Canonicalize(profile.ApprovedHttpsOrigin),
            Transport = new HttpClientPipelineTransport(http),
            RetryPolicy = new ClientRetryPolicy(0),
            NetworkTimeout = timeout,
        };
        var client = new OpenAIClient(new ApiKeyCredential(secret.Reveal()), options);
        return new ClientLifetime(client, http);
    }

    private static List<ChatMessage> BuildMinimizedMessages(
        string invocationId,
        InvocationContext? context,
        bool control)
    {
        var role = control
            ? "Return one JSON Agent Decision envelope and no participant-visible prose."
            : "Return only participant-visible message text.";
        return
        [
            ChatMessage.CreateSystemMessage(role),
            ChatMessage.CreateUserMessage(
                ProviderSafeInvocationContextSerializer.Serialize(invocationId, context)),
        ];
    }

    private static ModelExecutionAttemptResult DeterministicControl(ModelExecutionAttemptRequest request, byte[] utf8Json)
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

    private static string MapStatus(int status) =>
        status switch
        {
            408 or 504 => ExecutionFailureReasons.ProviderTimeout,
            >= 500 => ExecutionFailureReasons.ProviderUnavailable,
            429 => ExecutionFailureReasons.ProviderUnavailable,
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
        InstalledModelDeploymentProfile? profile,
        ChatTokenUsage? usage) =>
        new ModelExecutionFailed(reason)
        {
            Provenance = profile is null
                ? null
                : CreateProvenance(
                    profile,
                    reason,
                    usage?.InputTokenCount,
                    usage?.OutputTokenCount,
                    request.ProviderAttemptId,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    profile.ResolvedModelVersion,
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
        string resolvedModel,
        string phase) =>
        new(
            profile.AdapterKind,
            AdapterContractVersion,
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.RequestedModel,
            resolvedModel,
            outcome,
            inputTokens,
            outputTokens,
            string.IsNullOrWhiteSpace(providerAttemptId) ? null : $"pref.{providerAttemptId}",
            startedAt,
            completedAt,
            phase,
            providerAttemptId);

    private static bool ModelIdentityMatches(string frozenResolvedModel, string providerModel) =>
        string.Equals(providerModel, frozenResolvedModel, StringComparison.Ordinal);

    private sealed class ClientLifetime(OpenAIClient client, HttpClient http) : IDisposable
    {
        public OpenAIClient Client { get; } = client;

        public void Dispose() => http.Dispose();
    }
}

internal sealed class ApprovedOriginHandler(Uri approvedOrigin, HttpMessageHandler inner) : DelegatingHandler(inner)
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is null || !ApprovedOrigin.IsAllowed(request.RequestUri, approvedOrigin))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                ReasonPhrase = "origin_denied",
            });
        }

        return base.SendAsync(request, cancellationToken);
    }
}

internal static class ApprovedOrigin
{
    public static bool IsAllowed(Uri destination, Uri approved)
    {
        if (!string.Equals(destination.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(approved.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(destination.UserInfo)
            || !string.Equals(destination.Host, approved.Host, StringComparison.OrdinalIgnoreCase)
            || EffectivePort(destination) != EffectivePort(approved))
        {
            return false;
        }

        if (IPAddress.TryParse(destination.Host, out var address)
            && (IPAddress.IsLoopback(address)
                || IsLinkLocalOrMetadataOrPrivate(address)))
        {
            return false;
        }

        return true;
    }

    private static int EffectivePort(Uri uri) => uri.IsDefaultPort ? 443 : uri.Port;

    private static bool IsLinkLocalOrMetadataOrPrivate(IPAddress address)
    {
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length == 4)
        {
            if (bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254)
                || (bytes[0] == 169 && bytes[1] == 254 && bytes[2] == 169 && bytes[3] == 254))
            {
                return true;
            }

            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }
        }

        return false;
    }
}

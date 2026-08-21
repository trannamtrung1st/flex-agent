using System.ClientModel;
using System.ClientModel.Primitives;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using OpenAI;
using OpenAI.Chat;

namespace FlexAgent.Sessions.OpenAiCompatible;

public sealed class OpenAiCompatibleModelExecutionAdapter(
    IInstalledModelDeploymentProfileRegistry profiles,
    IModelDeploymentCredentialCatalog catalog,
    IProviderCredentialSecretSource secrets,
    IOpenAiCompatibleInstalledConfigurationRegistry configurations,
    HttpMessageHandler? transport = null,
    IEndpointAddressResolver? resolver = null) : IModelExecutionPort
{
    public const string AdapterContractVersion = OpenAiCompatibleAdapterContracts.AdapterContractVersion;

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

        using var secret = await secrets.TryReadAsync(resolved.SecretName, cancellationToken);
        if (secret is null)
        {
            return Fail(
                ExecutionFailureReasons.CredentialBindingFailed,
                startedAt,
                request,
                resolved.Profile,
                null);
        }

        try
        {
            using var lifetime = CreateClient(resolved.Configuration, secret, resolved.Profile.ControlTimeout);
            var chat = lifetime.Client.GetChatClient(resolved.Profile.ResolvedModelVersion);
            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = resolved.Profile.MaxOutputTokens,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            };
            var completion = await chat.CompleteChatAsync(
                BuildMinimizedMessages(request.AgentInvocationId, request.Context, control: true),
                options,
                cancellationToken);
            if (completion.Value.Content.Count == 0 || string.IsNullOrEmpty(completion.Value.Content[0].Text))
            {
                return Fail(ExecutionFailureReasons.MalformedControl, startedAt, request, resolved.Profile, null);
            }

            if (string.IsNullOrWhiteSpace(completion.Value.Model)
                || !ModelIdentityMatches(resolved.Profile.ResolvedModelVersion, completion.Value.Model))
            {
                return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Profile, null);
            }

            var utf8 = System.Text.Encoding.UTF8.GetBytes(completion.Value.Content[0].Text);
            var outcome = DeterministicControl(request, utf8);
            return outcome with
            {
                Provenance = CreateProvenance(
                    resolved.Profile,
                    Outcome(outcome),
                    completion.Value.Usage?.InputTokenCount,
                    completion.Value.Usage?.OutputTokenCount,
                    request.ProviderAttemptId,
                    startedAt,
                    DateTimeOffset.UtcNow,
                    resolved.Profile.ResolvedModelVersion,
                    ModelProviderRequestPhases.Control),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Fail(ExecutionAttemptOutcomeCategories.Cancelled, startedAt, request, resolved.Profile, null);
        }
        catch (TaskCanceledException)
        {
            return Fail(ExecutionFailureReasons.ProviderTimeout, startedAt, request, resolved.Profile, null);
        }
        catch (ClientResultException exception)
        {
            return Fail(MapStatus(exception.Status), startedAt, request, resolved.Profile, null);
        }
        catch (HttpRequestException)
        {
            return Fail(ExecutionFailureReasons.ProviderUnavailable, startedAt, request, resolved.Profile, null);
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
            yield return new ModelContentFailed(ExecutionFailureReasons.CredentialBindingFailed);
            yield break;
        }

        using var secret = await secrets.TryReadAsync(resolved.SecretName, cancellationToken);
        if (secret is null)
        {
            yield return ContentFailure(
                resolved.Profile,
                ExecutionFailureReasons.CredentialBindingFailed,
                request.ProviderAttemptId,
                DateTimeOffset.UtcNow);
            yield break;
        }

        using var lifetime = CreateClient(resolved.Configuration, secret, resolved.Profile.ContentTimeout);
        var chat = lifetime.Client.GetChatClient(resolved.Profile.ResolvedModelVersion);
        ChatCompletionOptions options = new()
        {
            MaxOutputTokenCount = resolved.Profile.MaxOutputTokens,
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
            catch (TaskCanceledException)
            {
                failureReason = ExecutionFailureReasons.ProviderTimeout;
                moved = false;
            }
            catch (ClientResultException)
            {
                failureReason = ExecutionFailureReasons.ProviderUnavailable;
                moved = false;
            }
            catch (HttpRequestException)
            {
                failureReason = ExecutionFailureReasons.ProviderUnavailable;
                moved = false;
            }

            if (failureReason is not null)
            {
                yield return ContentFailure(
                    resolved.Profile,
                    failureReason,
                    request.ProviderAttemptId,
                    startedAt);
                if (string.Equals(failureReason, ExecutionAttemptOutcomeCategories.Cancelled, StringComparison.Ordinal))
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                yield break;
            }

            if (!moved)
            {
                if (!observedResolvedModel)
                {
                    yield return ContentFailure(
                        resolved.Profile,
                        ExecutionFailureReasons.ProviderUnavailable,
                        request.ProviderAttemptId,
                        startedAt);
                    yield break;
                }

                yield return new ModelContentCompleted
                {
                    Provenance = CreateProvenance(
                        resolved.Profile,
                        ExecutionAttemptOutcomeCategories.ContentProduced,
                        null,
                        null,
                        request.ProviderAttemptId,
                        startedAt,
                        DateTimeOffset.UtcNow,
                        resolved.Profile.ResolvedModelVersion,
                        ModelProviderRequestPhases.Content),
                };
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(enumerator.Current.Model))
            {
                if (!ModelIdentityMatches(resolved.Profile.ResolvedModelVersion, enumerator.Current.Model))
                {
                    yield return ContentFailure(
                        resolved.Profile,
                        ExecutionFailureReasons.ProviderUnavailable,
                        request.ProviderAttemptId,
                        startedAt);
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

    private ResolvedExecution? TryResolve(
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

        if (string.Equals(resolution.Profile.AdapterKind, OpenAiCompatibleAdapterContracts.HistoricalAdapterKind, StringComparison.Ordinal)
            || string.Equals(resolution.Profile.AdapterContractVersion, OpenAiCompatibleAdapterContracts.HistoricalAdapterContractVersion, StringComparison.Ordinal)
            || !string.Equals(resolution.Profile.AdapterKind, OpenAiCompatibleAdapterContracts.AdapterKind, StringComparison.Ordinal)
            || !string.Equals(resolution.Profile.AdapterContractVersion, AdapterContractVersion, StringComparison.Ordinal))
        {
            return null;
        }

        var configuration = configurations.TryGet(
            resolution.Profile.ProfileId,
            resolution.Profile.ProfileVersion,
            resolution.Profile.ProfileDigest);
        if (configuration is null
            || !string.Equals(configuration.AdapterConfigurationDigest, resolution.Profile.AdapterConfigurationDigest, StringComparison.Ordinal))
        {
            return null;
        }

        failure = null;
        return new ResolvedExecution(resolution.Profile, resolution.SecretName, configuration);
    }

    private ClientLifetime CreateClient(
        OpenAiCompatibleInstalledConfiguration configuration,
        ProviderSecret secret,
        TimeSpan timeout)
    {
        var bounded = OpenAiCompatibleTransportFactory.Create(configuration, transport, resolver);
        var http = new HttpClient(bounded, disposeHandler: transport is null)
        {
            Timeout = timeout,
        };
        var options = new OpenAIClientOptions
        {
            Endpoint = configuration.Endpoint,
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
                profile.ResolvedModelVersion,
                ModelProviderRequestPhases.Content),
        };

    private static bool ModelIdentityMatches(string frozenResolvedModel, string providerModel) =>
        string.Equals(providerModel, frozenResolvedModel, StringComparison.Ordinal);

    private sealed record ResolvedExecution(
        InstalledModelDeploymentProfile Profile,
        string SecretName,
        OpenAiCompatibleInstalledConfiguration Configuration);

    private sealed class ClientLifetime(OpenAIClient client, HttpClient http) : IDisposable
    {
        public OpenAIClient Client { get; } = client;

        public void Dispose() => http.Dispose();
    }
}

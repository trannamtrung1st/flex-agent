using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using Npgsql;

namespace FlexAgent.Postgres.Integration.Tests.Support;

internal static class SessionPersistenceFixtures
{
    internal static readonly DateTimeOffset DefaultHardEndAtUtc =
        new(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

    internal static HostedFrozenTimingPolicy DefaultFrozenTimingPolicy() =>
        HostedFrozenTimingPolicy.UnboundedPolicy with { HardEndAtUtc = DefaultHardEndAtUtc };

    internal static Task InsertFrozenTimingAsync(
        NpgsqlTransaction transaction,
        SessionOwnership ownership,
        HostedFrozenTimingPolicy policy) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_frozen_timing (
                    organization_id, session_id, document, created_at)
                VALUES (
                    @OrganizationId,
                    @SessionId,
                    CAST(@Document AS jsonb),
                    NOW())
                """,
                new
                {
                    ownership.OrganizationId,
                    ownership.SessionId,
                    Document = HostedSessionFrozenTiming.ToDocumentJson(policy),
                },
                transaction));

    internal static async Task InsertActiveAsync(
        PostgresSessionRuntimeRepository repository,
        SessionOwnership ownership,
        SessionRuntime session,
        TrustedRuntimeActor participantActor,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default,
        AuthorizedServiceDelegationIssue? timerLaneDelegation = null,
        ICommitAuthorizationKernel? authorizationKernel = null,
        AuthorizedServiceDelegationIssue? invocationExecuteDelegation = null,
        HostedFrozenTimingPolicy? frozenTiming = null,
        bool seedDefaultFrozenTiming = true)
    {
        await repository.InsertActiveAsync(
            ownership,
            session,
            participantActor,
            transaction,
            cancellationToken,
            timerLaneDelegation,
            authorizationKernel,
            invocationExecuteDelegation);
        if (frozenTiming is not null)
        {
            await InsertFrozenTimingAsync(transaction, ownership, frozenTiming);
            return;
        }

        if (seedDefaultFrozenTiming)
        {
            await InsertFrozenTimingAsync(transaction, ownership, DefaultFrozenTimingPolicy());
        }
    }

    internal static PostgresSessionRuntimeRepository RuntimeRepository(
        ISessionAttemptTerminalSink? sink = null) =>
        new(sink ?? IgnoringSessionAttemptTerminalSink.Instance);

    internal static FrozenTextSessionRuntimePolicy ResolveEnabledTimerPolicy(
        int cooldownSeconds = 5,
        int maxTimerTriggeredInvocations = 8)
    {
        var values = new RuntimePolicyEffectiveValues
        {
            InvocationContractVersion = RuntimeContractVersions.InvocationV1,
            DecisionContractVersion = RuntimeContractVersions.DecisionV1,
            DecisionValidationPolicyVersion = RuntimeContractVersions.DecisionValidationPolicyV1,
            DecisionSchemaBindings =
            [
                new DecisionTypeSchemaBinding(RuntimeDecisionTypes.EmitMessage, RuntimeContractVersions.AgentDecisionSchemaV1),
                new DecisionTypeSchemaBinding(RuntimeDecisionTypes.NoAction, RuntimeContractVersions.AgentDecisionSchemaV1),
            ],
            PermittedNonTimerTriggers =
            [
                new RuntimeTriggerDescriptor(
                    RuntimeTriggerIdentifiers.ParticipantInputFamily,
                    RuntimeTriggerIdentifiers.ParticipantMessageType),
                new RuntimeTriggerDescriptor(
                    RuntimeTriggerIdentifiers.WorkflowEventFamily,
                    RuntimeTriggerIdentifiers.AgentOpeningType),
                new RuntimeTriggerDescriptor(
                    RuntimeTriggerIdentifiers.WorkflowEventFamily,
                    RuntimeTriggerIdentifiers.AgentClosingType),
            ],
            PermittedDecisionTypes = [RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.NoAction],
            AgentInitiatedOpeningPermitted = true,
            AgentInitiatedClosingPermitted = true,
            NoActionPermitted = true,
            InvocationBounds = new InvocationBounds(3, 10, 0, cooldownSeconds, 30),
            StreamingPublicationBounds = new StreamingPublicationBounds(512, 40, 64, 8_192, 2),
            TimerLane = new TimerLanePolicyValues
            {
                Enabled = true,
                DefaultDelay = "PT5M",
                MinRequestedDelay = "PT1M",
                MaxRequestedDelay = "PT30M",
                ClockBasis = TimerLaneClockBasis.ActiveSessionTime,
                PermittedStages = ["active"],
                PermittedDecisionTypes = [RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.NoAction],
                Budgets = new TimerLaneBudgets(5, maxTimerTriggeredInvocations, 10, 1, 30),
            },
            ExplicitlyDisabledCapabilities = P0TextSessionRuntimeCapabilityPolicy
                .RequiredExplicitlyDisabledCapabilities
                .ToArray(),
        };
        var baseline = new RuntimePolicyBaselineSource(
            "baseline.p0.text.0001",
            RuntimePolicyBaselineContentDigest.Compute(values),
            values);
        return FrozenRuntimePolicyResolver.Resolve(
            new RuntimePolicyResolutionRequest(baseline.BaselineDigest, baseline, [])).Policy
            ?? throw new InvalidOperationException("Test fixture policy resolution failed.");
    }

    internal static TrustedSessionBinding CreateBinding(
        Guid organizationId,
        int cooldownSeconds = 5,
        int maxTimerTriggeredInvocations = 8,
        Guid? activityId = null,
        string? configurationDigest = null,
        Guid? attemptId = null,
        Guid? sessionId = null,
        Guid? participantId = null)
    {
        var policy = ResolveEnabledTimerPolicy(cooldownSeconds, maxTimerTriggeredInvocations);
        return new TrustedSessionBinding(
            new SessionOwnership(
                organizationId,
                activityId ?? Guid.NewGuid(),
                participantId ?? Guid.NewGuid(),
                attemptId ?? Guid.NewGuid(),
                sessionId ?? Guid.NewGuid()),
            "cfg.p0.text",
            configurationDigest ?? policy.PolicyDigest,
            "man.p0.text",
            policy,
            [],
            [],
            [],
            CreateFrozenDeployment());
    }

    internal static InstalledModelDeploymentProfile CreateInstalledProfile(int maxProviderRequestAttempts = 4) =>
        InstalledModelDeploymentProfile.Create(
            "synthetic.fake.v1",
            "1",
            ModelDeploymentAdapterKinds.DeterministicFake,
            "sessions.fake.v1",
            new Uri("https://api.openai.com/"),
            "synthetic.model.pinned",
            "synthetic.model.pinned.2026-01-01",
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            maxProviderRequestAttempts,
            "synthetic.provider");

    internal static FrozenModelDeploymentBinding CreateFrozenDeployment()
    {
        var profile = CreateInstalledProfile();
        return new FrozenModelDeploymentBinding(
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.ProviderId,
            ModelDeploymentCredentialModes.OrganizationByok,
            "bind.opaque.0001",
            "bind.v1");
    }

    internal static ModelDeploymentCredentialCatalogRecord CreateCatalogRecord(Guid organizationId) =>
        new(
            "bind.opaque.0001",
            "bind.v1",
            organizationId,
            "synthetic.provider",
            ModelDeploymentCredentialModes.OrganizationByok,
            false,
            "org-a-openai");

    internal static TrustedTrigger OpeningTrigger(string triggerId = "trig.opening.1") =>
        new(
            RuntimeTriggerIdentifiers.WorkflowEventFamily,
            RuntimeTriggerIdentifiers.AgentOpeningType,
            triggerId,
            InvocationPurposes.AgentOpening,
            null,
            null);

    internal static TrustedRuntimeActor Actor(Guid actorId) =>
        new(actorId, "synthetic.test_actor");
}

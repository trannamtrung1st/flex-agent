using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

internal static class SessionRuntimeTestFixtures
{
    internal static readonly DateTimeOffset T0 = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);

    internal static SessionOwnership CreateOwnership() =>
        new(
            OrganizationId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ActivityId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ParticipantId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            AttemptId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            SessionId: Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

    internal static TrustedRuntimeActor CreateActor() =>
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "synthetic.test_actor");

    internal static InstalledModelDeploymentProfile CreateInstalledProfile(
        string providerId = "synthetic.provider",
        string credentialMode = ModelDeploymentCredentialModes.OrganizationByok,
        string requestedModel = "synthetic.model.pinned") =>
        InstalledModelDeploymentProfile.Create(
            profileId: "synthetic.fake.v1",
            profileVersion: "1",
            adapterKind: ModelDeploymentAdapterKinds.DeterministicFake,
            adapterContractVersion: "sessions.fake.v1",
            approvedHttpsOrigin: new Uri("https://api.openai.com/"),
            requestedModel: requestedModel,
            resolvedModelVersion: "synthetic.model.pinned.2026-01-01",
            capabilityProfileId: "p0.text.structured-control",
            credentialMode: credentialMode,
            maxOutputTokens: 256,
            controlTimeout: TimeSpan.FromSeconds(30),
            contentTimeout: TimeSpan.FromSeconds(60),
            maxProviderRequestAttempts: 2,
            providerId: providerId);

    internal static FrozenModelDeploymentBinding CreateFrozenDeployment(
        string providerId = "synthetic.provider",
        string credentialMode = ModelDeploymentCredentialModes.OrganizationByok,
        string bindingReference = "bind.opaque.0001",
        string bindingVersion = "bind.v1",
        string requestedModel = "synthetic.model.pinned")
    {
        var profile = CreateInstalledProfile(providerId, credentialMode, requestedModel);
        return new FrozenModelDeploymentBinding(
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.ProviderId,
            credentialMode,
            bindingReference,
            bindingVersion);
    }

    internal static ModelDeploymentCredentialCatalogRecord CreateCatalogRecord(
        Guid organizationId,
        string providerId = "synthetic.provider",
        string credentialMode = ModelDeploymentCredentialModes.OrganizationByok,
        string bindingReference = "bind.opaque.0001",
        string bindingVersion = "bind.v1",
        bool revoked = false,
        string secretName = "org-a-openai") =>
        new(
            bindingReference,
            bindingVersion,
            organizationId,
            providerId,
            credentialMode,
            revoked,
            secretName);

    internal static TrustedSessionBinding CreateBinding(
        FrozenTextSessionRuntimePolicy? policy = null,
        SessionOwnership? ownership = null,
        IReadOnlyList<ProtectedContentRef>? memoryReadRefs = null,
        FrozenModelDeploymentBinding? frozenModelDeployment = null,
        bool includeFrozenDeployment = true)
    {
        ownership ??= CreateOwnership();
        policy ??= RuntimePolicyTestFixtures.ResolveEnabledTimerPolicy();
        return new TrustedSessionBinding(
            Ownership: ownership,
            ConfigurationId: "cfg.p0.text",
            ConfigurationDigest: policy.PolicyDigest,
            ManifestId: "man.p0.text",
            Policy: policy,
            PermittedSubmissionRefs:
            [
                new ProtectedContentRef("sub:bound-v1", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            ],
            PermittedKnowledgeRefs:
            [
                new ProtectedContentRef("know:bound-v1", "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
            ],
            PermittedMemoryReadRefs: memoryReadRefs ?? [],
            FrozenModelDeployment: includeFrozenDeployment
                ? frozenModelDeployment ?? CreateFrozenDeployment()
                : null);
    }

    internal static SessionRuntime CreateActiveSession(
        FrozenTextSessionRuntimePolicy? policy = null,
        SessionOwnership? ownership = null,
        DateTimeOffset? startedAt = null,
        IReadOnlyList<ProtectedContentRef>? memoryReadRefs = null,
        bool includeFrozenDeployment = true)
    {
        var binding = CreateBinding(
            policy,
            ownership,
            memoryReadRefs,
            includeFrozenDeployment: includeFrozenDeployment);
        return SessionRuntime.CreateActive(binding, startedAt ?? T0);
    }

    internal static TrustedTrigger ParticipantTrigger(
        string triggerId = "trig.participant.1",
        string? turnId = "turn.1",
        string? responseSlotId = "slot.1") =>
        new(
            TriggerFamily: RuntimeTriggerIdentifiers.ParticipantInputFamily,
            TriggerType: RuntimeTriggerIdentifiers.ParticipantMessageType,
            TriggerId: triggerId,
            Purpose: InvocationPurposes.ParticipantTurnRespond,
            TurnId: turnId,
            ResponseSlotId: responseSlotId);

    internal static TrustedTrigger OpeningTrigger(string triggerId = "trig.opening.1") =>
        new(
            TriggerFamily: RuntimeTriggerIdentifiers.WorkflowEventFamily,
            TriggerType: RuntimeTriggerIdentifiers.AgentOpeningType,
            TriggerId: triggerId,
            Purpose: InvocationPurposes.AgentOpening,
            TurnId: null,
            ResponseSlotId: null);

    internal static TrustedTrigger ClosingTrigger(string triggerId = "trig.closing.1") =>
        new(
            TriggerFamily: RuntimeTriggerIdentifiers.WorkflowEventFamily,
            TriggerType: RuntimeTriggerIdentifiers.AgentClosingType,
            TriggerId: triggerId,
            Purpose: InvocationPurposes.AgentClosing,
            TurnId: null,
            ResponseSlotId: null);

    internal static TrustedTrigger TimerTrigger(string triggerId = "trig.timer.1") =>
        new(
            TriggerFamily: RuntimeTriggerIdentifiers.TimerEventFamily,
            TriggerType: RuntimeTriggerIdentifiers.TimerLaneDefaultType,
            TriggerId: triggerId,
            Purpose: InvocationPurposes.TimerLaneCheck,
            TurnId: null,
            ResponseSlotId: null);

    internal static NoActionRecommendation NoAction(
        string invocationId,
        string reasonCategory = NoActionReasonCategories.IntentionalSilence,
        NextTimerRecommendation? nextTimer = null) =>
        new(
            DecisionId: Guid.NewGuid().ToString("N"),
            InvocationId: invocationId,
            ProducedAt: T0.AddSeconds(2),
            ReasonCategory: reasonCategory,
            NextTimer: nextTimer);

    internal static EmitMessageRecommendation EmitMessage(
        string invocationId,
        string communicationPurpose = "participant_reply",
        string? turnId = "turn.1",
        string? responseSlotId = "slot.1",
        NextTimerRecommendation? nextTimer = null) =>
        new(
            DecisionId: Guid.NewGuid().ToString("N"),
            InvocationId: invocationId,
            ProducedAt: T0.AddSeconds(2),
            CommunicationPurpose: communicationPurpose,
            TurnId: turnId,
            ResponseSlotId: responseSlotId,
            NextTimer: nextTimer);

    internal static EnvelopeRecommendation Envelope(
        string invocationId,
        string disposition = DecisionDispositions.Respond,
        IReadOnlyList<OutputRecommendation>? outputs = null,
        IReadOnlyList<RequestedActionRecommendation>? requestedActions = null,
        string? noActionReasonCategory = null,
        string? decisionId = null,
        ProtectedContentRef? payloadRef = null) =>
        new(
            DecisionId: decisionId ?? "adec." + Guid.NewGuid().ToString("N"),
            InvocationId: invocationId,
            ProducedAt: T0.AddSeconds(2),
            Disposition: disposition,
            Outputs: outputs ?? [],
            RequestedActions: requestedActions ?? [],
            NoActionReasonCategory: noActionReasonCategory,
            PayloadRef: payloadRef);

    internal static OutputRecommendation MessageOutput(
        string localRef = "out.message.primary",
        string communicationPurpose = "participant_reply",
        string? turnId = "turn.1",
        string? responseSlotId = "slot.1",
        string? modelAgentOutputId = null,
        string? audience = null,
        IReadOnlyList<OutputLocalReference>? references = null,
        ProtectedContentRef? payloadRef = null) =>
        new(
            AgentOutputKinds.Message,
            localRef,
            communicationPurpose,
            turnId,
            responseSlotId,
            modelAgentOutputId,
            audience,
            references,
            payloadRef);

    internal static OutputRecommendation VoiceOutput(string localRef = "out.voice.primary") =>
        new(AgentOutputKinds.Voice, localRef);
}

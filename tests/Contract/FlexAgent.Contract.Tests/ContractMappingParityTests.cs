using System.Text.Json;
using System.Text.Json.Serialization;
using FlexAgent.Contracts.Audit;
using FlexAgent.Contracts.Enrollment;
using FlexAgent.Contracts.Evidence;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Contracts.Session;
using FlexAgent.Contracts.Submission;
using FlexAgent.Contracts.Transport;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class ContractMappingParityTests
{
    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly IReadOnlySet<string> AllowedKeywords =
        SchemaKeywordProfile.LoadAllowedKeywords(Path.Combine(ContractsRoot, "compatibility", "draft202012-keywords.profile.json"));

    private readonly Draft202012SchemaHarness _harness = new(AllowedKeywords);
    private readonly ContractCatalog _catalog = ContractCatalogLoader.Load(ContractsRoot);

    [Fact]
    public void Exported_contract_dto_surface_matches_catalog_categories()
    {
        var exported = typeof(ISessionCommandEnvelopeV1).Assembly
            .GetTypes()
            .Where(type => type is { IsPublic: true, IsAbstract: false })
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Contains("SessionMessageSendCommandV1", exported);
        Assert.Contains("SessionPauseCommandV1", exported);
        Assert.Contains("SessionResumeCommandV1", exported);
        Assert.Contains("SessionCompleteCommandV1", exported);
        Assert.Contains("SessionTerminateCommandV1", exported);
        Assert.Contains("SessionReconcileCommandV1", exported);
        Assert.Contains("SessionStateEventEnvelopeV1", exported);
        Assert.Contains("ResolvedExecutionManifestV1", exported);
        Assert.Contains("EvidenceLocatorV1", exported);
        Assert.Contains("AuditEventV1", exported);
        Assert.Contains("SafeErrorResponseV1", exported);
        Assert.Contains("SseSessionEventV1", exported);
        Assert.Contains("TrustedTriggerV1", exported);
        Assert.Contains("AdmittedAgentInvocationV1", exported);
        Assert.Contains("ExecutingAgentInvocationV1", exported);
        Assert.Contains("DecidedAgentInvocationV1", exported);
        Assert.Contains("ExecutionFailedAgentInvocationV1", exported);
        Assert.Contains("CancelledAgentInvocationV1", exported);
        Assert.Contains("DecisionProducedExecutionAttemptV1", exported);
        Assert.Contains("FailedExecutionAttemptV1", exported);
        Assert.Contains("EmitMessageAgentDecisionV1", exported);
        Assert.Contains("NoActionAgentDecisionV1", exported);
        Assert.Contains("RequestToolAgentDecisionV1", exported);
        Assert.Contains("ProposeTransitionAgentDecisionV1", exported);
        Assert.Contains("EscalateAgentDecisionV1", exported);
        Assert.Contains("AgentDecisionEnvelopeV2", exported);
        Assert.Contains("AgentOutputRecommendationV2", exported);
        Assert.Contains("AgentRequestedActionV2", exported);
        Assert.Contains("AcceptedDecisionValidationEffectV1", exported);
        Assert.Contains("RejectedDecisionValidationEffectV1", exported);
        Assert.Contains("SuppressedDecisionValidationEffectV1", exported);
        Assert.Contains("ExecutionFailedOutcomeV1", exported);
        Assert.Contains("CancelledOutcomeV1", exported);
        Assert.Contains("LateResultOutcomeV1", exported);
        Assert.Contains("PreExecutionRejectedOutcomeV1", exported);
        Assert.Contains("AttemptsExhaustedOutcomeV1", exported);
        Assert.Contains("TrustedTriggerProvenanceV1", exported);
        Assert.Contains("EnrollmentAssignCommandV1", exported);
        Assert.Contains("EnrollmentLifecycleCommandV1", exported);
        Assert.Contains("EnrollmentMutationOutcomeV1", exported);
        Assert.Contains("MyWorkAssignmentV1", exported);
        Assert.Contains("GrantAccommodationCommandV2", exported);
        Assert.Contains("DecideAccommodationCommandV2", exported);
        Assert.Contains("RevokeAccommodationCommandV2", exported);
        Assert.Contains("AccommodationMutationOutcomeV2", exported);
        Assert.Contains("EnrollmentTimingV2", exported);
        Assert.Contains("MyWorkTimingV2", exported);
        Assert.Contains("BeginIntakeCommandV2", exported);
        Assert.Contains("CompleteIntakeItemCommandV2", exported);
        Assert.Contains("IntakeRevisionCommandV2", exported);
        Assert.Contains("IntakeMutationOutcomeV2", exported);
        Assert.Contains("MyWorkSubmissionV2", exported);
        Assert.Contains("AcceptedVersionDetailV2", exported);
        Assert.Contains("ProtectedItemPreviewV2", exported);
        Assert.Contains("MyWorkAttemptReadinessV2", exported);
        Assert.Contains("AcknowledgeAttemptNoticeCommandV2", exported);
        Assert.Contains("StartAttemptCommandV2", exported);
        Assert.Contains("AcknowledgmentMutationOutcomeV2", exported);
        Assert.Contains("StartAttemptOutcomeV2", exported);
        Assert.Contains("SessionSnapshotV1", exported);
        Assert.Contains("SessionCommandOutcomeV1", exported);
        Assert.Contains("SessionHostedEventEnvelopeV1", exported);
        Assert.DoesNotContain(exported, name => name.Contains("Authorization", StringComparison.Ordinal));
        Assert.DoesNotContain(exported, name => name.Contains("Secret", StringComparison.Ordinal));
    }

    [Fact]
    public void Representative_dto_round_trip_validates_against_schema()
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var locator = new SessionLocatorV1("sess.synthetic.0001");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/command-envelope.v1.schema.json",
            new SessionMessageSendCommandV1(
                "v1",
                "session.message.send.v1",
                "cmd.synthetic.0001",
                "idem-synthetic-0001",
                locator,
                3,
                "12",
                new MessageSendPayloadV1("Synthetic participant message for contract validation.")));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/command-envelope.v1.schema.json",
            new SessionPauseCommandV1(
                "v1",
                "session.pause.v1",
                "cmd.synthetic.0002",
                "idem-synthetic-0002",
                locator,
                4,
                null,
                new PauseCommandPayloadV1("administrator_pause")));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/command-envelope.v1.schema.json",
            new SessionTerminateCommandV1(
                "v1",
                "session.terminate.v1",
                "cmd.synthetic.0005",
                "idem-synthetic-0005",
                locator,
                7,
                null,
                new TerminateCommandPayloadV1("participant_requested")));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/command-envelope.v1.schema.json",
            new SessionReconcileCommandV1(
                "v1",
                "session.reconcile.v1",
                "cmd.synthetic.0006",
                "idem-synthetic-0006",
                locator,
                8,
                "9007199254740993",
                new EmptyCommandPayloadV1()));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/state-event-envelope.v1.schema.json",
            new SessionStateEventEnvelopeV1(
                "v1",
                "session.message.accepted.v1",
                "sess.synthetic.0001",
                "9007199254740993",
                4,
                "2026-08-10T00:00:00Z",
                "corr.synthetic.0001",
                new SessionStateEventPayloadV1("Participant message accepted.", "turn.synthetic.0001")));

        var audit = new AuditEventV1(
            "audit-event.v1",
            "audit.synthetic.0001",
            new AuditActorV1("service", "session-resolver"),
            "org.synthetic.0001",
            "session.configuration.frozen",
            new AuditResourceRefV1("resolved_session_configuration", "rsc.synthetic.0001"),
            "succeeded",
            "freeze_complete",
            "2026-08-10T00:00:00Z",
            "corr.synthetic.0002",
            "required_durable");

        ValidateDto(schemas, "https://flex-agent.local/contracts/schemas/v1/audit/audit-event.v1.schema.json", audit);

        var error = new SafeErrorResponseV1("v1", "conflict", "corr.synthetic.0003", "reconcile", 4, "13");
        ValidateDto(schemas, "https://flex-agent.local/contracts/schemas/v1/transport/safe-error-response.v1.schema.json", error);

        var manifest = new ResolvedExecutionManifestV1(
            "v1",
            "manifest.synthetic.0002",
            new SessionOwnershipRefV1(
                "org.synthetic.0001",
                "act.synthetic.0001",
                "part.synthetic.0001",
                "att.synthetic.0001",
                "sess.synthetic.0001"),
            new ConfigurationRefV1(
                "rsc.synthetic.0001",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            [
                new ManifestRuntimeRecordV1(
                    "42",
                    "transcript.append.v1",
                    "session-worker",
                    "2026-08-10T00:00:02Z",
                    new ProtectedPayloadRefV1(
                        "prot.synthetic.0002",
                        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")),
            ],
            "completed",
            new TerminalSealV1(
                "manifest-jcs-sha256-v1",
                "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/manifest/resolved-execution-manifest.v1.schema.json",
            manifest);

        var evidence = new EvidenceLocatorV1(
            "evidence-locator.v1",
            "configuration.fact",
            new EvidenceSourceRefV1("cfg.synthetic.0001", "rev.0001", null),
            new EvidenceOwnershipRefV1(
                "org.synthetic.0001",
                "act.synthetic.0001",
                "part.synthetic.0001",
                "att.synthetic.0001",
                "sess.synthetic.0001",
                "eval.synthetic.0001"),
            new JsonPointerLocationV1("json_pointer", "/facts/0/value"),
            "exact_range",
            new EvidenceIntegrityV1(
                "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                "locator-adapter.v1",
                "verified"),
            new EvidenceCreatedByV1("evaluation-service", "inv.synthetic.0004"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/evidence/evidence-locator.v1.schema.json",
            evidence);

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/enrollment/enrollment-assign-command.v1.schema.json",
            new EnrollmentAssignCommandV1(
                "v1",
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaab"),
                "enr-assign-synthetic-0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/enrollment/enrollment-lifecycle-command.v1.schema.json",
            new EnrollmentLifecycleCommandV1(
                "v1",
                "temporary_restriction",
                1,
                "enr-suspend-synthetic-0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/enrollment/enrollment-mutation-outcome.v1.schema.json",
            new EnrollmentMutationOutcomeV1(
                "v1",
                true,
                "enrollment.assigned",
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                "active",
                1,
                "current",
                ["suspend_enrollment", "close_enrollment", "revoke_enrollment"]));

        var leakedAccommodationActions = JsonSerializer.SerializeToUtf8Bytes(
            new EnrollmentMutationOutcomeV1(
                "v1",
                true,
                "enrollment.assigned",
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                "active",
                1,
                "current",
                [
                    "suspend_enrollment",
                    "request_accommodation",
                    "revoke_accommodation",
                    "approve_fairness_exception",
                    "reject_fairness_exception",
                ]),
            SerializerOptions);
        var leaked = _harness.ValidateInstance(
            schemas["https://flex-agent.local/contracts/schemas/v1/enrollment/enrollment-mutation-outcome.v1.schema.json"],
            leakedAccommodationActions);
        Assert.False(leaked.IsValid);

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/enrollment/my-work-assignment.v1.schema.json",
            new MyWorkAssignmentV1(
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                "active",
                "current",
                "Campaign",
                "Task 1",
                "UTC",
                "2026-09-01T00:00:00Z",
                "2026-09-30T23:59:00Z",
                "2026-09-30T17:00:00Z",
                true,
                ["open_assignment"]));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/enrollment/grant-accommodation-command.v2.schema.json",
            new GrantAccommodationCommandV2(
                "v2",
                "submission_deadline_utc",
                "2026-10-07T17:00:00Z",
                "development.synthetic.timing",
                null,
                false,
                1,
                "acc-grant-synthetic-0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/enrollment/decide-accommodation-command.v2.schema.json",
            new DecideAccommodationCommandV2(
                "v2",
                true,
                1,
                "acc-decide-synthetic-0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/enrollment/revoke-accommodation-command.v2.schema.json",
            new RevokeAccommodationCommandV2(
                "v2",
                1,
                "acc-revoke-synthetic-0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/enrollment/accommodation-mutation-outcome.v2.schema.json",
            new AccommodationMutationOutcomeV2(
                "v2",
                true,
                "accommodation.granted",
                Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                "granted",
                1,
                ["request_accommodation", "revoke_accommodation"]));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/enrollment/enrollment-timing.v2.schema.json",
            new EnrollmentTimingV2(
                "v2",
                new EnrollmentTimingEnrollmentV2(
                    Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                    "active",
                    1,
                    "current",
                    ["request_accommodation", "revoke_accommodation"]),
                new TimingBaselineV2(
                    "2026-09-01T00:00:00Z",
                    "2026-09-30T23:59:00Z",
                    "2026-09-30T17:00:00Z",
                    "UTC",
                    2,
                    3600),
                new TimingEffectiveWindowV2(
                    "2026-09-01T00:00:00Z",
                    "2026-10-07T17:00:00Z",
                    "2026-09-01T00:00:00Z",
                    "2026-09-30T23:59:00Z",
                    3600,
                    "2026-08-24T08:00:00Z",
                    "too_early",
                    true,
                    "UTC",
                    "deadline_replacement"),
                [
                    new CurrentAccommodationEffectV2(
                        Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
                        "submission_deadline_utc",
                        "deadline_replacement"),
                ],
                true,
                ["submission_deadline_utc"],
                ["development.synthetic.timing"],
                [
                    new AccommodationHistoryItemV2(
                        Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
                        "submission_deadline_utc",
                        "granted",
                        "2026-10-07T17:00:00Z",
                        "development.synthetic.timing",
                        false,
                        1,
                        "2026-08-22T06:00:00Z",
                        "2026-08-22T06:00:00Z",
                        null),
                ]));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/enrollment/my-work-timing.v2.schema.json",
            new MyWorkTimingV2(
                "v2",
                new MyWorkTimingAssignmentV2(
                    Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                    "active",
                    "current",
                    "Campaign",
                    "Task 1",
                    "UTC",
                    "2026-09-01T00:00:00Z",
                    "2026-09-30T23:59:00Z",
                    "2026-09-30T17:00:00Z",
                    true,
                    ["open_assignment"]),
                new TimingEffectiveWindowV2(
                    "2026-09-01T00:00:00Z",
                    "2026-10-07T17:00:00Z",
                    "2026-09-01T00:00:00Z",
                    "2026-09-30T23:59:00Z",
                    3600,
                    "2026-08-24T08:00:00Z",
                    "too_early",
                    true,
                    "UTC",
                    "deadline_replacement"),
                "deadline_replacement"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/begin-intake-command.v2.schema.json",
            new BeginIntakeCommandV2("v2", "intake-begin-synthetic-0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/complete-intake-item-command.v2.schema.json",
            new CompleteIntakeItemCommandV2(
                "v2",
                "direct_text",
                null,
                null,
                "Direct text answer.",
                1,
                "intake-complete-synthetic-0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/intake-revision-command.v2.schema.json",
            new IntakeRevisionCommandV2("v2", 1, "intake-finalize-synthetic-0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/intake-mutation-outcome.v2.schema.json",
            new IntakeMutationOutcomeV2(
                "v2",
                true,
                "accepted",
                Guid.Parse("11111111-1111-4111-8111-111111111111"),
                Guid.Parse("22222222-2222-4222-8222-222222222222"),
                "accepted",
                2,
                Guid.Parse("33333333-3333-4333-8333-333333333333"),
                1,
                ["preview_item", "download_item", "return_to_my_work"]));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/my-work-submission.v2.schema.json",
            new MyWorkSubmissionV2(
                "v2",
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                "active",
                true,
                null,
                new MaterialRequirementsV2(
                    "submissions.material_policy.v1",
                    10,
                    26214400,
                    1048576,
                    "disabled_by_approved_policy",
                    [new MaterialCategoryLimitV2("direct_text", true, 1048576)]),
                null,
                [],
                ["begin_intake", "return_to_my_work"]));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/accepted-version-detail.v2.schema.json",
            new AcceptedVersionDetailV2(
                "v2",
                Guid.Parse("33333333-3333-4333-8333-333333333333"),
                2,
                "2026-08-25T00:00:00Z",
                [
                    new AcceptedVersionItemV2(
                        Guid.Parse("44444444-4444-4444-8444-444444444444"),
                        "direct_text",
                        null,
                        21,
                        true,
                        true),
                ],
                ["preview_item", "download_item", "return_to_my_work"]));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/protected-item-preview.v2.schema.json",
            new ProtectedItemPreviewV2(
                "v2",
                Guid.Parse("33333333-3333-4333-8333-333333333333"),
                Guid.Parse("44444444-4444-4444-8444-444444444444"),
                "direct_text",
                null,
                "text/plain",
                "Synthetic preview text."));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/acknowledge-attempt-notice-command.v2.schema.json",
            new AcknowledgeAttemptNoticeCommandV2(
                "v2",
                Guid.Parse("11111111-1111-4111-8111-111111111111"),
                Guid.Parse("22222222-2222-4222-8222-222222222222"),
                "affirmed",
                "attempt-ack-synthetic-0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/start-attempt-command.v2.schema.json",
            new StartAttemptCommandV2(
                "v2",
                "attempt-start-synthetic-0001",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/acknowledgment-mutation-outcome.v2.schema.json",
            new AcknowledgmentMutationOutcomeV2(
                "v2",
                true,
                "acknowledgment.recorded",
                Guid.Parse("33333333-3333-4333-8333-333333333333"),
                "affirmed"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/start-attempt-outcome.v2.schema.json",
            new StartAttemptOutcomeV2(
                "v2",
                true,
                "attempt.activated",
                "active_conflict",
                Guid.Parse("44444444-4444-4444-8444-444444444444"),
                1,
                Guid.Parse("55555555-5555-4555-8555-555555555555"),
                0,
                ["continue_attempt", "return_to_my_work"]));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v2/submission/my-work-attempt-readiness.v2.schema.json",
            new MyWorkAttemptReadinessV2(
                "v2",
                Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"),
                "eligible",
                1,
                1,
                "baseline",
                1,
                null,
                null,
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                [],
                [],
                [],
                ["start_attempt", "return_to_my_work"]));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/snapshot.v1.schema.json",
            new SessionSnapshotV1(
                "v1",
                "participant",
                Guid.Parse("55555555-5555-4555-8555-555555555555"),
                "active",
                3,
                "12",
                "2026-09-03T00:00:00Z",
                ["send_message", "complete_session", "reconcile", "return_to_my_work"],
                "none",
                null,
                new SessionAgentIdentityV1("Assessment Agent"),
                new SessionTimingProjectionV1("active_duration", 2400, "none", null),
                new SessionBoundSubmissionSummaryV1("Accepted Submission version 1", 1),
                new SessionTranscriptPageV1(
                    [
                        new SessionSnapshotTranscriptItemV1(
                            "msg.synthetic.0001",
                            "participant",
                            "accepted",
                            "11",
                            "11",
                            "I am ready to begin.",
                            "2026-09-03T00:00:01Z",
                            "turn.synthetic.0001"),
                    ],
                    false,
                    "11",
                    "12"),
                new SessionActivityProjectionV1("idle", null, null)));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/command-outcome.v1.schema.json",
            new SessionCommandOutcomeV1(
                "v1",
                true,
                "accepted",
                "session.message.accepted",
                "cmd.synthetic.0001",
                "session.message.send.v1",
                Guid.Parse("55555555-5555-4555-8555-555555555555"),
                "none",
                ["send_message", "complete_session", "reconcile"],
                4,
                "13",
                "msg.synthetic.0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/hosted-event-envelope.v1.schema.json",
            new SessionHostedEventEnvelopeV1(
                "v1",
                "session.hosted.message.accepted.v1",
                Guid.Parse("55555555-5555-4555-8555-555555555555"),
                "13",
                4,
                "2026-09-03T00:00:02Z",
                new SessionHostedEventPayloadV1(
                    "Participant message accepted.",
                    MessageId: "msg.synthetic.0001",
                    TurnId: "turn.synthetic.0001"),
                "131"));
    }

    private void ValidateDto(IReadOnlyDictionary<string, Json.Schema.JsonSchema> schemas, string schemaId, object dto)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(dto, dto.GetType(), SerializerOptions);
        var result = _harness.ValidateInstance(schemas[schemaId], json);
        Assert.True(result.IsValid, $"{schemaId}: {JsonSerializer.Serialize(result)}");
    }
}

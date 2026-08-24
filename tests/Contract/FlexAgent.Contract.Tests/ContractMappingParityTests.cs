using System.Text.Json;
using System.Text.Json.Serialization;
using FlexAgent.Contracts.Audit;
using FlexAgent.Contracts.Enrollment;
using FlexAgent.Contracts.Evidence;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Contracts.Session;
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
                new EmptyCommandPayloadV1()));

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
    }

    private void ValidateDto(IReadOnlyDictionary<string, Json.Schema.JsonSchema> schemas, string schemaId, object dto)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(dto, dto.GetType(), SerializerOptions);
        var result = _harness.ValidateInstance(schemas[schemaId], json);
        Assert.True(result.IsValid, $"{schemaId}: {JsonSerializer.Serialize(result)}");
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Contracts.Session;
using FlexAgent.Contracts.Transport;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class ContractRuntimeBoundaryTests
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
    public void Runtime_boundary_schemas_are_cataloged_with_fixtures()
    {
        var runtimeSchemaIds = new[]
        {
            "https://flex-agent.local/contracts/schemas/v1/session/trusted-trigger.v1.schema.json",
            "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation.v1.schema.json",
            "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation-execution-attempt.v1.schema.json",
            "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation-execution-outcome.v1.schema.json",
            "https://flex-agent.local/contracts/schemas/v1/session/agent-decision.v1.schema.json",
            "https://flex-agent.local/contracts/schemas/v1/session/decision-validation-effect.v1.schema.json",
            "https://flex-agent.local/contracts/schemas/v1/session/timer-schedule-revision.v1.schema.json",
            "https://flex-agent.local/contracts/schemas/v1/common/iso8601-positive-duration-fixture.v1.schema.json",
        };

        foreach (var schemaId in runtimeSchemaIds)
        {
            var entry = _catalog.RepresentativeSchemas.SingleOrDefault(e => e.SchemaId == schemaId);
            Assert.NotNull(entry);
            Assert.True(Directory.Exists(Path.Combine(ContractsRoot, entry!.FixtureDir)));
        }
    }

    [Fact]
    public void Sse_schema_includes_complete_and_work_event_types()
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var sseSchema = schemas["https://flex-agent.local/contracts/schemas/v1/transport/sse-event.v1.schema.json"];
        var json = JsonSerializer.Serialize(sseSchema);
        Assert.Contains("session.agent.complete.v1", json, StringComparison.Ordinal);
        Assert.Contains("session.agent.work.v1", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_boundary_dto_round_trip_validates_against_schema()
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var ownership = new SessionOwnershipRefV1(
            "org.synthetic.0001",
            "act.synthetic.0001",
            "part.synthetic.0001",
            "att.synthetic.0001",
            "sess.synthetic.0001");

        var trigger = new TrustedTriggerV1(
            "v1",
            "participant_input",
            "participant_input.message",
            "trig.synthetic.0001",
            "idem-trigger-0001",
            ownership,
            "participant_turn_response",
            "turn.synthetic.0001",
            "slot.synthetic.0001");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/trusted-trigger.v1.schema.json",
            trigger);

        var triggerProvenance = new TrustedTriggerProvenanceV1(
            "v1",
            "participant_input",
            "participant_input.message",
            "trig.synthetic.0001",
            "idem-trigger-0001",
            "participant_turn_response",
            "turn.synthetic.0001",
            "slot.synthetic.0001");

        var invocation = new InProgressAgentInvocationV1(
            "v1",
            "ainv.synthetic.0001",
            "v1",
            "participant_turn_response",
            ownership,
            triggerProvenance,
            "42",
            "admitted",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation.v1.schema.json",
            invocation);

        var decidedInvocation = new DecidedAgentInvocationV1(
            "v1",
            "ainv.synthetic.0002",
            "v1",
            "participant_turn_response",
            ownership,
            triggerProvenance,
            "43",
            "adec.synthetic.0002");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation.v1.schema.json",
            decidedInvocation);

        var attempt = new DecisionProducedExecutionAttemptV1(
            "v1",
            "eatt.synthetic.0001",
            "ainv.synthetic.0001",
            1,
            "2026-08-11T00:00:00Z",
            "2026-08-11T00:00:05Z",
            "adec.synthetic.0001");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation-execution-attempt.v1.schema.json",
            attempt);

        var failedAttempt = new FailedExecutionAttemptV1(
            "v1",
            "eatt.synthetic.0002",
            "ainv.synthetic.0001",
            2,
            "provider_timeout",
            "2026-08-11T00:00:05Z",
            "2026-08-11T00:00:10Z");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation-execution-attempt.v1.schema.json",
            failedAttempt);

        var executionOutcome = new AgentInvocationExecutionOutcomeV1(
            "v1",
            "eout.synthetic.0001",
            "ainv.synthetic.0001",
            "execution_failed",
            "provider_timeout",
            "2026-08-11T00:00:05Z",
            "eatt.synthetic.0001");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation-execution-outcome.v1.schema.json",
            executionOutcome);

        var noActionDecision = new NoActionAgentDecisionV1(
            "v1",
            "adec.synthetic.0002",
            "ainv.synthetic.0002",
            "2026-08-11T00:00:06Z",
            new NoActionDecisionPayloadV1("intentional_silence"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/agent-decision.v1.schema.json",
            noActionDecision);

        var validation = new AcceptedDecisionValidationEffectV1(
            "v1",
            "veff.synthetic.0002",
            "adec.synthetic.0002",
            "no_domain_effect",
            "2026-08-11T00:00:07Z",
            "44",
            "omitted");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/decision-validation-effect.v1.schema.json",
            validation);

        var suppressedValidation = new SuppressedDecisionValidationEffectV1(
            "v1",
            "veff.synthetic.0003",
            "adec.synthetic.0003",
            "visibility_bounded",
            "2026-08-11T00:00:08Z");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/decision-validation-effect.v1.schema.json",
            suppressedValidation);

        var schedule = new TimerScheduleRevisionV1(
            "v1",
            "tsrev.synthetic.0001",
            "sess.synthetic.0001",
            "1",
            "pending",
            "PT30S",
            "default_cadence",
            "2026-08-11T00:00:00Z",
            "PT30S");

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/timer-schedule-revision.v1.schema.json",
            schedule);

        var workEvent = new SseSessionEventV1(
            "v1",
            "session.agent.work.v1",
            "sess.synthetic.0001",
            "16",
            "2026-08-10T00:00:04Z",
            new SseSessionEventPayloadV1(
                "Turn resolved without Agent reply.",
                null,
                null,
                null,
                "turn.synthetic.0001",
                "resolved",
                "no_action",
                true));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/transport/sse-event.v1.schema.json",
            workEvent);

        var completeEvent = new SseSessionEventV1(
            "v1",
            "session.agent.complete.v1",
            "sess.synthetic.0001",
            "15",
            "2026-08-10T00:00:03Z",
            new SseSessionEventPayloadV1(
                "Agent response complete.",
                null,
                "amsg.synthetic.0001",
                null,
                null,
                null,
                null,
                null,
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                3));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/transport/sse-event.v1.schema.json",
            completeEvent);

        var emitMessageDecision = new EmitMessageAgentDecisionV1(
            "v1",
            "adec.synthetic.0001",
            "ainv.synthetic.0001",
            "2026-08-11T00:00:05Z",
            new EmitMessageDecisionPayloadV1(
                "participant_turn_reply",
                "turn.synthetic.0001",
                "slot.synthetic.0001"));

        ValidateDto(
            schemas,
            "https://flex-agent.local/contracts/schemas/v1/session/agent-decision.v1.schema.json",
            emitMessageDecision);
    }

    private void ValidateDto(IReadOnlyDictionary<string, Json.Schema.JsonSchema> schemas, string schemaId, object dto)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(dto, dto.GetType(), SerializerOptions);
        var result = _harness.ValidateInstance(schemas[schemaId], json);
        Assert.True(result.IsValid, $"{schemaId}: {JsonSerializer.Serialize(result)}");
    }
}

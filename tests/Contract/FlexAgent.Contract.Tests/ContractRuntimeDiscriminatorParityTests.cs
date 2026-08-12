using System.Text;
using System.Text.Json;
using FlexAgent.Contracts.Manifest;
using FlexAgent.Contracts.Session;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class ContractRuntimeDiscriminatorParityTests
{
    private const string ExecutionOutcomeSchemaId =
        "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation-execution-outcome.v1.schema.json";

    private const string InvocationSchemaId =
        "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation.v1.schema.json";

    private const string ExecutionAttemptSchemaId =
        "https://flex-agent.local/contracts/schemas/v1/session/agent-invocation-execution-attempt.v1.schema.json";

    private const string DecisionSchemaId =
        "https://flex-agent.local/contracts/schemas/v1/session/agent-decision.v1.schema.json";

    private const string ValidationEffectSchemaId =
        "https://flex-agent.local/contracts/schemas/v1/session/decision-validation-effect.v1.schema.json";

    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");

    private static readonly IReadOnlySet<string> AllowedKeywords =
        SchemaKeywordProfile.LoadAllowedKeywords(Path.Combine(ContractsRoot, "compatibility", "draft202012-keywords.profile.json"));

    private readonly Draft202012SchemaHarness _harness = new(AllowedKeywords);
    private readonly ContractCatalog _catalog = ContractCatalogLoader.Load(ContractsRoot);

    [Theory]
    [InlineData(
        """{"schema_version":"v1","execution_outcome_id":"eout.synthetic.0099","agent_invocation_id":"ainv.synthetic.0001","outcome_category":"pre_execution_rejected","reason_category":"state_ineligible","terminal_at":"2026-08-11T00:00:01Z","last_execution_attempt_id":"eatt.synthetic.0001"}""")]
    [InlineData(
        """{"schema_version":"v1","execution_outcome_id":"eout.synthetic.0098","agent_invocation_id":"ainv.synthetic.0001","outcome_category":"attempts_exhausted","reason_category":"retry_budget_exhausted","terminal_at":"2026-08-11T00:00:10Z"}""")]
    [InlineData(
        """{"schema_version":"v1","execution_outcome_id":"eout.synthetic.0097","agent_invocation_id":"ainv.synthetic.0001","outcome_category":"execution_failed","reason_category":"provider_timeout","terminal_at":"2026-08-11T00:00:05Z"}""")]
    [InlineData(
        """{"schema_version":"v1","execution_outcome_id":"eout.synthetic.0096","agent_invocation_id":"ainv.synthetic.0001","outcome_category":"late_result","reason_category":"late_provider_result","terminal_at":"2026-08-11T00:00:12Z"}""")]
    [InlineData(
        """{"schema_version":"v1","agent_invocation_id":"ainv.synthetic.0095","invocation_contract_version":"v1","purpose":"participant_turn_response","ownership":{"organization_id":"org.synthetic.0001","activity_id":"act.synthetic.0001","participant_id":"part.synthetic.0001","attempt_id":"att.synthetic.0001","session_id":"sess.synthetic.0001"},"trigger":{"schema_version":"v1","trigger_family":"participant_input","trigger_type":"participant_input.message","trigger_id":"trig.synthetic.0001","idempotency_key":"idem-trigger-0001","purpose":"participant_turn_response"},"session_sequence":"42","status":"decided"}""")]
    [InlineData(
        """{"schema_version":"v1","execution_attempt_id":"eatt.synthetic.0094","agent_invocation_id":"ainv.synthetic.0001","attempt_ordinal":1,"outcome_category":"decision_produced","started_at":"2026-08-11T00:00:00Z","completed_at":"2026-08-11T00:00:05Z"}""")]
    [InlineData(
        """{"schema_version":"v1","validation_effect_id":"veff.synthetic.0093","agent_decision_id":"adec.synthetic.0002","validation_outcome":"accepted","effect_outcome":"not_attempted","validated_at":"2026-08-11T00:00:07Z"}""")]
    public void Invalid_discriminator_payloads_reject_against_runtime_schemas(string json)
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var schemaId = json.Contains("\"validation_outcome\"", StringComparison.Ordinal)
            ? ValidationEffectSchemaId
            : json.Contains("\"execution_attempt_id\"", StringComparison.Ordinal)
                ? ExecutionAttemptSchemaId
                : json.Contains("\"execution_outcome_id\"", StringComparison.Ordinal)
                    ? ExecutionOutcomeSchemaId
                    : json.Contains("\"agent_invocation_id\"", StringComparison.Ordinal)
                        && json.Contains("\"status\"", StringComparison.Ordinal)
                        ? InvocationSchemaId
                        : DecisionSchemaId;

        var result = _harness.ValidateInstance(schemas[schemaId], Encoding.UTF8.GetBytes(json));
        Assert.False(result.IsValid, JsonSerializer.Serialize(result));
    }

    [Fact]
    public void Undefined_wire_enum_values_fail_closed_on_serialization()
    {
        var decision = new NoActionAgentDecisionV1(
            "v1",
            "adec.synthetic.9999",
            "ainv.synthetic.0001",
            "2026-08-11T00:00:06Z",
            new NoActionDecisionPayloadV1((NoActionReasonCategoryV1)999));

        Assert.Throws<JsonException>(() => SessionRuntimeContractJson.SerializeToUtf8Bytes(decision));
    }

    [Fact]
    public void Runtime_union_interfaces_serialize_branch_fields_through_declared_interface_type()
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var ownership = CreateOwnership();
        var trigger = CreateTrigger();

        IAgentInvocationV1 invocation = new DecidedAgentInvocationV1(
            "v1",
            "ainv.synthetic.0201",
            "v1",
            "participant_turn_response",
            ownership,
            trigger,
            "44",
            "adec.synthetic.0201");

        IAgentInvocationExecutionAttemptV1 attempt = new DecisionProducedExecutionAttemptV1(
            "v1",
            "eatt.synthetic.0201",
            "ainv.synthetic.0201",
            1,
            "2026-08-11T00:00:00Z",
            "2026-08-11T00:00:05Z",
            "adec.synthetic.0201");

        IAgentInvocationExecutionOutcomeV1 outcome = new ExecutionFailedOutcomeV1(
            "v1",
            "eout.synthetic.0201",
            "ainv.synthetic.0201",
            ExecutionFailedReasonCategoryV1.ProviderTimeout,
            "2026-08-11T00:00:05Z",
            "eatt.synthetic.0201");

        IAgentDecisionV1 decision = new EmitMessageAgentDecisionV1(
            "v1",
            "adec.synthetic.0201",
            "ainv.synthetic.0201",
            "2026-08-11T00:00:05Z",
            new EmitMessageDecisionPayloadV1(
                "participant_turn_reply",
                "turn.synthetic.0001",
                "slot.synthetic.0001"));

        IDecisionValidationEffectV1 validation = new AcceptedDecisionValidationEffectV1(
            "v1",
            "veff.synthetic.0201",
            "adec.synthetic.0201",
            AcceptedEffectOutcomeV1.NoDomainEffect,
            "2026-08-11T00:00:07Z",
            "44",
            TimerValidationOutcomeV1.Omitted);

        AssertInterfaceSerializesToValidSchema(schemas, InvocationSchemaId, invocation, "agent_decision_id");
        AssertInterfaceSerializesToValidSchema(schemas, ExecutionAttemptSchemaId, attempt, "agent_decision_id");
        AssertInterfaceSerializesToValidSchema(schemas, ExecutionOutcomeSchemaId, outcome, "last_execution_attempt_id");
        AssertInterfaceSerializesToValidSchema(schemas, DecisionSchemaId, decision, "emit_message");
        AssertInterfaceSerializesToValidSchema(schemas, ValidationEffectSchemaId, validation, "effect_outcome");
    }

    [Fact]
    public void Execution_outcome_branch_dtos_serialize_to_schema_valid_instances()
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var schema = schemas[ExecutionOutcomeSchemaId];

        IAgentInvocationExecutionOutcomeV1[] outcomes =
        [
            new ExecutionFailedOutcomeV1(
                "v1",
                "eout.synthetic.0101",
                "ainv.synthetic.0001",
                ExecutionFailedReasonCategoryV1.ProviderTimeout,
                "2026-08-11T00:00:05Z",
                "eatt.synthetic.0001"),
            new CancelledOutcomeV1(
                "v1",
                "eout.synthetic.0102",
                "ainv.synthetic.0001",
                CancelledReasonCategoryV1.LifecycleCancelled,
                "2026-08-11T00:00:06Z"),
            new LateResultOutcomeV1(
                "v1",
                "eout.synthetic.0103",
                "ainv.synthetic.0001",
                "2026-08-11T00:00:07Z",
                "eatt.synthetic.0002"),
            new PreExecutionRejectedOutcomeV1(
                "v1",
                "eout.synthetic.0104",
                "ainv.synthetic.0001",
                PreExecutionRejectedReasonCategoryV1.StateIneligible,
                "2026-08-11T00:00:01Z"),
            new AttemptsExhaustedOutcomeV1(
                "v1",
                "eout.synthetic.0105",
                "ainv.synthetic.0001",
                "2026-08-11T00:00:10Z",
                "eatt.synthetic.0003"),
        ];

        foreach (var outcome in outcomes)
        {
            var json = SessionRuntimeContractJson.SerializeToUtf8Bytes(outcome, outcome.GetType());
            var result = _harness.ValidateInstance(schema, json);
            Assert.True(result.IsValid, $"{outcome.GetType().Name}: {JsonSerializer.Serialize(result)}");
        }
    }

    [Fact]
    public void In_progress_invocation_status_is_bounded_by_concrete_dto_types()
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var schema = schemas[InvocationSchemaId];
        var ownership = CreateOwnership();
        var trigger = CreateTrigger();

        IAgentInvocationV1[] invocations =
        [
            new AdmittedAgentInvocationV1(
                "v1",
                "ainv.synthetic.0106",
                "v1",
                "participant_turn_response",
                ownership,
                trigger,
                "42"),
            new ExecutingAgentInvocationV1(
                "v1",
                "ainv.synthetic.0107",
                "v1",
                "participant_turn_response",
                ownership,
                trigger,
                "43"),
        ];

        foreach (var invocation in invocations)
        {
            var json = SessionRuntimeContractJson.SerializeToUtf8Bytes(invocation, invocation.GetType());
            var result = _harness.ValidateInstance(schema, json);
            Assert.True(result.IsValid, $"{invocation.GetType().Name}: {JsonSerializer.Serialize(result)}");
        }
    }

    private void AssertInterfaceSerializesToValidSchema<T>(
        IReadOnlyDictionary<string, Json.Schema.JsonSchema> schemas,
        string schemaId,
        T value,
        string requiredBranchField)
    {
        var json = SessionRuntimeContractJson.SerializeToUtf8Bytes(value);
        var jsonText = Encoding.UTF8.GetString(json);
        Assert.Contains(requiredBranchField, jsonText, StringComparison.Ordinal);

        var result = _harness.ValidateInstance(schemas[schemaId], json);
        Assert.True(result.IsValid, $"{typeof(T).Name}: {JsonSerializer.Serialize(result)}");
    }

    private static SessionOwnershipRefV1 CreateOwnership() =>
        new(
            "org.synthetic.0001",
            "act.synthetic.0001",
            "part.synthetic.0001",
            "att.synthetic.0001",
            "sess.synthetic.0001");

    private static TrustedTriggerProvenanceV1 CreateTrigger() =>
        new(
            "v1",
            "participant_input",
            "participant_input.message",
            "trig.synthetic.0001",
            "idem-trigger-0001",
            "participant_turn_response");
}

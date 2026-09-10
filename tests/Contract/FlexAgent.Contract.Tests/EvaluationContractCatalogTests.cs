using System.Text.Json;
using FlexAgent.Contracts.Evidence;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class EvaluationContractCatalogTests
{
    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");

    private static readonly IReadOnlySet<string> AllowedKeywords =
        SchemaKeywordProfile.LoadAllowedKeywords(Path.Combine(ContractsRoot, "compatibility", "draft202012-keywords.profile.json"));

    private readonly Draft202012SchemaHarness _harness = new(AllowedKeywords);
    private readonly ContractCatalog _catalog = ContractCatalogLoader.Load(ContractsRoot);

    private static readonly string[] RequiredEvaluationSchemaIds =
    [
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-procedure.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-request.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-work.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/deterministic-invocation.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-provider-artifact.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-model-request.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-model-response.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/criterion-judgment.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-annotation.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-replacement.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/evaluation/evaluation-review-handoff.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/review/review-work-item.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/review/review-case-read.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/review/review-criterion-read.v1.schema.json",
        "https://flex-agent.local/contracts/schemas/v1/review/review-evidence-open.v1.schema.json",
    ];

    private static readonly string[] InternalOnlySchemaFileNames =
    [
        "evaluation-procedure.v1.schema.json",
        "evaluation-request.v1.schema.json",
        "evaluation-work.v1.schema.json",
        "deterministic-invocation.v1.schema.json",
        "evaluation-provider-artifact.v1.schema.json",
        "evaluation-model-request.v1.schema.json",
        "evaluation-model-response.v1.schema.json",
        "criterion-judgment.v1.schema.json",
        "evaluation.v1.schema.json",
        "evaluation-annotation.v1.schema.json",
        "evaluation-replacement.v1.schema.json",
        "evaluation-review-handoff.v1.schema.json",
    ];

    private static readonly string[] PublicReviewSchemaFileNames =
    [
        "review-work-item.v1.schema.json",
        "review-case-read.v1.schema.json",
        "review-criterion-read.v1.schema.json",
        "review-evidence-open.v1.schema.json",
    ];

    [Fact]
    public void Catalog_declares_evaluation_procedure_and_evaluation_family()
    {
        var schemaIds = _catalog.RepresentativeSchemas.Select(entry => entry.SchemaId).ToArray();
        foreach (var schemaId in RequiredEvaluationSchemaIds)
        {
            Assert.Contains(schemaId, schemaIds);
            var entry = _catalog.RepresentativeSchemas.Single(e => e.SchemaId == schemaId);
            Assert.True(
                Directory.Exists(Path.Combine(ContractsRoot, entry.FixtureDir)),
                entry.FixtureDir);
        }
    }

    [Fact]
    public void Evidence_locator_v1_and_seal_remain_compatible()
    {
        var locator = _catalog.RepresentativeSchemas.Single(entry =>
            entry.SchemaId
            == "https://flex-agent.local/contracts/schemas/v1/evidence/evidence-locator.v1.schema.json");
        Assert.Equal("evidence_locator", locator.Category);
        Assert.Equal("fixtures/schema/v1/evidence/evidence-locator", locator.FixtureDir);

        var seal = _catalog.DigestSchemas.Single(entry => entry.ProcedureId == "evidence-set-jcs-sha256-v1");
        Assert.Equal(
            "https://flex-agent.local/contracts/schemas/v1/digest/evidence-set-seal-document.v1.schema.json",
            seal.SchemaId);

        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var locatorSchema = schemas[locator.SchemaId];
        var validLocator = File.ReadAllBytes(
            Path.Combine(ContractsRoot, "fixtures/schema/v1/evidence/evidence-locator/valid-configuration-json-pointer.json"));
        var result = _harness.ValidateInstance(locatorSchema, validLocator);
        Assert.True(result.IsValid, JsonSerializer.Serialize(result));

        var dto = new EvidenceLocatorV1(
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
        Assert.Equal("evidence-locator.v1", dto.LocatorSchema);
    }

    [Fact]
    public void Internal_evaluation_work_and_provider_contracts_stay_out_of_openapi_and_browser_typescript()
    {
        var openApi = File.ReadAllText(Path.Combine(ContractsRoot, _catalog.Projections.OpenApi));
        foreach (var fileName in InternalOnlySchemaFileNames)
        {
            Assert.DoesNotContain(fileName, openApi, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("EvaluationProcedureV1", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("EvaluationRequestV1", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("EvaluationWorkV1", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("DeterministicInvocationV1", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("EvaluationProviderArtifactV1", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("EvaluationModelRequestV1", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("EvaluationModelResponseV1", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("provider_request_body", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("provider_response_body", openApi, StringComparison.Ordinal);
        Assert.DoesNotContain("hidden_prompt", openApi, StringComparison.Ordinal);

        var typescriptV1 = File.ReadAllText(FindRepoFile(Path.Combine("web", "src", "contracts", "v1.ts")));
        var typescriptV2 = File.ReadAllText(FindRepoFile(Path.Combine("web", "src", "contracts", "v2.ts")));
        foreach (var name in new[]
                 {
                     "EvaluationProcedureV1",
                     "EvaluationRequestV1",
                     "EvaluationWorkV1",
                     "DeterministicInvocationV1",
                     "EvaluationProviderArtifactV1",
                     "EvaluationModelRequestV1",
                     "EvaluationModelResponseV1",
                     "CriterionJudgmentV1",
                     "EvaluationAnnotationV1",
                     "EvaluationReplacementV1",
                     "EvaluationReviewHandoffV1",
                 })
        {
            Assert.DoesNotContain(name, typescriptV1, StringComparison.Ordinal);
            Assert.DoesNotContain(name, typescriptV2, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Authenticated_reviewer_read_projections_are_public()
    {
        var openApi = File.ReadAllText(Path.Combine(ContractsRoot, _catalog.Projections.OpenApi));
        foreach (var fileName in PublicReviewSchemaFileNames)
        {
            Assert.Contains(fileName, openApi, StringComparison.Ordinal);
        }

        Assert.Contains("ReviewWorkItemV1", openApi, StringComparison.Ordinal);
        Assert.Contains("ReviewCaseReadV1", openApi, StringComparison.Ordinal);
        Assert.Contains("ReviewCriterionReadV1", openApi, StringComparison.Ordinal);
        Assert.Contains("ReviewEvidenceOpenV1", openApi, StringComparison.Ordinal);
        Assert.Contains("/v1/review/work", openApi, StringComparison.Ordinal);
        Assert.Contains("/v1/review/cases/{reviewCaseId}", openApi, StringComparison.Ordinal);
        Assert.Contains("Internal Evaluation", openApi, StringComparison.Ordinal);

        var typescriptV1 = File.ReadAllText(FindRepoFile(Path.Combine("web", "src", "contracts", "v1.ts")));
        Assert.Contains("export interface ReviewWorkItemV1", typescriptV1, StringComparison.Ordinal);
        Assert.Contains("export interface ReviewCaseReadV1", typescriptV1, StringComparison.Ordinal);
        Assert.Contains("export interface ReviewCriterionReadV1", typescriptV1, StringComparison.Ordinal);
        Assert.Contains("export interface ReviewEvidenceOpenV1", typescriptV1, StringComparison.Ordinal);
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}

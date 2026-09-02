using System.Text.Json;
using FlexAgent.Contract.Tests.Harness;

namespace FlexAgent.Contract.Tests;

public sealed class ContractCatalogTests
{
    private static readonly string ContractsRoot = Path.Combine(AppContext.BaseDirectory, "contracts");
    private static readonly IReadOnlySet<string> AllowedKeywords =
        SchemaKeywordProfile.LoadAllowedKeywords(Path.Combine(ContractsRoot, "compatibility", "draft202012-keywords.profile.json"));

    private readonly Draft202012SchemaHarness _harness = new(AllowedKeywords);
    private readonly ContractCatalog _catalog = ContractCatalogLoader.Load(
        Path.Combine(AppContext.BaseDirectory, "contracts"));

    [Fact]
    public void Catalog_declares_draft_2020_12_and_complete_representative_set()
    {
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", _catalog.SchemaDialect);
        Assert.Equal(41, _catalog.RepresentativeSchemas.Count);
        Assert.Equal(5, _catalog.DigestSchemas.Count);
        Assert.All(_catalog.RepresentativeSchemas, entry =>
            Assert.StartsWith(_catalog.IdNamespace, entry.SchemaId, StringComparison.Ordinal));
    }

    [Fact]
    public void Catalog_schemas_have_reference_closure_and_explicit_dialect()
    {
        ContractSchemaRegistry.AssertReferenceClosure(ContractsRoot, _catalog);
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        Assert.Equal(47, schemas.Count);
    }

    [Theory]
    [MemberData(nameof(ValidFixtureCases))]
    public void Representative_valid_fixtures_validate(string relativeFixturePath, string schemaId)
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var schema = schemas[schemaId];
        var instanceBytes = File.ReadAllBytes(Path.Combine(ContractsRoot, relativeFixturePath));
        var result = _harness.ValidateInstance(schema, instanceBytes);
        Assert.True(result.IsValid, JsonSerializer.Serialize(result));
    }

    [Theory]
    [MemberData(nameof(InvalidFixtureCases))]
    public void Representative_invalid_fixtures_reject(string relativeFixturePath, string schemaId)
    {
        var schemas = ContractSchemaRegistry.BuildCatalogSchemas(ContractsRoot, _catalog, AllowedKeywords);
        var schema = schemas[schemaId];
        var instanceBytes = File.ReadAllBytes(Path.Combine(ContractsRoot, relativeFixturePath));
        var result = _harness.ValidateInstance(schema, instanceBytes);
        Assert.False(result.IsValid, relativeFixturePath);
    }

    [Fact]
    public void OpenAPI_projection_exists_and_excludes_internal_only_fields()
    {
        var openApiPath = Path.Combine(ContractsRoot, _catalog.Projections.OpenApi);
        var content = File.ReadAllText(openApiPath);
        Assert.Contains("openapi: 3.1.0", content, StringComparison.Ordinal);
        Assert.DoesNotContain("internal_authorization_evidence", content, StringComparison.Ordinal);
        Assert.DoesNotContain("organization_scope_proof", content, StringComparison.Ordinal);
        Assert.Contains("SessionMessageSendCommandV1", content, StringComparison.Ordinal);
        Assert.Contains("EvidenceLocatorV1", content, StringComparison.Ordinal);
        Assert.Contains("PositiveInt64WireString", content, StringComparison.Ordinal);
        Assert.Contains("EnrollmentAssignCommandV1", content, StringComparison.Ordinal);
        Assert.Contains("MyWorkAssignmentV1", content, StringComparison.Ordinal);
        Assert.Contains("GrantAccommodationCommandV2", content, StringComparison.Ordinal);
        Assert.Contains("DecideAccommodationCommandV2", content, StringComparison.Ordinal);
        Assert.Contains("RevokeAccommodationCommandV2", content, StringComparison.Ordinal);
        Assert.Contains("EnrollmentTimingV2", content, StringComparison.Ordinal);
        Assert.Contains("MyWorkTimingV2", content, StringComparison.Ordinal);
        Assert.Contains("BeginIntakeCommandV2", content, StringComparison.Ordinal);
        Assert.Contains("CompleteIntakeItemCommandV2", content, StringComparison.Ordinal);
        Assert.Contains("IntakeRevisionCommandV2", content, StringComparison.Ordinal);
        Assert.Contains("IntakeMutationOutcomeV2", content, StringComparison.Ordinal);
        Assert.Contains("MyWorkSubmissionV2", content, StringComparison.Ordinal);
        Assert.Contains("AcceptedVersionDetailV2", content, StringComparison.Ordinal);
        Assert.Contains("ProtectedItemPreviewV2", content, StringComparison.Ordinal);
        Assert.Contains("MyWorkAttemptReadinessV2", content, StringComparison.Ordinal);
        Assert.Contains("AcknowledgeAttemptNoticeCommandV2", content, StringComparison.Ordinal);
        Assert.Contains("StartAttemptCommandV2", content, StringComparison.Ordinal);
        Assert.Contains("AcknowledgmentMutationOutcomeV2", content, StringComparison.Ordinal);
        Assert.Contains("StartAttemptOutcomeV2", content, StringComparison.Ordinal);
        Assert.Contains("SessionSnapshotV1", content, StringComparison.Ordinal);
        Assert.Contains("SessionCommandOutcomeV1", content, StringComparison.Ordinal);
        Assert.Contains("SessionHostedEventEnvelopeV1", content, StringComparison.Ordinal);
        Assert.Contains("/v1/sessions/{sessionId}", content, StringComparison.Ordinal);
        Assert.Contains("/v1/sessions/{sessionId}/commands", content, StringComparison.Ordinal);
        Assert.Contains("/v1/sessions/{sessionId}/events", content, StringComparison.Ordinal);
        Assert.Contains("/v2/assessment/my-work/{enrollmentId}/submission", content, StringComparison.Ordinal);
        Assert.Contains("/v2/assessment/activities/{activityId}/cohorts/{cohortId}/enrollments/{enrollmentId}/accommodations/{accommodationId}/decide", content, StringComparison.Ordinal);
        Assert.Contains("/v2/assessment/activities/{activityId}/cohorts/{cohortId}/enrollments/{enrollmentId}/accommodations/{accommodationId}/revoke", content, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenAPI_v2_enrollment_components_ref_canonical_schemas()
    {
        var openApiPath = Path.Combine(ContractsRoot, _catalog.Projections.OpenApi);
        var content = File.ReadAllText(openApiPath);
        var required = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["GrantAccommodationCommandV2"] = "../schemas/v2/enrollment/grant-accommodation-command.v2.schema.json",
            ["DecideAccommodationCommandV2"] = "../schemas/v2/enrollment/decide-accommodation-command.v2.schema.json",
            ["RevokeAccommodationCommandV2"] = "../schemas/v2/enrollment/revoke-accommodation-command.v2.schema.json",
            ["AccommodationMutationOutcomeV2"] = "../schemas/v2/enrollment/accommodation-mutation-outcome.v2.schema.json",
            ["EnrollmentTimingV2"] = "../schemas/v2/enrollment/enrollment-timing.v2.schema.json",
            ["MyWorkTimingV2"] = "../schemas/v2/enrollment/my-work-timing.v2.schema.json",
            ["BeginIntakeCommandV2"] = "../schemas/v2/submission/begin-intake-command.v2.schema.json",
            ["CompleteIntakeItemCommandV2"] = "../schemas/v2/submission/complete-intake-item-command.v2.schema.json",
            ["IntakeRevisionCommandV2"] = "../schemas/v2/submission/intake-revision-command.v2.schema.json",
            ["IntakeMutationOutcomeV2"] = "../schemas/v2/submission/intake-mutation-outcome.v2.schema.json",
            ["MyWorkSubmissionV2"] = "../schemas/v2/submission/my-work-submission.v2.schema.json",
            ["AcceptedVersionDetailV2"] = "../schemas/v2/submission/accepted-version-detail.v2.schema.json",
            ["ProtectedItemPreviewV2"] = "../schemas/v2/submission/protected-item-preview.v2.schema.json",
            ["MyWorkAttemptReadinessV2"] = "../schemas/v2/submission/my-work-attempt-readiness.v2.schema.json",
            ["AcknowledgeAttemptNoticeCommandV2"] = "../schemas/v2/submission/acknowledge-attempt-notice-command.v2.schema.json",
            ["StartAttemptCommandV2"] = "../schemas/v2/submission/start-attempt-command.v2.schema.json",
            ["AcknowledgmentMutationOutcomeV2"] = "../schemas/v2/submission/acknowledgment-mutation-outcome.v2.schema.json",
            ["StartAttemptOutcomeV2"] = "../schemas/v2/submission/start-attempt-outcome.v2.schema.json",
            ["SessionSnapshotV1"] = "../schemas/v1/session/snapshot.v1.schema.json",
            ["SessionCommandOutcomeV1"] = "../schemas/v1/session/command-outcome.v1.schema.json",
            ["SessionHostedEventEnvelopeV1"] = "../schemas/v1/session/hosted-event-envelope.v1.schema.json",
        };
        foreach (var (component, relativeSchema) in required)
        {
            Assert.Contains(
                $"{component}:\n      $ref: '{relativeSchema}'",
                content,
                StringComparison.Ordinal);
            Assert.True(
                File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(openApiPath)!, relativeSchema))),
                relativeSchema);
        }
    }

    public static TheoryData<string, string> ValidFixtureCases => DiscoverFixtures("valid-");

    public static TheoryData<string, string> InvalidFixtureCases => DiscoverFixtures("invalid-");

    private static TheoryData<string, string> DiscoverFixtures(string prefix)
    {
        var catalog = ContractCatalogLoader.Load(ContractsRoot);
        var data = new TheoryData<string, string>();
        foreach (var entry in catalog.RepresentativeSchemas)
        {
            var fixturesRoot = Path.Combine(ContractsRoot, entry.FixtureDir);
            foreach (var file in Directory.EnumerateFiles(fixturesRoot, $"{prefix}*.json", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(ContractsRoot, file).Replace('\\', '/');
                data.Add(relative, entry.SchemaId);
            }
        }

        return data;
    }
}

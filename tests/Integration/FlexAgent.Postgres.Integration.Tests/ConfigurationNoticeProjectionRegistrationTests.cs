using System.Text;
using Dapper;
using FlexAgent.CanonicalJson;
using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres.Integration.Tests.Support;

namespace FlexAgent.Postgres.Integration.Tests;

public sealed class ConfigurationNoticeProjectionRegistrationTests(PostgresIntegrationFixture fixture)
    : PostgresIntegrationTest(fixture)
{
    [Fact]
    public async Task Source_version_registration_persists_the_digest_covered_notice_projection()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var noticeId = Guid.CreateVersion7();
        var (utf8, digest) = Canonical(
            $$"""
            {"participant_notices":[{"content_digest":"{{new string('b', 64)}}","notice_id":"{{noticeId:D}}","notice_type":"consent","protected_content_ref":"notice:consent","required_outcome":"affirmed"}]}
            """);
        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, utf8, digest, "notice-proj-0001"),
            CancellationToken);
        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.NotNull(result.Identity);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var setCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT notice_count
            FROM configuration_participant_notice_projection_sets
            WHERE organization_id = @OrganizationId AND source_version_id = @VersionId
            """,
            new { OrganizationId = seeded.OrganizationId, VersionId = result.Identity!.VersionId });
        Assert.Equal(1, setCount);
        var notice = await connection.QuerySingleAsync<(Guid NoticeId, string NoticeType, string ContentDigest)>(
            """
            SELECT notice_id, notice_type, content_digest
            FROM configuration_participant_notice_projections
            WHERE organization_id = @OrganizationId AND source_version_id = @VersionId
            """,
            new { OrganizationId = seeded.OrganizationId, VersionId = result.Identity.VersionId });
        Assert.Equal(noticeId, notice.NoticeId);
        Assert.Equal(ParticipantNoticeTypes.Consent, notice.NoticeType);
        Assert.Equal(new string('b', 64), notice.ContentDigest);
    }

    [Fact]
    public async Task Invalid_notice_projection_rejects_registration_without_a_version_row()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var (utf8, digest) = Canonical("""{"participant_notices":{"notice_type":"instructions"}}""");
        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, utf8, digest, "notice-proj-invalid"),
            CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Equal(RegisterConfigurationSourceVersionFailureCodes.NoticeProjectionInvalid, result.OutcomeCode);

        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var versions = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)::int
            FROM configuration_source_versions
            WHERE organization_id = @OrganizationId
            """,
            new { OrganizationId = seeded.OrganizationId });
        Assert.Equal(0, versions);
    }

    [Fact]
    public async Task Empty_notice_array_records_a_verifiable_zero_count_set()
    {
        var seeded = await Fixture.SeedOrganizationAsync();
        var content = PostgresIntegrationFixture.LoadMinimalStableDomainCanonicalUtf8();
        var result = await Fixture.Services.RegisterHandler.HandleAsync(
            CreateCommand(seeded, content, PostgresIntegrationFixture.MinimalStableDomainDigest, "notice-proj-empty"),
            CancellationToken);
        Assert.True(result.Succeeded, result.OutcomeCode);
        await using var connection = await Fixture.Services.ConnectionAccessor.OpenConnectionAsync(CancellationToken);
        var setCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT notice_count
            FROM configuration_participant_notice_projection_sets
            WHERE organization_id = @OrganizationId AND source_version_id = @VersionId
            """,
            new { OrganizationId = seeded.OrganizationId, VersionId = result.Identity!.VersionId });
        Assert.Equal(0, setCount);
    }

    private static RegisterConfigurationSourceVersionCommand CreateCommand(
        SeededOrganization seeded,
        ReadOnlyMemory<byte> content,
        string digest,
        string idempotencyKey) =>
        new(
            seeded.Actor,
            seeded.Scope,
            seeded.ConfigurationSourceId,
            ConfigurationProcedureIds.RscJcsSha256V1,
            ConfigurationSchemaVersions.V1,
            content,
            digest,
            idempotencyKey,
            Guid.NewGuid(),
            "integration.test");

    private static (ReadOnlyMemory<byte> Utf8, string Digest) Canonical(string json)
    {
        var utf8 = Encoding.UTF8.GetBytes(json);
        var digest = CanonicalJsonProcessor.CanonicalizeSha256Hex(
            utf8,
            new CanonicalJsonLimits(65_536, 64, 4_096, 4_096));
        return (utf8, digest);
    }
}

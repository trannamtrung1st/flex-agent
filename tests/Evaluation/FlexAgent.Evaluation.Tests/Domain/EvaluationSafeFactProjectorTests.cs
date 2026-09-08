using System.Text;
using System.Text.Json;
using FlexAgent.CanonicalJson;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvaluationSafeFactProjectorTests
{
    private static readonly CanonicalJsonLimits Limits = new(65_536, 64, 4_096, 4_096);

    [Fact]
    public void Configuration_projection_strips_unsafe_fields_and_hashes_projection()
    {
        var configurationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var canonical = """
            {
              "organization_id":"org-secret",
              "credential_binding_reference":"cred.secret",
              "sources":[{"source_key":"rubric_evaluation","source_id":"22222222-2222-2222-2222-222222222222","source_version_id":"33333333-3333-3333-3333-333333333333","content_digest":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","prompt_text":"hidden"}],
              "permitted_submissions":[{"protected_ref":"submission.version.44444444-4444-4444-4444-444444444444","content_digest":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","raw_bytes":"hidden"}],
              "model_profile_id":"mdl.p0.text.synthetic",
              "model_profile_version":"mdl.p0.text.synthetic.v1",
              "model_profile_digest":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"
            }
            """u8.ToArray();
        var configurationDigest = CanonicalJsonProcessor.CanonicalizeSha256Hex(canonical.AsSpan(), Limits);

        var result = EvaluationSafeFactProjector.TryBuildConfigurationFact(
            configurationId,
            configurationDigest,
            canonical);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("cfg.11111111111111111111111111111111", result.Value!.SourceId);
        Assert.Equal($"rev.{configurationDigest}", result.Value.SourceVersion);
        Assert.Equal(
            CanonicalJsonProcessor.CanonicalizeSha256Hex(result.Value.ProjectionUtf8.Span, Limits),
            result.Value.ContentDigest);
        using var projection = JsonDocument.Parse(result.Value.ProjectionUtf8);
        Assert.False(projection.RootElement.TryGetProperty("organization_id", out _));
        Assert.False(projection.RootElement.TryGetProperty("credential_binding_reference", out _));
        Assert.True(projection.RootElement.TryGetProperty("sources", out var sources));
        Assert.False(sources[0].TryGetProperty("prompt_text", out _));
        Assert.True(projection.RootElement.TryGetProperty("model_profile_id", out _));
    }

    [Fact]
    public void Manifest_projection_keeps_provenance_without_unsafe_fields()
    {
        var manifestId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var canonical = """
            {
              "manifest_id":"55555555-5555-5555-5555-555555555555",
              "configuration_id":"11111111-1111-1111-1111-111111111111",
              "configuration_digest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
              "session_id":"66666666-6666-6666-6666-666666666666",
              "provenance":[{"source_key":"rubric_evaluation","source_id":"22222222-2222-2222-2222-222222222222","source_version_id":"33333333-3333-3333-3333-333333333333","content_digest":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","internal_note":"hidden"}]
            }
            """u8.ToArray();
        var manifestDigest = CanonicalJsonProcessor.CanonicalizeSha256Hex(canonical.AsSpan(), Limits);

        var result = EvaluationSafeFactProjector.TryBuildManifestFact(
            manifestId,
            manifestDigest,
            canonical);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal("mfst.55555555555555555555555555555555", result.Value!.SourceId);
        using var projection = JsonDocument.Parse(result.Value.ProjectionUtf8);
        Assert.False(projection.RootElement.TryGetProperty("session_id", out _));
        Assert.True(projection.RootElement.TryGetProperty("provenance", out var provenance));
        Assert.False(provenance[0].TryGetProperty("internal_note", out _));
    }

    [Fact]
    public void Configuration_projection_rejects_canonical_bytes_that_do_not_match_frozen_digest()
    {
        var configurationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var canonical = """{"sources":[]}"""u8.ToArray();

        var result = EvaluationSafeFactProjector.TryBuildConfigurationFact(
            configurationId,
            new string('c', 64),
            canonical);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("canonical_digest", result.Field);
    }

    [Fact]
    public void Manifest_projection_rejects_canonical_bytes_that_do_not_match_frozen_digest()
    {
        var manifestId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var canonical = """{"provenance":[]}"""u8.ToArray();

        var result = EvaluationSafeFactProjector.TryBuildManifestFact(
            manifestId,
            new string('d', 64),
            canonical);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.CitationIntegrity, result.OutcomeCode);
        Assert.Equal("canonical_digest", result.Field);
    }
}

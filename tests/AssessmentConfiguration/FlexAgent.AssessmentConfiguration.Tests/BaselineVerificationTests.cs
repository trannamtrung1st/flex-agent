using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Canonicalization;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Tests;

public sealed class BaselineVerificationTests
{
    [Fact]
    public void Unactivated_draft_has_no_verification_status()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var status = BaselineVerification.Status(draft, AssessmentFixtures.PermittedSources());
        Assert.Null(status);
    }

    [Fact]
    public void Activated_draft_with_current_sources_is_verified()
    {
        var draft = AssessmentFixtures.CreateDraft().Value! with { HasActivatedCohort = true };
        var digest = AssessmentFixtures.Digest('a');
        var status = BaselineVerification.Status(
            draft,
            AssessmentFixtures.PermittedSources(),
            BaselineDigestCheck.Present(digest, digest, draft.RevisionId, draft.RevisionId));
        Assert.Equal(BaselineVerification.Verified, status);
    }

    [Fact]
    public void Activated_draft_with_digest_mismatch_is_degraded()
    {
        var draft = AssessmentFixtures.CreateDraft().Value! with { HasActivatedCohort = true };
        var status = BaselineVerification.Status(
            draft,
            AssessmentFixtures.PermittedSources(),
            BaselineDigestCheck.Present(
                AssessmentFixtures.Digest('a'),
                AssessmentFixtures.Digest('b'),
                draft.RevisionId,
                draft.RevisionId));
        Assert.Equal(BaselineVerification.Degraded, status);
    }

    [Fact]
    public void Activated_draft_without_a_recomputed_digest_is_degraded()
    {
        var draft = AssessmentFixtures.CreateDraft().Value! with { HasActivatedCohort = true };
        var status = BaselineVerification.Status(
            draft,
            AssessmentFixtures.PermittedSources(),
            BaselineDigestCheck.Present(AssessmentFixtures.Digest('a'), null, draft.RevisionId, draft.RevisionId));
        Assert.Equal(BaselineVerification.Degraded, status);
    }

    [Fact]
    public void Activated_draft_without_revision_ids_is_degraded()
    {
        var draft = AssessmentFixtures.CreateDraft().Value! with { HasActivatedCohort = true };
        var digest = AssessmentFixtures.Digest('a');
        var status = BaselineVerification.Status(
            draft,
            AssessmentFixtures.PermittedSources(),
            new BaselineDigestCheck(digest, digest, true, null, null));
        Assert.Equal(BaselineVerification.Degraded, status);
    }

    [Fact]
    public void Activated_draft_without_a_digest_check_is_degraded()
    {
        var draft = AssessmentFixtures.CreateDraft().Value! with { HasActivatedCohort = true };
        var status = BaselineVerification.Status(draft, AssessmentFixtures.PermittedSources());
        Assert.Equal(BaselineVerification.Degraded, status);
    }

    [Fact]
    public void Activated_draft_without_a_bound_baseline_is_degraded()
    {
        var draft = AssessmentFixtures.CreateDraft().Value! with { HasActivatedCohort = true };
        var digest = AssessmentFixtures.Digest('a');
        var status = BaselineVerification.Status(
            draft,
            AssessmentFixtures.PermittedSources(),
            BaselineDigestCheck.Missing());
        Assert.Equal(BaselineVerification.Degraded, status);
    }

    [Fact]
    public void Activated_draft_with_a_bound_revision_mismatch_is_degraded()
    {
        var draft = AssessmentFixtures.CreateDraft().Value! with { HasActivatedCohort = true };
        var digest = AssessmentFixtures.Digest('a');
        var status = BaselineVerification.Status(
            draft,
            AssessmentFixtures.PermittedSources(),
            BaselineDigestCheck.Present(
                digest,
                digest,
                Guid.Parse("99999999-9999-4999-8999-999999999999"),
                draft.RevisionId));
        Assert.Equal(BaselineVerification.Degraded, status);
    }

    [Fact]
    public void Activated_draft_with_revoked_source_is_degraded()
    {
        var draft = AssessmentFixtures.CreateDraft().Value! with { HasActivatedCohort = true };
        var sources = AssessmentFixtures.PermittedSources();
        sources[6] = sources[6] with { LifecycleState = SourceLifecycleStates.Revoked };

        var digest = AssessmentFixtures.Digest('a');
        var status = BaselineVerification.Status(
            draft,
            sources,
            BaselineDigestCheck.Present(digest, digest, draft.RevisionId, draft.RevisionId));
        Assert.Equal(BaselineVerification.Degraded, status);
    }

    [Fact]
    public async Task Stored_baseline_document_recomputes_the_same_digest()
    {
        var draft = AssessmentFixtures.CreateDraft().Value!;
        var document = ActivationBaselineDocument.FromReadyDraft(draft, AssessmentFixtures.PermittedSources()).Value!;
        var digester = new ActivationBaselineDigester();
        var digest = digester.Digest(document);
        Assert.True(digest.Succeeded, digest.OutcomeCode);
        var store = new InMemoryAssessmentBaselineStore();
        var actor = new AssessmentActorContext(
            new TrustedActor(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "human.interactive"),
            new OrganizationScope(AssessmentFixtures.OrganizationId),
            AuthenticationStrengthEvaluator.AdministratorRelationship,
            new AuthenticationStrength("mfa", ["mfa"]),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            "https");
        var cohortId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        await store.InsertAsync(
            draft.OrganizationId,
            draft.ActivityId,
            cohortId,
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            document,
            digest.Value!,
            new InMemoryAssessmentTransaction(),
            actor,
            DateTimeOffset.UtcNow,
            TestContext.Current.CancellationToken);
        var found = await store.FindBoundAsync(
            draft.OrganizationId,
            draft.ActivityId,
            cohortId,
            TestContext.Current.CancellationToken);
        var recomputed = digester.Digest(found!.Document);

        Assert.Equal(digest.Value, found.ContentDigest);
        Assert.Equal(digest.Value, recomputed.Value);
        Assert.Equal(
            BaselineVerification.Verified,
            BaselineVerification.Status(
                draft with { HasActivatedCohort = true },
                AssessmentFixtures.PermittedSources(),
                BaselineDigestCheck.Present(
                    found.ContentDigest,
                    recomputed.Value,
                    draft.RevisionId,
                    draft.RevisionId)));
    }
}

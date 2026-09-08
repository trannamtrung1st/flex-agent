using System.Text.Json;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceLocatorCompletionVerifierTests
{
    [Fact]
    public void Completion_verifier_seals_verified_locators_from_session_and_submission_bundles()
    {
        using var locatorDocument = JsonDocument.Parse(
            """
            {
              "locator_schema":"evidence-locator.v1",
              "source_type":"submission.direct_text",
              "source_ref":{"source_id":"item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","source_version":"rev.0001"},
              "ownership_ref":{
                "organization_id":"org.synthetic.0001",
                "activity_id":"act.synthetic.0001",
                "participant_id":"part.synthetic.0001",
                "attempt_id":"att.synthetic.0001",
                "session_id":"sess.synthetic.0001",
                "evaluation_id":"eval.synthetic.0001"
              },
              "location":{"location_type":"whole_item","item_id":"item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"},
              "precision":"whole_item",
              "integrity":{
                "source_digest":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv.completion.0001"}
            }
            """);
        var ownership = new EvaluationOwnership(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"));
        var handoff = new EvaluationHandoffSnapshot(
            "handoff.completion.0001",
            ownership,
            "completed",
            Guid.CreateVersion7(),
            42,
            "manifest-jcs-sha256-v2",
            new string('f', 64),
            Guid.CreateVersion7(),
            new string('c', 64),
            Guid.CreateVersion7(),
            new string('d', 64));
        var sessionBundle = new EvaluationSessionEvidenceBundle(
            handoff,
            [],
            new EvaluationSafeFactProjection(
                "cfg.example",
                "rev.example",
                new string('c', 64),
                """{"sources":[]}"""u8.ToArray()),
            new EvaluationSafeFactProjection(
                "mfst.example",
                "rev.example",
                new string('d', 64),
                """{"provenance":[]}"""u8.ToArray()));
        var submissionBundle = new EvaluationSubmissionEvidenceBundle(
        [
            new EvaluationSubmissionMaterial(
                "item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "rev.0001",
                "submission.direct_text",
                new string('a', 64),
                new byte[256]),
        ]);
        var request = new EvidenceLocatorCompletionRequest(
            new EvaluationStableOwnershipReference(
                "org.synthetic.0001",
                "act.synthetic.0001",
                "part.synthetic.0001",
                "att.synthetic.0001",
                "sess.synthetic.0001",
                "eval.synthetic.0001"),
            handoff.HandoffId,
            [new EvidenceLocatorVerificationEntry("evidence.completion.0001", locatorDocument.RootElement.Clone())],
            PermitWholeItemFallback: false);

        var result = EvidenceLocatorCompletionVerifier.TryVerify(
            request,
            sessionBundle,
            submissionBundle);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Single(result.Value!.VerifiedLocators);
        Assert.Single(result.Value.SealedItems);
        Assert.Equal("evidence.completion.0001", result.Value.SealedItems[0].EvidenceId);
        Assert.Equal("verified", result.Value.SealedItems[0].VerificationState);
    }

    [Fact]
    public void Duplicate_evidence_ids_fail_before_verification()
    {
        using var locatorDocument = JsonDocument.Parse("{}");
        var ownership = new EvaluationOwnership(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        var request = new EvidenceLocatorCompletionRequest(
            EvaluationStableOwnershipReferenceFactory.From(ownership, Guid.CreateVersion7()),
            "handoff.duplicate",
            [
                new EvidenceLocatorVerificationEntry("evidence.duplicate.0001", locatorDocument.RootElement.Clone()),
                new EvidenceLocatorVerificationEntry("evidence.duplicate.0001", locatorDocument.RootElement.Clone()),
            ],
            PermitWholeItemFallback: false);

        var result = EvidenceLocatorCompletionVerifier.TryVerify(
            request,
            new EvaluationSessionEvidenceBundle(
                new EvaluationHandoffSnapshot(
                    "handoff.duplicate",
                    ownership,
                    "completed",
                    Guid.CreateVersion7(),
                    1,
                    "manifest-jcs-sha256-v2",
                    new string('f', 64),
                    Guid.CreateVersion7(),
                    new string('c', 64),
                    Guid.CreateVersion7(),
                    new string('d', 64)),
                []),
            null);

        Assert.False(result.Succeeded);
        Assert.Equal(EvaluationFailureCodes.DuplicateIdentity, result.OutcomeCode);
    }
}

using System.Text.Json;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class EvidenceLocatorMetadataProjectorTests
{
    [Fact]
    public void Submission_locator_projects_protected_submission_references()
    {
        var evidenceId = Guid.CreateVersion7();
        var itemRecordId = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var acceptedVersionId = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var sourceDigest = new string('c', 64);
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
                "source_digest":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv.metadata.0001"}
            }
            """);
        var handoff = new EvaluationHandoffSnapshot(
            "handoff.metadata.0001",
            new EvaluationOwnership(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7()),
            "completed",
            Guid.CreateVersion7(),
            42,
            "manifest-jcs-sha256-v2",
            new string('f', 64),
            Guid.CreateVersion7(),
            new string('c', 64),
            Guid.CreateVersion7(),
            new string('d', 64));
        var submissionBundle = new EvaluationSubmissionEvidenceBundle(
        [
            new EvaluationSubmissionMaterial(
                "item.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "rev.0001",
                "submission.direct_text",
                sourceDigest,
                new byte[256],
                acceptedVersionId,
                itemRecordId),
        ]);
        var verified = new VerifiedEvidenceLocator(
            "submission.direct_text",
            new string('a', 64),
            new string('b', 64),
            "verified",
            sourceDigest);

        var result = EvidenceLocatorMetadataProjector.TryCreate(
            evidenceId,
            locatorDocument.RootElement,
            verified,
            handoff,
            submissionBundle);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(evidenceId, result.Value!.EvidenceId);
        Assert.Equal(itemRecordId, result.Value.SourceId);
        Assert.Equal(acceptedVersionId, result.Value.SourceVersionId);
        Assert.Equal(sourceDigest, result.Value.SourceContentDigest);
        Assert.Equal("evidence-locator.v1", result.Value.LocatorSchema);
        Assert.Equal(64, result.Value.LocatorDigest.Length);
    }

    [Fact]
    public void Work_trace_locator_projects_deterministic_source_references()
    {
        var evidenceId = Guid.CreateVersion7();
        const string sourceId = "trace.synthetic.0001";
        const string sourceVersion = "rev.trace.0001";
        using var locatorDocument = JsonDocument.Parse(
            $$"""
            {
              "locator_schema":"evidence-locator.v1",
              "source_type":"session.work_trace",
              "source_ref":{
                "source_id":"{{sourceId}}",
                "source_version":"{{sourceVersion}}",
                "terminal_cutoff_sequence":"42"
              },
              "ownership_ref":{
                "organization_id":"org.synthetic.0001",
                "activity_id":"act.synthetic.0001",
                "participant_id":"part.synthetic.0001",
                "attempt_id":"att.synthetic.0001",
                "session_id":"sess.synthetic.0001",
                "evaluation_id":"eval.synthetic.0001"
              },
              "location":{"location_type":"whole_item","item_id":"{{sourceId}}"},
              "precision":"whole_item",
              "integrity":{
                "source_digest":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
                "adapter_version":"locator-adapter.v1",
                "verification_state":"verified"
              },
              "created_by":{"service_id":"evaluation-service","invocation_id":"inv.metadata.0002"}
            }
            """);
        var verified = new VerifiedEvidenceLocator(
            "session.work_trace",
            new string('a', 64),
            new string('b', 64),
            "verified",
            new string('d', 64));
        var handoff = new EvaluationHandoffSnapshot(
            "handoff.metadata.0002",
            new EvaluationOwnership(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7()),
            "completed",
            Guid.CreateVersion7(),
            42,
            "manifest-jcs-sha256-v2",
            new string('f', 64),
            Guid.CreateVersion7(),
            new string('c', 64),
            Guid.CreateVersion7(),
            new string('d', 64));

        var result = EvidenceLocatorMetadataProjector.TryCreate(
            evidenceId,
            locatorDocument.RootElement,
            verified,
            handoff,
            null);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Equal(
            EvaluationDeterministicGuid.CreateVersion5(EvaluationSourceNamespaces.WorkTraceItem, sourceId),
            result.Value!.SourceId);
        Assert.Equal(
            EvaluationDeterministicGuid.CreateVersion5(EvaluationSourceNamespaces.WorkTraceVersion, sourceVersion),
            result.Value.SourceVersionId);
    }
}

using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Evaluation.Domain;

public sealed record SealedEvidenceItemReference(
    string EvidenceId,
    string SourceType,
    string SourceRefDigest,
    string LocationDigest,
    string VerificationState);

public sealed record EvidenceSetOwnershipReference(
    string OrganizationId,
    string ActivityId,
    string ParticipantId,
    string AttemptId,
    string SessionId);

public sealed record EvidenceSetSealRequest(
    string EvidenceSetId,
    string EvaluationInvocationId,
    EvidenceSetOwnershipReference Ownership,
    string HandoffDigest,
    string FrozenInputDigest,
    IReadOnlyList<SealedEvidenceItemReference> Items);

public static class EvidenceSetSealComputer
{
    public const string ProcedureId = "evidence-set-jcs-sha256-v1";

    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 65_536,
        maxNestingDepth: 64,
        maxObjectProperties: 4_096,
        maxArrayElements: 4_096);

    public static EvaluationDecision<string> TryComputeDigest(EvidenceSetSealRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!EvaluationIdentity.IsStableId(request.EvidenceSetId)
            || !EvaluationIdentity.IsStableId(request.EvaluationInvocationId)
            || !EvaluationIdentity.IsSha256Hex(request.HandoffDigest)
            || !EvaluationIdentity.IsSha256Hex(request.FrozenInputDigest)
            || request.Items.Count is < 1 or > 128)
        {
            return EvaluationDecision<string>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (request.Items.Any(item =>
                !EvaluationIdentity.IsStableId(item.EvidenceId)
                || !EvaluationIdentity.IsSha256Hex(item.SourceRefDigest)
                || !EvaluationIdentity.IsSha256Hex(item.LocationDigest)
                || item.VerificationState is not ("verified" or "lower_precision" or "unavailable" or "integrity_changed")))
        {
            return EvaluationDecision<string>.Fail(EvaluationFailureCodes.InvalidField);
        }

        var duplicateEvidenceIds = request.Items
            .GroupBy(item => item.EvidenceId, StringComparer.Ordinal)
            .Any(group => group.Count() > 1);
        if (duplicateEvidenceIds)
        {
            return EvaluationDecision<string>.Fail(EvaluationFailureCodes.DuplicateIdentity);
        }

        var sortedItems = request.Items
            .OrderBy(item => item.SourceType, StringComparer.Ordinal)
            .ThenBy(item => item.SourceRefDigest, StringComparer.Ordinal)
            .ThenBy(item => item.LocationDigest, StringComparer.Ordinal)
            .ThenBy(item => item.EvidenceId, StringComparer.Ordinal)
            .Select(item => new
            {
                evidence_id = item.EvidenceId,
                source_type = item.SourceType,
                source_ref_digest = item.SourceRefDigest,
                location_digest = item.LocationDigest,
                verification_state = item.VerificationState,
            })
            .ToArray();

        var document = new
        {
            procedure_id = ProcedureId,
            schema_version = "v1",
            canonicalization_version = "rfc8785",
            evidence_set_id = request.EvidenceSetId,
            evaluation_invocation_id = request.EvaluationInvocationId,
            ownership = new
            {
                organization_id = request.Ownership.OrganizationId,
                activity_id = request.Ownership.ActivityId,
                participant_id = request.Ownership.ParticipantId,
                attempt_id = request.Ownership.AttemptId,
                session_id = request.Ownership.SessionId,
            },
            handoff_digest = request.HandoffDigest,
            frozen_input_digest = request.FrozenInputDigest,
            evidence_items = sortedItems,
        };

        return EvaluationDecision<string>.Ok(
            CanonicalJsonProcessor.CanonicalizeSha256Hex(
                JsonSerializer.SerializeToUtf8Bytes(document),
                Limits));
    }
}

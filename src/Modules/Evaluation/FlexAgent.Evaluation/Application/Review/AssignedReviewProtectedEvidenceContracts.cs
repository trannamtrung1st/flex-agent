using FlexAgent.Contracts.Evidence;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application.Review;

public sealed record AssignedReviewProtectedEvidenceRequest(
    Guid OrganizationId,
    Guid EvaluationId,
    Guid RequestId,
    Guid ActivityId,
    Guid ParticipantId,
    Guid AttemptId,
    Guid SessionId,
    Guid EvidenceId,
    string LocatorCanonicalJson,
    string LocatorDigest,
    string IntegrityState);

public sealed record AssignedReviewProtectedEvidenceResolution(
    EvidenceLocatorV1 Locator,
    string Availability,
    string? DisplayText,
    string? UnavailabilityNotice);

public interface IAssignedReviewProtectedEvidenceResolver
{
    Task<EvaluationDecision<AssignedReviewProtectedEvidenceResolution>> TryResolveAsync(
        AssignedReviewProtectedEvidenceRequest request,
        CancellationToken cancellationToken);
}

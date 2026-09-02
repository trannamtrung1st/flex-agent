using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class AttemptAcknowledgmentCoordinator(
    IEnrollmentAuthorizationPort authorization,
    IEnrollmentStore enrollments,
    IParticipantNoticePort noticePort,
    IAcknowledgmentLifecyclePort acknowledgments,
    IEnrollmentUnitOfWork unitOfWork,
    IEnrollmentSessionPort sessions) : IAttemptAcknowledgmentCoordinator
{
    public async Task<AcknowledgmentMutationOutcome> RecordAsync(
        AcknowledgeAttemptNoticeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (EnrollmentIdempotencyKey.Validate(command.IdempotencyKey) is { } invalid)
        {
            return new AcknowledgmentMutationOutcome(false, invalid, null, null);
        }

        if (EnrollmentAuthenticationPolicy.Evaluate(command.Actor, EnrollmentAuthorizationActions.Discover) is not null)
        {
            return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.Denied, null, null);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            command.Actor,
            EnrollmentAuthorizationActions.Discover,
            command.EnrollmentId,
            EnrollmentResourceTypes.Assignment,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.Denied, null, null);
        }

        try
        {
            return await unitOfWork.ExecuteAsync(command.Actor, async transaction =>
            {
                if (!await sessions.RevalidateLiveAsync(command.Actor, transaction, cancellationToken))
                {
                    return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.Denied, null, null);
                }

                var reauth = await authorization.ReauthorizeAsync(
                    command.Actor,
                    EnrollmentAuthorizationActions.Discover,
                    command.EnrollmentId,
                    EnrollmentResourceTypes.Assignment,
                    transaction,
                    cancellationToken);
                if (!reauth.IsPermitted)
                {
                    return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.Denied, null, null);
                }

                var enrollment = await enrollments.FindAsync(
                    command.Actor.Organization.OrganizationId,
                    command.EnrollmentId,
                    transaction,
                    cancellationToken);
                if (enrollment is null || enrollment.ParticipantActorId != command.Actor.Actor.ActorId)
                {
                    return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.Denied, null, null);
                }

                var notices = await noticePort.ListRequiredAsync(
                    enrollment.OrganizationId,
                    enrollment.ActivityId,
                    enrollment.CohortId,
                    enrollment.BaselineId,
                    transaction,
                    cancellationToken);
                if (notices is null)
                {
                    return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.Unavailable, null, null);
                }

                var notice = notices.FirstOrDefault(item =>
                    item.NoticeId == command.NoticeId && item.SourceVersionId == command.SourceVersionId);
                if (notice is null)
                {
                    return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.AcknowledgmentInvalid, null, null);
                }

                var digest = AcknowledgmentCommandDigest.Compute(
                    enrollment.OrganizationId,
                    enrollment.EnrollmentId,
                    enrollment.ParticipantActorId,
                    command.NoticeId,
                    command.SourceVersionId,
                    command.Outcome);
                if (!string.Equals(digest, command.TrustedCommandDigest, StringComparison.Ordinal))
                {
                    return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.IdempotencyConflict, null, null);
                }

                return await acknowledgments.RecordAsync(command, notice, transaction.CommitHandle, cancellationToken);
            },
            cancellationToken);
        }
        catch (EnrollmentSessionExpiredException)
        {
            return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.Denied, null, null);
        }
    }
}

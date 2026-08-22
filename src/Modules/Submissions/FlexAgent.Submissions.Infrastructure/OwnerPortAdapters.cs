using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Infrastructure;

public sealed class AssessmentActivatedCohortPort(IActivatedCohortBindingReader reader) : IActivatedCohortPort
{
    public Task<ActivatedCohortBinding?> FindActivatedAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        CancellationToken cancellationToken = default) =>
        MapAsync(reader.GetActivatedAsync(organizationId, activityId, cohortId, null, cancellationToken));

    public Task<ActivatedCohortBinding?> RevalidateAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default) =>
        MapAsync(reader.GetActivatedAsync(organizationId, activityId, cohortId, transaction.CommitHandle, cancellationToken));

    private static async Task<ActivatedCohortBinding?> MapAsync(Task<ActivatedCohortBindingSnapshot?> source)
    {
        var snapshot = await source;
        return snapshot is null
            ? null
            : new ActivatedCohortBinding(
                snapshot.OrganizationId,
                snapshot.ActivityId,
                snapshot.CohortId,
                snapshot.BaselineId,
                snapshot.BaselineDigest,
                snapshot.CohortState,
                snapshot.TaskSourceId,
                snapshot.TaskVersionId,
                snapshot.TaskContentDigest,
                snapshot.ActivityTitle,
                snapshot.TaskTitle,
                snapshot.TimeZoneId,
                snapshot.StartsAtUtc,
                snapshot.EndsAtUtc,
                snapshot.DeadlineUtc,
                EnrollmentLifecyclePolicy.RestrictedPreservationPolicyId,
                EnrollmentLifecyclePolicy.RestrictedPreservationVersion,
                snapshot.VerificationDegraded);
    }
}

public sealed class IdentityEnrollmentCandidatePort(IHumanDisplayProfileDirectory directory) : IEnrollmentCandidatePort
{
    public async Task<CursorPage<EnrollmentCandidate>> ListEligibleAsync(
        Guid organizationId,
        string? prefix,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var page = await directory.ListEligibleAsync(
            organizationId,
            EnrollmentAuthorizationActions.Receive,
            prefix,
            cursor,
            limit,
            cancellationToken);
        return new CursorPage<EnrollmentCandidate>(
            page.Items.Select(item => new EnrollmentCandidate(item.ActorId, item.DisplayLabel)).ToArray(),
            page.NextCursor,
            page.HasMore);
    }

    public async Task<EnrollmentCandidate?> RevalidateEligibleAsync(
        Guid organizationId,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var candidate = await directory.RevalidateEligibleAsync(
            organizationId,
            actorId,
            EnrollmentAuthorizationActions.Receive,
            transaction.CommitHandle,
            cancellationToken);
        return candidate is null ? null : new EnrollmentCandidate(candidate.ActorId, candidate.DisplayLabel);
    }

    public Task<string?> DisplayLabelAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default) =>
        directory.FindDisplayLabelAsync(organizationId, actorId, cancellationToken);
}

public sealed class IdentityEnrollmentSessionPort(IApplicationSessionCommitPort sessions) : IEnrollmentSessionPort
{
    public Task<bool> RevalidateLiveAsync(
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default) =>
        sessions.RevalidateLiveAsync(
            actor.ApplicationSessionId,
            actor.Actor.ActorId,
            actor.Organization.OrganizationId,
            transaction.CommitHandle,
            cancellationToken);

    public Task<bool> ConfirmLiveAsync(
        EnrollmentActorContext actor,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken = default) =>
        sessions.ConfirmLiveAsync(
            actor.ApplicationSessionId,
            actor.Actor.ActorId,
            actor.Organization.OrganizationId,
            transaction.CommitHandle,
            cancellationToken);
}

using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class EnrollmentQueryService(
    IEnrollmentAuthorizationPort authorization,
    IActivatedCohortPort cohorts,
    IEnrollmentCandidatePort candidates,
    IEnrollmentStore enrollments,
    IEnrollmentCursorSigner cursors) : IEnrollmentQueryService
{
    public async Task<EnrollmentDecision<CursorPage<EnrollmentCandidate>>> ListCandidatesAsync(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId,
        string? prefix,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfUnauthorizedAsync(
            actor,
            EnrollmentAuthorizationActions.CandidateRead,
            cohortId,
            EnrollmentResourceTypes.Cohort,
            cancellationToken);
        if (denied is not null)
        {
            return EnrollmentDecision<CursorPage<EnrollmentCandidate>>.Fail(denied);
        }

        if (!IsValidLimit(limit)
            || (prefix?.Length ?? 0) > EnrollmentPageBounds.MaximumQueryPrefixLength
            || (cursor?.Length ?? 0) > EnrollmentPageBounds.MaximumCursorLength)
        {
            return EnrollmentDecision<CursorPage<EnrollmentCandidate>>.Fail(EnrollmentFailureCodes.InvalidField);
        }

        if (await cohorts.FindActivatedAsync(actor.Organization.OrganizationId, activityId, cohortId, cancellationToken) is null)
        {
            return EnrollmentDecision<CursorPage<EnrollmentCandidate>>.Fail(EnrollmentFailureCodes.Denied);
        }

        var page = await candidates.ListEligibleAsync(
            actor.Organization.OrganizationId,
            prefix,
            cursor,
            limit,
            cancellationToken);
        return EnrollmentDecision<CursorPage<EnrollmentCandidate>>.Ok(page, "enrollment.ok");
    }

    public async Task<EnrollmentDecision<CursorPage<EnrollmentSummary>>> ListEnrollmentsAsync(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfUnauthorizedAsync(
            actor,
            EnrollmentAuthorizationActions.List,
            cohortId,
            EnrollmentResourceTypes.Cohort,
            cancellationToken);
        if (denied is not null)
        {
            return EnrollmentDecision<CursorPage<EnrollmentSummary>>.Fail(denied);
        }

        var scope = EnrollmentListCursorScope.ForEnrollments(actor, activityId, cohortId);
        if (!IsValidLimit(limit)
            || !EnrollmentListCursor.TryOpen(cursor, scope, cursors, out var afterTime, out var afterId))
        {
            return EnrollmentDecision<CursorPage<EnrollmentSummary>>.Fail(EnrollmentFailureCodes.InvalidField);
        }

        var page = await enrollments.ListForCohortAsync(
            actor.Organization.OrganizationId,
            activityId,
            cohortId,
            afterTime,
            afterId,
            limit,
            cancellationToken);
        var items = new List<EnrollmentSummary>(page.Items.Count);
        foreach (var enrollment in page.Items)
        {
            items.Add(await ToSummaryAsync(actor, enrollment, cancellationToken));
        }

        return EnrollmentDecision<CursorPage<EnrollmentSummary>>.Ok(
            new CursorPage<EnrollmentSummary>(items, SignNext(scope, page), page.HasMore),
            "enrollment.ok");
    }

    public async Task<EnrollmentDecision<EnrollmentDetail>> GetEnrollmentAsync(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfUnauthorizedAsync(
            actor,
            EnrollmentAuthorizationActions.Read,
            enrollmentId,
            EnrollmentResourceTypes.Enrollment,
            cancellationToken);
        if (denied is not null)
        {
            return EnrollmentDecision<EnrollmentDetail>.Fail(denied);
        }

        var enrollment = await enrollments.FindAsync(
            actor.Organization.OrganizationId,
            enrollmentId,
            null,
            cancellationToken);
        if (enrollment is null
            || enrollment.ActivityId != activityId
            || enrollment.CohortId != cohortId)
        {
            return EnrollmentDecision<EnrollmentDetail>.Fail(EnrollmentFailureCodes.Denied);
        }

        var history = await enrollments.ListHistoryAsync(
            actor.Organization.OrganizationId,
            enrollmentId,
            cancellationToken);
        return EnrollmentDecision<EnrollmentDetail>.Ok(
            new EnrollmentDetail(await ToSummaryAsync(actor, enrollment, cancellationToken), history),
            "enrollment.ok");
    }

    public async Task<EnrollmentDecision<CursorPage<AssignmentSummary>>> ListMyWorkAsync(
        EnrollmentActorContext actor,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfUnauthorizedAsync(
            actor,
            EnrollmentAuthorizationActions.Discover,
            actor.Actor.ActorId,
            EnrollmentResourceTypes.Assignment,
            cancellationToken);
        if (denied is not null)
        {
            return EnrollmentDecision<CursorPage<AssignmentSummary>>.Fail(denied);
        }

        var scope = EnrollmentListCursorScope.ForMyWork(actor);
        if (!IsValidLimit(limit)
            || !EnrollmentListCursor.TryOpen(cursor, scope, cursors, out var afterTime, out var afterId))
        {
            return EnrollmentDecision<CursorPage<AssignmentSummary>>.Fail(EnrollmentFailureCodes.InvalidField);
        }

        var page = await enrollments.ListCurrentForParticipantAsync(
            actor.Organization.OrganizationId,
            actor.Actor.ActorId,
            afterTime,
            afterId,
            limit,
            cancellationToken);
        var items = new List<AssignmentSummary>();
        foreach (var enrollment in page.Items)
        {
            if (enrollment.VisibilityForParticipant() == EnrollmentVisibilityStates.Unavailable)
            {
                continue;
            }

            items.Add(await ToAssignmentAsync(enrollment, cancellationToken));
        }

        return EnrollmentDecision<CursorPage<AssignmentSummary>>.Ok(
            new CursorPage<AssignmentSummary>(
                items,
                SignNext(scope, page),
                page.HasMore),
            "enrollment.ok");
    }

    public async Task<EnrollmentDecision<AssignmentSummary>> GetMyWorkAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        var denied = await DenyIfUnauthorizedAsync(
            actor,
            EnrollmentAuthorizationActions.Discover,
            enrollmentId,
            EnrollmentResourceTypes.Assignment,
            cancellationToken);
        if (denied is not null)
        {
            return EnrollmentDecision<AssignmentSummary>.Fail(denied);
        }

        var enrollment = await enrollments.FindAsync(
            actor.Organization.OrganizationId,
            enrollmentId,
            null,
            cancellationToken);
        if (enrollment is null
            || enrollment.ParticipantActorId != actor.Actor.ActorId
            || enrollment.VisibilityForParticipant() == EnrollmentVisibilityStates.Unavailable)
        {
            return EnrollmentDecision<AssignmentSummary>.Fail(EnrollmentFailureCodes.Denied);
        }

        return EnrollmentDecision<AssignmentSummary>.Ok(
            await ToAssignmentAsync(enrollment, cancellationToken),
            "enrollment.ok");
    }

    private async Task<string?> DenyIfUnauthorizedAsync(
        EnrollmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        CancellationToken cancellationToken)
    {
        var strength = EnrollmentAuthenticationPolicy.Evaluate(actor, action);
        if (strength is not null)
        {
            return strength;
        }

        var decision = await authorization.AuthorizeAdmissionAsync(
            actor,
            action,
            resourceId,
            resourceType,
            cancellationToken);
        return decision.IsPermitted ? null : EnrollmentFailureCodes.Denied;
    }

    private string? SignNext(EnrollmentListCursorScope scope, CursorPage<Enrollment> page) =>
        page.HasMore && page.Items.Count > 0
            ? EnrollmentListCursor.Issue(scope, page.Items[^1].UpdatedAtUtc, page.Items[^1].EnrollmentId, cursors)
            : null;

    private async Task<EnrollmentSummary> ToSummaryAsync(
        EnrollmentActorContext actor,
        Enrollment enrollment,
        CancellationToken cancellationToken)
    {
        var label = await candidates.DisplayLabelAsync(
            enrollment.OrganizationId,
            enrollment.ParticipantActorId,
            cancellationToken) ?? "Participant";
        return new EnrollmentSummary(
            enrollment.EnrollmentId,
            enrollment.ParticipantActorId,
            label,
            enrollment.Status,
            enrollment.Revision,
            enrollment.AssignedAtUtc,
            enrollment.UpdatedAtUtc,
            enrollment.VisibilityForParticipant(),
            EnrollmentProjection.AdministratorActions(
                enrollment.Status,
                actor.GrantedActions.ToHashSet(StringComparer.Ordinal)));
    }

    private async Task<AssignmentSummary> ToAssignmentAsync(
        Enrollment enrollment,
        CancellationToken cancellationToken)
    {
        var binding = await cohorts.FindActivatedAsync(
            enrollment.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            cancellationToken);
        var available = binding is not null && !binding.VerificationDegraded;
        return new AssignmentSummary(
            enrollment.EnrollmentId,
            enrollment.Status,
            enrollment.VisibilityForParticipant(),
            binding?.ActivityTitle,
            available ? binding?.TaskTitle : null,
            available ? binding?.TimeZoneId : null,
            available ? binding?.StartsAtUtc : null,
            available ? binding?.EndsAtUtc : null,
            available ? binding?.DeadlineUtc : null,
            available,
            EnrollmentProjection.ParticipantActions(enrollment.Status, available));
    }

    private static bool IsValidLimit(int limit) =>
        limit is >= 1 and <= EnrollmentPageBounds.MaximumLimit;
}

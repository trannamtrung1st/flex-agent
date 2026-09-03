using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class InMemoryAccommodationStore : IAccommodationStore
{
    private readonly List<Accommodation> _items = [];

    public IReadOnlyList<Accommodation> Items => _items;

    public void Restore(IReadOnlyList<Accommodation> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    public Task<Accommodation?> FindAsync(
        Guid organizationId,
        Guid accommodationId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken) =>
        Task.FromResult(_items.SingleOrDefault(item =>
            item.Parent.OrganizationId == organizationId && item.AccommodationId == accommodationId));

    public Task<IReadOnlyList<Accommodation>> ListForEnrollmentAsync(
        Guid organizationId,
        Guid enrollmentId,
        IEnrollmentTransaction? transaction,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Accommodation>>(
            _items.Where(item => item.Parent.OrganizationId == organizationId && item.Parent.EnrollmentId == enrollmentId)
                .OrderBy(item => item.CreatedAtUtc)
                .ToArray());

    public Task InsertAsync(
        Accommodation accommodation,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = actorId;
        _items.Add(accommodation);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        Accommodation accommodation,
        string? priorStatus,
        Guid actorId,
        IEnrollmentTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = priorStatus;
        _ = actorId;
        var index = _items.FindIndex(item =>
            item.Parent.OrganizationId == accommodation.Parent.OrganizationId
            && item.AccommodationId == accommodation.AccommodationId);
        if (index >= 0)
        {
            _items[index] = accommodation;
        }

        return Task.CompletedTask;
    }
}

public sealed class EnrollmentTimingQueryService(
    IEnrollmentAuthorizationPort authorization,
    IActivatedCohortPort cohorts,
    IEnrollmentStore enrollments,
    IAccommodationStore accommodations,
    IAccommodationPolicyPort policies,
    IEnrollmentClock? clock = null) : IEnrollmentTimingQueryService
{
    private readonly IEnrollmentClock _clock = clock ?? new SystemEnrollmentClock();

    public async Task<EnrollmentDecision<EnrollmentTimingDetail>> GetEnrollmentTimingAsync(
        EnrollmentActorContext actor,
        Guid activityId,
        Guid cohortId,
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        if (EnrollmentAuthenticationPolicy.Evaluate(actor, EnrollmentAuthorizationActions.Read) is not null)
        {
            return EnrollmentDecision<EnrollmentTimingDetail>.Fail(EnrollmentFailureCodes.Denied);
        }

        var allowed = await authorization.AuthorizeAdmissionAsync(
            actor,
            EnrollmentAuthorizationActions.Read,
            enrollmentId,
            EnrollmentResourceTypes.Enrollment,
            cancellationToken);
        if (!allowed.IsPermitted)
        {
            return EnrollmentDecision<EnrollmentTimingDetail>.Fail(EnrollmentFailureCodes.Denied);
        }

        var includeAccommodationMetadata =
            EnrollmentAuthenticationPolicy.Evaluate(actor, EnrollmentAuthorizationActions.ReadAccommodation) is null
            && (await authorization.AuthorizeAdmissionAsync(
                actor,
                EnrollmentAuthorizationActions.ReadAccommodation,
                enrollmentId,
                EnrollmentResourceTypes.Enrollment,
                cancellationToken)).IsPermitted;

        var enrollment = await enrollments.FindAsync(
            actor.Organization.OrganizationId,
            enrollmentId,
            null,
            cancellationToken);
        if (enrollment is null || enrollment.ActivityId != activityId || enrollment.CohortId != cohortId)
        {
            return EnrollmentDecision<EnrollmentTimingDetail>.Fail(EnrollmentFailureCodes.Denied);
        }

        var composed = await ComposeAsync(actor, enrollment, includeAccommodationMetadata, cancellationToken);
        if (!includeAccommodationMetadata)
        {
            composed = composed with
            {
                History = [],
                PermittedAccommodationDimensions = [],
                PermittedReasonCategories = [],
                PolicyAvailable = false,
                Timing = composed.Timing with
                {
                    CurrentAccommodations = [],
                    ParticipantConsequenceCode = AccommodationConsequenceCodes.None,
                },
            };
        }

        return EnrollmentDecision<EnrollmentTimingDetail>.Ok(composed, "enrollment.ok");
    }

    public async Task<EnrollmentDecision<AssignmentTimingSummary>> GetMyWorkTimingAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        if (EnrollmentAuthenticationPolicy.Evaluate(actor, EnrollmentAuthorizationActions.Discover) is not null)
        {
            return EnrollmentDecision<AssignmentTimingSummary>.Fail(EnrollmentFailureCodes.Denied);
        }

        var allowed = await authorization.AuthorizeAdmissionAsync(
            actor,
            EnrollmentAuthorizationActions.Discover,
            enrollmentId,
            EnrollmentResourceTypes.Assignment,
            cancellationToken);
        if (!allowed.IsPermitted)
        {
            return EnrollmentDecision<AssignmentTimingSummary>.Fail(EnrollmentFailureCodes.Denied);
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
            return EnrollmentDecision<AssignmentTimingSummary>.Fail(EnrollmentFailureCodes.Denied);
        }

        var detail = await ComposeAsync(actor, enrollment, includePolicy: false, cancellationToken);
        var binding = await cohorts.FindActivatedAsync(
            enrollment.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            cancellationToken);
        var titlesAvailable = binding is not null && !binding.VerificationDegraded;
        var assignment = new AssignmentSummary(
            enrollment.EnrollmentId,
            enrollment.Status,
            enrollment.VisibilityForParticipant(),
            binding?.ActivityTitle,
            titlesAvailable ? binding?.TaskTitle : null,
            titlesAvailable ? binding?.TimeZoneId : null,
            titlesAvailable ? binding?.StartsAtUtc : null,
            titlesAvailable ? binding?.EndsAtUtc : null,
            titlesAvailable ? binding?.DeadlineUtc : null,
            titlesAvailable,
            EnrollmentProjection.ParticipantActions(enrollment.Status, titlesAvailable));
        return EnrollmentDecision<AssignmentTimingSummary>.Ok(
            new AssignmentTimingSummary(
                assignment,
                detail.Timing,
                detail.Timing.IsAuthoritativeEligibility
                    ? detail.Timing.ParticipantConsequenceCode
                    : AccommodationConsequenceCodes.None),
            "enrollment.ok");
    }

    public async Task<EffectiveTiming?> ComposeAuthoritativeInTransactionAsync(
        Enrollment enrollment,
        IEnrollmentTransaction transaction,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var binding = await cohorts.RevalidateAsync(
            enrollment.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            transaction,
            cancellationToken);
        if (binding is null)
        {
            return null;
        }

        var baseline = TimingMapper.BaselineFrom(binding);
        var policy = await policies.ResolveCurrentAsync(
            enrollment.OrganizationId,
            baseline,
            asOfUtc,
            transaction,
            cancellationToken);
        var records = await accommodations.ListForEnrollmentAsync(
            enrollment.OrganizationId,
            enrollment.EnrollmentId,
            transaction,
            cancellationToken);
        return EffectiveTimingEvaluator.Evaluate(
            baseline,
            enrollment.Status,
            policy,
            records,
            asOfUtc);
    }

    private async Task<EnrollmentTimingDetail> ComposeAsync(
        EnrollmentActorContext actor,
        Enrollment enrollment,
        bool includePolicy,
        CancellationToken cancellationToken)
    {
        var binding = await cohorts.FindActivatedAsync(
            enrollment.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            cancellationToken);
        var history = await accommodations.ListForEnrollmentAsync(
            enrollment.OrganizationId,
            enrollment.EnrollmentId,
            null,
            cancellationToken);
        var baseline = binding is null
            ? new BaselineTiming(
                DateTimeOffset.MinValue,
                DateTimeOffset.MinValue,
                DateTimeOffset.MinValue,
                "UTC",
                1,
                null,
                new AccommodationPolicyIdentity(Guid.Empty, Guid.Empty, new string('0', 64)),
                true)
            : TimingMapper.BaselineFrom(binding);
        var policy = binding is null
            ? null
            : await policies.ResolveCurrentAsync(enrollment.OrganizationId, baseline, _clock.UtcNow, null, cancellationToken);
        var effectivePolicy = baseline.VerificationDegraded
            ? null
            : AccommodationPolicyNormalizer.EffectiveBounds(
                baseline.FrozenPolicy,
                baseline.FrozenPolicySnapshot,
                policy);
        var timing = EffectiveTimingEvaluator.Evaluate(
            baseline,
            enrollment.Status,
            policy,
            history,
            _clock.UtcNow);
        var eligiblePolicy = effectivePolicy is { EnvironmentEligible: true } ? effectivePolicy : null;
        var policyAvailable = eligiblePolicy is not null;
        var dimensions = includePolicy && eligiblePolicy is not null
            ? eligiblePolicy.Dimensions.Where(pair => pair.Value.Enabled).Select(pair => pair.Key).ToArray()
            : [];
        var reasons = includePolicy && eligiblePolicy is not null ? eligiblePolicy.ReasonCategories : [];
        var label = enrollment.ParticipantActorId.ToString("D");
        return new EnrollmentTimingDetail(
            new EnrollmentSummary(
                enrollment.EnrollmentId,
                enrollment.ParticipantActorId,
                label,
                enrollment.Status,
                enrollment.Revision,
                enrollment.AssignedAtUtc,
                enrollment.UpdatedAtUtc,
                enrollment.VisibilityForParticipant(),
                EnrollmentProjection.TimingAdministratorActions(
                    enrollment.Status,
                    actor.GrantedActions.ToHashSet(StringComparer.Ordinal),
                    policyAvailable)),
            baseline,
            timing,
            history,
            dimensions,
            reasons,
            policyAvailable);
    }
}

using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class SubmissionQueryService(
    IEnrollmentAuthorizationPort authorization,
    IEnrollmentStore enrollments,
    IIntakeStore intakes,
    ISubmissionVersionStore versions,
    IFrozenSubmissionRequirementPort frozenRequirements,
    IMaterialPolicyPort materialPolicies,
    IActivatedCohortPort cohorts,
    IArtifactStore? artifacts = null,
    IEnrollmentClock? clock = null,
    IEnrollmentAuditPort? audit = null,
    IEnrollmentUnitOfWork? unitOfWork = null,
    IProtectedArtifactCapabilityStore? capabilities = null) : ISubmissionQueryService
{
    private readonly IEnrollmentClock _clock = clock ?? new SystemEnrollmentClock();

    public async Task<QueryResult<MyWorkSubmissionProjection>> GetMyWorkSubmissionAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        CancellationToken cancellationToken = default)
    {
        if (EnrollmentAuthenticationPolicy.Evaluate(actor, EnrollmentAuthorizationActions.Discover) is not null)
        {
            return new QueryResult<MyWorkSubmissionProjection>(false, null, SubmissionFailureCodes.Unauthorized);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            actor,
            EnrollmentAuthorizationActions.Discover,
            enrollmentId,
            EnrollmentResourceTypes.Assignment,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return new QueryResult<MyWorkSubmissionProjection>(false, null, SubmissionFailureCodes.Unauthorized);
        }

        var enrollment = await enrollments.FindAsync(actor.Organization.OrganizationId, enrollmentId, null, cancellationToken);
        if (enrollment is null || enrollment.ParticipantActorId != actor.Actor.ActorId)
        {
            return new QueryResult<MyWorkSubmissionProjection>(false, null, SubmissionFailureCodes.NotFound);
        }

        var binding = await cohorts.FindActivatedAsync(
            actor.Organization.OrganizationId,
            enrollment.ActivityId,
            enrollment.CohortId,
            cancellationToken);
        var requirements = await ResolvePolicyAsync(actor.Organization.OrganizationId, binding, cancellationToken);
        string? unavailableReason = null;
        if (!string.Equals(enrollment.Status, EnrollmentStates.Active, StringComparison.Ordinal))
        {
            unavailableReason = SubmissionFailureCodes.EnrollmentNotActive;
        }
        else if (requirements is null)
        {
            unavailableReason = SubmissionFailureCodes.PolicyUnavailable;
        }

        var intakeAvailable = unavailableReason is null;
        var activeIntake = await intakes.FindActiveIntakeAsync(
            actor.Organization.OrganizationId,
            enrollmentId,
            null,
            cancellationToken);

        IReadOnlyList<AcceptedVersionSummary> history = [];
        var submissionId = await versions.FindSubmissionIdByEnrollmentAsync(
            actor.Organization.OrganizationId,
            enrollmentId,
            null,
            cancellationToken);
        if (submissionId is Guid resolvedSubmissionId)
        {
            history = await versions.ListVersionsAsync(
                actor.Organization.OrganizationId,
                resolvedSubmissionId,
                null,
                cancellationToken);
        }

        return new QueryResult<MyWorkSubmissionProjection>(
            true,
            new MyWorkSubmissionProjection(
                enrollmentId,
                enrollment.Status,
                intakeAvailable,
                unavailableReason,
                requirements,
                activeIntake is null
                    ? null
                    : new SubmissionIntakeProjection(
                        activeIntake.IntakeId,
                        activeIntake.SubmissionId,
                        activeIntake.Status,
                        activeIntake.Revision,
                        activeIntake.CreatedAtUtc,
                        activeIntake.UpdatedAtUtc,
                        activeIntake.CompleteReceiptAtUtc,
                        activeIntake.Items.Select(item => new SubmissionIntakeItemProjection(
                            item.ItemId,
                            item.Category,
                            item.Filename,
                            item.ByteCount,
                            item.ReceivedAtUtc is null ? "pending" : "received")).ToArray(),
                        requirements,
                        SubmissionLifecycle.PermittedActions(
                            intakeAvailable,
                            activeIntake.Status,
                            history.Count > 0)),
                history,
                SubmissionLifecycle.PermittedActions(
                    intakeAvailable,
                    activeIntake?.Status,
                    history.Count > 0)),
            null);
    }

    public async Task<QueryResult<AcceptedVersionDetail>> GetAcceptedVersionAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        if (EnrollmentAuthenticationPolicy.Evaluate(actor, EnrollmentAuthorizationActions.Discover) is not null)
        {
            return new QueryResult<AcceptedVersionDetail>(false, null, SubmissionFailureCodes.Unauthorized);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            actor,
            EnrollmentAuthorizationActions.Discover,
            enrollmentId,
            EnrollmentResourceTypes.Assignment,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return new QueryResult<AcceptedVersionDetail>(false, null, SubmissionFailureCodes.Unauthorized);
        }

        var enrollment = await enrollments.FindAsync(actor.Organization.OrganizationId, enrollmentId, null, cancellationToken);
        if (enrollment is null || enrollment.ParticipantActorId != actor.Actor.ActorId)
        {
            return new QueryResult<AcceptedVersionDetail>(false, null, SubmissionFailureCodes.NotFound);
        }

        var version = await versions.FindVersionAsync(actor.Organization.OrganizationId, versionId, null, cancellationToken);
        if (version is null || version.Scope.EnrollmentId != enrollmentId)
        {
            return new QueryResult<AcceptedVersionDetail>(false, null, SubmissionFailureCodes.NotFound);
        }

        return new QueryResult<AcceptedVersionDetail>(
            true,
            new AcceptedVersionDetail(
                new AcceptedVersionSummary(version.VersionId, version.VersionNumber, version.AcceptedAtUtc, version.Items.Count),
                version.Items.Select(item => new AcceptedVersionItemProjection(
                    item.ItemId,
                    item.Category,
                    item.Filename,
                    item.ByteCount,
                    true,
                    true)).ToArray(),
                SubmissionLifecycle.PermittedActions(false, IntakeStates.Accepted, true)),
            null);
    }

    public async Task<QueryResult<IntakeMutationOutcome>> GetIntakeAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        Guid intakeId,
        CancellationToken cancellationToken = default)
    {
        if (EnrollmentAuthenticationPolicy.Evaluate(actor, EnrollmentAuthorizationActions.Discover) is not null)
        {
            return new QueryResult<IntakeMutationOutcome>(false, null, SubmissionFailureCodes.Unauthorized);
        }

        var admission = await authorization.AuthorizeAdmissionAsync(
            actor,
            EnrollmentAuthorizationActions.Discover,
            enrollmentId,
            EnrollmentResourceTypes.Assignment,
            cancellationToken);
        if (!admission.IsPermitted)
        {
            return new QueryResult<IntakeMutationOutcome>(false, null, SubmissionFailureCodes.Unauthorized);
        }

        var enrollment = await enrollments.FindAsync(actor.Organization.OrganizationId, enrollmentId, null, cancellationToken);
        if (enrollment is null || enrollment.ParticipantActorId != actor.Actor.ActorId)
        {
            return new QueryResult<IntakeMutationOutcome>(false, null, SubmissionFailureCodes.NotFound);
        }

        var intake = await intakes.FindIntakeAsync(
            actor.Organization.OrganizationId,
            enrollmentId,
            intakeId,
            null,
            cancellationToken);
        if (intake is null)
        {
            return new QueryResult<IntakeMutationOutcome>(false, null, SubmissionFailureCodes.NotFound);
        }

        return new QueryResult<IntakeMutationOutcome>(
            true,
            new IntakeMutationOutcome(
                true,
                intake.Status,
                intake.IntakeId,
                intake.SubmissionId,
                intake.Status,
                intake.Revision,
                null,
                null,
                SubmissionLifecycle.PermittedActions(
                    string.Equals(enrollment.Status, EnrollmentStates.Active, StringComparison.Ordinal),
                    intake.Status,
                    string.Equals(intake.Status, IntakeStates.Accepted, StringComparison.Ordinal))),
            null);
    }

    public async Task<QueryResult<ProtectedItemContent>> GetAcceptedItemPreviewAsync(
        EnrollmentActorContext actor,
        Guid enrollmentId,
        Guid versionId,
        Guid itemId,
        CancellationToken cancellationToken = default,
        string accessKind = SubmissionPermittedActions.PreviewItem)
    {
        var versionResult = await GetAcceptedVersionAsync(actor, enrollmentId, versionId, cancellationToken);
        if (!versionResult.Found || versionResult.Value is null)
        {
            return new QueryResult<ProtectedItemContent>(false, null, versionResult.OutcomeCode);
        }

        var version = await versions.FindVersionAsync(actor.Organization.OrganizationId, versionId, null, cancellationToken);
        var item = version?.Items.FirstOrDefault(candidate => candidate.ItemId == itemId);
        if (version is null || item is null || artifacts is null)
        {
            return new QueryResult<ProtectedItemContent>(false, null, SubmissionFailureCodes.NotFound);
        }

        var now = _clock.UtcNow;
        var capability = new ProtectedArtifactCapability(
            Guid.CreateVersion7(),
            actor.Organization.OrganizationId,
            actor.Actor.ActorId,
            enrollmentId,
            versionId,
            itemId,
            accessKind,
            now.Add(SubmissionLifecycleClocks.ProtectedCapabilityLifetime),
            null);
        if (capabilities is not null)
        {
            capability = await capabilities.IssueAsync(capability, cancellationToken);
        }

        var redeem = ProtectedArtifactCapabilityRules.Redeem(
            capability,
            actor.Organization.OrganizationId,
            actor.Actor.ActorId,
            enrollmentId,
            versionId,
            itemId,
            accessKind,
            now);
        if (redeem is not null)
        {
            return new QueryResult<ProtectedItemContent>(false, null, redeem);
        }

        if (capabilities is not null)
        {
            await capabilities.MarkRedeemedAsync(capability.OrganizationId, capability.CapabilityId, now, cancellationToken);
        }

        if (audit is not null && unitOfWork is not null)
        {
            try
            {
                await unitOfWork.ExecuteAsync(
                    actor,
                    async transaction =>
                    {
                        await audit.WriteRequiredDurableAsync(
                            actor,
                            accessKind,
                            itemId,
                            EnrollmentResourceTypes.Assignment,
                            AuthorizationOutcomes.Permit,
                            null,
                            null,
                            transaction,
                            cancellationToken);
                        if (!transaction.AuditAccepted)
                        {
                            throw new EnrollmentAuditUnavailableException();
                        }

                        return true;
                    },
                    cancellationToken);
            }
            catch (EnrollmentAuditUnavailableException)
            {
                return new QueryResult<ProtectedItemContent>(false, null, SubmissionFailureCodes.AuditUnavailable);
            }
            catch (EnrollmentSessionExpiredException)
            {
                return new QueryResult<ProtectedItemContent>(false, null, SubmissionFailureCodes.Unauthorized);
            }
        }
        else if (audit is not null)
        {
            try
            {
                await audit.WriteRequiredDurableAsync(
                    actor,
                    accessKind,
                    itemId,
                    EnrollmentResourceTypes.Assignment,
                    AuthorizationOutcomes.Permit,
                    null,
                    null,
                    new PreviewAuditTransaction(),
                    cancellationToken);
            }
            catch (EnrollmentAuditUnavailableException)
            {
                return new QueryResult<ProtectedItemContent>(false, null, SubmissionFailureCodes.AuditUnavailable);
            }
        }
        else
        {
            return new QueryResult<ProtectedItemContent>(false, null, SubmissionFailureCodes.AuditUnavailable);
        }

        var gotten = await artifacts.GetExactVersionAsync(
            new ArtifactGetRequest(
                actor.Organization.OrganizationId,
                new StoredArtifactReference(
                    new ArtifactObjectKey(item.ArtifactObjectKey),
                    new ArtifactVersionId(item.ArtifactVersionId),
                    ArtifactDigest.FromHex(item.ContentDigest),
                    item.ByteCount)),
            cancellationToken);
        if (!gotten.Succeeded)
        {
            return new QueryResult<ProtectedItemContent>(false, null, SubmissionFailureCodes.NotFound);
        }

        var text = System.Text.Encoding.UTF8.GetString(gotten.Content.Span);
        var contentType = item.Category == MaterialCategories.MarkdownAttachment ? "text/markdown" : "text/plain";
        return new QueryResult<ProtectedItemContent>(
            true,
            new ProtectedItemContent(versionId, itemId, item.Category, item.Filename, contentType, text),
            null);
    }

    private sealed class PreviewAuditTransaction : IEnrollmentTransaction
    {
        public bool AuditAccepted { get; set; } = true;

        public bool OutboxAccepted { get; set; } = true;

        public bool AbortRequested { get; private set; }

        public object CommitHandle { get; } = new();

        public void AbortCommit() => AbortRequested = true;
    }

    private async Task<NormalizedMaterialPolicy?> ResolvePolicyAsync(
        Guid organizationId,
        ActivatedCohortBinding? binding,
        CancellationToken cancellationToken)
    {
        if (binding is null)
        {
            return null;
        }

        var frozen = await frozenRequirements.ResolveFrozenAsync(
            organizationId,
            binding.ActivityId,
            binding.CohortId,
            binding.TaskSourceId,
            binding.TaskVersionId,
            binding.TaskContentDigest,
            null,
            cancellationToken);
        var organization = await materialPolicies.ResolveCurrentAsync(
            organizationId,
            new PolicySourceRef(binding.FrozenPolicySourceId, binding.FrozenPolicyVersionId, binding.FrozenPolicyDigest),
            _clock.UtcNow,
            null,
            cancellationToken);
        return MaterialPolicyResolver.Intersect(frozen, organization);
    }
}

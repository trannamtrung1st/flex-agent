using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public sealed class AssessmentDraftHandler(
    IAssessmentAuthorizationPort authorization,
    IAssessmentSourceCatalog sourceCatalog,
    IAssessmentDraftStore draftStore,
    IAssessmentActivationUnitOfWork unitOfWork) : IAssessmentDraftHandler
{
    public async Task<AssessmentDecision<ActivityDraft>> CreateAsync(
        CreateAssessmentDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var strength = AssessmentAuthenticationPolicy.Evaluate(
            command.Actor,
            AssessmentAuthorizationActions.CreateActivity);
        if (strength is not null)
        {
            return AssessmentDecision<ActivityDraft>.Fail(strength);
        }

        var authorized = await authorization.AuthorizeAdmissionAsync(
            command.Actor,
            AssessmentAuthorizationActions.CreateActivity,
            Guid.Empty,
            AssessmentResourceTypes.Activity,
            cancellationToken);
        if (!authorized.IsPermitted)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.Denied);
        }

        return await unitOfWork.ExecuteAsync(async transaction =>
        {
            var created = ActivityDraft.Create(
            command.Actor.Organization.OrganizationId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            command.Title,
            command.Task,
            command.Timing,
            command.OrganizationPolicy,
            command.Agent,
            command.Harness,
            command.Workflow,
            command.AdaptiveFollowUp,
            command.Rubric,
            command.ModelDeployment,
            command.Knowledge,
            command.CapabilityProfile,
            command.ReviewRelease);
            if (!created.Succeeded || created.Value is null)
            {
                return created;
            }

            var sourceRejection = await ValidateSelectionsAsync(
                command.Actor,
                command.Environment,
                created.Value,
                transaction,
                cancellationToken);
            if (sourceRejection is not null)
            {
                return AssessmentDecision<ActivityDraft>.Fail(sourceRejection);
            }

            var createAuth = await authorization.ReauthorizeAsync(
                command.Actor,
                AssessmentAuthorizationActions.CreateActivity,
                created.Value.ActivityId,
                AssessmentResourceTypes.Activity,
                transaction,
                cancellationToken);
            var selectAuth = await authorization.ReauthorizeAsync(
                command.Actor,
                AssessmentAuthorizationActions.SelectSources,
                created.Value.ActivityId,
                AssessmentResourceTypes.Activity,
                transaction,
                cancellationToken);
            if (!createAuth.IsPermitted || !selectAuth.IsPermitted)
            {
                return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.Denied);
            }

            var cohort = AssessmentCohort.CreateEmpty(
                created.Value.OrganizationId,
                created.Value.ActivityId,
                Guid.CreateVersion7(),
                created.Value.RevisionId,
                created.Value.RevisionNumber);
            if (!cohort.Succeeded || cohort.Value is null)
            {
                return AssessmentDecision<ActivityDraft>.Fail(cohort.OutcomeCode);
            }

            await draftStore.AddAsync(created.Value, cohort.Value, transaction, cancellationToken);
            return created;
        }, cancellationToken);
    }

    public async Task<AssessmentDecision<ActivityDraft>> SaveAsync(
        SaveAssessmentDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var strength = AssessmentAuthenticationPolicy.Evaluate(
            command.Actor,
            AssessmentAuthorizationActions.SaveActivity);
        if (strength is not null)
        {
            return AssessmentDecision<ActivityDraft>.Fail(strength);
        }

        var authorized = await authorization.AuthorizeAdmissionAsync(
            command.Actor,
            AssessmentAuthorizationActions.SaveActivity,
            command.ActivityId,
            AssessmentResourceTypes.Activity,
            cancellationToken);
        if (!authorized.IsPermitted)
        {
            return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.Denied);
        }

        return await unitOfWork.ExecuteAsync(async transaction =>
        {
            var current = await draftStore.GetDraftAsync(
                command.Actor.Organization.OrganizationId,
                command.ActivityId,
                transaction,
                cancellationToken);
            if (current is null)
            {
                return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.Denied);
            }

            var saved = current.Save(command.ExpectedRevisionNumber, command.Content);
            if (!saved.Succeeded || saved.Value is null)
            {
                return saved;
            }

            var sourceRejection = await ValidateSelectionsAsync(
                command.Actor,
                command.Environment,
                saved.Value,
                transaction,
                cancellationToken);
            if (sourceRejection is not null)
            {
                return AssessmentDecision<ActivityDraft>.Fail(sourceRejection);
            }

            var saveAuth = await authorization.ReauthorizeAsync(
                command.Actor,
                AssessmentAuthorizationActions.SaveActivity,
                command.ActivityId,
                AssessmentResourceTypes.Activity,
                transaction,
                cancellationToken);
            var selectAuth = await authorization.ReauthorizeAsync(
                command.Actor,
                AssessmentAuthorizationActions.SelectSources,
                command.ActivityId,
                AssessmentResourceTypes.Activity,
                transaction,
                cancellationToken);
            if (!saveAuth.IsPermitted || !selectAuth.IsPermitted)
            {
                return AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.Denied);
            }

            var persisted = await draftStore.UpdateDraftAsync(saved.Value, transaction, cancellationToken);
            return persisted
                ? saved
                : AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.StaleRevision);
        }, cancellationToken);
    }

    public async Task<AssessmentDecision<ReadinessResult>> CheckReadinessAsync(
        CheckReadinessQuery query,
        CancellationToken cancellationToken = default)
    {
        var strength = AssessmentAuthenticationPolicy.Evaluate(
            query.Actor,
            AssessmentAuthorizationActions.CheckReadiness);
        if (strength is not null)
        {
            return AssessmentDecision<ReadinessResult>.Fail(strength);
        }

        var authorized = await authorization.AuthorizeAdmissionAsync(
            query.Actor,
            AssessmentAuthorizationActions.CheckReadiness,
            query.ActivityId,
            AssessmentResourceTypes.Activity,
            cancellationToken);
        if (!authorized.IsPermitted)
        {
            return AssessmentDecision<ReadinessResult>.Fail(AssessmentFailureCodes.Denied);
        }

        var draft = await draftStore.GetDraftAsync(
            query.Actor.Organization.OrganizationId,
            query.ActivityId,
            cancellationToken);
        if (draft is null)
        {
            return AssessmentDecision<ReadinessResult>.Fail(AssessmentFailureCodes.Denied);
        }

        var sources = await sourceCatalog.LoadExactAsync(
            draft.OrganizationId,
            CollectReferences(draft),
            cancellationToken);
        var result = ReadinessEvaluator.Evaluate(
            new ReadinessContext(draft, sources, AuditAvailable: true, query.Environment));
        return AssessmentDecision<ReadinessResult>.Ok(result);
    }

    public async Task<AssessmentDecision<IReadOnlyList<TrustedSourceDescriptor>>> ListSourceOptionsAsync(
        AssessmentActorContext actor,
        string environment,
        CancellationToken cancellationToken = default)
    {
        var strength = AssessmentAuthenticationPolicy.Evaluate(
            actor,
            AssessmentAuthorizationActions.SelectSources);
        if (strength is not null)
        {
            return AssessmentDecision<IReadOnlyList<TrustedSourceDescriptor>>.Fail(strength);
        }

        var authorized = await authorization.AuthorizeAdmissionAsync(
            actor,
            AssessmentAuthorizationActions.SelectSources,
            Guid.Empty,
            AssessmentResourceTypes.Activity,
            cancellationToken);
        if (!authorized.IsPermitted)
        {
            return AssessmentDecision<IReadOnlyList<TrustedSourceDescriptor>>.Fail(AssessmentFailureCodes.Denied);
        }

        var sources = await sourceCatalog.ListSelectableAsync(actor.Organization.OrganizationId, environment, cancellationToken);
        return AssessmentDecision<IReadOnlyList<TrustedSourceDescriptor>>.Ok(sources);
    }

    public async Task<AssessmentDecision<IReadOnlyList<ActivityDraft>>> ListActivitiesAsync(
        AssessmentActorContext actor,
        CancellationToken cancellationToken = default)
    {
        var denied = await AuthorizeReadAsync(actor, Guid.Empty, cancellationToken);
        if (denied is not null)
        {
            return AssessmentDecision<IReadOnlyList<ActivityDraft>>.Fail(denied);
        }

        var drafts = await draftStore.ListDraftsAsync(actor.Organization.OrganizationId, cancellationToken);
        return AssessmentDecision<IReadOnlyList<ActivityDraft>>.Ok(drafts);
    }

    public async Task<AssessmentDecision<ActivityDraft>> GetActivityAsync(
        AssessmentActorContext actor,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        var denied = await AuthorizeReadAsync(actor, activityId, cancellationToken);
        if (denied is not null)
        {
            return AssessmentDecision<ActivityDraft>.Fail(denied);
        }

        var draft = await draftStore.GetDraftAsync(actor.Organization.OrganizationId, activityId, cancellationToken);
        return draft is null
            ? AssessmentDecision<ActivityDraft>.Fail(AssessmentFailureCodes.Denied)
            : AssessmentDecision<ActivityDraft>.Ok(draft);
    }

    private async Task<string?> AuthorizeReadAsync(
        AssessmentActorContext actor,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        var strength = AssessmentAuthenticationPolicy.Evaluate(
            actor,
            AssessmentAuthorizationActions.ReadActivity);
        if (strength is not null)
        {
            return strength;
        }

        var authorized = await authorization.AuthorizeAdmissionAsync(
            actor,
            AssessmentAuthorizationActions.ReadActivity,
            activityId,
            AssessmentResourceTypes.Activity,
            cancellationToken);
        return authorized.IsPermitted ? null : AssessmentFailureCodes.Denied;
    }

    private async Task<string?> ValidateSelectionsAsync(
        AssessmentActorContext actor,
        string environment,
        ActivityDraft draft,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        var selectable = await sourceCatalog.LoadSelectableExactAsync(
            actor.Organization.OrganizationId,
            CollectReferences(draft),
            environment,
            transaction,
            cancellationToken);
        return AssessmentSourceSelection.Validate(draft, selectable);
    }

    internal static IReadOnlyList<ExactSourceRef> CollectReferences(ActivityDraft draft)
    {
        var references = new List<ExactSourceRef>
        {
            draft.Content.OrganizationPolicy,
            draft.Content.Agent,
            draft.Content.Harness,
            draft.Content.Workflow,
            draft.Content.AdaptiveFollowUp,
            draft.Content.Rubric,
            draft.Content.ModelDeployment,
            draft.Content.CapabilityProfile,
            draft.Content.ReviewRelease,
            draft.Content.Task.RequirementSource,
        };
        references.AddRange(draft.Content.Knowledge);
        if (draft.Content.Memory.Snapshot is { } snapshot)
        {
            references.Add(snapshot);
        }

        if (draft.Content.ApprovedException is { } exception)
        {
            references.Add(exception);
        }

        return references;
    }
}

public interface IAssessmentDraftStore
{
    Task AddAsync(
        ActivityDraft draft,
        AssessmentCohort cohort,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken);

    Task<ActivityDraft?> GetDraftAsync(
        Guid organizationId,
        Guid activityId,
        CancellationToken cancellationToken);

    Task<ActivityDraft?> GetDraftAsync(
        Guid organizationId,
        Guid activityId,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken);

    Task<bool> UpdateDraftAsync(
        ActivityDraft draft,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken);

    Task<bool> MarkActivatedAsync(
        Guid organizationId,
        Guid activityId,
        Guid expectedRevisionId,
        long expectedRevisionNumber,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken);

    Task<AssessmentCohort?> GetCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        CancellationToken cancellationToken);

    Task<AssessmentCohort?> GetCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ActivityDraft>> ListDraftsAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<AssessmentCohort?> FindCohortForActivityAsync(Guid organizationId, Guid activityId, CancellationToken cancellationToken);

    Task UpdateCohortAsync(
        AssessmentCohort cohort,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken);
}

public static class AssessmentAuthenticationPolicy
{
    private static readonly HashSet<string> AdministratorActions =
    [
        AssessmentAuthorizationActions.CreateActivity,
        AssessmentAuthorizationActions.SaveActivity,
        AssessmentAuthorizationActions.CheckReadiness,
        AssessmentAuthorizationActions.ActivateCohort,
        AssessmentAuthorizationActions.SelectSources,
    ];

    private static readonly HashSet<string> PrivilegedReadActions =
    [
        AssessmentAuthorizationActions.ReadActivity,
        AssessmentAuthorizationActions.ReconcileActivation,
        AssessmentAuthorizationActions.ReadBaseline,
        AssessmentAuthorizationActions.ReadBaselineProvenance,
    ];

    public static string? Evaluate(AssessmentActorContext actor, string action)
    {
        if (AdministratorActions.Contains(action)
            && !string.Equals(
                actor.Relationship,
                AuthenticationStrengthEvaluator.AdministratorRelationship,
                StringComparison.Ordinal))
        {
            return AssessmentFailureCodes.Denied;
        }

        if (PrivilegedReadActions.Contains(action)
            && !string.Equals(
                actor.Relationship,
                AuthenticationStrengthEvaluator.AdministratorRelationship,
                StringComparison.Ordinal)
            && !string.Equals(
                actor.Relationship,
                AuthenticationStrengthEvaluator.ReviewerRelationship,
                StringComparison.Ordinal))
        {
            return AssessmentFailureCodes.Denied;
        }

        return AuthenticationStrengthEvaluator.Evaluate(
            actor.Strength,
            actor.Relationship,
            action,
            AllowedAcr,
            AllowedAmr);
    }

    private static readonly HashSet<string> AllowedAcr = ["http://schemas.openid.net/pape/policies/2007/06/multi-factor", "mfa"];
    private static readonly HashSet<string> AllowedAmr = ["mfa", "otp", "hwk", "pwd mfa"];
}

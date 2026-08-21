using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public sealed class AssessmentDraftHandler(
    IAssessmentAuthorizationPort authorization,
    IAssessmentSourceCatalog sourceCatalog,
    IAssessmentDraftStore draftStore) : IAssessmentDraftHandler
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

        await draftStore.AddAsync(created.Value, cohort.Value, transaction: null, cancellationToken);
        return created;
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

        var current = await draftStore.GetDraftAsync(
            command.Actor.Organization.OrganizationId,
            command.ActivityId,
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

        await draftStore.UpdateDraftAsync(saved.Value, transaction: null, cancellationToken);
        return saved;
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

    Task<ActivityDraft?> GetDraftAsync(Guid organizationId, Guid activityId, CancellationToken cancellationToken);

    Task UpdateDraftAsync(
        ActivityDraft draft,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken);

    Task<AssessmentCohort?> GetCohortAsync(Guid organizationId, Guid activityId, Guid cohortId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ActivityDraft>> ListDraftsAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<AssessmentCohort?> FindCohortForActivityAsync(Guid organizationId, Guid activityId, CancellationToken cancellationToken);

    Task UpdateCohortAsync(
        AssessmentCohort cohort,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken);
}

public static class AssessmentAuthenticationPolicy
{
    public static string? Evaluate(AssessmentActorContext actor, string action) =>
        AuthenticationStrengthEvaluator.Evaluate(
            actor.Strength,
            actor.Relationship,
            action,
            AllowedAcr,
            AllowedAmr);

    private static readonly HashSet<string> AllowedAcr = ["http://schemas.openid.net/pape/policies/2007/06/multi-factor", "mfa"];
    private static readonly HashSet<string> AllowedAmr = ["mfa", "otp", "hwk", "pwd mfa"];
}

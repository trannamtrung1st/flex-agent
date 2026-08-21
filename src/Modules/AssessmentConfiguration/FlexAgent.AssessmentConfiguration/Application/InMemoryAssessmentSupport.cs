using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public sealed class InMemoryAssessmentDraftStore : IAssessmentDraftStore
{
    private readonly Dictionary<(Guid OrganizationId, Guid ActivityId), ActivityDraft> _drafts = new();
    private readonly Dictionary<(Guid OrganizationId, Guid ActivityId, Guid CohortId), AssessmentCohort> _cohorts = new();

    public Task AddAsync(
        ActivityDraft draft,
        AssessmentCohort cohort,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        _drafts[(draft.OrganizationId, draft.ActivityId)] = draft;
        _cohorts[(cohort.OrganizationId, cohort.ActivityId, cohort.CohortId)] = cohort;
        return Task.CompletedTask;
    }

    public Task<ActivityDraft?> GetDraftAsync(Guid organizationId, Guid activityId, CancellationToken cancellationToken) =>
        GetDraftAsync(organizationId, activityId, transaction: null, cancellationToken);

    public Task<ActivityDraft?> GetDraftAsync(
        Guid organizationId,
        Guid activityId,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        _ = transaction;
        _drafts.TryGetValue((organizationId, activityId), out var draft);
        return Task.FromResult(draft);
    }

    public Task UpdateDraftAsync(
        ActivityDraft draft,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        _ = transaction;
        _drafts[(draft.OrganizationId, draft.ActivityId)] = draft;
        LastWriteWasActivationMetadata = false;
        return Task.CompletedTask;
    }

    public bool LastWriteWasActivationMetadata { get; private set; }

    public Task<bool> MarkActivatedAsync(
        Guid organizationId,
        Guid activityId,
        Guid expectedRevisionId,
        long expectedRevisionNumber,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = transaction;
        if (!_drafts.TryGetValue((organizationId, activityId), out var draft)
            || draft.RevisionId != expectedRevisionId
            || draft.RevisionNumber != expectedRevisionNumber)
        {
            return Task.FromResult(false);
        }

        _drafts[(organizationId, activityId)] = draft with { HasActivatedCohort = true };
        LastWriteWasActivationMetadata = true;
        return Task.FromResult(true);
    }

    public Task<AssessmentCohort?> GetCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        CancellationToken cancellationToken) =>
        GetCohortAsync(organizationId, activityId, cohortId, transaction: null, cancellationToken);

    public Task<AssessmentCohort?> GetCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        _ = transaction;
        _cohorts.TryGetValue((organizationId, activityId, cohortId), out var cohort);
        return Task.FromResult(cohort);
    }

    public Task<IReadOnlyList<ActivityDraft>> ListDraftsAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ActivityDraft>>(
            _drafts.Values.Where(draft => draft.OrganizationId == organizationId).ToArray());

    public Task<AssessmentCohort?> FindCohortForActivityAsync(
        Guid organizationId,
        Guid activityId,
        CancellationToken cancellationToken) =>
        Task.FromResult(
            _cohorts.Values.SingleOrDefault(cohort =>
                cohort.OrganizationId == organizationId && cohort.ActivityId == activityId));

    public Task UpdateCohortAsync(
        AssessmentCohort cohort,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        _cohorts[(cohort.OrganizationId, cohort.ActivityId, cohort.CohortId)] = cohort;
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<AssessmentCohort> Cohorts => _cohorts.Values;
}

public sealed class InMemoryAssessmentSourceCatalog : IAssessmentSourceCatalog, IAssessmentSourceTransactionPort, IAssessmentDevelopmentSourceSeeder
{
    private readonly List<TrustedSourceDescriptor> _sources;

    public InMemoryAssessmentSourceCatalog()
        : this([])
    {
    }

    public InMemoryAssessmentSourceCatalog(IReadOnlyList<TrustedSourceDescriptor> sources)
    {
        _sources = [..sources];
    }

    public void EnsureOrganization(Guid organizationId)
    {
        if (_sources.Any(source => source.OrganizationId == organizationId))
        {
            return;
        }

        _sources.AddRange(AssessmentDevelopmentSources.ForOrganization(organizationId));
    }

    public Task<IReadOnlyList<TrustedSourceDescriptor>> LoadExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TrustedSourceDescriptor>>(
            _sources.Where(source => source.OrganizationId == organizationId && references.Any(source.Matches)).ToArray());

    public Task<IReadOnlyList<TrustedSourceDescriptor>> RevalidateExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        LoadExactAsync(organizationId, references, cancellationToken);
}

public sealed class InMemoryAssessmentAuthorizationPort(bool permit = true) : IAssessmentAuthorizationPort
{
    public bool Permit { get; set; } = permit;

    public Task<AuthorizationDecision> AuthorizeAdmissionAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        CancellationToken cancellationToken) =>
        Task.FromResult(Permit
            ? AuthorizationDecision.Permit(1)
            : AuthorizationDecision.Deny(AuthorizationReasonCodes.DeniedNoGrant));

    public Task<AuthorizationDecision> ReauthorizeAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        AuthorizeAdmissionAsync(actor, action, resourceId, resourceType, cancellationToken);
}

public sealed class InMemoryAssessmentTransaction : IAssessmentActivationTransaction
{
    public bool AuditAccepted { get; set; } = true;

    public bool OutboxAccepted { get; set; } = true;

    public object? PersistenceContext => null;
}

public sealed class InMemoryAssessmentBaselineStore : IAssessmentBaselineStore
{
    public int InsertCount { get; private set; }

    public Task InsertAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        Guid baselineId,
        ActivationBaselineDocument document,
        string contentDigest,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = (organizationId, activityId, cohortId, baselineId, document, contentDigest, transaction, cancellationToken);
        InsertCount++;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryAssessmentUnitOfWork : IAssessmentActivationUnitOfWork
{
    public InMemoryAssessmentTransaction Transaction { get; } = new();

    public Task<T> ExecuteAsync<T>(
        Func<IAssessmentActivationTransaction, Task<T>> action,
        CancellationToken cancellationToken) =>
        action(Transaction);
}

public sealed class InMemoryAssessmentAttemptStore : IAssessmentActivationAttemptStore
{
    private readonly Dictionary<(Guid OrganizationId, Guid ActivityId, Guid CohortId, string Key), AssessmentActivationAttempt> _attempts = new();

    public Task<AssessmentActivationAttempt?> FindAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = transaction;
        _attempts.TryGetValue((organizationId, activityId, cohortId, idempotencyKey), out var attempt);
        return Task.FromResult(attempt);
    }

    public Task InsertAsync(
        AssessmentActivationAttempt attempt,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = transaction;
        _attempts[(attempt.OrganizationId, attempt.ActivityId, attempt.CohortId, attempt.IdempotencyKey)] = attempt;
        return Task.CompletedTask;
    }
}

public sealed class EmptyAssessmentRelationshipResolver : IAssessmentRelationshipResolver
{
    public Task<AssessmentActorAuthorization> ResolveAsync(
        Guid actorId,
        Guid organizationId,
        CancellationToken cancellationToken) =>
        Task.FromResult(new AssessmentActorAuthorization(string.Empty, []));
}

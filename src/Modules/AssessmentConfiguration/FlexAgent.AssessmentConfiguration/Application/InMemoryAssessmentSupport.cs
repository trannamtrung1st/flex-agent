using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public sealed class InMemoryAssessmentDraftStore : IAssessmentDraftStore
{
    private readonly Dictionary<(Guid OrganizationId, Guid ActivityId), ActivityDraft> _drafts = new();
    private readonly Dictionary<(Guid OrganizationId, Guid ActivityId, Guid CohortId), AssessmentCohort> _cohorts = new();
    private readonly List<AssessmentRevisionProvenance> _provenance = [];

    public IReadOnlyList<AssessmentRevisionProvenance> Provenance => _provenance;

    public Task AddAsync(
        ActivityDraft draft,
        AssessmentCohort cohort,
        IAssessmentActivationTransaction? transaction,
        AssessmentRevisionProvenance provenance,
        CancellationToken cancellationToken)
    {
        _ = (transaction, cancellationToken);
        _drafts[(draft.OrganizationId, draft.ActivityId)] = draft;
        _cohorts[(cohort.OrganizationId, cohort.ActivityId, cohort.CohortId)] = cohort;
        _provenance.Add(provenance);
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

    public Task<bool> UpdateDraftAsync(
        ActivityDraft draft,
        IAssessmentActivationTransaction? transaction,
        AssessmentRevisionProvenance provenance,
        CancellationToken cancellationToken)
    {
        _ = (transaction, cancellationToken);
        if (!_drafts.TryGetValue((draft.OrganizationId, draft.ActivityId), out var current)
            || current.RevisionNumber != draft.RevisionNumber - 1
            || current.HasActivatedCohort)
        {
            return Task.FromResult(false);
        }

        _drafts[(draft.OrganizationId, draft.ActivityId)] = draft;
        _provenance.Add(provenance);
        LastWriteWasActivationMetadata = false;
        return Task.FromResult(true);
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

    public Task<IReadOnlyList<TrustedSourceDescriptor>> ListSelectableAsync(
        Guid organizationId,
        string environment,
        CancellationToken cancellationToken) =>
        FilterSelectableAsync(organizationId, environment);

    public Task<IReadOnlyList<TrustedSourceDescriptor>> LoadSelectableExactAsync(
        Guid organizationId,
        IReadOnlyList<ExactSourceRef> references,
        string environment,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = (transaction, cancellationToken);
        return Task.FromResult<IReadOnlyList<TrustedSourceDescriptor>>(
            Selectable(organizationId, environment)
                .Where(source => references.Any(source.Matches))
                .ToArray());
    }

    private Task<IReadOnlyList<TrustedSourceDescriptor>> FilterSelectableAsync(
        Guid organizationId,
        string environment) =>
        Task.FromResult<IReadOnlyList<TrustedSourceDescriptor>>(Selectable(organizationId, environment).ToArray());

    private IEnumerable<TrustedSourceDescriptor> Selectable(Guid organizationId, string environment) =>
        _sources.Where(source =>
            source.OrganizationId == organizationId
            && source.LifecycleState == SourceLifecycleStates.Available
            && source.TransactionallyRevalidatable
            && (environment != DeploymentEnvironments.Production || source.ProductionEligible));

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

    public HashSet<string> DeniedActions { get; } = [];

    public HashSet<string> DeniedOnReauthorize { get; } = [];

    public Task<AuthorizationDecision> AuthorizeAdmissionAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        CancellationToken cancellationToken) =>
        Task.FromResult(Permit && !DeniedActions.Contains(action)
            ? AuthorizationDecision.Permit(1)
            : AuthorizationDecision.Deny(AuthorizationReasonCodes.DeniedNoGrant));

    public Task<AuthorizationDecision> ReauthorizeAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = transaction;
        return DeniedOnReauthorize.Contains(action)
            ? Task.FromResult(AuthorizationDecision.Deny(AuthorizationReasonCodes.DeniedNoGrant))
            : AuthorizeAdmissionAsync(actor, action, resourceId, resourceType, cancellationToken);
    }
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
        AssessmentActorContext actor,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        _ = (organizationId, activityId, cohortId, baselineId, document, contentDigest, transaction, actor, occurredAtUtc, cancellationToken);
        InsertCount++;
        LastActorId = actor.Actor.ActorId;
        LastCorrelationId = actor.CorrelationId;
        return Task.CompletedTask;
    }

    public Guid? LastActorId { get; private set; }

    public Guid? LastCorrelationId { get; private set; }
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
    private readonly List<AssessmentActivationAttempt> _attempts = [];
    private readonly Dictionary<(Guid OrganizationId, Guid ActivityId, Guid CohortId, string Key), string> _bindings = [];

    public IReadOnlyList<AssessmentActivationAttempt> Items => _attempts;

    public Task AcquireIdempotencyLockAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = (organizationId, activityId, cohortId, idempotencyKey, transaction, cancellationToken);
        return Task.CompletedTask;
    }

    public Task<AssessmentActivationAttempt?> FindAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        Task.FromResult(Matching(organizationId, activityId, cohortId, idempotencyKey).LastOrDefault());

    public Task<AssessmentActivationAttempt?> FindSuccessfulAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken) =>
        Task.FromResult(Matching(organizationId, activityId, cohortId, idempotencyKey)
            .LastOrDefault(attempt => string.Equals(attempt.OutcomeCode, "assessment.activated", StringComparison.Ordinal)));

    public Task InsertAsync(
        AssessmentActivationAttempt attempt,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = transaction;
        if (string.Equals(attempt.OutcomeCode, "assessment.activated", StringComparison.Ordinal)
            && Matching(attempt.OrganizationId, attempt.ActivityId, attempt.CohortId, attempt.IdempotencyKey)
                .Any(item => string.Equals(item.OutcomeCode, "assessment.activated", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Assessment activation attempt key exists.");
        }

        _attempts.Add(attempt);
        return Task.CompletedTask;
    }

    public Task InsertRequestAuditAsync(
        AssessmentActorContext actor,
        string action,
        Guid resourceId,
        string resourceType,
        string outcome,
        string? reasonCode,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = (transaction, cancellationToken);
        RequestAudits.Add((actor, action, resourceId, resourceType, outcome, reasonCode));
        return Task.CompletedTask;
    }

    public List<(AssessmentActorContext Actor, string Action, Guid ResourceId, string ResourceType, string Outcome, string? ReasonCode)> RequestAudits { get; } = [];

    public Task<string> BindCommandDigestAsync(
        Guid organizationId,
        Guid activityId,
        Guid requestedCohortId,
        string idempotencyKey,
        string commandDigest,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        _ = (transaction, cancellationToken);
        var key = (organizationId, activityId, requestedCohortId, idempotencyKey);
        if (!_bindings.TryGetValue(key, out var bound))
        {
            _bindings[key] = commandDigest;
            return Task.FromResult(commandDigest);
        }

        return Task.FromResult(bound);
    }

    private IEnumerable<AssessmentActivationAttempt> Matching(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        string idempotencyKey) =>
        _attempts.Where(attempt =>
            attempt.OrganizationId == organizationId
            && attempt.ActivityId == activityId
            && attempt.CohortId == cohortId
            && string.Equals(attempt.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));
}

public sealed class EmptyAssessmentRelationshipResolver : IAssessmentRelationshipResolver
{
    public Task<AssessmentActorAuthorization> ResolveAsync(
        Guid actorId,
        Guid organizationId,
        CancellationToken cancellationToken) =>
        Task.FromResult(new AssessmentActorAuthorization(string.Empty, []));
}

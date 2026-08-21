using System.Text.Json;
using Dapper;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using Npgsql;

namespace FlexAgent.AssessmentConfiguration.Infrastructure;

public sealed class PostgresAssessmentDraftStore(
    PostgresConnectionAccessor connections,
    IAuditEventWriter auditEventWriter) : IAssessmentDraftStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(
        ActivityDraft draft,
        AssessmentCohort cohort,
        IAssessmentActivationTransaction? transaction,
        AssessmentRevisionProvenance provenance,
        CancellationToken cancellationToken)
    {
        if (transaction?.PersistenceContext is NpgsqlTransaction existing)
        {
            await InsertActivityAsync(draft, existing, cancellationToken);
            await InsertRevisionAsync(draft, provenance, existing, cancellationToken);
            await InsertCohortAsync(cohort, existing, cancellationToken);
            await WriteMutationAuditAsync(draft, provenance, existing, cancellationToken);
            return;
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connections, cancellationToken);
        await InsertActivityAsync(draft, scope.Transaction, cancellationToken);
        await InsertRevisionAsync(draft, provenance, scope.Transaction, cancellationToken);
        await InsertCohortAsync(cohort, scope.Transaction, cancellationToken);
        await WriteMutationAuditAsync(draft, provenance, scope.Transaction, cancellationToken);
        await scope.CommitAsync(cancellationToken);
    }

    public Task<ActivityDraft?> GetDraftAsync(Guid organizationId, Guid activityId, CancellationToken cancellationToken) =>
        GetDraftAsync(organizationId, activityId, transaction: null, cancellationToken);

    public async Task<ActivityDraft?> GetDraftAsync(
        Guid organizationId,
        Guid activityId,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string activitySql = """
            SELECT organization_id, activity_id, form, configured_type, current_revision_id,
                   current_revision_number, has_activated_cohort
            FROM assessment_activities
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
            """;
        const string lockedActivitySql = activitySql + " FOR UPDATE";
        const string revisionSql = """
            SELECT revision_id, revision_number, title, content
            FROM assessment_activity_revisions
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId AND revision_id = @RevisionId
            """;

        if (transaction?.PersistenceContext is NpgsqlTransaction existing)
        {
            var locked = await existing.Connection!.QuerySingleOrDefaultAsync<ActivityRow>(
                new CommandDefinition(
                    lockedActivitySql,
                    new { OrganizationId = organizationId, ActivityId = activityId },
                    existing,
                    cancellationToken: cancellationToken));
            if (locked is null)
            {
                return null;
            }

            var lockedRevision = await existing.Connection!.QuerySingleAsync<RevisionRow>(
                new CommandDefinition(
                    revisionSql,
                    new { OrganizationId = organizationId, ActivityId = activityId, RevisionId = locked.CurrentRevisionId },
                    existing,
                    cancellationToken: cancellationToken));
            return ToDraft(locked, lockedRevision);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var activity = await connection.QuerySingleOrDefaultAsync<ActivityRow>(
            new CommandDefinition(
                activitySql,
                new { OrganizationId = organizationId, ActivityId = activityId },
                cancellationToken: cancellationToken));
        if (activity is null)
        {
            return null;
        }

        var revision = await connection.QuerySingleAsync<RevisionRow>(
            new CommandDefinition(
                revisionSql,
                new { OrganizationId = organizationId, ActivityId = activityId, RevisionId = activity.CurrentRevisionId },
                cancellationToken: cancellationToken));
        return ToDraft(activity, revision);
    }

    public async Task<bool> UpdateDraftAsync(
        ActivityDraft draft,
        IAssessmentActivationTransaction? transaction,
        AssessmentRevisionProvenance provenance,
        CancellationToken cancellationToken)
    {
        try
        {
            if (transaction?.PersistenceContext is NpgsqlTransaction existing)
            {
                await InsertRevisionAsync(draft, provenance, existing, cancellationToken);
                var existingUpdated = await UpdateActivityHeadAsync(draft, existing, cancellationToken);
                if (existingUpdated == 1)
                {
                    await RetargetDraftCohortAsync(draft, existing, cancellationToken);
                    await WriteMutationAuditAsync(draft, provenance, existing, cancellationToken);
                }

                return existingUpdated == 1;
            }

            await using var scope = await PostgresTransactionScope.BeginAsync(connections, cancellationToken);
            await InsertRevisionAsync(draft, provenance, scope.Transaction, cancellationToken);
            var updated = await UpdateActivityHeadAsync(draft, scope.Transaction, cancellationToken);
            if (updated != 1)
            {
                await scope.RollbackAsync(cancellationToken);
                return false;
            }

            await RetargetDraftCohortAsync(draft, scope.Transaction, cancellationToken);
            await WriteMutationAuditAsync(draft, provenance, scope.Transaction, cancellationToken);
            await scope.CommitAsync(cancellationToken);
            return true;
        }
        catch (PostgresException exception) when (exception.SqlState is "23505")
        {
            return false;
        }
    }

    private static Task<int> UpdateActivityHeadAsync(
        ActivityDraft draft,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE assessment_activities
                SET current_revision_id = @RevisionId,
                    current_revision_number = @RevisionNumber,
                    updated_at = CLOCK_TIMESTAMP()
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND current_revision_number = @PreviousRevisionNumber
                  AND has_activated_cohort = FALSE
                """,
                new
                {
                    draft.OrganizationId,
                    draft.ActivityId,
                    RevisionId = draft.RevisionId,
                    RevisionNumber = draft.RevisionNumber,
                    PreviousRevisionNumber = draft.RevisionNumber - 1,
                },
                transaction,
                cancellationToken: cancellationToken));

    private static Task RetargetDraftCohortAsync(
        ActivityDraft draft,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE assessment_cohorts
                SET bound_revision_id = @RevisionId,
                    bound_revision_number = @RevisionNumber,
                    updated_at = CLOCK_TIMESTAMP()
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND state = 'draft'
                """,
                new
                {
                    draft.OrganizationId,
                    draft.ActivityId,
                    RevisionId = draft.RevisionId,
                    RevisionNumber = draft.RevisionNumber,
                },
                transaction,
                cancellationToken: cancellationToken));

    public async Task<bool> MarkActivatedAsync(
        Guid organizationId,
        Guid activityId,
        Guid expectedRevisionId,
        long expectedRevisionNumber,
        IAssessmentActivationTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (transaction.PersistenceContext is not NpgsqlTransaction existing)
        {
            throw new InvalidOperationException("Assessment activation metadata requires the PostgreSQL activation transaction.");
        }

        var updated = await existing.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE assessment_activities
                SET has_activated_cohort = TRUE,
                    updated_at = CLOCK_TIMESTAMP()
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND current_revision_id = @ExpectedRevisionId
                  AND current_revision_number = @ExpectedRevisionNumber
                """,
                new
                {
                    OrganizationId = organizationId,
                    ActivityId = activityId,
                    ExpectedRevisionId = expectedRevisionId,
                    ExpectedRevisionNumber = expectedRevisionNumber,
                },
                existing,
                cancellationToken: cancellationToken));
        return updated == 1;
    }

    public Task<AssessmentCohort?> GetCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        CancellationToken cancellationToken) =>
        GetCohortAsync(organizationId, activityId, cohortId, transaction: null, cancellationToken);

    public async Task<AssessmentCohort?> GetCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.organization_id, c.activity_id, c.cohort_id, c.state, c.bound_revision_id,
                   c.bound_revision_number, b.baseline_id, bl.content_digest
            FROM assessment_cohorts c
            LEFT JOIN assessment_cohort_baseline_bindings b
                ON b.organization_id = c.organization_id
               AND b.activity_id = c.activity_id
               AND b.cohort_id = c.cohort_id
            LEFT JOIN assessment_activation_baselines bl
                ON bl.organization_id = b.organization_id
               AND bl.activity_id = b.activity_id
               AND bl.baseline_id = b.baseline_id
            WHERE c.organization_id = @OrganizationId
              AND c.activity_id = @ActivityId
              AND c.cohort_id = @CohortId
            """;
        var lockedSql = sql + " FOR UPDATE OF c";

        CohortRow? row;
        if (transaction?.PersistenceContext is NpgsqlTransaction existing)
        {
            row = await existing.Connection!.QuerySingleOrDefaultAsync<CohortRow>(
                new CommandDefinition(
                    lockedSql,
                    new { OrganizationId = organizationId, ActivityId = activityId, CohortId = cohortId },
                    existing,
                    cancellationToken: cancellationToken));
        }
        else
        {
            await using var connection = await connections.OpenConnectionAsync(cancellationToken);
            row = await connection.QuerySingleOrDefaultAsync<CohortRow>(
                new CommandDefinition(
                    sql,
                    new { OrganizationId = organizationId, ActivityId = activityId, CohortId = cohortId },
                    cancellationToken: cancellationToken));
        }

        return row is null
            ? null
            : new AssessmentCohort(
                row.OrganizationId,
                row.ActivityId,
                row.CohortId,
                row.State,
                row.BoundRevisionId,
                row.BoundRevisionNumber,
                row.BaselineId,
                row.ContentDigest);
    }

    public async Task<IReadOnlyList<ActivityDraft>> ListDraftsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var activities = await connection.QueryAsync<ActivityRow>(
            """
            SELECT organization_id, activity_id, form, configured_type, current_revision_id,
                   current_revision_number, has_activated_cohort
            FROM assessment_activities
            WHERE organization_id = @OrganizationId
            ORDER BY updated_at DESC
            """,
            new { OrganizationId = organizationId });

        var drafts = new List<ActivityDraft>();
        foreach (var activity in activities)
        {
            var revision = await connection.QuerySingleAsync<RevisionRow>(
                """
                SELECT revision_id, revision_number, title, content
                FROM assessment_activity_revisions
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND revision_id = @RevisionId
                """,
                new
                {
                    OrganizationId = organizationId,
                    ActivityId = activity.ActivityId,
                    RevisionId = activity.CurrentRevisionId,
                });
            drafts.Add(ToDraft(activity, revision));
        }

        return drafts;
    }

    public async Task<AssessmentCohort?> FindCohortForActivityAsync(
        Guid organizationId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<CohortRow>(
            """
            SELECT c.organization_id, c.activity_id, c.cohort_id, c.state, c.bound_revision_id,
                   c.bound_revision_number, b.baseline_id, bl.content_digest
            FROM assessment_cohorts c
            LEFT JOIN assessment_cohort_baseline_bindings b
                ON b.organization_id = c.organization_id
               AND b.activity_id = c.activity_id
               AND b.cohort_id = c.cohort_id
            LEFT JOIN assessment_activation_baselines bl
                ON bl.organization_id = b.organization_id
               AND bl.activity_id = b.activity_id
               AND bl.baseline_id = b.baseline_id
            WHERE c.organization_id = @OrganizationId
              AND c.activity_id = @ActivityId
            ORDER BY c.created_at
            LIMIT 1
            """,
            new { OrganizationId = organizationId, ActivityId = activityId });
        return row is null
            ? null
            : new AssessmentCohort(
                row.OrganizationId,
                row.ActivityId,
                row.CohortId,
                row.State,
                row.BoundRevisionId,
                row.BoundRevisionNumber,
                row.BaselineId,
                row.ContentDigest);
    }

    public async Task UpdateCohortAsync(
        AssessmentCohort cohort,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction?.PersistenceContext is NpgsqlTransaction existing)
        {
            await UpdateCohortRowAsync(cohort, existing, cancellationToken);
            return;
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connections, cancellationToken);
        await UpdateCohortRowAsync(cohort, scope.Transaction, cancellationToken);
        await scope.CommitAsync(cancellationToken);
    }

    private static Task UpdateCohortRowAsync(
        AssessmentCohort cohort,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE assessment_cohorts
                SET state = @State,
                    bound_revision_id = @BoundRevisionId,
                    bound_revision_number = @BoundRevisionNumber,
                    updated_at = CLOCK_TIMESTAMP()
                WHERE organization_id = @OrganizationId
                  AND activity_id = @ActivityId
                  AND cohort_id = @CohortId
                """,
                new
                {
                    cohort.OrganizationId,
                    cohort.ActivityId,
                    cohort.CohortId,
                    cohort.State,
                    cohort.BoundRevisionId,
                    cohort.BoundRevisionNumber,
                },
                transaction,
                cancellationToken: cancellationToken));

    private static async Task InsertActivityAsync(
        ActivityDraft draft,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO assessment_activities (
                    organization_id, activity_id, form, configured_type, current_revision_id,
                    current_revision_number, has_activated_cohort, created_at, updated_at)
                VALUES (
                    @OrganizationId, @ActivityId, @Form, @ConfiguredType, @RevisionId,
                    @RevisionNumber, @HasActivatedCohort, CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP())
                """,
                new
                {
                    draft.OrganizationId,
                    draft.ActivityId,
                    draft.Form,
                    draft.ConfiguredType,
                    RevisionId = draft.RevisionId,
                    RevisionNumber = draft.RevisionNumber,
                    draft.HasActivatedCohort,
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private static async Task InsertRevisionAsync(
        ActivityDraft draft,
        AssessmentRevisionProvenance provenance,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO assessment_activity_revisions (
                    organization_id, activity_id, revision_id, revision_number, title, content, created_at,
                    previous_revision_id, actor_id, actor_type, correlation_id, change_category, saved_at)
                VALUES (
                    @OrganizationId, @ActivityId, @RevisionId, @RevisionNumber, @Title, @Content::jsonb, CLOCK_TIMESTAMP(),
                    @PreviousRevisionId, @ActorId, @ActorType, @CorrelationId, @ChangeCategory, CLOCK_TIMESTAMP())
                """,
                new
                {
                    draft.OrganizationId,
                    draft.ActivityId,
                    RevisionId = draft.RevisionId,
                    RevisionNumber = draft.RevisionNumber,
                    draft.Content.Title,
                    Content = JsonSerializer.Serialize(draft.Content, JsonOptions),
                    provenance.PreviousRevisionId,
                    ActorId = provenance.Actor.Actor.ActorId,
                    ActorType = provenance.Actor.Actor.ActorType,
                    CorrelationId = provenance.Actor.CorrelationId,
                    provenance.ChangeCategory,
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private async Task WriteMutationAuditAsync(
        ActivityDraft draft,
        AssessmentRevisionProvenance provenance,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var action = string.Equals(provenance.ChangeCategory, AssessmentRevisionChangeCategories.Created, StringComparison.Ordinal)
            ? AssessmentAuthorizationActions.CreateActivity
            : AssessmentAuthorizationActions.SaveActivity;
        await WriteDecisionAuditAsync(
            draft,
            provenance,
            action,
            provenance.MutationAuthorization,
            transaction,
            cancellationToken);
        if (provenance.AuditSourceSelection && provenance.SourceAuthorization is { } sourceAuthorization)
        {
            await WriteDecisionAuditAsync(
                draft,
                provenance,
                AssessmentAuthorizationActions.SelectSources,
                sourceAuthorization,
                transaction,
                cancellationToken);
        }
    }

    private Task WriteDecisionAuditAsync(
        ActivityDraft draft,
        AssessmentRevisionProvenance provenance,
        string action,
        AuthorizationDecision decision,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        auditEventWriter.InsertAsync(
            new AuditEventWriteModel(
                Guid.CreateVersion7(),
                draft.OrganizationId,
                "v1",
                DateTimeOffset.UtcNow,
                provenance.Actor.CorrelationId,
                provenance.Actor.Actor.ActorType,
                provenance.Actor.Actor.ActorId,
                action,
                AssessmentResourceTypes.Activity,
                draft.ActivityId,
                "permit",
                null,
                decision.RelationshipVersion,
                provenance.Actor.SourceChannel,
                PayloadDigest: null,
                decision.AuthorizationReferenceType,
                decision.AuthorizationReferenceId),
            transaction,
            cancellationToken);

    private static async Task InsertCohortAsync(
        AssessmentCohort cohort,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO assessment_cohorts (
                    organization_id, activity_id, cohort_id, state, bound_revision_id,
                    bound_revision_number, created_at, updated_at)
                VALUES (
                    @OrganizationId, @ActivityId, @CohortId, @State, @BoundRevisionId,
                    @BoundRevisionNumber, CLOCK_TIMESTAMP(), CLOCK_TIMESTAMP())
                """,
                new
                {
                    cohort.OrganizationId,
                    cohort.ActivityId,
                    cohort.CohortId,
                    cohort.State,
                    cohort.BoundRevisionId,
                    cohort.BoundRevisionNumber,
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private static ActivityDraft ToDraft(ActivityRow activity, RevisionRow revision)
    {
        var content = JsonSerializer.Deserialize<AssessmentDraftContent>(revision.Content, JsonOptions)
            ?? throw new InvalidOperationException("assessment revision content is unreadable.");
        return new ActivityDraft(
            activity.OrganizationId,
            activity.ActivityId,
            revision.RevisionId,
            revision.RevisionNumber,
            activity.Form,
            activity.ConfiguredType,
            content,
            activity.HasActivatedCohort);
    }

    private sealed record ActivityRow(
        Guid OrganizationId,
        Guid ActivityId,
        string Form,
        string ConfiguredType,
        Guid CurrentRevisionId,
        long CurrentRevisionNumber,
        bool HasActivatedCohort);

    private sealed record RevisionRow(Guid RevisionId, long RevisionNumber, string Title, string Content);

    private sealed record CohortRow(
        Guid OrganizationId,
        Guid ActivityId,
        Guid CohortId,
        string State,
        Guid BoundRevisionId,
        long BoundRevisionNumber,
        Guid? BaselineId,
        string? ContentDigest);
}

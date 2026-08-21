using System.Text.Json;
using Dapper;
using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.Postgres;
using Npgsql;

namespace FlexAgent.AssessmentConfiguration.Infrastructure;

public sealed class PostgresAssessmentDraftStore(PostgresConnectionAccessor connections) : IAssessmentDraftStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(
        ActivityDraft draft,
        AssessmentCohort cohort,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction?.PersistenceContext is NpgsqlTransaction existing)
        {
            await InsertActivityAsync(draft, existing, cancellationToken);
            await InsertRevisionAsync(draft, existing, cancellationToken);
            await InsertCohortAsync(cohort, existing, cancellationToken);
            return;
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connections, cancellationToken);
        await InsertActivityAsync(draft, scope.Transaction, cancellationToken);
        await InsertRevisionAsync(draft, scope.Transaction, cancellationToken);
        await InsertCohortAsync(cohort, scope.Transaction, cancellationToken);
        await scope.CommitAsync(cancellationToken);
    }

    public async Task<ActivityDraft?> GetDraftAsync(Guid organizationId, Guid activityId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var activity = await connection.QuerySingleOrDefaultAsync<ActivityRow>(
            """
            SELECT organization_id, activity_id, form, configured_type, current_revision_id,
                   current_revision_number, has_activated_cohort
            FROM assessment_activities
            WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
            """,
            new { OrganizationId = organizationId, ActivityId = activityId });
        if (activity is null)
        {
            return null;
        }

        var revision = await connection.QuerySingleAsync<RevisionRow>(
            """
            SELECT revision_id, revision_number, title, content
            FROM assessment_activity_revisions
            WHERE organization_id = @OrganizationId AND revision_id = @RevisionId
            """,
            new { OrganizationId = organizationId, RevisionId = activity.CurrentRevisionId });

        return ToDraft(activity, revision);
    }

    public async Task UpdateDraftAsync(
        ActivityDraft draft,
        IAssessmentActivationTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction?.PersistenceContext is NpgsqlTransaction existing)
        {
            await InsertRevisionAsync(draft, existing, cancellationToken);
            await UpdateActivityHeadAsync(draft, existing, cancellationToken);
            return;
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connections, cancellationToken);
        await InsertRevisionAsync(draft, scope.Transaction, cancellationToken);
        await UpdateActivityHeadAsync(draft, scope.Transaction, cancellationToken);
        await scope.CommitAsync(cancellationToken);
    }

    private static Task UpdateActivityHeadAsync(
        ActivityDraft draft,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken) =>
        transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE assessment_activities
                SET current_revision_id = @RevisionId,
                    current_revision_number = @RevisionNumber,
                    has_activated_cohort = @HasActivatedCohort,
                    updated_at = CLOCK_TIMESTAMP()
                WHERE organization_id = @OrganizationId AND activity_id = @ActivityId
                """,
                new
                {
                    draft.OrganizationId,
                    draft.ActivityId,
                    RevisionId = draft.RevisionId,
                    RevisionNumber = draft.RevisionNumber,
                    draft.HasActivatedCohort,
                },
                transaction,
                cancellationToken: cancellationToken));

    public async Task<AssessmentCohort?> GetCohortAsync(
        Guid organizationId,
        Guid activityId,
        Guid cohortId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<CohortRow>(
            """
            SELECT c.organization_id, c.activity_id, c.cohort_id, c.state, c.bound_revision_id,
                   c.bound_revision_number, b.baseline_id, bl.content_digest
            FROM assessment_cohorts c
            LEFT JOIN assessment_cohort_baseline_bindings b
                ON b.organization_id = c.organization_id AND b.cohort_id = c.cohort_id
            LEFT JOIN assessment_activation_baselines bl
                ON bl.organization_id = b.organization_id AND bl.baseline_id = b.baseline_id
            WHERE c.organization_id = @OrganizationId
              AND c.activity_id = @ActivityId
              AND c.cohort_id = @CohortId
            """,
            new { OrganizationId = organizationId, ActivityId = activityId, CohortId = cohortId });
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
                WHERE organization_id = @OrganizationId AND revision_id = @RevisionId
                """,
                new { OrganizationId = organizationId, RevisionId = activity.CurrentRevisionId });
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
                ON b.organization_id = c.organization_id AND b.cohort_id = c.cohort_id
            LEFT JOIN assessment_activation_baselines bl
                ON bl.organization_id = b.organization_id AND bl.baseline_id = b.baseline_id
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
                WHERE organization_id = @OrganizationId AND cohort_id = @CohortId
                """,
                new
                {
                    cohort.OrganizationId,
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
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await transaction.Connection!.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO assessment_activity_revisions (
                    organization_id, activity_id, revision_id, revision_number, title, content, created_at)
                VALUES (
                    @OrganizationId, @ActivityId, @RevisionId, @RevisionNumber, @Title, @Content::jsonb, CLOCK_TIMESTAMP())
                """,
                new
                {
                    draft.OrganizationId,
                    draft.ActivityId,
                    RevisionId = draft.RevisionId,
                    RevisionNumber = draft.RevisionNumber,
                    draft.Content.Title,
                    Content = JsonSerializer.Serialize(draft.Content, JsonOptions),
                },
                transaction,
                cancellationToken: cancellationToken));
    }

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

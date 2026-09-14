using System.Globalization;
using System.Text;
using System.Text.Json;
using Dapper;
using FlexAgent.Contracts.Evidence;
using FlexAgent.Contracts.Review;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Application.Review;
using FlexAgent.Evaluation.Domain;
using FlexAgent.Postgres;

namespace FlexAgent.Evaluation.Infrastructure.Review;

public sealed class PostgresAssignedReviewQueryService(
    PostgresConnectionAccessor connections,
    IActiveReviewAssignmentPort assignments,
    IAssignedReviewProtectedEvidenceResolver evidenceResolver) : IAssignedReviewQueryService
{
    public async Task<EvaluationDecision<AssignedReviewWorkListPage>> ListWorkAsync(
        AssignedReviewActorContext actor,
        AssignedReviewWorkListRequest request,
        CancellationToken cancellationToken)
    {
        if (!AssignedReviewAdmission.CanListWork(actor))
        {
            return EvaluationDecision<AssignedReviewWorkListPage>.Fail(ReviewFailureCodes.Denied);
        }

        var limit = AssignedReviewAdmission.NormalizeWorkLimit(request.Limit);
        if (!TryDecodeCursor(request.Cursor, out var cursorUpdatedAt, out var cursorReviewCaseId))
        {
            return EvaluationDecision<AssignedReviewWorkListPage>.Fail(ReviewFailureCodes.InvalidField);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<CaseRow>(
            new CommandDefinition(
                """
                SELECT
                    review_case.review_case_id,
                    review_case.case_state,
                    review_case.candidate_state,
                    review_case.updated_at,
                    review_case.current_candidate_evaluation_id,
                    review_case.activity_id,
                    review_case.participant_id,
                    evaluation.aggregate_status,
                    evaluation.completed_at,
                    request.state AS request_state
                FROM review_case_assignments AS assignment
                INNER JOIN review_cases AS review_case
                  ON review_case.organization_id = assignment.organization_id
                 AND review_case.review_case_id = assignment.review_case_id
                LEFT JOIN evaluations AS evaluation
                  ON evaluation.organization_id = review_case.organization_id
                 AND evaluation.evaluation_id = review_case.current_candidate_evaluation_id
                LEFT JOIN LATERAL (
                    SELECT request.state
                    FROM evaluation_requests AS request
                    WHERE request.organization_id = review_case.organization_id
                      AND request.activity_id = review_case.activity_id
                      AND request.participant_id = review_case.participant_id
                      AND request.attempt_id = review_case.attempt_id
                      AND request.session_id = review_case.session_id
                    ORDER BY request.created_at DESC, request.request_id DESC
                    LIMIT 1
                ) AS request ON TRUE
                WHERE assignment.organization_id = @OrganizationId
                  AND assignment.reviewer_actor_id = @ActorId
                  AND assignment.assignment_state = 'assigned'
                  AND assignment.revoked_at IS NULL
                  AND (
                        @CursorUpdatedAt IS NULL
                        OR review_case.updated_at < @CursorUpdatedAt
                        OR (
                            review_case.updated_at = @CursorUpdatedAt
                            AND review_case.review_case_id < @CursorReviewCaseId))
                ORDER BY review_case.updated_at DESC, review_case.review_case_id DESC
                LIMIT @FetchLimit;
                """,
                new
                {
                    OrganizationId = actor.OrganizationId,
                    ActorId = actor.ActorId,
                    CursorUpdatedAt = cursorUpdatedAt,
                    CursorReviewCaseId = cursorReviewCaseId,
                    FetchLimit = limit + 1,
                },
                cancellationToken: cancellationToken))).AsList();

        var hasMore = rows.Count > limit;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var labels = await LoadLabelsAsync(connection, actor.OrganizationId, rows, cancellationToken);
        var items = rows
            .Select(row => AssignedReviewProjectionMapper.MapWorkItem(MapSnapshot(row, labels)))
            .ToArray();
        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = EncodeCursor(last.updated_at, last.review_case_id);
        }

        return EvaluationDecision<AssignedReviewWorkListPage>.Ok(
            new AssignedReviewWorkListPage(items, nextCursor, hasMore));
    }

    public async Task<EvaluationDecision<ReviewCaseReadV1>> GetCaseAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        CancellationToken cancellationToken)
    {
        if (!AssignedReviewAdmission.CanReadCase(actor)
            || !await assignments.HasActiveAssignmentAsync(actor, reviewCaseId, cancellationToken))
        {
            return EvaluationDecision<ReviewCaseReadV1>.Fail(ReviewFailureCodes.Denied);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var row = await LoadCaseRowAsync(connection, actor, reviewCaseId, cancellationToken);
        if (row is null)
        {
            return EvaluationDecision<ReviewCaseReadV1>.Fail(ReviewFailureCodes.Denied);
        }

        var labels = await LoadLabelsAsync(
            connection,
            actor.OrganizationId,
            [row],
            cancellationToken);
        var snapshot = MapSnapshot(row, labels);
        var criteria = await LoadCriterionSummariesAsync(
            connection,
            actor.OrganizationId,
            snapshot.EvaluationId,
            cancellationToken);
        return EvaluationDecision<ReviewCaseReadV1>.Ok(
            AssignedReviewProjectionMapper.MapCase(snapshot, criteria));
    }

    public async Task<EvaluationDecision<ReviewCriterionReadV1>> GetCriterionAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        string criterionId,
        CancellationToken cancellationToken)
    {
        if (!AssignedReviewAdmission.CanReadCriterion(actor)
            || string.IsNullOrWhiteSpace(criterionId)
            || !await assignments.HasActiveContentCapabilityAsync(actor, reviewCaseId, cancellationToken))
        {
            return EvaluationDecision<ReviewCriterionReadV1>.Fail(ReviewFailureCodes.Denied);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var row = await LoadCaseRowAsync(connection, actor, reviewCaseId, cancellationToken);
        if (row?.current_candidate_evaluation_id is not Guid evaluationId)
        {
            return EvaluationDecision<ReviewCriterionReadV1>.Fail(ReviewFailureCodes.Denied);
        }

        var criterion = await connection.QuerySingleOrDefaultAsync<CriterionRow>(
            new CommandDefinition(
                """
                SELECT
                    judgment_id,
                    criterion_id,
                    criterion_version,
                    evaluator_mode,
                    status,
                    confidence,
                    uncertainty_json::text AS uncertainty_json,
                    rationale,
                    score_json::text AS score_json,
                    provisional_feedback
                FROM evaluation_criterion_judgments
                WHERE organization_id = @OrganizationId
                  AND evaluation_id = @EvaluationId
                  AND criterion_id = @CriterionId;
                """,
                new
                {
                    OrganizationId = actor.OrganizationId,
                    EvaluationId = evaluationId,
                    CriterionId = criterionId,
                },
                cancellationToken: cancellationToken));
        if (criterion is null)
        {
            return EvaluationDecision<ReviewCriterionReadV1>.Fail(ReviewFailureCodes.Denied);
        }

        var evidenceRows = await connection.QueryAsync<EvidenceRow>(
            new CommandDefinition(
                """
                SELECT
                    evidence.evidence_id,
                    evidence.source_type,
                    evidence.precision,
                    evidence.integrity_state
                FROM evaluation_criterion_judgment_evidence_refs AS evidence_ref
                INNER JOIN evaluation_evidence_items AS evidence
                  ON evidence.organization_id = evidence_ref.organization_id
                 AND evidence.evaluation_id = evidence_ref.evaluation_id
                 AND evidence.evidence_id = evidence_ref.evidence_id
                WHERE evidence_ref.organization_id = @OrganizationId
                  AND evidence_ref.evaluation_id = @EvaluationId
                  AND evidence_ref.judgment_id = @JudgmentId
                ORDER BY evidence_ref.reference_ordinal, evidence.evidence_id
                LIMIT 32;
                """,
                new
                {
                    OrganizationId = actor.OrganizationId,
                    EvaluationId = evaluationId,
                    JudgmentId = criterion.judgment_id,
                },
                cancellationToken: cancellationToken));
        var evidenceReferences = evidenceRows
            .Select(item => AssignedReviewProjectionMapper.MapEvidenceReference(
                new AssignedReviewEvidenceSnapshot(
                    item.evidence_id,
                    item.source_type,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    item.precision,
                    item.integrity_state,
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty,
                    Guid.Empty)))
            .ToArray();

        return EvaluationDecision<ReviewCriterionReadV1>.Ok(
            AssignedReviewProjectionMapper.MapCriterion(
                reviewCaseId,
                evaluationId,
                MapCriterion(criterion),
                evidenceReferences));
    }

    public async Task<EvaluationDecision<ReviewEvidenceOpenV1>> OpenEvidenceAsync(
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        string evidenceId,
        CancellationToken cancellationToken)
    {
        if (!AssignedReviewAdmission.CanOpenEvidence(actor)
            || !TryParseStableEvidenceId(evidenceId, out var evidenceGuid)
            || !await assignments.HasActiveContentCapabilityAsync(actor, reviewCaseId, cancellationToken))
        {
            return EvaluationDecision<ReviewEvidenceOpenV1>.Fail(ReviewFailureCodes.Denied);
        }

        await using var connection = await connections.OpenConnectionAsync(cancellationToken);
        var row = await LoadCaseRowAsync(connection, actor, reviewCaseId, cancellationToken);
        if (row?.current_candidate_evaluation_id is not Guid evaluationId)
        {
            return EvaluationDecision<ReviewEvidenceOpenV1>.Fail(ReviewFailureCodes.Denied);
        }

        var evidence = await connection.QuerySingleOrDefaultAsync<EvidenceDetailRow>(
            new CommandDefinition(
                """
                SELECT
                    evidence.evidence_id,
                    evidence.request_id,
                    evidence.source_type,
                    evidence.source_id::text AS source_id,
                    evidence.source_version_id::text AS source_version_id,
                    evidence.source_content_digest,
                    evidence.locator_schema,
                    evidence.locator_digest,
                    evidence.locator_canonical_json::text AS locator_canonical_json,
                    evidence.precision,
                    evidence.integrity_state,
                    evidence.activity_id,
                    evidence.participant_id,
                    evidence.attempt_id,
                    evidence.session_id
                FROM evaluation_evidence_items AS evidence
                INNER JOIN review_cases AS review_case
                  ON review_case.organization_id = evidence.organization_id
                 AND review_case.activity_id = evidence.activity_id
                 AND review_case.participant_id = evidence.participant_id
                 AND review_case.attempt_id = evidence.attempt_id
                 AND review_case.session_id = evidence.session_id
                WHERE evidence.organization_id = @OrganizationId
                  AND evidence.evaluation_id = @EvaluationId
                  AND evidence.evidence_id = @EvidenceId
                  AND review_case.review_case_id = @ReviewCaseId;
                """,
                new
                {
                    OrganizationId = actor.OrganizationId,
                    EvaluationId = evaluationId,
                    EvidenceId = evidenceGuid,
                    ReviewCaseId = reviewCaseId,
                },
                cancellationToken: cancellationToken));
        if (evidence is null)
        {
            return EvaluationDecision<ReviewEvidenceOpenV1>.Fail(ReviewFailureCodes.Denied);
        }

        var resolved = await evidenceResolver.TryResolveAsync(
            new AssignedReviewProtectedEvidenceRequest(
                actor.OrganizationId,
                evaluationId,
                evidence.request_id,
                evidence.activity_id,
                evidence.participant_id,
                evidence.attempt_id,
                evidence.session_id,
                evidenceGuid,
                evidence.locator_canonical_json ?? string.Empty,
                evidence.locator_digest,
                evidence.integrity_state),
            cancellationToken);
        if (!resolved.Succeeded || resolved.Value is null)
        {
            return EvaluationDecision<ReviewEvidenceOpenV1>.Fail(resolved.OutcomeCode);
        }

        return EvaluationDecision<ReviewEvidenceOpenV1>.Ok(
            AssignedReviewProjectionMapper.MapEvidenceOpen(
                reviewCaseId,
                evaluationId,
                evidenceGuid,
                resolved.Value.Locator,
                resolved.Value.Availability,
                resolved.Value.DisplayText,
                resolved.Value.UnavailabilityNotice));
    }

    private static async Task<CaseRow?> LoadCaseRowAsync(
        Npgsql.NpgsqlConnection connection,
        AssignedReviewActorContext actor,
        Guid reviewCaseId,
        CancellationToken cancellationToken) =>
        await connection.QuerySingleOrDefaultAsync<CaseRow>(
            new CommandDefinition(
                """
                SELECT
                    review_case.review_case_id,
                    review_case.case_state,
                    review_case.candidate_state,
                    review_case.updated_at,
                    review_case.current_candidate_evaluation_id,
                    review_case.activity_id,
                    review_case.participant_id,
                    evaluation.aggregate_status,
                    evaluation.completed_at,
                    request.state AS request_state
                FROM review_case_assignments AS assignment
                INNER JOIN review_cases AS review_case
                  ON review_case.organization_id = assignment.organization_id
                 AND review_case.review_case_id = assignment.review_case_id
                LEFT JOIN evaluations AS evaluation
                  ON evaluation.organization_id = review_case.organization_id
                 AND evaluation.evaluation_id = review_case.current_candidate_evaluation_id
                LEFT JOIN LATERAL (
                    SELECT request.state
                    FROM evaluation_requests AS request
                    WHERE request.organization_id = review_case.organization_id
                      AND request.activity_id = review_case.activity_id
                      AND request.participant_id = review_case.participant_id
                      AND request.attempt_id = review_case.attempt_id
                      AND request.session_id = review_case.session_id
                    ORDER BY request.created_at DESC, request.request_id DESC
                    LIMIT 1
                ) AS request ON TRUE
                WHERE assignment.organization_id = @OrganizationId
                  AND assignment.review_case_id = @ReviewCaseId
                  AND assignment.reviewer_actor_id = @ActorId
                  AND assignment.assignment_state = 'assigned'
                  AND assignment.revoked_at IS NULL;
                """,
                new
                {
                    OrganizationId = actor.OrganizationId,
                    ReviewCaseId = reviewCaseId,
                    ActorId = actor.ActorId,
                },
                cancellationToken: cancellationToken));

    private static AssignedReviewCaseSnapshot MapSnapshot(
        CaseRow row,
        LabelLookup labels)
    {
        var hasEvaluation = row.current_candidate_evaluation_id is not null;
        var processingState = AssignedReviewProjectionMapper.MapEvaluationProcessingState(
            row.request_state,
            row.aggregate_status,
            hasEvaluation);
        var integrityState = AssignedReviewProjectionMapper.MapIntegrityState(
            row.case_state,
            row.candidate_state);
        labels.TryGetValue(row.activity_id, out var campaignLabel);
        labels.TryGetParticipant(row.participant_id, out var participantLabel);
        return new AssignedReviewCaseSnapshot(
            row.review_case_id,
            row.case_state,
            row.candidate_state,
            row.updated_at,
            AssignedReviewProjectionMapper.DefaultTimeZoneId,
            row.current_candidate_evaluation_id,
            row.completed_at,
            processingState,
            "assigned",
            integrityState,
            AssignedReviewProjectionMapper.MapNextAction(processingState),
            campaignLabel,
            null,
            participantLabel,
            null,
            hasEvaluation ? null : "Evaluation is not yet available for inspection.");
    }

    private static async Task<IReadOnlyList<ReviewCriterionSummaryV1>> LoadCriterionSummariesAsync(
        Npgsql.NpgsqlConnection connection,
        Guid organizationId,
        Guid? evaluationId,
        CancellationToken cancellationToken)
    {
        if (evaluationId is null)
        {
            return [];
        }

        var rows = await connection.QueryAsync<CriterionRow>(
            new CommandDefinition(
                """
                SELECT
                    judgment_id,
                    criterion_id,
                    criterion_version,
                    evaluator_mode,
                    status,
                    confidence,
                    uncertainty_json::text AS uncertainty_json,
                    rationale,
                    score_json::text AS score_json,
                    provisional_feedback
                FROM evaluation_criterion_judgments
                WHERE organization_id = @OrganizationId
                  AND evaluation_id = @EvaluationId
                ORDER BY criterion_id
                LIMIT @Limit;
                """,
                new
                {
                    OrganizationId = organizationId,
                    EvaluationId = evaluationId.Value,
                    Limit = AssignedReviewAdmission.MaxCriterionSummaries,
                },
                cancellationToken: cancellationToken));

        return rows.Select(row => AssignedReviewProjectionMapper.MapCriterionSummary(MapCriterion(row))).ToArray();
    }

    private static AssignedReviewCriterionSnapshot MapCriterion(CriterionRow row)
    {
        var uncertainty = JsonSerializer.Deserialize<List<string>>(row.uncertainty_json) ?? [];
        object? score = row.score_json is null
            ? null
            : JsonSerializer.Deserialize<object>(row.score_json);
        return new AssignedReviewCriterionSnapshot(
            row.criterion_id,
            row.criterion_version,
            FormatCriterionLabel(row.criterion_id),
            row.evaluator_mode,
            row.status,
            row.confidence,
            uncertainty,
            row.rationale,
            score,
            row.provisional_feedback);
    }

    private static string FormatCriterionLabel(string criterionId) =>
        criterionId.Replace('.', ' ');

    private static async Task<LabelLookup> LoadLabelsAsync(
        Npgsql.NpgsqlConnection connection,
        Guid organizationId,
        IReadOnlyList<CaseRow> rows,
        CancellationToken cancellationToken)
    {
        var activityIds = rows.Select(row => row.activity_id).Distinct().ToArray();
        var participantIds = rows.Select(row => row.participant_id).Distinct().ToArray();
        if (activityIds.Length == 0 && participantIds.Length == 0)
        {
            return LabelLookup.Empty;
        }

        var activityLabels = activityIds.Length == 0
            ? []
            : await connection.QueryAsync<LabelRow>(
                new CommandDefinition(
                    """
                    SELECT activity.activity_id, revision.title
                    FROM assessment_activity_revisions AS revision
                    INNER JOIN assessment_activities AS activity
                      ON activity.organization_id = revision.organization_id
                     AND activity.current_revision_id = revision.revision_id
                    WHERE revision.organization_id = @OrganizationId
                      AND activity.activity_id = ANY(@ActivityIds);
                    """,
                    new { OrganizationId = organizationId, ActivityIds = activityIds },
                    cancellationToken: cancellationToken));

        var participantLabels = participantIds.Length == 0
            ? []
            : await connection.QueryAsync<ParticipantLabelRow>(
                new CommandDefinition(
                    """
                    SELECT actor_id, display_label
                    FROM identity_human_display_profiles
                    WHERE organization_id = @OrganizationId
                      AND actor_id = ANY(@ParticipantIds);
                    """,
                    new { OrganizationId = organizationId, ParticipantIds = participantIds },
                    cancellationToken: cancellationToken));

        return new LabelLookup(activityLabels, participantLabels);
    }

    private static bool TryParseStableEvidenceId(string evidenceId, out Guid evidenceGuid)
    {
        evidenceGuid = Guid.Empty;
        if (EvaluationEvidenceSourceIdentity.TryParseStableEvidenceId(evidenceId, out evidenceGuid))
        {
            return true;
        }

        return Guid.TryParse(evidenceId, out evidenceGuid) && evidenceGuid != Guid.Empty;
    }

    private static bool TryDecodeCursor(string? cursor, out DateTimeOffset? updatedAt, out Guid? reviewCaseId)
    {
        updatedAt = null;
        reviewCaseId = null;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = decoded.Split('|', 2);
            if (parts.Length != 2
                || !DateTimeOffset.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedUpdatedAt)
                || !Guid.TryParse(parts[1], out var parsedReviewCaseId))
            {
                return false;
            }

            updatedAt = parsedUpdatedAt;
            reviewCaseId = parsedReviewCaseId;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string EncodeCursor(DateTimeOffset updatedAt, Guid reviewCaseId) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{updatedAt:O}|{reviewCaseId}"));

    private sealed class LabelLookup
    {
        public static LabelLookup Empty { get; } = new([], []);

        private readonly Dictionary<Guid, string> _activities;
        private readonly Dictionary<Guid, string> _participants;

        public LabelLookup(IEnumerable<LabelRow> activities, IEnumerable<ParticipantLabelRow> participants)
        {
            _activities = activities.ToDictionary(row => row.activity_id, row => row.title);
            _participants = participants.ToDictionary(row => row.actor_id, row => row.display_label);
        }

        public bool TryGetValue(Guid activityId, out string? label) =>
            _activities.TryGetValue(activityId, out label);

        public bool TryGetParticipant(Guid participantId, out string? label) =>
            _participants.TryGetValue(participantId, out label);
    }

    private sealed record CaseRow(
        Guid review_case_id,
        string case_state,
        string candidate_state,
        DateTimeOffset updated_at,
        Guid? current_candidate_evaluation_id,
        Guid activity_id,
        Guid participant_id,
        string? aggregate_status,
        DateTimeOffset? completed_at,
        string? request_state);

    private sealed record CriterionRow(
        Guid judgment_id,
        string criterion_id,
        string criterion_version,
        string evaluator_mode,
        string status,
        string confidence,
        string uncertainty_json,
        string rationale,
        string? score_json,
        string? provisional_feedback);

    private sealed record EvidenceRow(
        Guid evidence_id,
        string source_type,
        string precision,
        string integrity_state);

    private sealed record EvidenceDetailRow(
        Guid evidence_id,
        Guid request_id,
        string source_type,
        string source_id,
        string source_version_id,
        string source_content_digest,
        string locator_schema,
        string locator_digest,
        string? locator_canonical_json,
        string precision,
        string integrity_state,
        Guid activity_id,
        Guid participant_id,
        Guid attempt_id,
        Guid session_id);

    private sealed record LabelRow(Guid activity_id, string title);

    private sealed record ParticipantLabelRow(Guid actor_id, string display_label);
}

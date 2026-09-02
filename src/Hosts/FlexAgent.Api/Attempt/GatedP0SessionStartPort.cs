using Dapper;
using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Postgres;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;
using Npgsql;

namespace FlexAgent.Api;

public sealed class GatedP0SessionStartPort(
    IHostEnvironment environment,
    PostgresSessionRuntimeRepository? sessions) : ISessionStartCommitPort
{
    public bool CanCommit =>
        !environment.IsProduction() && !environment.IsEnvironment("Staging");

    public async Task<SessionStartCommitResult> CommitActiveAsync(
        SessionStartCommitRequest request,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        var productionLocked = environment.IsProduction() || environment.IsEnvironment("Staging");
        if (productionLocked)
        {
            return new SessionStartCommitResult(false, AttemptFailureCodes.Unavailable, null, null);
        }

        var npgsql = commitTransaction as NpgsqlTransaction;

        var policy = ResolveDevelopmentPolicy();
        if (policy is null)
        {
            return new SessionStartCommitResult(false, AttemptFailureCodes.Unavailable, null, null);
        }

        var ownership = new SessionOwnership(
            request.Scope.OrganizationId,
            request.Scope.ActivityId,
            request.Scope.ParticipantActorId,
            request.AttemptId,
            request.SessionId);
        IReadOnlyList<ResolvedSourceReference> baselineSources;
        IReadOnlyList<ResolvedSourceReference> revalidatedSources;
        var loaded = await TryLoadFrozenAndRevalidatedSourcesAsync(
                request.Scope,
                npgsql,
                cancellationToken);
        if (loaded is null)
        {
            if (npgsql is not null)
            {
                return new SessionStartCommitResult(false, AttemptFailureCodes.Unavailable, null, null);
            }

            baselineSources = RequiredDevelopmentSources(request.Scope.BaselineId);
            revalidatedSources = baselineSources;
        }
        else
        {
            baselineSources = loaded.Value.Baseline;
            revalidatedSources = loaded.Value.Revalidated;
        }
        var submissionRefs = request.SubmissionBindings
            .Where(binding => binding.ContentDigest.Length == 64)
            .Select(binding => new ProtectedContentRef(
                $"submission:{binding.VersionId:D}",
                binding.ContentDigest))
            .ToArray();
        if (submissionRefs.Length == 0)
        {
            return new SessionStartCommitResult(false, AttemptFailureCodes.Unavailable, null, null);
        }

        var model = CreateDevelopmentFrozenDeployment();
        var resolved = P0ResolvedSessionConfigurationResolver.Resolve(
            new P0ResolvedConfigurationRequest(
                request.ConfigurationId,
                request.ManifestId,
                ownership,
                baselineSources,
                revalidatedSources,
                policy,
                model,
                submissionRefs,
                false,
                false,
                false,
                false,
                false));
        if (!resolved.Succeeded || resolved.Value is null)
        {
            return new SessionStartCommitResult(false, AttemptFailureCodes.Unavailable, null, null);
        }

        if (npgsql is not null && sessions is null)
        {
            return new SessionStartCommitResult(false, AttemptFailureCodes.Unavailable, null, null);
        }

        if (npgsql is not null && sessions is not null)
        {
            var binding = new TrustedSessionBinding(
                ownership,
                request.ConfigurationId.ToString("D"),
                resolved.Value.ConfigurationDigest,
                request.ManifestId.ToString("D"),
                policy,
                submissionRefs,
                [],
                [],
                model);
            var runtime = SessionRuntime.CreateActive(binding, request.StartedAtUtc);
            await sessions.InsertActiveAsync(
                ownership,
                runtime,
                new TrustedRuntimeActor(request.Scope.ParticipantActorId, HumanInteractiveActorTypes.Interactive),
                npgsql,
                cancellationToken);
            var connection = npgsql.Connection ?? throw new InvalidOperationException("commit.transaction.required");
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_resolved_configurations (
                        organization_id, configuration_id, configuration_digest, canonical_json, created_at)
                    VALUES (@OrganizationId, @ConfigurationId, @ConfigurationDigest, @CanonicalJson, @CreatedAt)
                    """,
                    new
                    {
                        ownership.OrganizationId,
                        request.ConfigurationId,
                        resolved.Value.ConfigurationDigest,
                        CanonicalJson = resolved.Value.CanonicalJson,
                        CreatedAt = request.StartedAtUtc,
                    },
                    npgsql,
                    cancellationToken: cancellationToken));
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO session_initial_manifests (
                        organization_id, manifest_id, configuration_id, manifest_digest, canonical_json, created_at)
                    VALUES (@OrganizationId, @ManifestId, @ConfigurationId, @ManifestDigest, @CanonicalJson, @CreatedAt)
                    """,
                    new
                    {
                        ownership.OrganizationId,
                        request.ManifestId,
                        request.ConfigurationId,
                        resolved.Value.ManifestDigest,
                        CanonicalJson = resolved.Value.InitialManifestJson,
                        CreatedAt = request.StartedAtUtc,
                    },
                    npgsql,
                    cancellationToken: cancellationToken));
        }

        return new SessionStartCommitResult(
            true,
            "session.started",
            resolved.Value.ConfigurationDigest,
            resolved.Value.ManifestDigest);
    }

    private static FrozenTextSessionRuntimePolicy? ResolveDevelopmentPolicy()
    {
        var values = new RuntimePolicyEffectiveValues
        {
            InvocationContractVersion = RuntimeContractVersions.InvocationV1,
            DecisionContractVersion = RuntimeContractVersions.DecisionV1,
            DecisionValidationPolicyVersion = RuntimeContractVersions.DecisionValidationPolicyV1,
            DecisionSchemaBindings =
            [
                new DecisionTypeSchemaBinding(RuntimeDecisionTypes.EmitMessage, RuntimeContractVersions.AgentDecisionSchemaV1),
                new DecisionTypeSchemaBinding(RuntimeDecisionTypes.NoAction, RuntimeContractVersions.AgentDecisionSchemaV1),
            ],
            PermittedNonTimerTriggers =
            [
                new RuntimeTriggerDescriptor(
                    RuntimeTriggerIdentifiers.ParticipantInputFamily,
                    RuntimeTriggerIdentifiers.ParticipantMessageType),
                new RuntimeTriggerDescriptor(
                    RuntimeTriggerIdentifiers.WorkflowEventFamily,
                    RuntimeTriggerIdentifiers.AgentOpeningType),
                new RuntimeTriggerDescriptor(
                    RuntimeTriggerIdentifiers.WorkflowEventFamily,
                    RuntimeTriggerIdentifiers.AgentClosingType),
            ],
            PermittedDecisionTypes = [RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.NoAction],
            AgentInitiatedOpeningPermitted = true,
            AgentInitiatedClosingPermitted = true,
            NoActionPermitted = true,
            InvocationBounds = new InvocationBounds(3, 10, 0, 5, 30),
            StreamingPublicationBounds = new StreamingPublicationBounds(512, 40, 64, 8_192, 2),
            TimerLane = new TimerLanePolicyValues
            {
                Enabled = false,
                DefaultDelay = "PT5M",
                MinRequestedDelay = "PT1M",
                MaxRequestedDelay = "PT30M",
                ClockBasis = TimerLaneClockBasis.ActiveSessionTime,
                PermittedStages = ["active"],
                PermittedDecisionTypes = [RuntimeDecisionTypes.EmitMessage, RuntimeDecisionTypes.NoAction],
                Budgets = new TimerLaneBudgets(5, 8, 10, 1, 30),
            },
            ExplicitlyDisabledCapabilities = P0TextSessionRuntimeCapabilityPolicy
                .RequiredExplicitlyDisabledCapabilities
                .ToArray(),
        };
        var digest = RuntimePolicyBaselineContentDigest.Compute(values);
        return FrozenRuntimePolicyResolver.Resolve(
            new RuntimePolicyResolutionRequest(digest, new RuntimePolicyBaselineSource("baseline.p0.text.dev", digest, values), [])).Policy;
    }

    private static FrozenModelDeploymentBinding CreateDevelopmentFrozenDeployment()
    {
        var profile = InstalledModelDeploymentProfile.Create(
            "synthetic.fake.v1",
            "1",
            ModelDeploymentAdapterKinds.DeterministicFake,
            "sessions.fake.v1",
            new Uri("https://api.openai.com/"),
            "synthetic.model.pinned",
            "synthetic.model.pinned.2026-01-01",
            "p0.text.structured-control",
            ModelDeploymentCredentialModes.OrganizationByok,
            256,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            4,
            "synthetic.provider");
        return new FrozenModelDeploymentBinding(
            profile.ProfileId,
            profile.ProfileVersion,
            profile.ProfileDigest,
            profile.ProviderId,
            ModelDeploymentCredentialModes.OrganizationByok,
            "bind.opaque.dev",
            "bind.v1");
    }

    private static readonly HashSet<string> RequiredSourceKeys =
    [
        "organization_policy",
        "agent",
        "harness",
        "workflow",
        "model_deployment",
        "task_submission",
        "capability",
    ];

    private static async Task<(
            IReadOnlyList<ResolvedSourceReference> Baseline,
            IReadOnlyList<ResolvedSourceReference> Revalidated)?> TryLoadFrozenAndRevalidatedSourcesAsync(
        SubmissionParentScope scope,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        if (transaction is null)
        {
            return null;
        }

        var connection = transaction.Connection ?? throw new InvalidOperationException("commit.transaction.required");
        var rows = (await connection.QueryAsync<FrozenSourceRow>(
            new CommandDefinition(
                """
                SELECT
                    COALESCE(ref->>'sourceKey', ref->>'source_key') AS SourceKey,
                    COALESCE(ref->>'sourceId', ref->>'source_id')::uuid AS SourceId,
                    COALESCE(ref->>'sourceVersion', ref->>'source_version')::uuid AS SourceVersionId,
                    COALESCE(ref->>'contentDigest', ref->>'content_digest') AS ContentDigest
                FROM assessment_activation_baselines b
                CROSS JOIN LATERAL jsonb_array_elements(
                    COALESCE(b.document->'sourceReferences', b.document->'source_references', '[]'::jsonb)) ref
                WHERE b.organization_id = @OrganizationId
                  AND b.activity_id = @ActivityId
                  AND b.baseline_id = @BaselineId
                """,
                new
                {
                    scope.OrganizationId,
                    scope.ActivityId,
                    scope.BaselineId,
                },
                transaction,
                cancellationToken: cancellationToken))).ToArray();
        if (RequiredSourceKeys.Any(required =>
                rows.All(row => !string.Equals(row.SourceKey, required, StringComparison.Ordinal))))
        {
            return null;
        }

        var baseline = rows
            .Where(row => row.SourceKey.Length > 0 && row.ContentDigest.Length == 64)
            .Select(row => new ResolvedSourceReference(
                row.SourceKey,
                row.SourceId,
                row.SourceVersionId,
                row.ContentDigest))
            .ToArray();
        var versionIds = baseline.Select(item => item.SourceVersionId).Distinct().ToArray();
        var registered = (await connection.QueryAsync<RegisteredVersionRow>(
            new CommandDefinition(
                """
                SELECT
                    configuration_source_id AS SourceId,
                    id AS SourceVersionId,
                    content_digest AS ContentDigest
                FROM configuration_source_versions
                WHERE organization_id = @OrganizationId
                  AND id = ANY(@VersionIds)
                """,
                new
                {
                    scope.OrganizationId,
                    VersionIds = versionIds,
                },
                transaction,
                cancellationToken: cancellationToken))).ToArray();
        var revalidated = new List<ResolvedSourceReference>(baseline.Length);
        foreach (var frozen in baseline)
        {
            var live = registered.FirstOrDefault(item =>
                item.SourceId == frozen.SourceId && item.SourceVersionId == frozen.SourceVersionId);
            if (live is null)
            {
                return null;
            }

            revalidated.Add(new ResolvedSourceReference(
                frozen.SourceKey,
                live.SourceId,
                live.SourceVersionId,
                live.ContentDigest));
        }

        return (baseline, revalidated);
    }

    private sealed record FrozenSourceRow(
        string SourceKey,
        Guid SourceId,
        Guid SourceVersionId,
        string ContentDigest);

    private sealed record RegisteredVersionRow(
        Guid SourceId,
        Guid SourceVersionId,
        string ContentDigest);

    // Isolated Development scaffold when the commit transaction has no frozen baseline.
    private static IReadOnlyList<ResolvedSourceReference> RequiredDevelopmentSources(Guid baselineId)
    {
        var digest = new string('c', 64);
        return
        [
            new ResolvedSourceReference("organization_policy", StableId("organization_policy", baselineId), StableId("organization_policy.version", baselineId), digest),
            new ResolvedSourceReference("agent", StableId("agent", baselineId), StableId("agent.version", baselineId), digest),
            new ResolvedSourceReference("harness", StableId("harness", baselineId), StableId("harness.version", baselineId), digest),
            new ResolvedSourceReference("workflow", StableId("workflow", baselineId), StableId("workflow.version", baselineId), digest),
            new ResolvedSourceReference("model_deployment", StableId("model_deployment", baselineId), StableId("model_deployment.version", baselineId), digest),
            new ResolvedSourceReference("task_submission", StableId("task_submission", baselineId), StableId("task_submission.version", baselineId), digest),
            new ResolvedSourceReference("capability", StableId("capability", baselineId), StableId("capability.version", baselineId), digest),
        ];
    }

    private static Guid StableId(string family, Guid seed)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{family}:{seed:D}"));
        hash[6] = (byte)((hash[6] & 0x0f) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash.AsSpan(0, 16));
    }
}

public sealed class PostgresAcknowledgmentLifecyclePort : IAcknowledgmentLifecyclePort
{
    public async Task<AcknowledgmentMutationOutcome> RecordAsync(
        AcknowledgeAttemptNoticeCommand command,
        RequiredNoticeProjection notice,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        var transaction = PostgresCommitTransaction.Required(commitTransaction);
        var connection = transaction.Connection ?? throw new InvalidOperationException("commit.transaction.required");
        var existing = await connection.QuerySingleOrDefaultAsync<AckRow>(
            new CommandDefinition(
                """
                SELECT record_id AS RecordId, outcome AS Outcome, command_digest AS CommandDigest
                FROM session_acknowledgment_records
                WHERE organization_id = @OrganizationId
                  AND enrollment_id = @EnrollmentId
                  AND idempotency_key = @IdempotencyKey
                """,
                new
                {
                    OrganizationId = command.Actor.Organization.OrganizationId,
                    command.EnrollmentId,
                    command.IdempotencyKey,
                },
                transaction,
                cancellationToken: cancellationToken));
        if (existing is not null)
        {
            if (!string.Equals(existing.CommandDigest, command.TrustedCommandDigest, StringComparison.Ordinal))
            {
                return new AcknowledgmentMutationOutcome(false, AttemptFailureCodes.IdempotencyConflict, null, null);
            }

            return new AcknowledgmentMutationOutcome(true, "acknowledgment.reconciled", existing.RecordId, existing.Outcome);
        }

        var recordId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO session_acknowledgment_records (
                    organization_id, record_id, enrollment_id, participant_actor_id, notice_id, source_id,
                    source_version_id, source_content_digest, notice_content_digest, outcome, recorded_at,
                    bound_attempt_id, idempotency_key, command_digest)
                VALUES (
                    @OrganizationId, @RecordId, @EnrollmentId, @ParticipantActorId, @NoticeId, @SourceId,
                    @SourceVersionId, @SourceContentDigest, @NoticeContentDigest, @Outcome, CLOCK_TIMESTAMP(),
                    NULL, @IdempotencyKey, @CommandDigest)
                """,
                new
                {
                    OrganizationId = command.Actor.Organization.OrganizationId,
                    RecordId = recordId,
                    command.EnrollmentId,
                    ParticipantActorId = command.Actor.Actor.ActorId,
                    notice.NoticeId,
                    notice.SourceId,
                    notice.SourceVersionId,
                    SourceContentDigest = notice.ContentDigest,
                    NoticeContentDigest = notice.ContentDigest,
                    command.Outcome,
                    command.IdempotencyKey,
                    CommandDigest = command.TrustedCommandDigest,
                },
                transaction,
                cancellationToken: cancellationToken));
        return new AcknowledgmentMutationOutcome(true, "acknowledgment.recorded", recordId, command.Outcome);
    }

    public async Task<IReadOnlyList<CurrentAcknowledgmentFact>> ListCurrentAsync(
        Guid organizationId,
        Guid enrollmentId,
        Guid participantActorId,
        IReadOnlyList<RequiredNoticeProjection> notices,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        if (notices.Count == 0)
        {
            return [];
        }

        var transaction = PostgresCommitTransaction.Required(commitTransaction);
        var connection = transaction.Connection ?? throw new InvalidOperationException("commit.transaction.required");
        var listed = await connection.QueryAsync<CurrentAcknowledgmentFact>(
            new CommandDefinition(
                """
                SELECT record_id AS RecordId, enrollment_id AS EnrollmentId, participant_actor_id AS ParticipantActorId,
                       notice_id AS NoticeId, source_version_id AS SourceVersionId,
                       notice_content_digest AS ContentDigest, outcome AS Outcome, recorded_at AS RecordedAtUtc,
                       bound_attempt_id AS BoundAttemptId
                FROM session_acknowledgment_records
                WHERE organization_id = @OrganizationId
                  AND enrollment_id = @EnrollmentId
                  AND participant_actor_id = @ParticipantActorId
                  AND bound_attempt_id IS NULL
                  AND notice_id = ANY(@NoticeIds)
                  AND source_version_id = ANY(@SourceVersionIds)
                """,
                new
                {
                    OrganizationId = organizationId,
                    EnrollmentId = enrollmentId,
                    ParticipantActorId = participantActorId,
                    NoticeIds = notices.Select(item => item.NoticeId).ToArray(),
                    SourceVersionIds = notices.Select(item => item.SourceVersionId).ToArray(),
                },
                transaction,
                cancellationToken: cancellationToken));
        return AcknowledgmentSelection.CurrentBindable(listed.ToArray(), notices);
    }

    public async Task<string?> BindToAttemptAsync(
        IReadOnlyList<CurrentAcknowledgmentFact> records,
        Guid attemptId,
        Guid enrollmentId,
        Guid participantActorId,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        var transaction = PostgresCommitTransaction.Required(commitTransaction);
        foreach (var record in records)
        {
            if (record.EnrollmentId != enrollmentId
                || record.ParticipantActorId != participantActorId
                || record.BoundAttemptId is Guid alreadyBound && alreadyBound != attemptId)
            {
                return AttemptFailureCodes.AcknowledgmentInvalid;
            }

            var connection = transaction.Connection ?? throw new InvalidOperationException("commit.transaction.required");
            var updated = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE session_acknowledgment_records AS records
                    SET bound_attempt_id = @AttemptId
                    FROM submissions_enrollments AS enrollments
                    WHERE records.organization_id = enrollments.organization_id
                      AND records.record_id = @RecordId
                      AND records.enrollment_id = @EnrollmentId
                      AND enrollments.enrollment_id = @EnrollmentId
                      AND records.participant_actor_id = @ParticipantActorId
                      AND (records.bound_attempt_id IS NULL OR records.bound_attempt_id = @AttemptId)
                    """,
                    new
                    {
                        AttemptId = attemptId,
                        record.RecordId,
                        EnrollmentId = enrollmentId,
                        ParticipantActorId = participantActorId,
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            if (updated != 1)
            {
                return AttemptFailureCodes.AcknowledgmentInvalid;
            }
        }

        return null;
    }

    private sealed record AckRow(Guid RecordId, string Outcome, string CommandDigest);
}

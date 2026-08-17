using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class ManifestTerminalizationTests
{
    [Fact]
    public void Fixture_seal_document_matches_manifest_jcs_sha256_v1()
    {
        var digest = ManifestTerminalSealComputer.ComputeDigest(
            new ManifestSealDocument(
                ProcedureId: ManifestSealProcedures.ManifestJcsSha256V1,
                SchemaVersion: "v1",
                CanonicalizationVersion: "rfc8785",
                ManifestSchemaVersion: "v1",
                ConfigurationId: "rsc.synthetic.0001",
                ConfigurationDigest: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                RuntimeRecords:
                [
                    new ManifestSealRuntimeRecord(
                        Sequence: 1,
                        RecordType: ManifestRuntimeRecordTypes.ModelInvocationV1,
                        PayloadDigest: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                ],
                TerminalState: "completed",
                TerminalReason: TerminalReasonCategories.ParticipantCompleted,
                OrganizationId: "org.synthetic.0001",
                ActivityId: "act.synthetic.0001",
                ParticipantId: "part.synthetic.0001",
                AttemptId: "att.synthetic.0001",
                SessionId: "sess.synthetic.0001"));

        Assert.Equal("dd8b1ae4935ff8f0d78d2cb6d67ad022894312641e6f4542c34a90da61562f93", digest);
    }

    [Fact]
    public void Fixture_seal_document_v2_binds_cutoff_sequence()
    {
        var digest = ManifestTerminalSealComputer.ComputeDigest(
            new ManifestSealDocument(
                ProcedureId: ManifestSealProcedures.ManifestJcsSha256V2,
                SchemaVersion: "v2",
                CanonicalizationVersion: "rfc8785",
                ManifestSchemaVersion: "v1",
                ConfigurationId: "rsc.synthetic.0001",
                ConfigurationDigest: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                RuntimeRecords:
                [
                    new ManifestSealRuntimeRecord(
                        Sequence: 1,
                        RecordType: ManifestRuntimeRecordTypes.ModelInvocationV1,
                        PayloadDigest: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
                ],
                TerminalState: "completed",
                TerminalReason: TerminalReasonCategories.ParticipantCompleted,
                OrganizationId: "org.synthetic.0001",
                ActivityId: "act.synthetic.0001",
                ParticipantId: "part.synthetic.0001",
                AttemptId: "att.synthetic.0001",
                SessionId: "sess.synthetic.0001",
                CutoffSequence: 42));

        Assert.Equal("88af584e1981d1327a0bb76073651ab14e9a68539f001f229e85ed5cfc40f1a5", digest);
    }

    [Fact]
    public void Create_active_appends_default_timer_provenance_without_a_seal()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();

        var record = Assert.Single(session.ManifestRuntimeRecords);
        Assert.Equal(1, record.ManifestSequence);
        Assert.Equal(ManifestRuntimeRecordTypes.TimerEventV1, record.RecordType);
        Assert.Equal(session.TimerSchedules[0].ScheduleRevisionId, record.PayloadRef.ProtectedRef);
        Assert.Null(session.TerminalRecord);
        Assert.Null(session.EvaluationHandoff);
        Assert.False(session.VerifyTerminalSeal());
    }

    [Fact]
    public void Participant_and_agent_work_append_invocation_transcript_and_timer_records()
    {
        var values = RuntimePolicyTestFixtures.CreateEnabledTimerEffectiveValues() with
        {
            InvocationBounds = new InvocationBounds(3, 10, 0, CooldownSeconds: 0, 30),
        };
        var session = SessionRuntimeTestFixtures.CreateActiveSession(
            RuntimePolicyTestFixtures.ResolvePolicy(values));
        var opening = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0);
        var openingId = opening.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            openingId,
            SessionRuntimeTestFixtures.NoAction(
                openingId,
                nextTimer: new NextTimerRecommendation("PT2M", "1")),
            SessionRuntimeTestFixtures.T0.AddSeconds(2)).Succeeded);
        var admitted = session.AcceptParticipantMessage(
            "msg.p.1",
            "turn.1",
            "slot.1",
            "trig.p.1",
            "idem.p.1",
            SessionRuntimeTestFixtures.T0.AddSeconds(3));
        var participantId = admitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            participantId,
            SessionRuntimeTestFixtures.EmitMessage(participantId),
            SessionRuntimeTestFixtures.T0.AddSeconds(4)).Succeeded);
        Assert.True(session.CommitAgentResponseFragment(
            new AgentResponseFragmentCommit(participantId, 1, "Hello", "agen.p.1"),
            SessionRuntimeTestFixtures.T0.AddSeconds(5)).Succeeded);
        Assert.True(session.CompleteAgentResponseMessage(
            participantId,
            SessionRuntimeTestFixtures.T0.AddSeconds(6)).Succeeded);

        Assert.Contains(
            session.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.ModelInvocationV1
                && record.PayloadRef.ProtectedRef == openingId);
        Assert.Contains(
            session.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.ModelInvocationV1
                && record.PayloadRef.ProtectedRef == $"{openingId}.outcome");
        Assert.Contains(
            session.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TranscriptAppendV1
                && record.PayloadRef.ProtectedRef == "msg.p.1");
        Assert.Contains(
            session.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TranscriptAppendV1
                && record.PayloadRef.ProtectedRef == session.AgentMessages[0].MessageId);
        Assert.Contains(
            session.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TimerEventV1
                && record.PayloadRef.ProtectedRef == session.TimerSchedules[1].ScheduleRevisionId);
        Assert.Equal(
            Enumerable.Range(1, session.ManifestRuntimeRecords.Count).Select(value => (long)value).ToArray(),
            session.ManifestRuntimeRecords.Select(record => record.ManifestSequence).ToArray());
        Assert.Equal(
            ProtectedContentRef.DigestUtf8($"decided:{RuntimeDecisionTypes.NoAction}"),
            session.ManifestRuntimeRecords.Single(record =>
                record.PayloadRef.ProtectedRef == $"{openingId}.outcome").PayloadRef.ContentDigest);
        Assert.Equal(
            ProtectedContentRef.DigestUtf8($"decided:{RuntimeDecisionTypes.EmitMessage}"),
            session.ManifestRuntimeRecords.Single(record =>
                record.PayloadRef.ProtectedRef == $"{participantId}.outcome").PayloadRef.ContentDigest);
    }

    [Fact]
    public void Execution_failure_and_late_result_append_invocation_outcome_records()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var admitted = session.AcceptParticipantMessage(
            "msg.p.fail",
            "turn.fail",
            "slot.fail",
            "trig.fail",
            "idem.fail",
            SessionRuntimeTestFixtures.T0);
        var invocationId = admitted.Invocation!.AgentInvocationId;
        Assert.True(session.CompleteInvocation(
            invocationId,
            new ExecutionFailureCompletion(ExecutionFailureReasons.ProviderTimeout),
            SessionRuntimeTestFixtures.T0.AddSeconds(1)).Succeeded);

        var outcome = Assert.Single(
            session.ManifestRuntimeRecords,
            record => record.PayloadRef.ProtectedRef == $"{invocationId}.outcome");
        Assert.Equal(ManifestRuntimeRecordTypes.ModelInvocationV1, outcome.RecordType);
        Assert.Equal(
            ProtectedContentRef.DigestUtf8($"failed:{ExecutionOutcomeCategories.ExecutionFailed}"),
            outcome.PayloadRef.ContentDigest);

        var completing = SessionRuntimeTestFixtures.CreateActiveSession(
            ownership: SessionRuntimeTestFixtures.CreateOwnership() with
            {
                SessionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            });
        var lateAdmitted = completing.AcceptParticipantMessage(
            "msg.p.late",
            "turn.late",
            "slot.late",
            "trig.late",
            "idem.late",
            SessionRuntimeTestFixtures.T0);
        completing.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        var lateId = lateAdmitted.Invocation!.AgentInvocationId;
        var late = completing.CompleteInvocation(
            lateId,
            SessionRuntimeTestFixtures.NoAction(lateId),
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Equal(InvocationCompletionOutcomeCodes.LateResult, late.OutcomeCode);
        Assert.Contains(
            completing.ManifestRuntimeRecords,
            record => record.PayloadRef.ProtectedRef == $"{lateId}.outcome"
                && record.PayloadRef.ContentDigest
                    == ProtectedContentRef.DigestUtf8($"failed:{ExecutionOutcomeCategories.LateResult}"));
    }

    [Fact]
    public void Pause_and_resume_append_timer_provenance()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var revisionId = session.TimerSchedules[0].ScheduleRevisionId;
        session.Pause(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        session.Resume(SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Contains(
            session.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TimerEventV1
                && record.PayloadRef.ProtectedRef == $"{revisionId}.paused");
        Assert.Contains(
            session.ManifestRuntimeRecords,
            record => record.RecordType == ManifestRuntimeRecordTypes.TimerEventV1
                && record.PayloadRef.ProtectedRef == $"{revisionId}.resumed");
    }

    [Fact]
    public void Duplicate_admission_does_not_append_a_second_invocation_record()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var first = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0);
        var countAfterFirst = session.ManifestRuntimeRecords.Count;
        var retry = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.True(first.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.Reconciled, retry.OutcomeCode);
        Assert.Equal(countAfterFirst, session.ManifestRuntimeRecords.Count);
    }

    [Fact]
    public void Completed_terminal_seals_the_manifest_and_exposes_eligible_handoff()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0);
        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        session.Complete(SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Equal(SessionLifecycleState.Completed, session.LifecycleState);
        Assert.NotNull(session.TerminalRecord);
        Assert.Equal(TerminalReasonCategories.ParticipantCompleted, session.TerminalRecord!.ReasonCategory);
        Assert.Equal(AttemptTerminalMappings.Completed, session.TerminalRecord.AttemptMapping);
        Assert.Equal(ManifestSealProcedures.ManifestJcsSha256V2, session.TerminalRecord.ProcedureId);
        Assert.True(session.VerifyTerminalSeal());
        Assert.NotNull(session.EvaluationHandoff);
        Assert.Equal(EvaluationHandoffEligibilities.Eligible, session.EvaluationHandoff!.Eligibility);
        Assert.Equal(session.TerminalRecord.SealDigest, session.EvaluationHandoff.SealDigest);
        Assert.Equal(session.CutoffSequence, session.EvaluationHandoff.CutoffSequence);
    }

    [Fact]
    public void Terminated_and_aborted_sessions_seal_but_remain_ineligible_for_evaluation()
    {
        var terminated = SessionRuntimeTestFixtures.CreateActiveSession();
        terminated.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        terminated.Terminate(SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var aborted = SessionRuntimeTestFixtures.CreateActiveSession(
            ownership: SessionRuntimeTestFixtures.CreateOwnership() with
            {
                SessionId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            });
        aborted.Abort(SessionRuntimeTestFixtures.T0.AddSeconds(1));

        Assert.Equal(SessionLifecycleState.Terminated, terminated.LifecycleState);
        Assert.True(terminated.VerifyTerminalSeal());
        Assert.Equal(EvaluationHandoffEligibilities.Ineligible, terminated.EvaluationHandoff!.Eligibility);
        Assert.Equal(AttemptTerminalMappings.Aborted, terminated.TerminalRecord!.AttemptMapping);
        Assert.Equal(SessionLifecycleState.Aborted, aborted.LifecycleState);
        Assert.True(aborted.VerifyTerminalSeal());
        Assert.Equal(EvaluationHandoffEligibilities.Ineligible, aborted.EvaluationHandoff!.Eligibility);
        Assert.Equal(TerminalReasonCategories.UnrecoverableFailure, aborted.TerminalRecord!.ReasonCategory);
    }

    [Fact]
    public void Altered_or_reordered_runtime_records_fail_seal_verification()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        session.Complete(SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var original = session.ManifestRuntimeRecords[0];
        session.ReplaceManifestRecordForVerification(
            0,
            ManifestRuntimeRecord.Rehydrate(
                original.ManifestSequence,
                ManifestRuntimeRecordTypes.ModelInvocationV1,
                original.ServiceActor,
                original.OccurredAt,
                new ProtectedContentRef("ainv.forged", original.PayloadRef.ContentDigest),
                original.SessionSequence));

        Assert.False(session.VerifyTerminalSeal());
    }

    [Fact]
    public void Changing_only_cutoff_sequence_fails_seal_verification()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        session.Complete(SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var sealedCutoff = Assert.NotNull(session.CutoffSequence);
        session.ReplaceTerminalCutoffForVerification(sealedCutoff + 1);

        Assert.Equal(ManifestSealProcedures.ManifestJcsSha256V2, session.TerminalRecord!.ProcedureId);
        Assert.NotEqual(sealedCutoff, session.TerminalRecord.CutoffSequence);
        Assert.False(session.VerifyTerminalSeal());
    }

    [Fact]
    public void Missing_v2_cutoff_fails_seal_verification_without_throwing()
    {
        var live = SessionRuntimeTestFixtures.CreateActiveSession();
        live.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        live.Complete(SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var restored = SessionRuntime.Rehydrate(
            live.Binding,
            live.LifecycleState,
            live.SessionVersion,
            live.SessionSequence,
            live.CutoffSequence,
            live.LastCommittedAt,
            manifestRecords: live.ManifestRuntimeRecords,
            terminalRecord: live.TerminalRecord! with { CutoffSequence = null });

        Assert.False(restored.VerifyTerminalSeal());
    }

    [Fact]
    public void Completing_session_has_no_handoff_and_rejects_post_cutoff_admission()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        var admitted = session.AdmitTrustedTrigger(
            SessionRuntimeTestFixtures.OpeningTrigger(),
            "idem.open",
            SessionRuntimeTestFixtures.T0.AddSeconds(2));

        Assert.Equal(SessionLifecycleState.Completing, session.LifecycleState);
        Assert.Null(session.EvaluationHandoff);
        Assert.Null(session.TerminalRecord);
        Assert.False(admitted.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.LifecycleIneligible, admitted.OutcomeCode);
    }

    [Fact]
    public void Rehydrate_restores_manifest_seal_and_handoff()
    {
        var live = SessionRuntimeTestFixtures.CreateActiveSession();
        live.BeginCompleting(SessionRuntimeTestFixtures.T0.AddSeconds(1));
        live.Complete(SessionRuntimeTestFixtures.T0.AddSeconds(2));
        var restored = SessionRuntime.Rehydrate(
            live.Binding,
            live.LifecycleState,
            live.SessionVersion,
            live.SessionSequence,
            live.CutoffSequence,
            live.LastCommittedAt,
            manifestRecords: live.ManifestRuntimeRecords,
            terminalRecord: live.TerminalRecord,
            evaluationHandoff: live.EvaluationHandoff);

        Assert.Equal(live.ManifestRuntimeRecords.Count, restored.ManifestRuntimeRecords.Count);
        Assert.Equal(live.TerminalRecord!.SealDigest, restored.TerminalRecord!.SealDigest);
        Assert.Equal(EvaluationHandoffEligibilities.Eligible, restored.EvaluationHandoff!.Eligibility);
        Assert.True(restored.VerifyTerminalSeal());
    }
}

using System.Collections.Concurrent;
using System.Security.Cryptography;
using FlexAgent.Contracts.Browser;
using FlexAgent.Contracts.Transport;
using FlexAgent.SyntheticBrowser.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FlexAgent.SyntheticBrowser.Application;

public sealed class SyntheticBrowserOptions
{
    public const string SectionName = "SyntheticBrowser";
    public bool Enabled { get; set; }
    public TimeSpan GrantLifetime { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(8);
}

public sealed class SyntheticScenarioState
{
    public string ActivityLifecycle { get; set; } = "draft";
    public int ActivityVersion { get; set; } = 1;
    public bool EnrollmentCreated { get; set; }
    public bool SubmissionAccepted { get; set; }
    public string? SubmissionPreview { get; set; }
    public bool AttemptStarted { get; set; }
    public string SessionLifecycle { get; set; } = "not_started";
    public int SessionVersion { get; set; } = 1;
    public long SessionSequence { get; set; }
    public List<SessionTranscriptItemV1> Transcript { get; } = [];
    public string ReviewLifecycle { get; set; } = "awaiting_evaluation";
    public string ReleaseLifecycle { get; set; } = "not_ready";
    public string ResultLifecycle { get; set; } = "neutral_pre_release";
    public bool PermissionRevoked { get; set; }
    public HashSet<string> ProcessedIdempotencyKeys { get; } = [];
}

public sealed class SyntheticBrowserService : ISyntheticBrowserService
{
    private readonly SyntheticBrowserOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ConcurrentDictionary<string, ScenarioGrantRecord> _grants = new();
    private readonly ConcurrentDictionary<string, SyntheticSessionRecord> _sessions = new();
    private readonly ConcurrentDictionary<string, SyntheticScenarioState> _scenarioStates = new();

    private const string SyntheticOrgId = "org.synthetic.demo";
    private const string SyntheticOrgName = "Synthetic Demo Organization";
    private const string SyntheticActivityId = "act.synthetic.campaign-001";
    private const string SyntheticEnrollmentId = "enr.synthetic.001";
    private const string SyntheticSessionId = "sess.synthetic.001";
    private const string SyntheticReviewCaseId = "rev.synthetic.001";
    private const string SyntheticReleaseId = "rel.synthetic.001";
    private const string SyntheticResultId = "res.synthetic.001";

    public SyntheticBrowserService(IOptions<SyntheticBrowserOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public bool IsEnabled =>
        _options.Enabled && !_environment.IsProduction();

    public ScenarioGrantResponseV1 CreateScenarioGrant(ScenarioGrantRequestV1 request)
    {
        EnsureEnabled();
        ValidateScenario(request.ScenarioId, request.ActorStage);

        var grantToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTimeOffset.UtcNow.Add(_options.GrantLifetime);
        var record = new ScenarioGrantRecord
        {
            GrantToken = grantToken,
            ScenarioId = request.ScenarioId,
            ActorStage = request.ActorStage,
            ExpiresAt = expiresAt,
        };

        _grants[grantToken] = record;
        _scenarioStates.TryAdd(request.ScenarioId, new SyntheticScenarioState());

        if (request.ScenarioId == SyntheticScenarioIds.PermissionRevoked)
        {
            _scenarioStates[request.ScenarioId].PermissionRevoked = true;
        }

        return new ScenarioGrantResponseV1(BrowserSchemaVersion.V1, grantToken, expiresAt);
    }

    public ScenarioGrantExchangeResponseV1? ExchangeGrant(string grantToken)
    {
        EnsureEnabled();

        if (!_grants.TryGetValue(grantToken, out var grant))
        {
            return null;
        }

        if (grant.IsConsumed || grant.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        grant.IsConsumed = true;
        var sessionId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        var expiresAt = DateTimeOffset.UtcNow.Add(_options.SessionLifetime);
        var session = new SyntheticSessionRecord
        {
            SessionId = sessionId,
            ScenarioId = grant.ScenarioId,
            ActorStage = grant.ActorStage,
            ActorId = ResolveActorId(grant.ActorStage),
            ExpiresAt = expiresAt,
        };

        _sessions[sessionId] = session;
        return new ScenarioGrantExchangeResponseV1(BrowserSchemaVersion.V1, sessionId, expiresAt);
    }

    public SyntheticSessionRecord? ResolveSession(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return session;
    }

    public ActorContextV1? GetActorContext(SyntheticSessionRecord session)
    {
        if (IsAccessDenied(session))
        {
            return null;
        }

        return new ActorContextV1(
            BrowserSchemaVersion.V1,
            session.ActorId,
            ResolveActorDisplayName(session.ActorStage),
            SyntheticOrgId,
            SyntheticOrgName,
            ResolveCapabilities(session.ActorStage),
            session.ActorStage,
            true);
    }

    public NavigationProjectionV1 GetNavigation(SyntheticSessionRecord session)
    {
        if (IsAccessDenied(session))
        {
            return new NavigationProjectionV1(BrowserSchemaVersion.V1, []);
        }

        var caps = ResolveCapabilities(session.ActorStage);
        var destinations = new List<NavigationDestinationV1>
        {
            Nav("home", "Home", "/", "p0", true, null),
            Nav("activities", "Activities", "/activities", "p0", caps.Contains("activity_admin"), caps.Contains("activity_admin") ? null : "No Activity administration access"),
            Nav("agents", "Agents", "/agents", "p1", true, null),
            Nav("harnesses", "Harnesses", "/harnesses", "p1", true, null),
            Nav("my-work", "My work", "/my-work", "p0", caps.Contains("participant"), caps.Contains("participant") ? null : "No assigned Participant work"),
            Nav("review-work", "Review work", "/review-work", "p0", caps.Contains("reviewer"), caps.Contains("reviewer") ? null : "No active Review assignment"),
            Nav("release-work", "Release work", "/release-work", "p0", caps.Contains("release"), caps.Contains("release") ? null : "No Release authority"),
            Nav("results", "Results", "/results", "p0", caps.Contains("participant"), caps.Contains("participant") ? null : "No authorized Results"),
            Nav("governance", "Governance", "/governance", "p0", caps.Contains("governance"), caps.Contains("governance") ? null : "No governance access"),
        };

        return new NavigationProjectionV1(BrowserSchemaVersion.V1, destinations);
    }

    public HomeProjectionV1 GetHome(SyntheticSessionRecord session)
    {
        if (IsAccessDenied(session))
        {
            return new HomeProjectionV1(BrowserSchemaVersion.V1, "Access unavailable", [], []);
        }

        var state = GetState(session.ScenarioId);
        var items = new List<HomeWorkItemV1>();

        if (session.ActorStage == SyntheticActorStages.Administrator)
        {
            if (state.ActivityLifecycle == "draft")
            {
                items.Add(new HomeWorkItemV1("hw-1", "Assessment Campaign draft", "Draft · Not activated", "campaign_administration", $"/activities/{SyntheticActivityId}", "Continue setup"));
            }
            else if (state.ActivityLifecycle == "activated" && !state.EnrollmentCreated)
            {
                items.Add(new HomeWorkItemV1("hw-2", "Assign Participants", "Activated cohort", "campaign_administration", $"/activities/{SyntheticActivityId}/enrollment", "Assign Participant"));
            }
        }

        if (session.ActorStage == SyntheticActorStages.Participant && state.EnrollmentCreated)
        {
            items.Add(new HomeWorkItemV1("hw-3", "Assessment assignment", ResolveAssignmentStatus(state), "participant_work", "/my-work", ResolveParticipantNextAction(state)));
        }

        if (session.ActorStage == SyntheticActorStages.Reviewer && state.SessionLifecycle is "completed" or "terminated")
        {
            items.Add(new HomeWorkItemV1("hw-4", "Review case", state.ReviewLifecycle == "ready_for_review" ? "Ready for review" : "Awaiting evaluation", "review", $"/review-work/{SyntheticReviewCaseId}", "Open case"));
        }

        if (session.ActorStage == SyntheticActorStages.ReleaseActor && state.ReviewLifecycle == "approved")
        {
            items.Add(new HomeWorkItemV1("hw-5", "Result ready · Not released", "Approved · Awaiting Release", "release", $"/release-work/{SyntheticReleaseId}", "Preview and Release"));
        }

        return new HomeProjectionV1(
            BrowserSchemaVersion.V1,
            $"Welcome, {ResolveActorDisplayName(session.ActorStage)}",
            items,
            []);
    }

    public ActivitiesListProjectionV1 GetActivities(SyntheticSessionRecord session)
    {
        RequireCapability(session, "activity_admin");
        var state = GetState(session.ScenarioId);
        var activities = new List<ActivitySummaryV1>
        {
            new(SyntheticActivityId, "Synthetic Assessment Campaign", "Campaign", "Assessment", FormatActivityStatus(state), $"/activities/{SyntheticActivityId}"),
        };

        var actions = new List<PermittedActionV1>();
        if (state.ActivityLifecycle == "draft")
        {
            actions.Add(Action("create_campaign", "New assessment Campaign", "Start a new Campaign draft", false));
        }

        return new ActivitiesListProjectionV1(BrowserSchemaVersion.V1, activities, actions);
    }

    public ActivityDetailProjectionV1? GetActivityDetail(SyntheticSessionRecord session, string activityId)
    {
        RequireCapability(session, "activity_admin");
        if (!string.Equals(activityId, SyntheticActivityId, StringComparison.Ordinal))
        {
            return null;
        }

        var state = GetState(session.ScenarioId);
        var readiness = BuildReadinessCategories(state);
        var actions = new List<PermittedActionV1>();

        if (state.ActivityLifecycle == "draft")
        {
            actions.Add(Action("save_draft", "Save draft", null, false));
            if (readiness.All(c => !c.IsBlocking))
            {
                actions.Add(Action("activate_cohort", "Activate cohort", "Material values become immutable", false));
            }
        }
        else if (state.ActivityLifecycle == "activated")
        {
            actions.Add(Action("assign_participants", "Assign Participants", null, false));
        }

        return new ActivityDetailProjectionV1(
            BrowserSchemaVersion.V1,
            SyntheticActivityId,
            "Synthetic Assessment Campaign",
            "Campaign",
            "Assessment",
            state.ActivityLifecycle,
            state.ActivityVersion,
            readiness,
            actions,
            state.ActivityLifecycle == "activated" ? "Activated baseline v1 · immutable" : null);
    }

    public EnrollmentProjectionV1? GetEnrollment(SyntheticSessionRecord session, string activityId)
    {
        RequireCapability(session, "activity_admin");
        if (!string.Equals(activityId, SyntheticActivityId, StringComparison.Ordinal))
        {
            return null;
        }

        var state = GetState(session.ScenarioId);
        var enrollments = state.EnrollmentCreated
            ? new List<EnrollmentSummaryV1> { new(SyntheticEnrollmentId, "Synthetic Participant", "Active") }
            : [];

        var actions = new List<PermittedActionV1>();
        if (state.ActivityLifecycle == "activated" && !state.EnrollmentCreated)
        {
            actions.Add(Action("assign_participant", "Assign Participant", null, false));
        }

        return new EnrollmentProjectionV1(
            BrowserSchemaVersion.V1,
            SyntheticActivityId,
            state.ActivityLifecycle,
            enrollments,
            state.EnrollmentCreated ? [] : [new ParticipantChoiceV1("part.synthetic.001", "Synthetic Participant")],
            actions);
    }

    public AssignmentProjectionV1? GetMyWorkAssignment(SyntheticSessionRecord session, string? enrollmentId)
    {
        RequireCapability(session, "participant");
        var state = GetState(session.ScenarioId);

        if (!state.EnrollmentCreated)
        {
            return new AssignmentProjectionV1(
                BrowserSchemaVersion.V1,
                SyntheticEnrollmentId,
                "Synthetic Assessment Campaign",
                "Complete the synthetic assessment task using permitted text material.",
                "UTC",
                "2026-12-31T23:59:59Z",
                "Not available",
                [],
                [],
                "no_assignment");
        }

        var versions = state.SubmissionAccepted
            ? new List<SubmissionVersionV1> { new("subv.synthetic.001", "Version 1", "Accepted", state.SubmissionPreview ?? "Synthetic submission content for demonstration.") }
            : [];

        var actions = new List<PermittedActionV1>();
        if (!state.SubmissionAccepted)
        {
            actions.Add(Action("submit_text", "Submit text", "Provide .txt or .md content", false));
        }
        else if (!state.AttemptStarted)
        {
            actions.Add(Action("start_attempt", "Start Attempt", "Consumes one Attempt entitlement", false));
        }
        else if (state.SessionLifecycle == "active")
        {
            actions.Add(Action("open_session", "Continue Session", null, false));
        }

        return new AssignmentProjectionV1(
            BrowserSchemaVersion.V1,
            SyntheticEnrollmentId,
            "Synthetic Assessment Campaign",
            "Complete the synthetic assessment task using permitted text material.",
            "UTC",
            "2026-12-31T23:59:59Z",
            state.AttemptStarted ? "Active" : "Available",
            versions,
            actions,
            ResolveAssignmentLifecycle(state));
    }

    public SessionProjectionV1? GetSession(SyntheticSessionRecord session, string sessionId)
    {
        if (!string.Equals(sessionId, SyntheticSessionId, StringComparison.Ordinal))
        {
            return null;
        }

        var state = GetState(session.ScenarioId);
        var canAccess = session.ActorStage is SyntheticActorStages.Participant or SyntheticActorStages.Administrator;
        if (!canAccess || !state.AttemptStarted)
        {
            return null;
        }

        var actions = new List<PermittedActionV1>();
        if (state.SessionLifecycle == "active")
        {
            actions.Add(Action("send_message", "Send", null, false));
            actions.Add(Action("pause_session", "Pause", null, false));
            actions.Add(Action("complete_session", "Complete Session", "Ends the Session deliberately", false));
        }
        else if (state.SessionLifecycle == "paused")
        {
            actions.Add(Action("resume_session", "Resume", null, false));
        }

        return new SessionProjectionV1(
            BrowserSchemaVersion.V1,
            SyntheticSessionId,
            state.SessionLifecycle,
            state.SessionLifecycle == "active" ? "45:00" : null,
            state.Transcript,
            actions,
            state.SubmissionAccepted ? "Submission v1 · accepted" : null,
            state.SessionVersion,
            state.SessionSequence > 0 ? state.SessionSequence.ToString() : null);
    }

    public ReviewWorkProjectionV1 GetReviewWork(SyntheticSessionRecord session)
    {
        RequireCapability(session, "reviewer");
        var state = GetState(session.ScenarioId);
        var cases = new List<ReviewCaseSummaryV1>();

        if (state.SessionLifecycle is "completed" or "terminated")
        {
            cases.Add(new ReviewCaseSummaryV1(
                SyntheticReviewCaseId,
                "Synthetic review case",
                state.ReviewLifecycle == "ready_for_review" ? "Ready for review" : "Awaiting evaluation",
                $"/review-work/{SyntheticReviewCaseId}"));
        }

        return new ReviewWorkProjectionV1(BrowserSchemaVersion.V1, cases, []);
    }

    public ReviewCaseDetailProjectionV1? GetReviewCase(SyntheticSessionRecord session, string caseId)
    {
        RequireCapability(session, "reviewer");
        if (!string.Equals(caseId, SyntheticReviewCaseId, StringComparison.Ordinal))
        {
            return null;
        }

        var state = GetState(session.ScenarioId);
        var evidence = new List<EvidenceItemV1>
        {
            new("ev.synthetic.001", "Transcript excerpt", "session.transcript · lines 1-5", "Participant: Hello, I am ready to begin."),
        };

        var criteria = new List<CriterionResultV1>
        {
            new("crit.synthetic.001", "Task completion", state.ReviewLifecycle == "awaiting_evaluation" ? "Pending" : "Met", evidence),
        };

        var actions = new List<PermittedActionV1>();
        if (state.ReviewLifecycle == "ready_for_review")
        {
            actions.Add(Action("approve", "Approve", null, false));
            actions.Add(Action("reject", "Reject", null, true));
            actions.Add(Action("escalate", "Escalate", null, false));
        }

        return new ReviewCaseDetailProjectionV1(
            BrowserSchemaVersion.V1,
            SyntheticReviewCaseId,
            state.ReviewLifecycle == "approved" ? "Approved" : "In review",
            "Evaluation candidate v1",
            criteria,
            actions,
            null,
            state.ReviewLifecycle,
            1);
    }

    public ReleaseWorkProjectionV1 GetReleaseWork(SyntheticSessionRecord session)
    {
        RequireCapability(session, "release");
        var state = GetState(session.ScenarioId);
        var items = new List<ReleaseItemSummaryV1>();

        if (state.ReviewLifecycle == "approved")
        {
            items.Add(new ReleaseItemSummaryV1(
                SyntheticReleaseId,
                "Synthetic Result",
                state.ReleaseLifecycle == "released" ? "Released" : "Result ready · Not released",
                $"/release-work/{SyntheticReleaseId}"));
        }

        return new ReleaseWorkProjectionV1(BrowserSchemaVersion.V1, items, []);
    }

    public ReleaseDetailProjectionV1? GetReleaseDetail(SyntheticSessionRecord session, string releaseId)
    {
        RequireCapability(session, "release");
        if (!string.Equals(releaseId, SyntheticReleaseId, StringComparison.Ordinal))
        {
            return null;
        }

        var state = GetState(session.ScenarioId);
        var actions = new List<PermittedActionV1>();
        if (state.ReleaseLifecycle != "released")
        {
            actions.Add(Action("release_result", "Release Result", "Makes the Result visible to the Participant", false));
        }

        return new ReleaseDetailProjectionV1(
            BrowserSchemaVersion.V1,
            SyntheticReleaseId,
            state.ReleaseLifecycle == "released" ? "Released" : "Result ready · Not released",
            "Synthetic participant-facing Result preview text.",
            "Participant only · same Organization",
            actions,
            1,
            state.ReleaseLifecycle);
    }

    public ResultsProjectionV1 GetResults(SyntheticSessionRecord session)
    {
        RequireCapability(session, "participant");
        var state = GetState(session.ScenarioId);
        var results = new List<ResultItemV1>
        {
            new(SyntheticResultId, "Synthetic Assessment Campaign", FormatResultStatus(state), $"/results/{SyntheticResultId}"),
        };

        return new ResultsProjectionV1(BrowserSchemaVersion.V1, results, []);
    }

    public ResultDetailProjectionV1? GetResultDetail(SyntheticSessionRecord session, string resultId)
    {
        RequireCapability(session, "participant");
        if (!string.Equals(resultId, SyntheticResultId, StringComparison.Ordinal))
        {
            return null;
        }

        var state = GetState(session.ScenarioId);
        return new ResultDetailProjectionV1(
            BrowserSchemaVersion.V1,
            SyntheticResultId,
            FormatResultStatus(state),
            state.ReleaseLifecycle == "released" ? "Synthetic released Result content." : null,
            state.ResultLifecycle,
            null);
    }

    public GovernanceProjectionV1 GetGovernance(SyntheticSessionRecord session)
    {
        RequireCapability(session, "governance");
        var entries = new List<GovernanceEntryV1>
        {
            new("gov.synthetic.001", "activity.draft.saved", "Synthetic Administrator", "2026-08-10T10:00:00Z", "succeeded"),
            new("gov.synthetic.002", "cohort.activated", "Synthetic Administrator", "2026-08-10T11:00:00Z", "succeeded"),
        };

        return new GovernanceProjectionV1(BrowserSchemaVersion.V1, entries, [], true);
    }

    public PlannedTierProjectionV1 GetPlannedTier(SyntheticSessionRecord session, string moduleName)
    {
        return new PlannedTierProjectionV1(
            BrowserSchemaVersion.V1,
            moduleName,
            "p1",
            $"{moduleName} authoring is planned for P1. No P0 controls are available.",
            []);
    }

    public BrowserCommandResultV1 ExecuteCommand(SyntheticSessionRecord session, BrowserCommandEnvelopeV1 command)
    {
        if (IsAccessDenied(session))
        {
            return Denied("Access has changed.");
        }

        var state = GetState(session.ScenarioId);

        if (state.ProcessedIdempotencyKeys.Contains(command.IdempotencyKey))
        {
            return Success(state, "Reconciled duplicate request.");
        }

        if (session.ScenarioId == SyntheticScenarioIds.StaleRevision &&
            command.ExpectedVersion.HasValue &&
            command.ExpectedVersion.Value < state.ActivityVersion)
        {
            return Conflict("Revision is stale. Refresh and retry.");
        }

        if (session.ScenarioId == SyntheticScenarioIds.UncertainReconciliation &&
            command.CommandType == "activity.activate_cohort")
        {
            return Uncertain("Activation outcome uncertain. Reconcile from current state.");
        }

        state.ProcessedIdempotencyKeys.Add(command.IdempotencyKey);

        return command.CommandType switch
        {
            "activity.save_draft" => HandleSaveDraft(state),
            "activity.activate_cohort" => HandleActivate(state),
            "enrollment.assign" => HandleAssign(state),
            "submission.submit_text" => HandleSubmit(state, command),
            "attempt.start" => HandleStartAttempt(state),
            "session.send_message" => HandleSendMessage(state, command),
            "session.pause" => HandlePause(state),
            "session.resume" => HandleResume(state),
            "session.complete" => HandleComplete(state),
            "review.approve" => HandleReviewApprove(state),
            "review.reject" => HandleReviewReject(state),
            "release.confirm" => HandleRelease(state),
            _ => Denied("Action is not permitted."),
        };
    }

    public IEnumerable<SseSessionEventV1> GetSessionEvents(SyntheticSessionRecord session, string sessionId)
    {
        if (!string.Equals(sessionId, SyntheticSessionId, StringComparison.Ordinal))
        {
            yield break;
        }

        var state = GetState(session.ScenarioId);
        if (state.SessionLifecycle != "active")
        {
            yield break;
        }

        state.SessionSequence++;
        yield return new SseSessionEventV1(
            BrowserSchemaVersion.V1,
            "session.agent.fragment.v1",
            SyntheticSessionId,
            state.SessionSequence.ToString(),
            DateTimeOffset.UtcNow.ToString("O"),
            new SseSessionEventPayloadV1(
                "Agent response fragment",
                1,
                "msg.synthetic.agent.001",
                "Thank you for your response. "));

        state.SessionSequence++;
        yield return new SseSessionEventV1(
            BrowserSchemaVersion.V1,
            "session.agent.complete.v1",
            SyntheticSessionId,
            state.SessionSequence.ToString(),
            DateTimeOffset.UtcNow.ToString("O"),
            new SseSessionEventPayloadV1(
                "Agent response complete",
                null,
                "msg.synthetic.agent.001",
                "This is a synthetic demonstration."));
    }

    private BrowserCommandResultV1 HandleSaveDraft(SyntheticScenarioState state)
    {
        state.ActivityVersion++;
        return Success(state, "Draft saved.");
    }

    private BrowserCommandResultV1 HandleActivate(SyntheticScenarioState state)
    {
        state.ActivityLifecycle = "activated";
        state.ActivityVersion++;
        return Success(state, "Cohort activated.");
    }

    private BrowserCommandResultV1 HandleAssign(SyntheticScenarioState state)
    {
        state.EnrollmentCreated = true;
        return Success(state, "Participant assigned.");
    }

    private BrowserCommandResultV1 HandleSubmit(SyntheticScenarioState state, BrowserCommandEnvelopeV1 command)
    {
        state.SubmissionAccepted = true;
        state.Transcript.Clear();
        var preview = command.Payload?.GetValueOrDefault("submission_text");
        if (!string.IsNullOrWhiteSpace(preview))
        {
            state.SubmissionPreview = preview;
        }

        return Success(state, "Submission accepted as version 1.");
    }

    private BrowserCommandResultV1 HandleStartAttempt(SyntheticScenarioState state)
    {
        state.AttemptStarted = true;
        state.SessionLifecycle = "active";
        state.Transcript.Add(new SessionTranscriptItemV1("msg.sys.001", "system", "Session started. Good luck.", "confirmed", DateTimeOffset.UtcNow.ToString("O")));
        return Success(state, "Attempt started.");
    }

    private BrowserCommandResultV1 HandleSendMessage(SyntheticScenarioState state, BrowserCommandEnvelopeV1 command)
    {
        var text = command.Payload?.GetValueOrDefault("message_text") ?? "";
        state.Transcript.Add(new SessionTranscriptItemV1(
            $"msg.part.{state.Transcript.Count + 1}",
            "participant",
            text,
            "confirmed",
            DateTimeOffset.UtcNow.ToString("O")));
        state.SessionVersion++;
        return Success(state, "Message sent.");
    }

    private BrowserCommandResultV1 HandlePause(SyntheticScenarioState state)
    {
        state.SessionLifecycle = "paused";
        return Success(state, "Session paused.");
    }

    private BrowserCommandResultV1 HandleResume(SyntheticScenarioState state)
    {
        state.SessionLifecycle = "active";
        return Success(state, "Session resumed.");
    }

    private BrowserCommandResultV1 HandleComplete(SyntheticScenarioState state)
    {
        state.SessionLifecycle = "completed";
        state.ReviewLifecycle = "ready_for_review";
        return Success(state, "Session completed.");
    }

    private BrowserCommandResultV1 HandleReviewApprove(SyntheticScenarioState state)
    {
        state.ReviewLifecycle = "approved";
        state.ReleaseLifecycle = "ready";
        return Success(state, "Review decision recorded: Approved.");
    }

    private BrowserCommandResultV1 HandleReviewReject(SyntheticScenarioState state)
    {
        state.ReviewLifecycle = "rejected";
        return Success(state, "Review decision recorded: Rejected.");
    }

    private BrowserCommandResultV1 HandleRelease(SyntheticScenarioState state)
    {
        state.ReleaseLifecycle = "released";
        state.ResultLifecycle = "released";
        return Success(state, "Result released.");
    }

    private static BrowserCommandResultV1 Success(SyntheticScenarioState state, string message) =>
        new(BrowserSchemaVersion.V1, "succeeded", Guid.NewGuid().ToString("N"), state.ActivityVersion, state.ActivityLifecycle, "none", message);

    private static BrowserCommandResultV1 Denied(string message) =>
        new(BrowserSchemaVersion.V1, "denied", Guid.NewGuid().ToString("N"), null, null, "contact_administrator", message);

    private static BrowserCommandResultV1 Conflict(string message) =>
        new(BrowserSchemaVersion.V1, "conflict", Guid.NewGuid().ToString("N"), null, null, "reconcile", message);

    private static BrowserCommandResultV1 Uncertain(string message) =>
        new(BrowserSchemaVersion.V1, "uncertain", Guid.NewGuid().ToString("N"), null, null, "reconcile", message);

    private SyntheticScenarioState GetState(string scenarioId) =>
        _scenarioStates.GetOrAdd(scenarioId, _ => new SyntheticScenarioState());

    private bool IsAccessDenied(SyntheticSessionRecord session)
    {
        if (session.ScenarioId == SyntheticScenarioIds.DeniedAccess)
        {
            return true;
        }

        return GetState(session.ScenarioId).PermissionRevoked;
    }

    private void RequireCapability(SyntheticSessionRecord session, string capability)
    {
        if (IsAccessDenied(session))
        {
            throw new SyntheticAccessDeniedException();
        }

        if (!ResolveCapabilities(session.ActorStage).Contains(capability))
        {
            throw new SyntheticAccessDeniedException();
        }
    }

    private void EnsureEnabled()
    {
        if (!IsEnabled)
        {
            throw new SyntheticBrowserDisabledException();
        }
    }

    private static void ValidateScenario(string scenarioId, string actorStage)
    {
        var validScenarios = new HashSet<string>(StringComparer.Ordinal)
        {
            SyntheticScenarioIds.CampaignFullJourney,
            SyntheticScenarioIds.DeniedAccess,
            SyntheticScenarioIds.StaleRevision,
            SyntheticScenarioIds.PermissionRevoked,
            SyntheticScenarioIds.UncertainReconciliation,
        };

        if (!validScenarios.Contains(scenarioId))
        {
            throw new ArgumentException("Unknown scenario.", nameof(scenarioId));
        }

        var validStages = new HashSet<string>(StringComparer.Ordinal)
        {
            SyntheticActorStages.Administrator,
            SyntheticActorStages.Participant,
            SyntheticActorStages.Reviewer,
            SyntheticActorStages.ReleaseActor,
        };

        if (!validStages.Contains(actorStage))
        {
            throw new ArgumentException("Unknown actor stage.", nameof(actorStage));
        }
    }

    private static string ResolveActorId(string actorStage) => actorStage switch
    {
        SyntheticActorStages.Administrator => "actor.synthetic.admin",
        SyntheticActorStages.Participant => "actor.synthetic.participant",
        SyntheticActorStages.Reviewer => "actor.synthetic.reviewer",
        SyntheticActorStages.ReleaseActor => "actor.synthetic.release",
        _ => "actor.synthetic.unknown",
    };

    private static string ResolveActorDisplayName(string actorStage) => actorStage switch
    {
        SyntheticActorStages.Administrator => "Synthetic Administrator",
        SyntheticActorStages.Participant => "Synthetic Participant",
        SyntheticActorStages.Reviewer => "Synthetic Reviewer",
        SyntheticActorStages.ReleaseActor => "Synthetic Release Actor",
        _ => "Synthetic Actor",
    };

    private static IReadOnlyList<string> ResolveCapabilities(string actorStage) => actorStage switch
    {
        SyntheticActorStages.Administrator => ["activity_admin", "governance", "session_control"],
        SyntheticActorStages.Participant => ["participant"],
        SyntheticActorStages.Reviewer => ["reviewer"],
        SyntheticActorStages.ReleaseActor => ["release"],
        _ => [],
    };

    private static NavigationDestinationV1 Nav(
        string id, string label, string route, string tier, bool available, string? reason) =>
        new(id, label, route, tier, available, reason);

    private static PermittedActionV1 Action(string id, string label, string? desc, bool destructive) =>
        new(id, label, desc, destructive);

    private static string FormatActivityStatus(SyntheticScenarioState state) => state.ActivityLifecycle switch
    {
        "draft" => "Draft",
        "activated" => "Activated",
        _ => state.ActivityLifecycle,
    };

    private static string FormatResultStatus(SyntheticScenarioState state) => state.ResultLifecycle switch
    {
        "neutral_pre_release" => "Not yet available",
        "released" => "Released",
        "corrected" => "Corrected",
        "unavailable" => "Unavailable",
        _ => state.ResultLifecycle,
    };

    private static string ResolveAssignmentStatus(SyntheticScenarioState state)
    {
        if (!state.SubmissionAccepted)
        {
            return "Submission required";
        }

        if (!state.AttemptStarted)
        {
            return "Ready to start";
        }

        return state.SessionLifecycle switch
        {
            "active" => "Session in progress",
            "paused" => "Session paused",
            "completed" => "Session completed",
            _ => "Assigned",
        };
    }

    private static string ResolveParticipantNextAction(SyntheticScenarioState state)
    {
        if (!state.SubmissionAccepted)
        {
            return "Submit work";
        }

        if (!state.AttemptStarted)
        {
            return "Start Attempt";
        }

        return "Continue Session";
    }

    private static string ResolveAssignmentLifecycle(SyntheticScenarioState state)
    {
        if (!state.EnrollmentCreated)
        {
            return "no_assignment";
        }

        if (!state.SubmissionAccepted)
        {
            return "submission_required";
        }

        if (!state.AttemptStarted)
        {
            return "ready_to_start";
        }

        return state.SessionLifecycle;
    }

    private static IReadOnlyList<ReadinessCategoryV1> BuildReadinessCategories(SyntheticScenarioState state)
    {
        var agentReady = true;
        var harnessReady = true;
        var rubricReady = state.ActivityLifecycle != "draft" || state.ActivityVersion > 1;

        return
        [
            new("agent", "Agent source", agentReady ? "Ready" : "Missing", !agentReady, null),
            new("harness", "Harness source", harnessReady ? "Ready" : "Missing", !harnessReady, null),
            new("rubric", "Evaluation rubric", rubricReady ? "Ready" : "Incomplete", !rubricReady, "Rubric requires saved draft"),
            new("timing", "Timing policy", "Ready", false, null),
        ];
    }
}

public sealed class SyntheticBrowserDisabledException : Exception
{
    public SyntheticBrowserDisabledException() : base("Synthetic browser adapter is disabled.") { }
}

public sealed class SyntheticAccessDeniedException : Exception
{
    public SyntheticAccessDeniedException() : base("Access denied.") { }
}

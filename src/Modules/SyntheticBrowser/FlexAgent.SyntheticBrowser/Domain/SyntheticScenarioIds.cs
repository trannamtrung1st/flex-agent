namespace FlexAgent.SyntheticBrowser.Domain;

public static class SyntheticScenarioIds
{
    public const string CampaignFullJourney = "campaign-full-journey";
    public const string DeniedAccess = "denied-access";
    public const string StaleRevision = "stale-revision";
    public const string PermissionRevoked = "permission-revoked";
    public const string UncertainReconciliation = "uncertain-reconciliation";
    public const string SessionOpeningClosing = "session-opening-closing";
    public const string SessionParticipantNoAction = "session-participant-no-action";
    public const string SessionTimerNoAction = "session-timer-no-action";
    public const string SessionRejectedDecision = "session-rejected-decision";
    public const string SessionAcceptedEffectFailure = "session-accepted-effect-failure";
    public const string SessionExecutionFailure = "session-execution-failure";
    public const string SessionDefaultTimer = "session-default-timer";
    public const string SessionTimerReplacementAccepted = "session-timer-replacement-accepted";
    public const string SessionTimerReplacementRejected = "session-timer-replacement-rejected";
    public const string SessionTimerReplacementOmitted = "session-timer-replacement-omitted";
    public const string SessionDuplicateConcurrentRevision = "session-duplicate-concurrent-revision";
    public const string SessionTimerVisibleWork = "session-timer-visible-work";
    public const string SessionPauseResume = "session-pause-resume";
    public const string SessionReconnect = "session-reconnect";
    public const string SessionCutoff = "session-cutoff";

    public static readonly HashSet<string> Known = new(StringComparer.Ordinal)
    {
        CampaignFullJourney,
        DeniedAccess,
        StaleRevision,
        PermissionRevoked,
        UncertainReconciliation,
        SessionOpeningClosing,
        SessionParticipantNoAction,
        SessionTimerNoAction,
        SessionRejectedDecision,
        SessionAcceptedEffectFailure,
        SessionExecutionFailure,
        SessionDefaultTimer,
        SessionTimerReplacementAccepted,
        SessionTimerReplacementRejected,
        SessionTimerReplacementOmitted,
        SessionDuplicateConcurrentRevision,
        SessionTimerVisibleWork,
        SessionPauseResume,
        SessionReconnect,
        SessionCutoff,
    };
}

public static class SyntheticActorStages
{
    public const string Administrator = "administrator";
    public const string Participant = "participant";
    public const string Reviewer = "reviewer";
    public const string ReleaseActor = "release_actor";
}

namespace FlexAgent.SyntheticBrowser.Domain;

public static class SyntheticScenarioIds
{
    public const string CampaignFullJourney = "campaign-full-journey";
    public const string DeniedAccess = "denied-access";
    public const string StaleRevision = "stale-revision";
    public const string PermissionRevoked = "permission-revoked";
    public const string UncertainReconciliation = "uncertain-reconciliation";
}

public static class SyntheticActorStages
{
    public const string Administrator = "administrator";
    public const string Participant = "participant";
    public const string Reviewer = "reviewer";
    public const string ReleaseActor = "release_actor";
}

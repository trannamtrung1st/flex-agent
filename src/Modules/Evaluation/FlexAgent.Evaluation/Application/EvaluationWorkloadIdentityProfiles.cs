namespace FlexAgent.Evaluation.Application;

/// <summary>
/// Workload identity profile identifiers mirrored from IdentityAccess without
/// taking a module dependency. Values must stay aligned with
/// <c>FlexAgent.IdentityAccess.Application.WorkloadIdentityProfiles</c>.
/// </summary>
public static class EvaluationWorkloadIdentityProfiles
{
    public const string SyntheticConfiguredActor = "synthetic.configured_actor";
    public const string OAuthClientCredentialsJwt = "oauth_client_credentials_jwt";
}

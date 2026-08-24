using FlexAgent.IdentityAccess.Domain;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public static class EnrollmentAuthenticationPolicy
{
    private static readonly HashSet<string> AdministratorActions =
    [
        EnrollmentAuthorizationActions.CandidateRead,
        EnrollmentAuthorizationActions.List,
        EnrollmentAuthorizationActions.Read,
        EnrollmentAuthorizationActions.Assign,
        EnrollmentAuthorizationActions.Suspend,
        EnrollmentAuthorizationActions.Restore,
        EnrollmentAuthorizationActions.Close,
        EnrollmentAuthorizationActions.Revoke,
        EnrollmentAuthorizationActions.ReadAccommodation,
        EnrollmentAuthorizationActions.GrantAccommodation,
        EnrollmentAuthorizationActions.DecideAccommodation,
        EnrollmentAuthorizationActions.RevokeAccommodation,
    ];

    private static readonly HashSet<string> AllowedAcr =
        ["http://schemas.openid.net/pape/policies/2007/06/multi-factor", "mfa"];

    private static readonly HashSet<string> AllowedAmr = ["mfa", "otp", "hwk", "pwd mfa"];

    public static bool IsAdministratorAction(string action) =>
        AdministratorActions.Contains(action);

    public static bool IsAdministratorDestinationGrant(string action) =>
        IsAdministratorAction(action)
        || action is
            AssessmentConfiguration.Domain.AssessmentAuthorizationActions.CreateActivity
            or AssessmentConfiguration.Domain.AssessmentAuthorizationActions.SaveActivity
            or AssessmentConfiguration.Domain.AssessmentAuthorizationActions.CheckReadiness
            or AssessmentConfiguration.Domain.AssessmentAuthorizationActions.ActivateCohort
            or AssessmentConfiguration.Domain.AssessmentAuthorizationActions.SelectSources
            or AssessmentConfiguration.Domain.AssessmentAuthorizationActions.ReadActivity
            or AssessmentConfiguration.Domain.AssessmentAuthorizationActions.ReconcileActivation
            or AssessmentConfiguration.Domain.AssessmentAuthorizationActions.ReadBaseline
            or AssessmentConfiguration.Domain.AssessmentAuthorizationActions.ReadBaselineProvenance;

    public static bool IsParticipantOnlyGrant(string action) =>
        action is EnrollmentAuthorizationActions.Discover or EnrollmentAuthorizationActions.Receive;

    public static string? Evaluate(EnrollmentActorContext actor, string action)
    {
        if (string.Equals(action, EnrollmentAuthorizationActions.Discover, StringComparison.Ordinal)
            || string.Equals(action, EnrollmentAuthorizationActions.Receive, StringComparison.Ordinal))
        {
            return null;
        }

        if (!AdministratorActions.Contains(action))
        {
            return EnrollmentFailureCodes.Denied;
        }

        if (!string.Equals(
                actor.Relationship,
                AuthenticationStrengthEvaluator.AdministratorRelationship,
                StringComparison.Ordinal))
        {
            return EnrollmentFailureCodes.Denied;
        }

        return AuthenticationStrengthEvaluator.Evaluate(
            actor.Strength,
            actor.Relationship,
            action,
            AllowedAcr,
            AllowedAmr) is { } strength
            ? EnrollmentFailureCodes.Denied
            : null;
    }

    public static string AuditClass(string action, bool isDenial) =>
        AdministratorActions.Contains(action) && action is
            EnrollmentAuthorizationActions.Assign
            or EnrollmentAuthorizationActions.Suspend
            or EnrollmentAuthorizationActions.Restore
            or EnrollmentAuthorizationActions.Close
            or EnrollmentAuthorizationActions.Revoke
            or EnrollmentAuthorizationActions.GrantAccommodation
            or EnrollmentAuthorizationActions.DecideAccommodation
            or EnrollmentAuthorizationActions.RevokeAccommodation
            ? EnrollmentAuditClasses.RequiredDurable
            : isDenial && AdministratorActions.Contains(action)
                ? EnrollmentAuditClasses.Bufferable
                : action is EnrollmentAuthorizationActions.CandidateRead
                    or EnrollmentAuthorizationActions.List
                    or EnrollmentAuthorizationActions.Read
                    or EnrollmentAuthorizationActions.ReadAccommodation
                    ? EnrollmentAuditClasses.Bufferable
                    : EnrollmentAuditClasses.OperationalSample;
}

namespace FlexAgent.Submissions.Domain;

public static class EnrollmentLifecycle
{
    public static bool TryValidate(
        string currentStatus,
        string targetStatus,
        string reasonCode,
        out string? outcomeCode)
    {
        outcomeCode = null;
        var allowed = (currentStatus, targetStatus, reasonCode) switch
        {
            (EnrollmentStates.Active, EnrollmentStates.Suspended, EnrollmentReasonCodes.TemporaryRestriction)
                => EnrollmentOutcomes.Suspended,
            (EnrollmentStates.Active, EnrollmentStates.Closed, EnrollmentReasonCodes.ActivityOrEnrollmentEnd)
                => EnrollmentOutcomes.Closed,
            (EnrollmentStates.Active, EnrollmentStates.Revoked, EnrollmentReasonCodes.AccessRevoked)
                => EnrollmentOutcomes.Revoked,
            (EnrollmentStates.Suspended, EnrollmentStates.Active, EnrollmentReasonCodes.RestrictionRemoved)
                => EnrollmentOutcomes.Restored,
            (EnrollmentStates.Suspended, EnrollmentStates.Closed, EnrollmentReasonCodes.ActivityOrEnrollmentEnd)
                => EnrollmentOutcomes.Closed,
            (EnrollmentStates.Suspended, EnrollmentStates.Revoked, EnrollmentReasonCodes.AccessRevoked)
                => EnrollmentOutcomes.Revoked,
            _ => null,
        };

        if (allowed is null)
        {
            outcomeCode = currentStatus is EnrollmentStates.Closed or EnrollmentStates.Revoked
                ? EnrollmentFailureCodes.Terminal
                : EnrollmentFailureCodes.InvalidReason;
            return false;
        }

        outcomeCode = allowed;
        return true;
    }

    public static string RequiredReason(string operationKind) => operationKind switch
    {
        EnrollmentOperationKinds.Suspend => EnrollmentReasonCodes.TemporaryRestriction,
        EnrollmentOperationKinds.Restore => EnrollmentReasonCodes.RestrictionRemoved,
        EnrollmentOperationKinds.Close => EnrollmentReasonCodes.ActivityOrEnrollmentEnd,
        EnrollmentOperationKinds.Revoke => EnrollmentReasonCodes.AccessRevoked,
        _ => string.Empty,
    };

    public static string TargetStatus(string operationKind) => operationKind switch
    {
        EnrollmentOperationKinds.Suspend => EnrollmentStates.Suspended,
        EnrollmentOperationKinds.Restore => EnrollmentStates.Active,
        EnrollmentOperationKinds.Close => EnrollmentStates.Closed,
        EnrollmentOperationKinds.Revoke => EnrollmentStates.Revoked,
        _ => string.Empty,
    };
}

public static class EnrollmentProjection
{
    public static string Visibility(string status) => status switch
    {
        EnrollmentStates.Active => EnrollmentVisibilityStates.Current,
        EnrollmentStates.Suspended => EnrollmentVisibilityStates.Restricted,
        _ => EnrollmentVisibilityStates.Unavailable,
    };

    public static bool PermitsNewIntakeOrStart(string status) =>
        string.Equals(status, EnrollmentStates.Active, StringComparison.Ordinal);

    public static bool IsLive(string status) =>
        status is EnrollmentStates.Active or EnrollmentStates.Suspended;

    public static IReadOnlyList<string> AdministratorActions(
        string status,
        IReadOnlySet<string> granted,
        bool accommodationPolicyAvailable = true)
    {
        var actions = new List<string>();
        if (!IsLive(status))
        {
            return actions;
        }

        if (status == EnrollmentStates.Active
            && granted.Contains(EnrollmentAuthorizationActions.Suspend))
        {
            actions.Add(EnrollmentClientActions.Suspend);
        }

        if (status == EnrollmentStates.Suspended
            && granted.Contains(EnrollmentAuthorizationActions.Restore))
        {
            actions.Add(EnrollmentClientActions.Restore);
        }

        if (granted.Contains(EnrollmentAuthorizationActions.Close))
        {
            actions.Add(EnrollmentClientActions.Close);
        }

        if (granted.Contains(EnrollmentAuthorizationActions.Revoke))
        {
            actions.Add(EnrollmentClientActions.Revoke);
        }

        if (status == EnrollmentStates.Active
            && granted.Contains(EnrollmentAuthorizationActions.GrantAccommodation)
            && accommodationPolicyAvailable)
        {
            actions.Add(EnrollmentClientActions.RequestAccommodation);
        }

        if (status == EnrollmentStates.Active
            && granted.Contains(EnrollmentAuthorizationActions.RevokeAccommodation))
        {
            actions.Add(EnrollmentClientActions.RevokeAccommodation);
        }

        if (granted.Contains(EnrollmentAuthorizationActions.DecideAccommodation)
            && accommodationPolicyAvailable)
        {
            actions.Add(EnrollmentClientActions.ApproveException);
            actions.Add(EnrollmentClientActions.RejectException);
        }

        return actions;
    }

    public static IReadOnlyList<string> ParticipantActions(string status, bool summaryAvailable)
    {
        if (status == EnrollmentStates.Active && summaryAvailable)
        {
            return [EnrollmentClientActions.OpenAssignment];
        }

        if (status == EnrollmentStates.Suspended)
        {
            return [EnrollmentClientActions.ReturnToMyWork];
        }

        return [];
    }
}

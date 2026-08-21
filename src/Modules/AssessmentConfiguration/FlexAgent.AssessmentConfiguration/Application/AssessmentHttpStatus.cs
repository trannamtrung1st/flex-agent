using FlexAgent.AssessmentConfiguration.Domain;
using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.AssessmentConfiguration.Application;

public static class AssessmentHttpStatus
{
    public static int ForDraftMutation(bool succeeded, string outcomeCode, int successStatus = 200)
    {
        if (succeeded)
        {
            return successStatus;
        }

        if (IsAccessFailure(outcomeCode))
        {
            return 403;
        }

        return IsClientFieldFailure(outcomeCode) ? 400 : 409;
    }

    private static bool IsClientFieldFailure(string outcomeCode) =>
        outcomeCode is AssessmentFailureCodes.InvalidField
            or AssessmentFailureCodes.MissingSource
            or AssessmentFailureCodes.MutableSource
            or AssessmentFailureCodes.RevokedSource
            or AssessmentFailureCodes.UnavailableSource
            or AssessmentFailureCodes.WrongScope
            or AssessmentFailureCodes.DigestMismatch
            or AssessmentFailureCodes.Incompatible
            or AssessmentFailureCodes.InvalidMemory
            or AssessmentFailureCodes.InvalidTiming
            or AssessmentFailureCodes.ProhibitedCapability
            or AssessmentFailureCodes.Widening
            or AssessmentFailureCodes.MissingException;

    public static bool IsAccessFailure(string outcomeCode) =>
        string.Equals(outcomeCode, AssessmentFailureCodes.Denied, StringComparison.Ordinal)
        || string.Equals(outcomeCode, HumanAuthenticationReasonCodes.UnrecognizedAuthenticationStrength, StringComparison.Ordinal)
        || string.Equals(outcomeCode, HumanAuthenticationReasonCodes.InsufficientAuthenticationStrength, StringComparison.Ordinal);
}

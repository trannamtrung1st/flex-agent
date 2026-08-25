namespace FlexAgent.Api;

public static class AssessmentActivateRequestValidator
{
    public static bool IsValid(Guid expectedRevisionId, long expectedRevisionNumber) =>
        expectedRevisionId != Guid.Empty
        && expectedRevisionNumber >= 1;
}

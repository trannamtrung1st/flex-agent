namespace FlexAgent.Submissions.Domain;

public sealed record ProtectedArtifactCapability(
    Guid CapabilityId,
    Guid OrganizationId,
    Guid ActorId,
    Guid EnrollmentId,
    Guid VersionId,
    Guid ItemId,
    string Action,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RedeemedAtUtc);

public static class ProtectedArtifactCapabilityRules
{
    public static string? Redeem(
        ProtectedArtifactCapability capability,
        Guid organizationId,
        Guid actorId,
        Guid enrollmentId,
        Guid versionId,
        Guid itemId,
        string action,
        DateTimeOffset nowUtc)
    {
        if (capability.OrganizationId != organizationId
            || capability.ActorId != actorId
            || capability.EnrollmentId != enrollmentId
            || capability.VersionId != versionId
            || capability.ItemId != itemId
            || !string.Equals(capability.Action, action, StringComparison.Ordinal))
        {
            return SubmissionFailureCodes.CapabilityMismatch;
        }

        if (nowUtc >= capability.ExpiresAtUtc)
        {
            return SubmissionFailureCodes.CapabilityExpired;
        }

        if (capability.RedeemedAtUtc is not null
            && string.Equals(action, SubmissionPermittedActions.DownloadItem, StringComparison.Ordinal))
        {
            return SubmissionFailureCodes.CapabilityMismatch;
        }

        return null;
    }
}

public static class SubmissionTelemetryBands
{
    public static string ByteBand(long byteCount) => byteCount switch
    {
        <= 0 => "0",
        <= 1_024 => "1_1kib",
        <= 1_048_576 => "1kib_1mib",
        <= 10_485_760 => "1mib_10mib",
        _ => "over_10mib",
    };

    public static string CountBand(int count) => count switch
    {
        <= 0 => "0",
        1 => "1",
        <= 5 => "2_5",
        <= 10 => "6_10",
        _ => "over_10",
    };
}

namespace FlexAgent.IdentityAccess.Application;

public sealed record HumanDisplayCandidate(Guid ActorId, string DisplayLabel);

public sealed record HumanDisplayCandidatePage(
    IReadOnlyList<HumanDisplayCandidate> Items,
    bool HasMore);

public interface IHumanDisplayProfileDirectory
{
    Task<HumanDisplayCandidatePage> ListEligibleAsync(
        Guid organizationId,
        string requiredAction,
        string? prefix,
        Guid? afterActorId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<HumanDisplayCandidate?> RevalidateEligibleAsync(
        Guid organizationId,
        Guid actorId,
        string requiredAction,
        object? commitTransaction,
        CancellationToken cancellationToken = default);

    Task<string?> FindDisplayLabelAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken cancellationToken = default);
}

public static class HumanDisplayLabel
{
    public const int MaxLength = 80;

    public static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength || trimmed.Any(char.IsControl))
        {
            return null;
        }

        return trimmed;
    }
}

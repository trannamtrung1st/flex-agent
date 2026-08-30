using System.Text;

namespace FlexAgent.IdentityAccess.Domain;

public static class SeatedOperatorDisplayName
{
    public const int MaxLength = 120;

    public static string? Compose(string? givenName, string? familyName, string? preferredUsername)
    {
        var given = Normalize(givenName);
        var family = Normalize(familyName);
        if (given is not null || family is not null)
        {
            return Truncate(given is null ? family! : family is null ? given : $"{given} {family}");
        }

        var username = Normalize(preferredUsername);
        return username is null ? null : Truncate(username);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var collapsed = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = collapsed.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                collapsed.Append(' ');
                pendingSpace = false;
            }

            collapsed.Append(character);
        }

        return collapsed.Length == 0 ? null : collapsed.ToString();
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLength ? value : value[..MaxLength].TrimEnd();
}

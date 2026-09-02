using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Submissions.Domain;

public static class AttemptSubmissionProvenance
{
    public static string ForAcceptedVersion(AcceptedSubmissionVersion version)
    {
        var lines = new List<string>
        {
            version.VersionId.ToString("D"),
            version.VersionNumber.ToString(CultureInfo.InvariantCulture),
        };
        if (version.Items.Count == 0)
        {
            lines.Add($"policy:{version.PolicyDigest}");
        }
        else
        {
            foreach (var item in version.Items.OrderBy(entry => entry.ItemId))
            {
                lines.Add($"{item.ItemId:D}:{item.ContentDigest}");
            }
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines))))
            .ToLowerInvariant();
    }
}

using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.Submissions.Domain;

public static class SubmissionCommandDigest
{
    public static string Compute(string operationKind, params string[] parts)
    {
        var builder = new StringBuilder(operationKind);
        foreach (var part in parts)
        {
            builder.Append('|');
            builder.Append(part);
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public static class SubmissionAuthorizationActions
{
    public const string BeginIntake = "submissions.intake.begin";
    public const string CompleteIntakeItem = "submissions.intake.complete_item";
    public const string CancelIntake = "submissions.intake.cancel";
    public const string FinalizeIntake = "submissions.intake.finalize";
    public const string ReadSubmission = "submissions.submission.read";
    public const string PreviewItem = "submissions.submission.preview";
    public const string DownloadItem = "submissions.submission.download";
}

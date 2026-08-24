using System.Text;

namespace FlexAgent.Submissions.Domain;

public static class MaterialContentValidator
{
    public static MaterialValidationResult ValidateUtf8(ReadOnlySpan<byte> content)
    {
        if (!System.Text.Unicode.Utf8.IsValid(content))
        {
            return MaterialValidationResult.Fail(SubmissionFailureCodes.InvalidEncoding);
        }

        return MaterialValidationResult.Pass();
    }

    public static MaterialValidationResult ValidateDirectText(ReadOnlySpan<byte> content, NormalizedMaterialPolicy policy)
    {
        var limit = policy.Categories.FirstOrDefault(c => c.Category == MaterialCategories.DirectText);
        if (limit is null || !limit.Available)
        {
            return MaterialValidationResult.Fail(SubmissionFailureCodes.InvalidCategory);
        }

        if (content.Length > limit.MaxBytes)
        {
            return MaterialValidationResult.Fail(SubmissionFailureCodes.Oversized);
        }

        return ValidateUtf8(content);
    }

    public static MaterialValidationResult ValidateAttachment(
        ReadOnlySpan<byte> content,
        string? filename,
        string? declaredMime,
        NormalizedMaterialPolicy policy)
    {
        var extension = Path.GetExtension(filename ?? string.Empty).ToLowerInvariant();
        var isMarkdown = extension == ".md";
        var category = isMarkdown
            ? MaterialCategories.MarkdownAttachment
            : MaterialCategories.PlainTextAttachment;

        var limit = policy.Categories.FirstOrDefault(c => c.Category == category);
        if (limit is null || !limit.Available)
        {
            return MaterialValidationResult.Fail(SubmissionFailureCodes.InvalidCategory);
        }

        if (!limit.AllowedExtensions.Contains(extension, StringComparer.Ordinal))
        {
            return MaterialValidationResult.Fail(SubmissionFailureCodes.InvalidCategory);
        }

        if (content.Length > limit.MaxBytes)
        {
            return MaterialValidationResult.Fail(SubmissionFailureCodes.Oversized);
        }

        var utf8 = ValidateUtf8(content);
        if (!utf8.Succeeded)
        {
            return utf8;
        }

        if (!string.IsNullOrWhiteSpace(declaredMime)
            && !limit.DetectedContentTypes.Contains(declaredMime, StringComparer.OrdinalIgnoreCase))
        {
            return MaterialValidationResult.Fail(SubmissionFailureCodes.InvalidContentType);
        }

        return MaterialValidationResult.Pass(category);
    }

    public static MaterialScanOutcome EvaluateScanner(MaterialScannerMode mode, MaterialScanOutcome? scanOutcome)
    {
        if (mode == MaterialScannerMode.DisabledByApprovedPolicy)
        {
            return MaterialScanOutcome.Clean;
        }

        return scanOutcome switch
        {
            MaterialScanOutcome.Clean => MaterialScanOutcome.Clean,
            MaterialScanOutcome.Rejected => MaterialScanOutcome.Rejected,
            MaterialScanOutcome.Unavailable => MaterialScanOutcome.Unavailable,
            MaterialScanOutcome.Inconclusive => MaterialScanOutcome.Inconclusive,
            _ => MaterialScanOutcome.Unavailable,
        };
    }
}

public sealed record MaterialValidationResult(bool Succeeded, string OutcomeCode, string? DetectedCategory = null)
{
    public static MaterialValidationResult Pass(string? category = null) =>
        new(true, "valid", category);

    public static MaterialValidationResult Fail(string code) =>
        new(false, code);
}

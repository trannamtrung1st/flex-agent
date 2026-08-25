namespace FlexAgent.Api;

public sealed class EnrollmentCursorSigningOptions
{
    public const string SectionName = "Enrollment:CursorSigning";

    public string CurrentKeyId { get; set; } = "current";

    public string? PreviousKeyId { get; set; }
}

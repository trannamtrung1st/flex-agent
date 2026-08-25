namespace FlexAgent.Api;

public sealed class SessionEventTestIdentityOptions
{
    public const string SectionName = "SessionEvents:TestIdentity";

    public bool Enabled { get; set; }

    public string? HarnessApiKey { get; set; }
}

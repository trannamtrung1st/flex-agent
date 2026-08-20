using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlexAgent.Sessions.OpenRouter.Tests;

internal sealed record OpenRouterSanitizedQualificationRecord(
    string SchemaVersion,
    string RequestPolicyVersion,
    string AdapterContractVersion,
    string QualificationScope,
    string Model,
    string ProviderIdentity,
    string ProfileDigest,
    string AdapterConfigurationDigest,
    int? ControlHttp,
    string ControlClass,
    string ControlCache,
    string? ControlFinishReason,
    int? ControlTokensIn,
    int? ControlTokensOut,
    int? ContentHttp,
    string ContentClass,
    string ContentCache,
    string? ContentFinishReason,
    int? ContentTokensIn,
    int? ContentTokensOut,
    string QualificationOutcome,
    string? DenialReason)
{
    public const string CurrentSchemaVersion = "openrouter.sanitized-qualification.v1";

    public string ToSanitizedJson() =>
        JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            });
}

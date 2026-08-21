using System.Text.Json;

namespace FlexAgent.Sessions.OpenAiCompatible;

public sealed record OpenAiCompatibleQualificationRecord(
    string AdapterKind,
    string AdapterContractVersion,
    string ProfileId,
    string ProfileVersion,
    string ProfileDigest,
    string AdapterConfigurationDigest,
    string QualifiedFor);

public static class OpenAiCompatibleQualificationRecords
{
    public const string ExactProfile = OpenAiCompatibleAdapterContracts.QualificationScope;
    public const string DoNotEnable = "do_not_enable";

    public static OpenAiCompatibleQualificationRecord Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var info = new FileInfo(path);
        if (!info.Exists || info.Length <= 0 || info.Length > 262_144)
        {
            throw new ArgumentException("Qualification record is missing, empty, or oversized.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Qualification record must be a JSON object.");
        }

        RejectUnexpectedProperties(document.RootElement);
        return new OpenAiCompatibleQualificationRecord(
            RequiredString(document.RootElement, "adapterKind"),
            RequiredString(document.RootElement, "adapterContractVersion"),
            RequiredString(document.RootElement, "profileId"),
            RequiredString(document.RootElement, "profileVersion"),
            RequiredString(document.RootElement, "profileDigest"),
            RequiredString(document.RootElement, "adapterConfigurationDigest"),
            RequiredString(document.RootElement, "qualifiedFor"));
    }

    public static bool TryAccept(
        OpenAiCompatibleQualificationRecord record,
        OpenAiCompatibleInstalledConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(configuration);
        if (IsNonEnableableIdentity(configuration.Profile.ProfileId)
            || string.Equals(record.QualifiedFor, DoNotEnable, StringComparison.Ordinal)
            || record.AdapterConfigurationDigest.Trim('0').Length == 0)
        {
            return false;
        }

        return string.Equals(record.AdapterKind, OpenAiCompatibleAdapterContracts.AdapterKind, StringComparison.Ordinal)
            && string.Equals(record.AdapterContractVersion, OpenAiCompatibleAdapterContracts.AdapterContractVersion, StringComparison.Ordinal)
            && string.Equals(record.QualifiedFor, ExactProfile, StringComparison.Ordinal)
            && string.Equals(record.ProfileId, configuration.Profile.ProfileId, StringComparison.Ordinal)
            && string.Equals(record.ProfileVersion, configuration.Profile.ProfileVersion, StringComparison.Ordinal)
            && string.Equals(record.ProfileDigest, configuration.Profile.ProfileDigest, StringComparison.Ordinal)
            && string.Equals(record.AdapterConfigurationDigest, configuration.AdapterConfigurationDigest, StringComparison.Ordinal);
    }

    public static bool IsNonEnableableIdentity(string profileId) =>
        profileId.Contains("example", StringComparison.OrdinalIgnoreCase)
        || profileId.Contains("do-not-enable", StringComparison.OrdinalIgnoreCase);

    private static readonly HashSet<string> AllowedQualificationProperties = new(StringComparer.Ordinal)
    {
        "adapterKind",
        "adapterContractVersion",
        "profileId",
        "profileVersion",
        "profileDigest",
        "adapterConfigurationDigest",
        "qualifiedFor",
    };

    private static void RejectUnexpectedProperties(JsonElement item)
    {
        foreach (var property in item.EnumerateObject())
        {
            if (!AllowedQualificationProperties.Contains(property.Name))
            {
                throw new ArgumentException(
                    $"Qualification record contains unexpected property '{property.Name}'.");
            }
        }
    }

    private static string RequiredString(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"Qualification record is missing '{name}'.");
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException($"Qualification record is missing '{name}'.");
        }

        return text;
    }
}

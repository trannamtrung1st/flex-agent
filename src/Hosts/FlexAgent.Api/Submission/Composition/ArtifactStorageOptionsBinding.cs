using FlexAgent.Submissions.Infrastructure.ObjectStorage;

namespace FlexAgent.Api;

internal static class ArtifactStorageOptionsBinding
{
    public const string SectionName = "ArtifactStorage";

    public static void Configure(S3ArtifactStoreOptions options, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.GetSection(SectionName).Bind(options);
        options.ForcePathStyle = true;
        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            options.BucketName = "flex-agent-artifacts";
        }

        if (string.IsNullOrWhiteSpace(options.AccessKeyId))
        {
            options.AccessKeyId = "access_key";
        }

        if (string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            options.SecretAccessKey = "secret_key";
        }
    }

    public static bool IsValidWhenConfigured(S3ArtifactStoreOptions options, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        if (string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            return false;
        }

        var productionLocked = environment.IsProduction() || environment.IsEnvironment("Staging");
        if (!productionLocked)
        {
            return true;
        }

        return !string.Equals(options.AccessKeyId, "access_key", StringComparison.Ordinal)
            && !string.Equals(options.SecretAccessKey, "secret_key", StringComparison.Ordinal);
    }

    public static S3ArtifactStoreOptions CreateSnapshot(IConfiguration configuration)
    {
        var options = new S3ArtifactStoreOptions();
        Configure(options, configuration);
        return options;
    }
}

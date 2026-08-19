using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterSecretSourceTests
{
    [Fact]
    public async Task Owner_only_unix_modes_are_required_and_group_readable_files_are_rejected()
    {
        Assert.SkipUnless(
            UnixOwnerOnlyMountedFileProviderSecretSource.PlatformSupportsUnixModes(),
            "Unix owner-only mode checks apply on the current provider-qualification target.");
        var cancellation = TestContext.Current.CancellationToken;

        var root = Directory.CreateTempSubdirectory("flex-agent-or-secrets-");
        try
        {
            var keyPath = Path.Combine(root.FullName, "openrouter-api-key");
            await File.WriteAllTextAsync(keyPath, "sk-or-canary-secret-do-not-leak", cancellation);
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(root.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            var source = new UnixOwnerOnlyMountedFileProviderSecretSource(root.FullName);
            using var allowed = await source.TryReadAsync("openrouter-api-key", cancellation);
            Assert.NotNull(allowed);

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
            }

            using var groupReadable = await source.TryReadAsync("openrouter-api-key", cancellation);
            Assert.Null(groupReadable);

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                File.SetUnixFileMode(root.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupExecute);
            }

            using var groupDir = await source.TryReadAsync("openrouter-api-key", cancellation);
            Assert.Null(groupDir);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Default_mounted_file_source_still_reads_group_readable_files()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var root = Directory.CreateTempSubdirectory("flex-agent-default-secrets-");
        try
        {
            var keyPath = Path.Combine(root.FullName, "org-a-openai");
            await File.WriteAllTextAsync(keyPath, "sk-test-not-for-production", cancellation);
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
            }

            var source = new MountedFileProviderSecretSource(root.FullName);
            using var secret = await source.TryReadAsync("org-a-openai", cancellation);
            Assert.NotNull(secret);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}

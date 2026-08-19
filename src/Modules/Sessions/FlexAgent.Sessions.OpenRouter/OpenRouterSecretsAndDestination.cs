using System.Net;
using FlexAgent.Sessions.Application;

namespace FlexAgent.Sessions.OpenRouter;

public sealed class UnixOwnerOnlyMountedFileProviderSecretSource : IProviderCredentialSecretSource
{
    private readonly MountedFileProviderSecretSource _inner;
    private readonly string _rootDirectory;

    public UnixOwnerOnlyMountedFileProviderSecretSource(string rootDirectory, int maxUtf8Bytes = MountedFileProviderSecretSource.MaxSecretUtf8Bytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = rootDirectory;
        _inner = new MountedFileProviderSecretSource(rootDirectory, maxUtf8Bytes);
    }

    public async Task<ProviderSecret?> TryReadAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
        {
            return null;
        }

        var root = Path.GetFullPath(_rootDirectory);
        if (!Directory.Exists(root) || !HasOwnerOnlyDirectoryMode(root))
        {
            return null;
        }

        var combined = Path.GetFullPath(Path.Combine(root, secretName));
        if (File.Exists(combined) && !HasOwnerOnlyFileMode(combined))
        {
            return null;
        }

        return await _inner.TryReadAsync(secretName, cancellationToken);
    }

    public static bool HasOwnerOnlyDirectoryMode(string path)
    {
        if (!TryGetUnixMode(path, out var mode))
        {
            return false;
        }

        return !HasGroupOrOther(mode)
            && mode.HasFlag(UnixFileMode.UserRead)
            && mode.HasFlag(UnixFileMode.UserWrite)
            && mode.HasFlag(UnixFileMode.UserExecute);
    }

    public static bool HasOwnerOnlyFileMode(string path)
    {
        if (!TryGetUnixMode(path, out var mode))
        {
            return false;
        }

        return !HasGroupOrOther(mode) && mode.HasFlag(UnixFileMode.UserRead);
    }

    public static bool PlatformSupportsUnixModes() =>
        OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD();

    private static bool TryGetUnixMode(string path, out UnixFileMode mode)
    {
        mode = default;
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
        {
            return false;
        }

        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                return false;
            }

            mode = File.GetUnixFileMode(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static bool HasGroupOrOther(UnixFileMode mode) =>
        mode.HasFlag(UnixFileMode.GroupRead)
        || mode.HasFlag(UnixFileMode.GroupWrite)
        || mode.HasFlag(UnixFileMode.GroupExecute)
        || mode.HasFlag(UnixFileMode.OtherRead)
        || mode.HasFlag(UnixFileMode.OtherWrite)
        || mode.HasFlag(UnixFileMode.OtherExecute);
}

internal sealed class OpenRouterDestinationHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is null || !OpenRouterDestination.IsAllowed(request.RequestUri))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                ReasonPhrase = "origin_denied",
            });
        }

        return base.SendAsync(request, cancellationToken);
    }
}

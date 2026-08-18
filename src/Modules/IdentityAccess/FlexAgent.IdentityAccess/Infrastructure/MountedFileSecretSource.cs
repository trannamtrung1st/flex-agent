using FlexAgent.IdentityAccess.Application;

namespace FlexAgent.IdentityAccess.Infrastructure;

public sealed class MountedFileSecretSource(string rootDirectory) : ISecretSource
{
    public async Task<string?> TryReadAsync(string secretName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);
        if (secretName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || secretName.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || secretName.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal)
            || secretName is "." or "..")
        {
            return null;
        }

        var path = Path.GetFullPath(Path.Combine(rootDirectory, secretName));
        var root = Path.GetFullPath(rootDirectory);
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !string.Equals(path, root, StringComparison.Ordinal))
        {
            return null;
        }

        if (!File.Exists(path))
        {
            return null;
        }

        var value = await File.ReadAllTextAsync(path, cancellationToken);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

using System.Text;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public sealed class ProviderSecret : IDisposable
{
    private char[]? _value;

    public ProviderSecret(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _value = value.ToCharArray();
    }

    public string Reveal()
    {
        ObjectDisposedException.ThrowIf(_value is null, this);
        return new string(_value);
    }

    public override string ToString() => "[redacted]";

    public override int GetHashCode() => 0;

    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public void Dispose()
    {
        if (_value is null)
        {
            return;
        }

        Array.Clear(_value);
        _value = null;
        GC.SuppressFinalize(this);
    }
}

public interface IProviderCredentialSecretSource
{
    Task<ProviderSecret?> TryReadAsync(string secretName, CancellationToken cancellationToken = default);
}

public interface IModelProviderAttemptProvenanceWriter
{
    Task WriteAsync(
        SessionOwnership ownership,
        string agentInvocationId,
        int invocationAttemptOrdinal,
        ModelProviderAttemptProvenance provenance,
        CancellationToken cancellationToken);

    Task<int> CountAsync(
        SessionOwnership ownership,
        string agentInvocationId,
        CancellationToken cancellationToken);

    Task<ProviderRequestReservationResult> TryReserveStartedAsync(
        DurableInvocationWorkItem claimedWork,
        string agentInvocationId,
        int invocationAttemptOrdinal,
        int maxProviderRequestAttempts,
        ModelProviderAttemptProvenance started,
        TimeSpan lease,
        CancellationToken cancellationToken);
}

public sealed record ProviderRequestReservationResult(
    bool Reserved,
    bool LostClaimAuthority,
    DateTimeOffset? RenewedClaimLeaseUntil = null)
{
    public static ProviderRequestReservationResult LostClaim { get; } = new(false, true);

    public static ProviderRequestReservationResult BudgetExhausted(DateTimeOffset? renewedClaimLeaseUntil = null) =>
        new(false, false, renewedClaimLeaseUntil);

    public static ProviderRequestReservationResult Succeeded(DateTimeOffset? renewedClaimLeaseUntil) =>
        new(true, false, renewedClaimLeaseUntil);
}

public sealed class MountedFileProviderSecretSource : IProviderCredentialSecretSource
{
    public const int MaxSecretUtf8Bytes = 8_192;

    private readonly string _rootDirectory;
    private readonly int _maxUtf8Bytes;

    public MountedFileProviderSecretSource(string rootDirectory, int maxUtf8Bytes = MaxSecretUtf8Bytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = rootDirectory;
        _maxUtf8Bytes = maxUtf8Bytes;
    }

    public async Task<ProviderSecret?> TryReadAsync(string secretName, CancellationToken cancellationToken = default)
    {
        var path = TryResolveRegularFile(secretName);
        if (path is null)
        {
            return null;
        }

        var info = new FileInfo(path);
        if (info.Length <= 0 || info.Length > _maxUtf8Bytes)
        {
            return null;
        }

        var value = (await File.ReadAllTextAsync(path, cancellationToken)).Trim();
        return string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) > _maxUtf8Bytes
            ? null
            : new ProviderSecret(value);
    }

    private string? TryResolveRegularFile(string secretName)
    {
        if (string.IsNullOrWhiteSpace(secretName)
            || secretName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || secretName.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || secretName.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal)
            || secretName is "." or "..")
        {
            return null;
        }

        var root = Path.GetFullPath(_rootDirectory);
        var combined = Path.GetFullPath(Path.Combine(root, secretName));
        if (!combined.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !string.Equals(combined, root, StringComparison.Ordinal))
        {
            return null;
        }

        if (!File.Exists(combined))
        {
            return null;
        }

        var attributes = File.GetAttributes(combined);
        if ((attributes & FileAttributes.ReparsePoint) != 0
            || (attributes & FileAttributes.Directory) != 0)
        {
            return null;
        }

        var resolved = File.ResolveLinkTarget(combined, returnFinalTarget: true);
        if (resolved is not null)
        {
            return null;
        }

        return combined;
    }
}

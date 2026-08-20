using System.Globalization;
using System.Text;

namespace FlexAgent.Sessions.OpenRouter;

public sealed class OpenRouterQualificationBudget
{
    public const string HistoricalFormat = "openrouter_qualification_budget.v1";
    public const string Phase21Format = "openrouter_qualification_budget.phase21.v1";
    private const int MaxStateBytes = 128;
    private static readonly UnixFileMode OwnerReadWrite =
        UnixFileMode.UserRead | UnixFileMode.UserWrite;

    private readonly string _path;
    private readonly string _format;
    private readonly int _maximum;

    public OpenRouterQualificationBudget(string path)
        : this(path, HistoricalFormat, OpenRouterLiveQualification.MaxInferenceRequests)
    {
    }

    public static OpenRouterQualificationBudget CreatePhase21(string path) =>
        new(path, Phase21Format, OpenRouterLiveQualification.Phase21MaxInferenceRequests);

    private OpenRouterQualificationBudget(string path, string format, int maximum)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        if (maximum is < 1 or > OpenRouterLiveQualification.MaxInferenceRequests)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }

        _path = Path.GetFullPath(path);
        _format = format;
        _maximum = maximum;
    }

    public bool TryRead(out int reservedRequestCount)
    {
        reservedRequestCount = 0;
        try
        {
            if (!TryOpenExistingState(FileAccess.Read, FileShare.Read, out var stream)
                || stream is null)
            {
                return false;
            }

            using (stream)
            {
                if (stream.Length == 0)
                {
                    return true;
                }

                return TryReadState(stream, out reservedRequestCount);
            }
        }
        catch (Exception ex) when (IsClosedFailure(ex))
        {
            reservedRequestCount = 0;
            return false;
        }
    }

    public bool TryReserve(out int reservedRequestCount) =>
        TryReserveCore(expectedCurrent: null, out reservedRequestCount);

    public bool TryReserveExpected(int expectedCurrent, out int reservedRequestCount)
    {
        if (expectedCurrent is < 0 || expectedCurrent > _maximum)
        {
            reservedRequestCount = 0;
            return false;
        }

        return TryReserveCore(expectedCurrent, out reservedRequestCount);
    }

    private bool TryReserveCore(int? expectedCurrent, out int reservedRequestCount)
    {
        reservedRequestCount = 0;
        try
        {
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
            {
                return false;
            }

            var directory = Path.GetDirectoryName(_path);
            if (string.IsNullOrWhiteSpace(directory)
                || !Directory.Exists(directory)
                || !UnixOwnerOnlyMountedFileProviderSecretSource.HasOwnerOnlyDirectoryMode(directory)
                || IsReparsePoint(_path))
            {
                return false;
            }

            var options = new FileStreamOptions
            {
                Mode = FileMode.OpenOrCreate,
                Access = FileAccess.ReadWrite,
                Share = FileShare.None,
                Options = FileOptions.WriteThrough,
                UnixCreateMode = OwnerReadWrite,
            };
            using var stream = new FileStream(_path, options);
            if (IsReparsePoint(_path)
                || !UnixOwnerOnlyMountedFileProviderSecretSource.HasOwnerOnlyFileMode(_path)
                || stream.Length > MaxStateBytes)
            {
                return false;
            }

            var current = 0;
            if (stream.Length > 0 && !TryReadState(stream, out current))
            {
                return false;
            }

            if (expectedCurrent is int expected && current != expected)
            {
                reservedRequestCount = current;
                return false;
            }

            if (current >= _maximum)
            {
                reservedRequestCount = current;
                return false;
            }

            reservedRequestCount = checked(current + 1);
            var state = Encoding.UTF8.GetBytes(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{_format}\n{reservedRequestCount}\n{_maximum}\n"));
            stream.Position = 0;
            stream.Write(state);
            stream.SetLength(state.Length);
            stream.Flush(flushToDisk: true);
            return true;
        }
        catch (Exception ex) when (IsClosedFailure(ex))
        {
            reservedRequestCount = 0;
            return false;
        }
    }

    private bool TryOpenExistingState(FileAccess access, FileShare share, out FileStream? stream)
    {
        stream = null;
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
        {
            return false;
        }

        var directory = Path.GetDirectoryName(_path);
        if (string.IsNullOrWhiteSpace(directory)
            || !Directory.Exists(directory)
            || !UnixOwnerOnlyMountedFileProviderSecretSource.HasOwnerOnlyDirectoryMode(directory)
            || !File.Exists(_path)
            || IsReparsePoint(_path))
        {
            return false;
        }

        var options = new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = access,
            Share = share,
        };
        stream = new FileStream(_path, options);
        if (IsReparsePoint(_path)
            || !UnixOwnerOnlyMountedFileProviderSecretSource.HasOwnerOnlyFileMode(_path)
            || stream.Length > MaxStateBytes)
        {
            stream.Dispose();
            stream = null;
            return false;
        }

        return true;
    }

    private static bool IsClosedFailure(Exception ex) =>
        ex is IOException
            or UnauthorizedAccessException
            or PlatformNotSupportedException
            or NotSupportedException
            or ArgumentException
            or OverflowException;

    private bool TryReadState(FileStream stream, out int requestCount)
    {
        requestCount = 0;
        stream.Position = 0;
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        var lines = Encoding.UTF8.GetString(bytes).Split('\n');
        return lines.Length == 4
            && lines[0] == _format
            && int.TryParse(lines[1], NumberStyles.None, CultureInfo.InvariantCulture, out requestCount)
            && requestCount >= 0
            && requestCount <= _maximum
            && int.TryParse(lines[2], NumberStyles.None, CultureInfo.InvariantCulture, out var configuredMaximum)
            && configuredMaximum == _maximum
            && lines[3].Length == 0;
    }

    private static bool IsReparsePoint(string path) =>
        File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
}

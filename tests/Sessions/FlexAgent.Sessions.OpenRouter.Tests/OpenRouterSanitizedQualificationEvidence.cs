using System.Text;

namespace FlexAgent.Sessions.OpenRouter.Tests;

internal static class OpenRouterSanitizedQualificationEvidence
{
    public static bool TryWriteAtomic(string path, OpenRouterSanitizedQualificationRecord record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(record);
        try
        {
            var destination = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(destination);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return false;
            }

            var json = record.ToSanitizedJson();
            var temporary = destination + ".tmp";
            File.WriteAllText(temporary, json, Encoding.UTF8);
            File.Move(temporary, destination, overwrite: true);
            return File.Exists(destination) && !File.Exists(temporary);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }
}

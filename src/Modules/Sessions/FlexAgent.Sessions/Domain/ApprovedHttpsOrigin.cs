namespace FlexAgent.Sessions.Domain;

public static class ApprovedHttpsOrigin
{
    public static Uri Canonicalize(Uri origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        if (!string.Equals(origin.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || origin.IsLoopback
            || string.IsNullOrWhiteSpace(origin.Host)
            || !string.IsNullOrEmpty(origin.UserInfo)
            || !string.IsNullOrEmpty(origin.Query)
            || !string.IsNullOrEmpty(origin.Fragment)
            || HasDisallowedPath(origin))
        {
            throw new ArgumentOutOfRangeException(nameof(origin));
        }

        var port = origin.IsDefaultPort ? 443 : origin.Port;
        return new Uri($"https://{origin.Host.ToLowerInvariant()}:{port}/", UriKind.Absolute);
    }

    public static string DigestSource(Uri origin) => Canonicalize(origin).GetLeftPart(UriPartial.Authority);

    private static bool HasDisallowedPath(Uri origin)
    {
        var path = origin.AbsolutePath;
        return !string.IsNullOrEmpty(path) && path != "/";
    }
}

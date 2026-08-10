using FlexAgent.Contracts.Browser;

namespace FlexAgent.SyntheticBrowser.Application;

internal static class SyntheticCommandValidation
{
    internal static BrowserCommandResultV1? ValidateEnvelope(BrowserCommandEnvelopeV1 command)
    {
        if (!string.Equals(command.SchemaVersion, BrowserSchemaVersion.V1, StringComparison.Ordinal))
        {
            return Denied("Unsupported schema version.");
        }

        if (string.IsNullOrWhiteSpace(command.CommandId))
        {
            return Denied("Command identifier is required.");
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return Denied("Idempotency key is required.");
        }

        if (string.IsNullOrWhiteSpace(command.CommandType))
        {
            return Denied("Command type is required.");
        }

        return null;
    }

    internal static BrowserCommandResultV1? RequirePayloadValue(
        BrowserCommandEnvelopeV1 command,
        string key)
    {
        if (string.IsNullOrWhiteSpace(command.Payload?.GetValueOrDefault(key)))
        {
            return Denied("Required command payload is missing.");
        }

        return null;
    }

    private static BrowserCommandResultV1 Denied(string message) =>
        new(BrowserSchemaVersion.V1, "denied", Guid.NewGuid().ToString("N"), null, null, "contact_administrator", message);
}

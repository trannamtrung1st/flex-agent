using System.Globalization;

namespace FlexAgent.Sessions.Domain;

/// <summary>
/// Deterministic hosted SSE cursor distinct from domain <c>session_sequence</c>.
/// One Session sequence may project multiple hosted events; each slot is stable
/// across later mutations so a previously issued cursor remains valid.
/// </summary>
public static class HostedStreamCursors
{
    public const int SlotsPerSequence = 10;
    public const int SlotQueued = 0;
    public const int SlotAccepted = 1;
    public const int SlotWorking = 2;
    public const int SlotNoAction = 3;
    public const int SlotFailed = 4;
    public const int SlotFragment = 5;
    public const int SlotComplete = 6;
    public const int SlotTerminal = 7;

    public static long Encode(long sessionSequence, int slot)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sessionSequence, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, SlotsPerSequence);
        return checked(sessionSequence * SlotsPerSequence + slot);
    }

    public static string Wire(long sessionSequence, int slot) =>
        Encode(sessionSequence, slot).ToString(CultureInfo.InvariantCulture);

    public static long Parse(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)
            || !long.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            return 0;
        }

        return value;
    }
}

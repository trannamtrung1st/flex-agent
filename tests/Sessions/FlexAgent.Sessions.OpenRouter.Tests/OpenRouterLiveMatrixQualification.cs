using System.Text;
using FlexAgent.Sessions.Application;

namespace FlexAgent.Sessions.OpenRouter.Tests;

internal static class OpenRouterLiveMatrixQualification
{
    public static bool TryQualify(
        ModelExecutionAttemptResult control,
        IReadOnlyList<ModelContentEvent> events,
        int maxOutputTokens,
        out string denialReason)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(events);
        if (control is not ModelExecutionStructuredControl)
        {
            denialReason = "control_not_admitted";
            return false;
        }

        if (events.OfType<ModelContentFailed>().Any())
        {
            denialReason = "content_failed";
            return false;
        }

        var completed = events.OfType<ModelContentCompleted>().SingleOrDefault();
        if (completed is null)
        {
            denialReason = "content_incomplete";
            return false;
        }

        var deltas = events.OfType<ModelContentTextDelta>().ToArray();
        var visibleUtf8Bytes = deltas.Sum(delta => Encoding.UTF8.GetByteCount(delta.ExactUtf8Text));
        if (deltas.Length == 0 || visibleUtf8Bytes <= 0)
        {
            denialReason = "missing_visible_content";
            return false;
        }

        if (completed.Provenance?.OutputTokenCount is null
            || completed.Provenance.OutputTokenCount >= maxOutputTokens)
        {
            denialReason = "length_truncated";
            return false;
        }

        denialReason = string.Empty;
        return true;
    }

    public static bool TryAuthorizeContentAfterControl(
        ModelExecutionAttemptResult control,
        out string denialReason)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (control is not ModelExecutionStructuredControl)
        {
            denialReason = "control_not_admitted";
            return false;
        }

        denialReason = string.Empty;
        return true;
    }
}
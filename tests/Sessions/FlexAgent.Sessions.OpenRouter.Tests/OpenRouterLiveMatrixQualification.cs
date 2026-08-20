using System.Text;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

internal static class OpenRouterLiveMatrixQualification
{
    public static bool TryQualify(
        ModelExecutionAttemptResult control,
        IReadOnlyList<ModelContentEvent> events,
        out string denialReason)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(events);
        if (control is not ModelExecutionStructuredControl)
        {
            denialReason = "control_not_admitted";
            return false;
        }

        if (!OpenRouterAdapterContracts.IsApprovedNonTruncationFinishReason(control.Provenance?.TerminalFinishReason))
        {
            denialReason = "length_truncated";
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

        if (!OpenRouterAdapterContracts.IsApprovedNonTruncationFinishReason(completed.Provenance?.TerminalFinishReason)
            || completed.Provenance?.OutputTokenCount is null
            || completed.Provenance.OutputTokenCount
                >= OpenRouterAdapterContracts.VisibleContentAcceptanceMaxOutputTokens)
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
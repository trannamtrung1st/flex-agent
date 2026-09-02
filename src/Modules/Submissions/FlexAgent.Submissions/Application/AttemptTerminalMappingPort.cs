using FlexAgent.Submissions.Domain;

namespace FlexAgent.Submissions.Application;

public sealed class AttemptTerminalMappingPort(IAttemptStore attempts) : IAttemptTerminalMappingPort
{
    public async Task MapTerminalAsync(
        Guid organizationId,
        Guid attemptId,
        string terminalStatus,
        string reasonCategory,
        DateTimeOffset terminalAtUtc,
        object commitTransaction,
        CancellationToken cancellationToken = default)
    {
        var transaction = commitTransaction as IEnrollmentTransaction
            ?? throw new InvalidOperationException("commit.transaction.required");
        var current = await attempts.FindByIdAsync(organizationId, attemptId, transaction, cancellationToken);
        if (current is null)
        {
            throw new InvalidOperationException("attempt.missing");
        }

        if (current.Status is AttemptStates.Completed or AttemptStates.Aborted)
        {
            return;
        }

        var mapped = string.Equals(terminalStatus, AttemptStates.Completed, StringComparison.Ordinal)
            ? current.Complete(terminalAtUtc, reasonCategory)
            : current.Abort(terminalAtUtc, reasonCategory);
        if (!mapped.Succeeded || mapped.Value is null)
        {
            throw new InvalidOperationException(mapped.OutcomeCode);
        }

        await attempts.UpdateTerminalAsync(mapped.Value, transaction, cancellationToken);
    }
}

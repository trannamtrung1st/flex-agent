using FlexAgent.Postgres;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Infrastructure;

public sealed class PostgresAdmitTrustedTriggerCoordinator(
    PostgresConnectionAccessor connectionAccessor,
    PostgresSessionRuntimeRepository runtimeRepository,
    IAdmitTrustedTriggerHandler admissionHandler)
{
    public async Task<TriggerAdmissionResult> AdmitAsync(
        AdmitTrustedTriggerCommand command,
        TrustedSessionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(binding);

        if (command.Ownership != binding.Ownership)
        {
            return new TriggerAdmissionResult(
                false,
                TriggerAdmissionOutcomeCodes.OwnershipMismatch,
                null,
                null);
        }

        await using var scope = await PostgresTransactionScope.BeginAsync(connectionAccessor, cancellationToken);
        try
        {
            var session = await runtimeRepository.LoadForUpdateAsync(
                command.Ownership,
                binding,
                scope.Transaction,
                cancellationToken);
            if (session is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TriggerAdmissionResult(false, TriggerAdmissionOutcomeCodes.Denied, null, null);
            }

            var authoritativeUtc = await runtimeRepository.ReadAuthoritativeUtcAsync(
                scope.Transaction,
                cancellationToken);
            var result = admissionHandler.Handle(command, session, authoritativeUtc);
            if (!result.Succeeded || result.Invocation is null)
            {
                await scope.RollbackAsync(cancellationToken);
                return result;
            }

            if (result.OutcomeCode == TriggerAdmissionOutcomeCodes.Reconciled)
            {
                await scope.CommitAsync(cancellationToken);
                return result;
            }

            var saved = await runtimeRepository.TrySaveAdmissionAsync(
                command.Ownership,
                command.ExpectedSessionVersion,
                session,
                result.Invocation,
                scope.Transaction,
                cancellationToken);
            if (!saved)
            {
                await scope.RollbackAsync(cancellationToken);
                return new TriggerAdmissionResult(false, TriggerAdmissionOutcomeCodes.StaleVersion, null, null);
            }

            await scope.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

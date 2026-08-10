using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Domain;
using Npgsql;

namespace FlexAgent.IdentityAccess.Infrastructure;

public interface ICommitAuthorizationKernel : IAuthorizationKernel
{
    Task<AuthorizationDecision> ReauthorizeInTransactionAsync(
        AuthorizationRequest request,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default);
}

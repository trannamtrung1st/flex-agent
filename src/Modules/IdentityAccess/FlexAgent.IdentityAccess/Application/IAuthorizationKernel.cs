using FlexAgent.IdentityAccess.Domain;

namespace FlexAgent.IdentityAccess.Application;

public interface IAuthorizationKernel
{
    Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default);
}

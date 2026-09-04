using FlexAgent.IdentityAccess.Domain;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using Npgsql;

namespace FlexAgent.Sessions.Infrastructure;

internal static class SessionHostedCommandCommitAuthorization
{
    public static async Task<AuthorizationDecision> ReauthorizeAsync(
        ICommitAuthorizationKernel kernel,
        TrustedRuntimeActor actor,
        SessionOwnership ownership,
        string commandType,
        Guid correlationId,
        string sourceChannel,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var action = RequiredAuthorizationAction(commandType);
        if (action is null)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.DeniedNoGrant);
        }

        return await kernel.ReauthorizeInTransactionAsync(
            new AuthorizationRequest(
                new TrustedActor(actor.ActorId, actor.ActorType),
                new OrganizationScope(ownership.OrganizationId),
                action,
                new ResourceScope(
                    new OrganizationScope(ownership.OrganizationId),
                    AuthorizationResourceTypes.Session,
                    ownership.SessionId),
                sourceChannel,
                correlationId,
                null,
                ownership.ActivityId,
                ownership.ParticipantId,
                ownership.AttemptId),
            transaction,
            cancellationToken);
    }

    public static async Task<AuthorizationDecision> ReauthorizeLifecycleAsync(
        ICommitAuthorizationKernel kernel,
        TrustedRuntimeActor actor,
        SessionOwnership ownership,
        string transition,
        Guid correlationId,
        string sourceChannel,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var action = LifecycleAuthorizationAction(transition);
        if (action is null)
        {
            return AuthorizationDecision.Deny(AuthorizationReasonCodes.DeniedNoGrant);
        }

        return await kernel.ReauthorizeInTransactionAsync(
            new AuthorizationRequest(
                new TrustedActor(actor.ActorId, actor.ActorType),
                new OrganizationScope(ownership.OrganizationId),
                action,
                new ResourceScope(
                    new OrganizationScope(ownership.OrganizationId),
                    AuthorizationResourceTypes.Session,
                    ownership.SessionId),
                sourceChannel,
                correlationId,
                null,
                ownership.ActivityId,
                ownership.ParticipantId,
                ownership.AttemptId),
            transaction,
            cancellationToken);
    }

    private static string? RequiredAuthorizationAction(string commandType) =>
        commandType switch
        {
            "session.message.send.v1" => AuthorizationActions.SendSessionMessage,
            "session.pause.v1" => AuthorizationActions.PauseSession,
            "session.resume.v1" => AuthorizationActions.ResumeSession,
            "session.complete.v1" => AuthorizationActions.CompleteSession,
            "session.terminate.v1" => AuthorizationActions.TerminateSession,
            "session.reconcile.v1" => AuthorizationActions.ReconcileSession,
            _ => null,
        };

    private static string? LifecycleAuthorizationAction(string transition) =>
        transition switch
        {
            SessionLifecycleTransitions.Pause => AuthorizationActions.PauseSession,
            SessionLifecycleTransitions.Resume => AuthorizationActions.ResumeSession,
            SessionLifecycleTransitions.BeginCompleting => AuthorizationActions.CompleteSession,
            SessionLifecycleTransitions.Complete => AuthorizationActions.CompleteSession,
            SessionLifecycleTransitions.Terminate => AuthorizationActions.TerminateSession,
            _ => null,
        };
}

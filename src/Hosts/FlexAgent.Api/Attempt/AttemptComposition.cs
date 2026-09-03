using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Infrastructure;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Infrastructure;

namespace FlexAgent.Api;

public static partial class SubmissionEndpointExtensions
{
    public static IServiceCollection AddAttemptStart(this IServiceCollection services, bool postgres)
    {
        services.AddSingleton<IRetryEntitlementReader>(_ => EmptyRetryEntitlementReader.Instance);
        services.AddSingleton<IAttemptTerminalMappingPort, AttemptTerminalMappingPort>();
        services.AddSingleton<ISessionAttemptTerminalSink, SubmissionsSessionAttemptTerminalSink>();
        services.AddSingleton<IFrozenAttemptTimingCapture>(provider =>
            provider.GetService<IHostedSessionFrozenTimingSource>() as IFrozenAttemptTimingCapture
            ?? UnavailableFrozenAttemptTimingCapture.Instance);
        services.AddSingleton<AttemptStartCoordinator>();
        services.AddSingleton<IAttemptStartCoordinator>(static provider => provider.GetRequiredService<AttemptStartCoordinator>());
        services.AddSingleton<IAttemptReadinessQuery>(static provider => provider.GetRequiredService<AttemptStartCoordinator>());
        services.AddSingleton<IAttemptAcknowledgmentCoordinator, AttemptAcknowledgmentCoordinator>();
        services.AddSingleton<ISessionStartCommitPort>(provider =>
            new GatedP0SessionStartPort(
                provider.GetRequiredService<IHostEnvironment>(),
                provider.GetService<PostgresSessionRuntimeRepository>(),
                provider.GetService<IConfiguration>(),
                provider.GetService<ICommitAuthorizationKernel>()));

        if (postgres)
        {
            services.AddSingleton<IAttemptStore, PostgresAttemptStore>();
            services.AddSingleton<IStartOperationStore, PostgresStartOperationStore>();
            services.AddSingleton<IParticipantNoticePort, PostgresParticipantNoticePort>();
            services.AddSingleton<IAcknowledgmentLifecyclePort, PostgresAcknowledgmentLifecyclePort>();
            services.AddSingleton<IExactAcceptedVersionReader, PostgresExactAcceptedVersionReader>();
            return services;
        }

        services.AddSingleton<InMemoryAttemptStore>();
        services.AddSingleton<IAttemptStore>(static provider => provider.GetRequiredService<InMemoryAttemptStore>());
        services.AddSingleton<InMemoryStartOperationStore>();
        services.AddSingleton<IStartOperationStore>(static provider => provider.GetRequiredService<InMemoryStartOperationStore>());
        services.AddSingleton<IParticipantNoticePort>(_ => EmptyParticipantNoticePort.Instance);
        services.AddSingleton<InMemoryAcknowledgmentLifecyclePort>();
        services.AddSingleton<IAcknowledgmentLifecyclePort>(static provider =>
            provider.GetRequiredService<InMemoryAcknowledgmentLifecyclePort>());
        return services;
    }
}

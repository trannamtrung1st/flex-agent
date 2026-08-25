using FlexAgent.AssessmentConfiguration.Application;
using FlexAgent.AssessmentConfiguration.Canonicalization;
using FlexAgent.AssessmentConfiguration.Infrastructure;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;

namespace FlexAgent.Api;

public static partial class AssessmentEndpointExtensions
{
    public static IServiceCollection AddAssessmentConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = HumanAuthenticationPersistencePolicy.ResolveConnectionString(configuration);
        var productionLocked = environment.IsProduction() || environment.IsEnvironment("Staging");
        if (string.IsNullOrWhiteSpace(connectionString) && productionLocked)
        {
            return services;
        }

        services.AddSingleton<IActivationBaselineDigester, ActivationBaselineDigester>();
        services.AddSingleton<IAssessmentCommandDigest, AssessmentCommandDigest>();
        services.AddSingleton<IAssessmentDraftHandler, AssessmentDraftHandler>();
        services.AddSingleton<IAssessmentClock, SystemAssessmentClock>();
        services.AddSingleton<IAssessmentActivationCoordinator, AssessmentActivationCoordinator>();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            if (services.All(descriptor => descriptor.ServiceType != typeof(Npgsql.NpgsqlDataSource)))
            {
                services.AddSingleton(_ => Npgsql.NpgsqlDataSource.Create(connectionString));
                services.AddSingleton<PostgresConnectionAccessor>();
            }

            if (services.All(descriptor => descriptor.ServiceType != typeof(IAuthorizationKernel)))
            {
                services.AddSingleton<IAuthorizationKernel, PostgresAuthorizationKernel>();
            }

            if (services.All(descriptor => descriptor.ServiceType != typeof(ICommitAuthorizationKernel)))
            {
                services.AddSingleton<ICommitAuthorizationKernel>(sp =>
                    sp.GetService<IAuthorizationKernel>() as ICommitAuthorizationKernel
                    ?? ActivatorUtilities.CreateInstance<PostgresAuthorizationKernel>(sp));
            }

            if (services.All(descriptor => descriptor.ServiceType != typeof(IAuditEventWriter)))
            {
                services.AddSingleton<IAuditEventWriter, PostgresAuditEventWriter>();
            }

            if (services.All(descriptor => descriptor.ServiceType != typeof(IOutboxItemWriter)))
            {
                services.AddSingleton<IOutboxItemWriter, PostgresOutboxItemWriter>();
            }

            services.AddSingleton<PostgresAssessmentSourceCatalog>();
            services.AddSingleton<IAssessmentSourceCatalog>(sp => sp.GetRequiredService<PostgresAssessmentSourceCatalog>());
            services.AddSingleton<IAssessmentSourceTransactionPort>(sp => sp.GetRequiredService<PostgresAssessmentSourceCatalog>());
            services.AddSingleton<IAssessmentDevelopmentSourceSeeder, NoOpAssessmentDevelopmentSourceSeeder>();
            services.AddSingleton<IAssessmentDraftStore, PostgresAssessmentDraftStore>();
            services.AddSingleton<IAssessmentAuthorizationPort, KernelAssessmentAuthorizationPort>();
            services.AddSingleton<IAssessmentRelationshipResolver, PostgresAssessmentRelationshipResolver>();
            services.AddSingleton<IAssessmentActivationUnitOfWork, PostgresAssessmentUnitOfWork>();
            services.AddSingleton<IAssessmentBaselineStore, PostgresAssessmentBaselineStore>();
            services.AddSingleton<IAssessmentActivationAttemptStore, PostgresAssessmentAttemptStore>();
            return services;
        }

        services.AddSingleton<IAssessmentDraftStore, InMemoryAssessmentDraftStore>();
        services.AddSingleton<InMemoryAssessmentSourceCatalog>();
        services.AddSingleton<IAssessmentSourceCatalog>(sp => sp.GetRequiredService<InMemoryAssessmentSourceCatalog>());
        services.AddSingleton<IAssessmentSourceTransactionPort>(sp => sp.GetRequiredService<InMemoryAssessmentSourceCatalog>());
        services.AddSingleton<IAssessmentDevelopmentSourceSeeder>(sp => sp.GetRequiredService<InMemoryAssessmentSourceCatalog>());
        services.AddSingleton<IAssessmentAuthorizationPort>(_ => new InMemoryAssessmentAuthorizationPort(permit: false));
        services.AddSingleton<IAssessmentRelationshipResolver, EmptyAssessmentRelationshipResolver>();
        services.AddSingleton<IAssessmentActivationUnitOfWork, InMemoryAssessmentUnitOfWork>();
        services.AddSingleton<IAssessmentBaselineStore, InMemoryAssessmentBaselineStore>();
        services.AddSingleton<IAssessmentActivationAttemptStore, InMemoryAssessmentAttemptStore>();
        return services;
    }
}

file sealed class NoOpAssessmentDevelopmentSourceSeeder : IAssessmentDevelopmentSourceSeeder
{
    public void EnsureOrganization(Guid organizationId)
    {
        _ = organizationId;
    }
}

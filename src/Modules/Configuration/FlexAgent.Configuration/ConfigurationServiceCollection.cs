using FlexAgent.Configuration.Application;
using FlexAgent.Configuration.Infrastructure;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.IdentityAccess.Infrastructure;
using FlexAgent.Postgres;
using FlexAgent.Postgres.Audit;
using FlexAgent.Postgres.Outbox;

namespace FlexAgent.Configuration;

public static class ConfigurationServiceCollection
{
    public static ServiceBundle Create(string connectionString)
    {
        var dataSourceFactory = new PostgresDataSourceFactory();
        var dataSource = dataSourceFactory.Create(connectionString);
        var connectionAccessor = new PostgresConnectionAccessor(dataSource);

        var authorizationKernel = new PostgresAuthorizationKernel(connectionAccessor);
        var versionRepository = new PostgresConfigurationSourceVersionRepository(connectionAccessor);
        var idempotencyRepository = new PostgresConfigurationSourceVersionIdempotencyRepository();
        var digestVerifier = new ConfigurationDigestVerifier();
        var auditWriter = new PostgresAuditEventWriter();
        var outboxWriter = new PostgresOutboxItemWriter();

        var handler = new RegisterConfigurationSourceVersionHandler(
            authorizationKernel,
            authorizationKernel,
            versionRepository,
            idempotencyRepository,
            digestVerifier,
            connectionAccessor,
            auditWriter,
            outboxWriter);

        return new ServiceBundle(
            connectionAccessor,
            authorizationKernel,
            versionRepository,
            idempotencyRepository,
            new PostgresGrantRepository(connectionAccessor),
            handler);
    }

    public sealed record ServiceBundle(
        PostgresConnectionAccessor ConnectionAccessor,
        IAuthorizationKernel AuthorizationKernel,
        PostgresConfigurationSourceVersionRepository VersionRepository,
        PostgresConfigurationSourceVersionIdempotencyRepository IdempotencyRepository,
        PostgresGrantRepository GrantRepository,
        IRegisterConfigurationSourceVersionHandler RegisterHandler);
}

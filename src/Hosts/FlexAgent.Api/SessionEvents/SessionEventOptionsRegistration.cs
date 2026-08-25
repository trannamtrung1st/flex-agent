namespace FlexAgent.Api;

internal static class SessionEventOptionsRegistration
{
    public static IServiceCollection AddSessionEventOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SessionEventSubscriptionOptions>()
            .BindConfiguration(SessionEventSubscriptionOptions.SectionName);

        services.AddOptions<SessionEventTestIdentityOptions>()
            .BindConfiguration(SessionEventTestIdentityOptions.SectionName);

        return services;
    }
}

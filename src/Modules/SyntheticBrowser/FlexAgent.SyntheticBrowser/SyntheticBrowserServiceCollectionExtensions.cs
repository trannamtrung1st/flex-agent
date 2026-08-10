using FlexAgent.SyntheticBrowser.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlexAgent.SyntheticBrowser;

public static class SyntheticBrowserServiceCollectionExtensions
{
    public static IServiceCollection AddSyntheticBrowser(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SyntheticBrowserOptions>(configuration.GetSection(SyntheticBrowserOptions.SectionName));
        services.AddSingleton<ISyntheticBrowserService, SyntheticBrowserService>();
        return services;
    }
}

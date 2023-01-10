namespace RoadRegistry.BackOffice.Api.Infrastructure.Extensions;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Options;
using TicketingService.Abstractions;
using TicketingService.Proxy.HttpProxy;

public static class TicketingExtensions
{
    private static IServiceCollection AddHttpProxyTicketing(
        this IServiceCollection services,
        Func<IConfiguration, string> baseUrlProvider)
    {
        services.AddHttpClient<ITicketing, HttpProxyTicketing>((sp, c) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            c.BaseAddress = new Uri(baseUrlProvider(configuration).TrimEnd('/'));
        });

        return services;
    }

    public static IServiceCollection AddTicketing(
        this IServiceCollection services)
    {
        return services
            .AddHttpProxyTicketing(GetBaseUrl)
            .AddSingleton<ITicketingUrl>(sp =>
                new TicketingUrl(GetBaseUrl(sp.GetRequiredService<IConfiguration>()))
            );
    }

    private static string GetBaseUrl(IConfiguration configuration)
    {
        return configuration.GetSection(TicketingOptions.ConfigurationKey).GetRequiredValue<string>(nameof(TicketingOptions.InternalBaseUrl));
    }
}
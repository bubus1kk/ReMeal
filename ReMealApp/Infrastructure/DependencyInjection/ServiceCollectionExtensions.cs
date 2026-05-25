using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ReMeal.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<
            IPartnerAnalyticsService,
            PartnerAnalyticsService>();

        return services;
    }
}
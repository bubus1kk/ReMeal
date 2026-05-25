using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using ReMealApp.ViewModels.Analytics;

namespace Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddScoped<IPartnerAnalyticsService, PartnerAnalyticsService>();

        services.AddTransient<PartnerAnalyticsViewModel>();

        return services;
    }
}
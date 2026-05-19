using Microsoft.Extensions.DependencyInjection;
using ReMeal.Application.Interfaces;
using ReMeal.Application.Services;

namespace ReMeal.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFoodPointService, FoodPointService>();
        services.AddScoped<ILotService, LotService>();
        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReMeal.Application.Interfaces;
using ReMeal.Infrastructure.Data;
using ReMeal.Infrastructure.Repositories;

namespace ReMeal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IFoodPointRepository, FoodPointRepository>();
        services.AddScoped<IFoodLotRepository, FoodLotRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}

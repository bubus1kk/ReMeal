using Microsoft.EntityFrameworkCore;
using ReMeal.Domain.Entities;

namespace ReMeal.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<FoodPoint> FoodPoints => Set<FoodPoint>();

    public DbSet<FoodLot> FoodLots => Set<FoodLot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

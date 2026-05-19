using Domain.Entities;
using Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public sealed class ReMealDbContext : DbContext
    {
        public ReMealDbContext(DbContextOptions<ReMealDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<FoodPoint> FoodPoints => Set<FoodPoint>();

        public DbSet<FoodLot> FoodLots => Set<FoodLot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new FoodPointConfiguration());
            modelBuilder.ApplyConfiguration(new FoodLotConfiguration());
        }
    }
}

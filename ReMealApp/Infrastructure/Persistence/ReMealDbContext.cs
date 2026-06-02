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

        public DbSet<LotComponent> LotComponents => Set<LotComponent>();

        public DbSet<Booking> Bookings => Set<Booking>();

        public DbSet<BookingStatusReference> BookingStatuses => Set<BookingStatusReference>();

        public DbSet<LotStatusReference> LotStatuses => Set<LotStatusReference>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new FoodPointConfiguration());
            modelBuilder.ApplyConfiguration(new FoodLotConfiguration());
            modelBuilder.ApplyConfiguration(new LotComponentConfiguration());
            modelBuilder.ApplyConfiguration(new BookingStatusReferenceConfiguration());
            modelBuilder.ApplyConfiguration(new LotStatusReferenceConfiguration());
            modelBuilder.ApplyConfiguration(new BookingConfiguration());
        }
    }
}

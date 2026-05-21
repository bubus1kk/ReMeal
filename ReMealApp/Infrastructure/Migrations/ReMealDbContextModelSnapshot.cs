using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(ReMealDbContext))]
    partial class ReMealDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            BuildTargetModel(modelBuilder);
        }

        internal static void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity<User>(builder =>
            {
                builder.ToTable("Users");
                builder.HasKey(x => x.Id);
                builder.HasIndex(x => x.Login).IsUnique();

                builder.Property(x => x.Id).ValueGeneratedOnAdd();
                builder.Property(x => x.Login).HasMaxLength(67).IsRequired();
                builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
                builder.Property(x => x.FullName).HasMaxLength(67).IsRequired();
                builder.Property(x => x.Email).HasMaxLength(67).IsRequired();
                builder.Property(x => x.Phone).HasMaxLength(67).IsRequired();
                builder.Property(x => x.AvatarPath).HasMaxLength(1024).HasDefaultValue(string.Empty).IsRequired();
                builder.Property(x => x.Role)
                    .HasConversion(new EnumToStringConverter<UserRole>())
                    .HasMaxLength(67)
                    .HasDefaultValue(UserRole.StudentCustomer)
                    .IsRequired();
            });

            modelBuilder.Entity<FoodPoint>(builder =>
            {
                builder.ToTable("FoodPoints");
                builder.HasKey(x => x.Id);
                builder.HasIndex(x => x.OwnerId);

                builder.Property(x => x.Id).ValueGeneratedOnAdd();
                builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
                builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
                builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
                builder.Property(x => x.Phone).HasMaxLength(50).IsRequired();
                builder.Property(x => x.OwnerId).IsRequired();
                builder.Property(x => x.CreatedAt).IsRequired();
                builder.Property(x => x.IsActive).IsRequired();

                builder.HasOne(x => x.Owner)
                    .WithMany(x => x.FoodPoints)
                    .HasForeignKey(x => x.OwnerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FoodLot>(builder =>
            {
                builder.ToTable("FoodLots");
                builder.HasKey(x => x.Id);
                builder.HasIndex(x => x.FoodPointId);
                builder.HasIndex(x => x.PickupDeadline);
                builder.HasIndex(x => x.Status);

                builder.Property(x => x.Id).ValueGeneratedOnAdd();
                builder.Property(x => x.FoodPointId).IsRequired();
                builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
                builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
                builder.Property(x => x.Composition).HasMaxLength(2000).IsRequired();
                builder.Property(x => x.TotalQuantity).IsRequired();
                builder.Property(x => x.AvailableQuantity).IsRequired();
                builder.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
                builder.Property(x => x.PickupDeadline).IsRequired();
                builder.Property(x => x.Status).HasConversion<int>().IsRequired();
                builder.Property(x => x.ImagePath).HasMaxLength(1024).IsRequired(false);
                builder.Property(x => x.CreatedAt).IsRequired();
                builder.Property(x => x.UpdatedAt).IsRequired();

                builder.HasOne(x => x.FoodPoint)
                    .WithMany(x => x.Lots)
                    .HasForeignKey(x => x.FoodPointId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LotComponent>(builder =>
            {
                builder.ToTable("LotComponents", table =>
                {
                    table.HasCheckConstraint("CK_LotComponents_Quantity_Positive", "Quantity > 0");
                });

                builder.HasKey(x => x.Id);
                builder.HasIndex(x => x.LotId);
                builder.HasIndex(x => new { x.LotId, x.SortOrder });

                builder.Property(x => x.Id).ValueGeneratedNever();
                builder.Property(x => x.LotId).IsRequired();
                builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
                builder.Property(x => x.Quantity).IsRequired();
                builder.Property(x => x.Unit).HasMaxLength(20).HasDefaultValue(LotComponent.DefaultUnit).IsRequired();
                builder.Property(x => x.Composition).HasMaxLength(2000).IsRequired(false);
                builder.Property(x => x.ImagePath).HasMaxLength(1024).IsRequired(false);
                builder.Property(x => x.SortOrder).IsRequired();

                builder.HasOne(x => x.Lot)
                    .WithMany(x => x.Components)
                    .HasForeignKey(x => x.LotId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

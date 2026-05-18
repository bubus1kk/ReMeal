using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReMeal.Domain.Entities;

namespace ReMeal.Infrastructure.Configurations;

public class FoodPointConfiguration : IEntityTypeConfiguration<FoodPoint>
{
    public void Configure(EntityTypeBuilder<FoodPoint> builder)
    {
        builder.ToTable("FoodPoints");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Phone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OwnerId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasMany(x => x.Lots)
            .WithOne(x => x.FoodPoint)
            .HasForeignKey(x => x.FoodPointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OwnerId);
    }
}

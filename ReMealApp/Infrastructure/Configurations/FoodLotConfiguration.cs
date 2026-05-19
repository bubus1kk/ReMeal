using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class FoodLotConfiguration : IEntityTypeConfiguration<FoodLot>
    {
        public void Configure(EntityTypeBuilder<FoodLot> builder)
        {
            builder.ToTable("FoodLots");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(2000);

            builder.Property(x => x.Composition)
                .HasMaxLength(2000);

            builder.Property(x => x.TotalQuantity)
                .IsRequired();

            builder.Property(x => x.AvailableQuantity)
                .IsRequired();

            builder.Property(x => x.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.PickupDeadline)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            builder.HasIndex(x => x.FoodPointId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PickupDeadline);
        }
    }
}

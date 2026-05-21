using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class LotComponentConfiguration : IEntityTypeConfiguration<LotComponent>
    {
        public void Configure(EntityTypeBuilder<LotComponent> builder)
        {
            builder.ToTable("LotComponents", table =>
            {
                table.HasCheckConstraint("CK_LotComponents_Quantity_Positive", "Quantity > 0");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.LotId)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.Unit)
                .HasMaxLength(20)
                .HasDefaultValue(LotComponent.DefaultUnit)
                .IsRequired();

            builder.Property(x => x.Composition)
                .HasMaxLength(2000)
                .IsRequired(false);

            builder.Property(x => x.ImagePath)
                .HasMaxLength(1024)
                .IsRequired(false);

            builder.Property(x => x.SortOrder)
                .IsRequired();

            builder.HasOne(x => x.Lot)
                .WithMany(x => x.Components)
                .HasForeignKey(x => x.LotId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.LotId);
            builder.HasIndex(x => new { x.LotId, x.SortOrder });
        }
    }
}

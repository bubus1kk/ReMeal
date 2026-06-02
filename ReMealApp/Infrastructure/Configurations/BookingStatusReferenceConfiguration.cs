using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class BookingStatusReferenceConfiguration : IEntityTypeConfiguration<BookingStatusReference>
{
    public void Configure(EntityTypeBuilder<BookingStatusReference> builder)
    {
        builder.ToTable("BookingStatuses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasData(
            new { Id = (int)BookingStatus.Active, Name = "Активное" },
            new { Id = (int)BookingStatus.Cancelled, Name = "Отменено" },
            new { Id = (int)BookingStatus.Issued, Name = "Выдано" });
    }
}

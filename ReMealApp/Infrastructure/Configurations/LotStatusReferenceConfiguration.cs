using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class LotStatusReferenceConfiguration : IEntityTypeConfiguration<LotStatusReference>
{
    public void Configure(EntityTypeBuilder<LotStatusReference> builder)
    {
        builder.ToTable("LotStatuses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasData(
            new { Id = (int)LotStatus.Active, Name = "Активный" },
            new { Id = (int)LotStatus.SoldOut, Name = "Распродан" },
            new { Id = (int)LotStatus.Expired, Name = "Истек" },
            new { Id = (int)LotStatus.Cancelled, Name = "Отменен" });
    }
}

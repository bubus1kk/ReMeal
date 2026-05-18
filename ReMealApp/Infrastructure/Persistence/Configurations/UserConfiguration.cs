using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(user => user.Id);

            builder.Property(user => user.Login)
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(user => user.Login)
                .IsUnique();

            builder.Property(user => user.PasswordHash)
                .HasMaxLength(512)
                .IsRequired();

            builder.Property(user => user.FullName)
                .HasMaxLength(160)
                .IsRequired();

            builder.Property(user => user.Email)
                .HasMaxLength(160)
                .IsRequired();

            builder.Property(user => user.Phone)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(user => user.Role)
                .HasConversion<string>()
                .HasMaxLength(64)
                .HasDefaultValue(UserRole.StudentCustomer)
                .IsRequired();
        }
    }
}

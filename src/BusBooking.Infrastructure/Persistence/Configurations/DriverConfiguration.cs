using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.LicenseNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.LicenseNumber)
            .IsUnique();

        builder.Property(x => x.LicenseExpiryDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.IsActive);

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}

using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class BusConfiguration : IEntityTypeConfiguration<Bus>
{
    public void Configure(EntityTypeBuilder<Bus> builder)
    {
        builder.ToTable("Buses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.RegistrationNumber)
            .IsUnique();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.BusType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(x => x.Status);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.SeatLayout)
            .WithMany()
            .HasForeignKey(x => x.SeatLayoutId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

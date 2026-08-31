using BusBooking.Domain.Entities;
using BusBooking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BookingNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.BookingNumber).IsUnique();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.TripId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);

        builder.Navigation(x => x.Passengers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Passengers)
            .WithOne()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

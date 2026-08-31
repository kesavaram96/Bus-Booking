using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class TripSeatConfiguration : IEntityTypeConfiguration<TripSeat>
{
    public void Configure(EntityTypeBuilder<TripSeat> builder)
    {
        builder.ToTable("TripSeats");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.LockId)
            .HasMaxLength(100);

        // A seat must never be duplicated within a trip.
        builder.HasIndex(x => new { x.TripId, x.SeatId }).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Seat)
            .WithMany()
            .HasForeignKey(x => x.SeatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

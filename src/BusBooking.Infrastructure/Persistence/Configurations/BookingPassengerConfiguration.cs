using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class BookingPassengerConfiguration : IEntityTypeConfiguration<BookingPassenger>
{
    public void Configure(EntityTypeBuilder<BookingPassenger> builder)
    {
        builder.ToTable("BookingPassengers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Gender)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.NIC)
            .HasMaxLength(20);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.Fare)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.HasIndex(x => x.BookingId);

        // Two separate FKs to the same RouteStops table — distinguished by ForeignKey, no
        // shared navigation, so EF Core generates two independent constraints without ambiguity.
        builder.HasOne(x => x.PickupStop)
            .WithMany()
            .HasForeignKey(x => x.PickupStopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DropOffStop)
            .WithMany()
            .HasForeignKey(x => x.DropOffStopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Seat)
            .WithMany()
            .HasForeignKey(x => x.SeatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

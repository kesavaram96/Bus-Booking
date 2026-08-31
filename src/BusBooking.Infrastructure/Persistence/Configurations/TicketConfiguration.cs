using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TicketNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.TicketNumber).IsUnique();

        builder.Property(x => x.TicketCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(x => x.TicketCode).IsUnique();

        // One ticket per passenger.
        builder.HasIndex(x => x.BookingPassengerId).IsUnique();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.BookingId);

        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BookingPassenger)
            .WithMany()
            .HasForeignKey(x => x.BookingPassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("Trips");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TripDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.DepartureTime)
            .IsRequired();

        builder.Property(x => x.ExpectedArrivalTime)
            .IsRequired();

        builder.Property(x => x.Fare)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.TripDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RouteId);
        builder.HasIndex(x => x.BusId);

        // Matches customer trip search's exact filter combination (route + date + scheduled-only).
        builder.HasIndex(x => new { x.RouteId, x.TripDate, x.Status });

        builder.HasOne(x => x.Route)
            .WithMany()
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Bus)
            .WithMany()
            .HasForeignKey(x => x.BusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

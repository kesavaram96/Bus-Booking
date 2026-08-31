using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("RouteStops", t => t.HasCheckConstraint("CK_RouteStops_StopOrder_Positive", "[StopOrder] > 0"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StopName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.StopOrder)
            .IsRequired();

        builder.Property(x => x.ExpectedArrivalTime);

        builder.Property(x => x.ExpectedDepartureTime);

        builder.Property(x => x.AllowPickup)
            .IsRequired();

        builder.Property(x => x.AllowDropOff)
            .IsRequired();

        builder.HasIndex(x => new { x.RouteId, x.StopOrder })
            .IsUnique();
    }
}

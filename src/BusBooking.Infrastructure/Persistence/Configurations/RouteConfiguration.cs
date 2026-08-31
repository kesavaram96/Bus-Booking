using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("Routes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.From)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.To)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Supports customer trip search by From/To (Phase 09).
        builder.HasIndex(x => new { x.From, x.To });
        builder.HasIndex(x => x.IsActive);

        builder.Navigation(x => x.Stops)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Stops)
            .WithOne()
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

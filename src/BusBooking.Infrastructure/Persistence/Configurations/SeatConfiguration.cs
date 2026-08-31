using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seats", t =>
        {
            t.HasCheckConstraint("CK_Seats_Row_NonNegative", "[Row] >= 0");
            t.HasCheckConstraint("CK_Seats_Column_NonNegative", "[Column] >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SeatNumber)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Row)
            .IsRequired();

        builder.Property(x => x.Column)
            .IsRequired();

        builder.Property(x => x.PositionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => new { x.SeatLayoutId, x.SeatNumber })
            .IsUnique();
    }
}

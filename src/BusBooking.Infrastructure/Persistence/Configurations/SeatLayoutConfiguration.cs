using BusBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class SeatLayoutConfiguration : IEntityTypeConfiguration<SeatLayout>
{
    public void Configure(EntityTypeBuilder<SeatLayout> builder)
    {
        builder.ToTable("SeatLayouts", t =>
        {
            t.HasCheckConstraint("CK_SeatLayouts_Rows_Positive", "[Rows] > 0");
            t.HasCheckConstraint("CK_SeatLayouts_Columns_Positive", "[Columns] > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Rows)
            .IsRequired();

        builder.Property(x => x.Columns)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Navigation(x => x.Seats)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Seats)
            .WithOne()
            .HasForeignKey(x => x.SeatLayoutId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

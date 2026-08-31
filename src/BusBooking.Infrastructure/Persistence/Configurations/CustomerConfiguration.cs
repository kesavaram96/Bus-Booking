using BusBooking.Domain.Entities;
using BusBooking.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NIC)
            .HasMaxLength(20);

        builder.Property(x => x.DateOfBirth)
            .HasColumnType("date");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Shared primary key 1:1 with the Identity user: Customer.Id IS the ApplicationUser.Id.
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Customer>(c => c.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

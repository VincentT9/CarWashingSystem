using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.BookingID);
            builder.Property(b => b.BookingDate).IsRequired();
            builder.Property(b => b.BookingStatus).HasConversion<int>();
            builder.Property(b => b.EstimatedTotalAmount).HasPrecision(18, 2);
            builder.Property(b => b.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.HasOne(b => b.Customer)
                   .WithMany(c => c.Bookings)
                   .HasForeignKey(b => b.CustomerID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.Vehicle)
                   .WithMany(v => v.Bookings)
                   .HasForeignKey(b => b.VehicleID)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(b => b.Branch)
                   .WithMany(br => br.Bookings)
                   .HasForeignKey(b => b.BranchID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

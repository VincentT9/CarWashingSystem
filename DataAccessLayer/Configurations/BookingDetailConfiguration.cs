using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class BookingDetailConfiguration : IEntityTypeConfiguration<BookingDetail>
    {
        public void Configure(EntityTypeBuilder<BookingDetail> builder)
        {
            builder.HasKey(d => d.BookingDetailID);
            builder.Property(d => d.Quantity).IsRequired();
            builder.Property(d => d.UnitPrice).HasPrecision(18, 2);
            builder.HasOne(d => d.Booking).WithMany(b => b.BookingDetails).HasForeignKey(d => d.BookingID).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(d => d.Service).WithMany(s => s.BookingDetails).HasForeignKey(d => d.ServiceID).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

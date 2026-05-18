using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.PaymentID);
            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.PaymentMethod).HasConversion<int>();
            builder.Property(p => p.PaymentStatus).HasConversion<int>();
            builder.HasOne(p => p.Booking).WithMany(b => b.Payments).HasForeignKey(p => p.BookingID).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

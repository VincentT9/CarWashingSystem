using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class WashHistoryConfiguration : IEntityTypeConfiguration<WashHistory>
    {
        public void Configure(EntityTypeBuilder<WashHistory> builder)
        {
            builder.HasKey(w => w.WashHistoryID);
            builder.Property(w => w.WashDate).IsRequired();
            builder.Property(w => w.ActualTotalAmount).HasPrecision(18, 2);
            builder.Property(w => w.DiscountAmount).HasPrecision(18, 2);
            builder.Property(w => w.FinalAmount).HasPrecision(18, 2);
            builder.Property(w => w.RewardUsed).HasPrecision(18,2);
            builder.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.HasOne(w => w.Booking).WithMany(b => b.WashHistories).HasForeignKey(w => w.BookingID).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

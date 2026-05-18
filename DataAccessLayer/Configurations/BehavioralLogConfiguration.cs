using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class BehavioralLogConfiguration : IEntityTypeConfiguration<BehavioralLog>
    {
        public void Configure(EntityTypeBuilder<BehavioralLog> builder)
        {
            builder.HasKey(b => b.LogID);
            builder.Property(b => b.ActionType).HasConversion<int>();
            builder.Property(b => b.ActionTime).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(b => b.PointsChanged).HasDefaultValue(0);
            builder.Property(b => b.SpendingAmount).HasPrecision(18, 2).HasDefaultValue(0);
            builder.Property(b => b.RewardUsed).HasPrecision(18, 2).HasDefaultValue(0);
            builder.HasOne(b => b.Customer)
                    .WithMany(c => c.BehavioralLogs)
                    .HasForeignKey(b => b.CustomerID)
                    .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(b => b.Booking)
                   .WithMany(bk => bk.BehavioralLogs)
                   .HasForeignKey(b => b.BookingID)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

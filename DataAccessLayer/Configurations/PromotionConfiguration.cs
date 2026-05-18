using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.HasKey(p => p.PromotionID);
            builder.Property(p => p.PromotionName).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Description).HasMaxLength(1000);
            builder.Property(p => p.DiscountPercent).HasPrecision(5, 2);
            builder.Property(p => p.MinimumSpend).HasPrecision(18, 2);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.HasOne(p => p.MinTier).WithMany(t => t.Promotions).HasForeignKey(p => p.MinTierID).OnDelete(DeleteBehavior.SetNull);
        }
    }
}

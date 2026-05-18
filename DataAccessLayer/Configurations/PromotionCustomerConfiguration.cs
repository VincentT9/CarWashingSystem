using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class PromotionCustomerConfiguration : IEntityTypeConfiguration<PromotionCustomer>
    {
        public void Configure(EntityTypeBuilder<PromotionCustomer> builder)
        {
            builder.HasKey(pc => pc.PromotionCustomerID);
            builder.HasOne(pc => pc.Promotion).WithMany(p => p.PromotionCustomers).HasForeignKey(pc => pc.PromotionID).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(pc => pc.Customer).WithMany(c => c.PromotionCustomers).HasForeignKey(pc => pc.CustomerID).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

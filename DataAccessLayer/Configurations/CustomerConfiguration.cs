using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.HasKey(c => c.CustomerID);
            builder.Property(c => c.CurrentPoints).HasDefaultValue(0);
            builder.Property(c => c.LifetimePoints).HasDefaultValue(0);
            builder.Property(c => c.TotalSpent).HasPrecision(18, 2).HasDefaultValue(0);
            builder.Property(c => c.TotalVisits).HasDefaultValue(0);
            builder.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.HasOne(c => c.User).WithMany(u => u.Customers).HasForeignKey(c => c.UserID).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(c => c.Tier).WithMany(t => t.Customers).HasForeignKey(c => c.TierID).OnDelete(DeleteBehavior.SetNull);
        }
    }
}

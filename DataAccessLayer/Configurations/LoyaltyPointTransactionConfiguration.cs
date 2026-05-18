using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class LoyaltyPointTransactionConfiguration : IEntityTypeConfiguration<LoyaltyPointTransaction>
    {
        public void Configure(EntityTypeBuilder<LoyaltyPointTransaction> builder)
        {
            builder.HasKey(t => t.TransactionID);
            builder.Property(t => t.Points).IsRequired();
            builder.Property(t => t.TransactionType).HasConversion<int>();
            builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.HasOne(t => t.Customer).WithMany(c => c.Transactions).HasForeignKey(t => t.CustomerID).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

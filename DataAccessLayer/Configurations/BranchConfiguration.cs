using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.HasKey(b => b.BranchID);
            builder.Property(b => b.BranchName).IsRequired().HasMaxLength(200);
            builder.Property(b => b.Address).HasMaxLength(500);
            builder.Property(b => b.PhoneNumber).HasMaxLength(50);
            builder.Property(b => b.Status).HasConversion<int>();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.UserID);
            builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.FullName).HasMaxLength(200);
            builder.Property(u => u.Email).HasMaxLength(200);
            builder.Property(u => u.PhoneNumber).HasMaxLength(50);
            builder.Property(u => u.Status).HasConversion<int>();
            builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.HasMany(u => u.Customers).WithOne(c => c.User).HasForeignKey(c => c.UserID).OnDelete(DeleteBehavior.Cascade);
        }
    }
}

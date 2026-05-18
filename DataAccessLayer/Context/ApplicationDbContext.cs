using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Entity;

namespace DataAccessLayer.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<LoyaltyTier> LoyaltyTiers { get; set; } = null!;
        public DbSet<Vehicle> Vehicles { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Booking> Bookings { get; set; } = null!;
        public DbSet<BookingDetail> BookingDetails { get; set; } = null!;
        public DbSet<WashHistory> WashHistories { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<LoyaltyPointTransaction> LoyaltyPointTransactions { get; set; } = null!;
        public DbSet<Promotion> Promotions { get; set; } = null!;
        public DbSet<PromotionCustomer> PromotionCustomers { get; set; } = null!;
        public DbSet<BehavioralLog> BehavioralLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}

using BusinessLayer.IService;
using BusinessLayer.Service;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessLayer.Tests.Customers
{
    public class CustomerServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<ICurrentCustomerService> _currentCustomerMock;

        public CustomerServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _currentCustomerMock = new Mock<ICurrentCustomerService>();
        }

        [Fact]
        public async Task GetProfileByCustomerIdAsync_ValidId_ReturnsProfileWithTierPerks()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var userId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var tierId = Guid.NewGuid();

            var user = new User
            {
                UserID = userId,
                Username = "testcustomer",
                PasswordHash = "hashedpassword",
                Email = "customer@example.com",
                FullName = "Test Customer",
                PhoneNumber = "0900000000"
            };

            var tier = new LoyaltyTier
            {
                TierID = tierId,
                TierName = "Gold",
                TierRank = 2
            };

            var customer = new Customer
            {
                CustomerID = customerId,
                UserID = userId,
                User = user,
                TierID = tierId,
                Tier = tier,
                CurrentPoints = 150,
                LifetimePoints = 500,
                TotalSpent = 2500000,
                TotalVisits = 5
            };

            var benefit = new TierBenefit
            {
                TierBenefitID = Guid.NewGuid(),
                TierID = tierId,
                BenefitName = "10% Discount on All Washes",
                IsActive = true
            };

            context.Users.Add(user);
            context.LoyaltyTiers.Add(tier);
            context.Customers.Add(customer);
            context.TierBenefits.Add(benefit);
            await context.SaveChangesAsync();

            _currentCustomerMock.Setup(x => x.GetCurrentCustomerIdAsync()).ReturnsAsync(customerId);
            var service = new CustomerService(context, _currentCustomerMock.Object);

            var profile = await service.GetMyProfileAsync();

            Assert.NotNull(profile);
            Assert.Equal("testcustomer", profile.Username);
            Assert.Equal("Gold", profile.TierName);
            Assert.Equal(150, profile.CurrentPoints);
            Assert.Single(profile.TierPerks);
            Assert.Equal("10% Discount on All Washes", profile.TierPerks.First());
        }

        [Fact]
        public async Task GetProfileByCustomerIdAsync_NonExistentCustomer_ThrowsKeyNotFoundException()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var service = new CustomerService(context, _currentCustomerMock.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProfileByCustomerIdAsync(Guid.NewGuid()));
        }
    }
}

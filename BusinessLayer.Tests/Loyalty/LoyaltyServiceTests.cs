using BusinessLayer.Dtos.Loyalty;
using BusinessLayer.IService;
using BusinessLayer.Service.Loyalty;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessLayer.Tests.Loyalty
{
    public class LoyaltyServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<IBehavioralLogWriter> _behavioralLogWriterMock;

        public LoyaltyServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _behavioralLogWriterMock = new Mock<IBehavioralLogWriter>();
        }

        [Fact]
        public async Task GetSettingsAsync_ReturnsDefaultLoyaltySettings()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var service = new LoyaltyService(context, _behavioralLogWriterMock.Object);

            var settings = await service.GetSettingsAsync();

            Assert.NotNull(settings);
            Assert.Equal(10000m, settings.PointEarnRateAmount);
            Assert.Equal(1, settings.PointEarnRatePoints);
            Assert.Equal(12, settings.PointExpiryMonths);
        }

        [Fact]
        public async Task CreateTierAsync_ValidRequest_CreatesLoyaltyTier()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var service = new LoyaltyService(context, _behavioralLogWriterMock.Object);

            var request = new CreateLoyaltyTierRequest
            {
                TierName = "Silver",
                TierRank = 1,
                MinSpentThreshold = 1000000,
                MinVisitsThreshold = 3,
                PointMultiplier = 1.2m,
                QualificationPeriodMonths = 6
            };

            var result = await service.CreateTierAsync(request);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal("Silver", result.Data.TierName);

            var tierInDb = await context.LoyaltyTiers.FirstOrDefaultAsync(t => t.TierName == "Silver");
            Assert.NotNull(tierInDb);
            Assert.Equal(1.2m, tierInDb.PointMultiplier);
        }

        [Fact]
        public async Task GetPointBalanceAsync_CustomerExists_ReturnsCorrectPoints()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var customerId = Guid.NewGuid();

            var tier = new LoyaltyTier { TierID = Guid.NewGuid(), TierName = "Bronze", TierRank = 1, PointMultiplier = 1.0m };
            var customer = new Customer
            {
                CustomerID = customerId,
                CurrentPoints = 250,
                LifetimePoints = 1000,
                TierID = tier.TierID,
                Tier = tier
            };

            context.LoyaltyTiers.Add(tier);
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new LoyaltyService(context, _behavioralLogWriterMock.Object);

            var result = await service.GetPointBalanceAsync(customerId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(250, result.Data.CurrentPoints);
            Assert.Equal(1000, result.Data.LifetimePoints);
            Assert.Equal("Bronze", result.Data.TierName);
        }

        [Fact]
        public async Task CompleteWashAsync_EarnsPointsAndUpdateCustomerStats()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var customerId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();

            var tier = new LoyaltyTier { TierID = Guid.NewGuid(), TierName = "Bronze", TierRank = 1, PointMultiplier = 1.0m, Status = LoyaltyTierStatusEnum.Active };
            var customer = new Customer
            {
                CustomerID = customerId,
                CurrentPoints = 0,
                LifetimePoints = 0,
                TotalSpent = 0,
                TotalVisits = 0,
                TierID = tier.TierID,
                Tier = tier
            };

            context.LoyaltyTiers.Add(tier);
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var service = new LoyaltyService(context, _behavioralLogWriterMock.Object);

            var payload = new WashCompletionPayload
            {
                BookingId = bookingId,
                CustomerId = customerId,
                ServiceId = serviceId,
                Amount = 200000,
                CompletedAt = DateTime.UtcNow
            };

            await service.CompleteWashAsync(payload);

            var updatedCustomer = await context.Customers.FirstAsync(c => c.CustomerID == customerId);
            Assert.Equal(200000, updatedCustomer.TotalSpent);
            Assert.Equal(1, updatedCustomer.TotalVisits);
            Assert.Equal(20, updatedCustomer.CurrentPoints); // 200,000 / 10,000 = 20 points
        }
    }
}

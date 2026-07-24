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
    public class PromotionServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<IBehavioralLogWriter> _behavioralLogWriterMock;

        public PromotionServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _behavioralLogWriterMock = new Mock<IBehavioralLogWriter>();
        }

        [Fact]
        public async Task CreatePromotionAsync_ValidRequest_CreatesActivePromotion()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var service = new LoyaltyService(context, _behavioralLogWriterMock.Object);

            var request = new CreatePromotionRequest
            {
                PromotionName = "Summer Special 20%",
                Code = "SUMMER20",
                DiscountType = PromotionDiscountTypeEnum.Percentage,
                DiscountValue = 20,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                Priority = 1
            };

            var result = await service.CreatePromotionAsync(request);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal("Summer Special 20%", result.Data.PromotionName);
            Assert.Equal("SUMMER20", result.Data.Code);

            var promotionInDb = await context.Promotions.FirstOrDefaultAsync(p => p.Code == "SUMMER20");
            Assert.NotNull(promotionInDb);
            Assert.Equal(PromotionStatusEnum.Active, promotionInDb.Status);
        }

        [Fact]
        public async Task SendPromotionAsync_ValidCustomers_DeliversPromotionToCustomers()
        {
            using var context = new ApplicationDbContext(_dbOptions);

            var promotionId = Guid.NewGuid();
            var customer1Id = Guid.NewGuid();
            var customer2Id = Guid.NewGuid();

            context.Promotions.Add(new Promotion
            {
                PromotionID = promotionId,
                PromotionName = "VIP Voucher",
                Code = "VIPVOUCHER",
                Status = PromotionStatusEnum.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(15)
            });

            context.Customers.Add(new Customer { CustomerID = customer1Id });
            context.Customers.Add(new Customer { CustomerID = customer2Id });
            await context.SaveChangesAsync();

            var service = new LoyaltyService(context, _behavioralLogWriterMock.Object);

            var sendRequest = new SendPromotionRequest
            {
                CustomerIds = new List<Guid> { customer1Id, customer2Id }
            };

            var result = await service.SendPromotionAsync(promotionId, sendRequest);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.SentCount);
            Assert.Equal(0, result.Data.SkippedCount);

            var sentRecords = await context.PromotionCustomers.Where(pc => pc.PromotionID == promotionId).ToListAsync();
            Assert.Equal(2, sentRecords.Count);
        }
    }
}

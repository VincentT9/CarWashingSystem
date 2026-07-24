using BusinessLayer.Dtos.Operations;
using BusinessLayer.IService;
using BusinessLayer.IService.Operations;
using BusinessLayer.Service.Operations;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessLayer.Tests.Operations
{
    public class OperationsServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<IWashCompletionService> _washCompletionMock;
        private readonly Mock<IBehavioralLogWriter> _behavioralLogMock;

        public OperationsServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _washCompletionMock = new Mock<IWashCompletionService>();
            _behavioralLogMock = new Mock<IBehavioralLogWriter>();
        }

        private OperationsService CreateService(ApplicationDbContext dbContext)
        {
            return new OperationsService(dbContext, _washCompletionMock.Object, _behavioralLogMock.Object);
        }

        [Fact]
        public async Task CreateServiceAsync_ValidRequest_CreatesService()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var service = CreateService(context);

            var request = new CreateServiceRequest
            {
                Name = "Express Wash",
                Description = "Quick exterior wash",
                Price = 120000,
                DurationMinutes = 20
            };

            var result = await service.CreateServiceAsync(request);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal("Express Wash", result.Data.Name);
            Assert.Equal(120000, result.Data.Price);

            var serviceInDb = await context.Services.FirstOrDefaultAsync(s => s.ServiceName == "Express Wash");
            Assert.NotNull(serviceInDb);
        }

        [Fact]
        public async Task CreateBranchAsync_ValidRequest_CreatesBranch()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var service = CreateService(context);

            var request = new CreateBranchRequest
            {
                Name = "District 7 Branch",
                Address = "456 Nguyen Van Linh",
                Phone = "0909123456",
                OpenTime = new TimeSpan(8, 0, 0),
                CloseTime = new TimeSpan(20, 0, 0)
            };

            var result = await service.CreateBranchAsync(request);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal("District 7 Branch", result.Data.Name);
        }

        [Fact]
        public async Task CreatePaymentAsync_ValidRequest_CreatesPendingPayment()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var bookingId = Guid.NewGuid();

            context.Bookings.Add(new Booking
            {
                BookingID = bookingId,
                EstimatedTotalAmount = 150000,
                BookingStatus = BookingStatusEnum.Pending,
                ScheduledStart = DateTime.UtcNow,
                ScheduledEnd = DateTime.UtcNow.AddMinutes(30)
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var request = new CreatePaymentRequest
            {
                BookingId = bookingId,
                Amount = 150000,
                Method = "Cash",
                Note = "Payment upon arrival"
            };

            var result = await service.CreatePaymentAsync(request);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal(PaymentStatusEnum.Pending.ToString(), result.Data.Status);
            Assert.Equal(150000, result.Data.Amount);
        }

        [Fact]
        public async Task MarkPaymentPaidAsync_PendingPayment_UpdatesStatusToPaid()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var paymentId = Guid.NewGuid();

            context.Payments.Add(new Payment
            {
                PaymentID = paymentId,
                BookingID = Guid.NewGuid(),
                Amount = 150000,
                PaymentMethod = PaymentMethodEnum.Cash,
                PaymentStatus = PaymentStatusEnum.Pending
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var request = new MarkPaymentPaidRequest
            {
                ReferenceNumber = "REF123456",
                Note = "Cash received"
            };

            var result = await service.MarkPaymentPaidAsync(paymentId, request);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal(PaymentStatusEnum.Paid.ToString(), result.Data.Status);
            Assert.Equal("REF123456", result.Data.ReferenceNumber);
        }
    }
}

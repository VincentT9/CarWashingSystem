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
using ServiceEntity = DataAccessLayer.Entity.Service;

namespace BusinessLayer.Tests.Operations
{
    public class BookingServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<IWashCompletionService> _washCompletionMock;
        private readonly Mock<IBehavioralLogWriter> _behavioralLogMock;

        public BookingServiceTests()
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
        public async Task CreateBookingAsync_ValidParameters_CreatesPendingBooking()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var customerId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();
            var washBayId = Guid.NewGuid();

            var tier = new LoyaltyTier { TierID = Guid.NewGuid(), TierName = "Bronze", PriorityLevel = 1, BookingWindowDays = 7, Status = LoyaltyTierStatusEnum.Active };
            var customer = new Customer { CustomerID = customerId, UserID = Guid.NewGuid(), TierID = tier.TierID, Tier = tier };
            var vehicle = new Vehicle { VehicleID = vehicleId, CustomerID = customerId, LicensePlate = "51H12345" };
            var branch = new Branch { BranchID = branchId, BranchName = "District 1 Branch", Address = "123 Main St", OpenTime = new TimeSpan(7, 0, 0), CloseTime = new TimeSpan(21, 0, 0), Status = BranchStatusEnum.Open };
            var washBay = new WashBay { WashBayID = washBayId, BranchID = branchId, BayName = "Bay 1", Status = WashBayStatusEnum.Active };
            var serviceEntity = new ServiceEntity { ServiceID = serviceId, ServiceName = "Basic Wash", Price = 80000, EstimatedDuration = TimeSpan.FromMinutes(30), Status = ServiceStatusEnum.Active };

            context.LoyaltyTiers.Add(tier);
            context.Customers.Add(customer);
            context.Vehicles.Add(vehicle);
            context.Branches.Add(branch);
            context.WashBays.Add(washBay);
            context.Services.Add(serviceEntity);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var request = new CreateBookingRequest
            {
                VehicleId = vehicleId,
                BranchId = branchId,
                ServiceId = serviceId,
                BookingStartTime = DateTime.Today.AddDays(1).AddHours(10),
                Note = "Clean wheels"
            };

            var result = await service.CreateBookingAsync(request, customerId);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal(BookingStatusEnum.Pending.ToString(), result.Data.Status);
            Assert.Equal(80000, result.Data.TotalAmount);
        }

        [Fact]
        public async Task CancelBookingAsync_PendingBooking_UpdatesStatusToCancelled()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var customerId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();

            var booking = new Booking
            {
                BookingID = bookingId,
                CustomerID = customerId,
                BookingStatus = BookingStatusEnum.Pending,
                ScheduledStart = DateTime.Today.AddDays(1).AddHours(10),
                ScheduledEnd = DateTime.Today.AddDays(1).AddHours(11)
            };

            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var request = new CancelBookingRequest
            {
                Reason = "Change of schedule"
            };

            var result = await service.CancelBookingAsync(bookingId, request, customerId, false);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Data);
            Assert.Equal(BookingStatusEnum.Cancelled.ToString(), result.Data.Status);
        }
    }
}

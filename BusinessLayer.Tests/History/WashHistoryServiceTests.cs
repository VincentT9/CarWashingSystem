using BusinessLayer.IService;
using BusinessLayer.Service;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessLayer.Tests.History
{
    public class WashHistoryServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<ICurrentCustomerService> _currentCustomerMock;

        public WashHistoryServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _currentCustomerMock = new Mock<ICurrentCustomerService>();
        }

        [Fact]
        public async Task GetMyHistoryAsync_ReturnsPagedWashHistoryForCustomer()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var customerId = Guid.NewGuid();
            _currentCustomerMock.Setup(x => x.GetCurrentCustomerIdAsync()).ReturnsAsync(customerId);

            var branch = new Branch { BranchID = Guid.NewGuid(), BranchName = "District 1 Branch" };
            var vehicle = new Vehicle { VehicleID = Guid.NewGuid(), CustomerID = customerId, LicensePlate = "51H12345" };
            var washService = new Service { ServiceID = Guid.NewGuid(), ServiceName = "Full Car Wash", Price = 150000 };

            var booking = new Booking
            {
                BookingID = Guid.NewGuid(),
                CustomerID = customerId,
                BranchID = branch.BranchID,
                VehicleID = vehicle.VehicleID,
                Branch = branch,
                Vehicle = vehicle,
                BookingDetails = [new BookingDetail { ServiceID = washService.ServiceID, Service = washService }]
            };

            var history = new WashHistory
            {
                WashHistoryID = Guid.NewGuid(),
                BookingID = booking.BookingID,
                Booking = booking,
                WashDate = DateTime.UtcNow,
                ActualTotalAmount = 150000,
                DiscountAmount = 0,
                FinalAmount = 150000,
                PointsEarned = 15
            };

            context.Branches.Add(branch);
            context.Vehicles.Add(vehicle);
            context.Services.Add(washService);
            context.Bookings.Add(booking);
            context.WashHistories.Add(history);
            await context.SaveChangesAsync();

            var service = new WashHistoryService(context, _currentCustomerMock.Object);

            var result = await service.GetMyHistoryAsync(1, 10);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal("51H12345", result.Items.First().VehiclePlate);
            Assert.Equal("District 1 Branch", result.Items.First().BranchName);
        }

        [Fact]
        public async Task GetMyHistoryDetailAsync_NotFound_ThrowsKeyNotFoundException()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var customerId = Guid.NewGuid();
            _currentCustomerMock.Setup(x => x.GetCurrentCustomerIdAsync()).ReturnsAsync(customerId);

            var service = new WashHistoryService(context, _currentCustomerMock.Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetMyHistoryDetailAsync(Guid.NewGuid()));
        }
    }
}

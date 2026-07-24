using BusinessLayer.Dtos.Vehicle;
using BusinessLayer.IService;
using BusinessLayer.Service;
using BusinessLayer.Validators;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BusinessLayer.Tests.Vehicles
{
    public class VehicleServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly Mock<ICurrentCustomerService> _currentCustomerMock;
        private readonly Mock<IVehicleOwnershipValidator> _ownershipValidatorMock;

        public VehicleServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _currentCustomerMock = new Mock<ICurrentCustomerService>();
            _ownershipValidatorMock = new Mock<IVehicleOwnershipValidator>();
        }

        private VehicleService CreateService(ApplicationDbContext dbContext)
        {
            return new VehicleService(
                dbContext,
                _currentCustomerMock.Object,
                _ownershipValidatorMock.Object,
                new CreateVehicleRequestValidator(),
                new UpdateVehicleRequestValidator()
            );
        }

        [Fact]
        public void CreateVehicleRequestValidator_EmptyLicensePlate_FailsValidation()
        {
            var validator = new CreateVehicleRequestValidator();
            var dto = new CreateVehicleRequestDto
            {
                LicensePlate = ""
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVehicleRequestDto.LicensePlate));
        }

        [Fact]
        public async Task CreateVehicleAsync_ValidPayload_CreatesVehicle()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var customerId = Guid.NewGuid();
            _currentCustomerMock.Setup(x => x.GetCurrentCustomerIdAsync()).ReturnsAsync(customerId);

            var service = CreateService(context);

            var dto = new CreateVehicleRequestDto
            {
                LicensePlate = "51H-12345",
                VehicleType = VehicleTypeEnum.Sedan,
                Brand = "Toyota",
                Model = "Camry",
                Color = "White"
            };

            var result = await service.CreateVehicleAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("51H12345", result.LicensePlate);
            Assert.Equal(customerId, result.CustomerID);
            Assert.Equal(VehicleStatusEnum.Active, result.Status);

            var vehicleInDb = await context.Vehicles.FirstOrDefaultAsync(v => v.VehicleID == result.VehicleID);
            Assert.NotNull(vehicleInDb);
        }

        [Fact]
        public async Task CreateVehicleAsync_DuplicateLicensePlate_ThrowsInvalidOperationException()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var customerId = Guid.NewGuid();
            _currentCustomerMock.Setup(x => x.GetCurrentCustomerIdAsync()).ReturnsAsync(customerId);

            context.Vehicles.Add(new Vehicle
            {
                CustomerID = customerId,
                LicensePlate = "51H12345",
                VehicleType = VehicleTypeEnum.Sedan,
                Status = VehicleStatusEnum.Active
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new CreateVehicleRequestDto
            {
                LicensePlate = "51H-12345",
                VehicleType = VehicleTypeEnum.SUV
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateVehicleAsync(dto));
        }

        [Fact]
        public async Task UpdateVehicleAsync_ValidPayload_UpdatesVehicle()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var customerId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();

            _currentCustomerMock.Setup(x => x.GetCurrentCustomerIdAsync()).ReturnsAsync(customerId);

            context.Vehicles.Add(new Vehicle
            {
                VehicleID = vehicleId,
                CustomerID = customerId,
                LicensePlate = "30A99999",
                VehicleType = VehicleTypeEnum.Sedan,
                Brand = "Honda",
                Model = "Civic",
                Color = "Black",
                Status = VehicleStatusEnum.Active
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var updateDto = new UpdateVehicleRequestDto
            {
                Color = "Red",
                Model = "Civic RS"
            };

            var result = await service.UpdateVehicleAsync(vehicleId, updateDto);

            Assert.NotNull(result);
            Assert.Equal("Red", result.Color);
            Assert.Equal("Civic RS", result.Model);

            _ownershipValidatorMock.Verify(x => x.EnsureOwnedByCustomerAsync(vehicleId, customerId), Times.Once);
        }
    }
}

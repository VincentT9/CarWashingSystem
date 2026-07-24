using BusinessLayer.Service;
using DataAccessLayer.Context;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ServiceEntity = DataAccessLayer.Entity.Service;

namespace BusinessLayer.Tests.Services
{
    public class ServiceCatalogServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;

        public ServiceCatalogServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task GetActiveServicesAsync_ReturnsOnlyActiveServicesOrderedByPrice()
        {
            using var context = new ApplicationDbContext(_dbOptions);

            var activeService1 = new ServiceEntity
            {
                ServiceID = Guid.NewGuid(),
                ServiceName = "Premium Wash",
                Price = 200000,
                Status = ServiceStatusEnum.Active
            };

            var activeService2 = new ServiceEntity
            {
                ServiceID = Guid.NewGuid(),
                ServiceName = "Basic Wash",
                Price = 80000,
                Status = ServiceStatusEnum.Active
            };

            var inactiveService = new ServiceEntity
            {
                ServiceID = Guid.NewGuid(),
                ServiceName = "Deprecated Wash",
                Price = 50000,
                Status = ServiceStatusEnum.Inactive
            };

            context.Services.AddRange(activeService1, activeService2, inactiveService);
            await context.SaveChangesAsync();

            var service = new ServiceCatalogService(context);

            var result = await service.GetActiveServicesAsync();

            Assert.Equal(2, result.Count);
            Assert.Equal("Basic Wash", result[0].ServiceName);
            Assert.Equal("Premium Wash", result[1].ServiceName);
        }

        [Fact]
        public async Task IsValidActiveServiceIdAsync_ChecksServiceStatusCorrectly()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var activeId = Guid.NewGuid();
            var inactiveId = Guid.NewGuid();

            context.Services.Add(new ServiceEntity { ServiceID = activeId, ServiceName = "Active", Price = 100000, Status = ServiceStatusEnum.Active });
            context.Services.Add(new ServiceEntity { ServiceID = inactiveId, ServiceName = "Inactive", Price = 100000, Status = ServiceStatusEnum.Inactive });
            await context.SaveChangesAsync();

            var service = new ServiceCatalogService(context);

            Assert.True(await service.IsValidActiveServiceIdAsync(activeId));
            Assert.False(await service.IsValidActiveServiceIdAsync(inactiveId));
            Assert.False(await service.IsValidActiveServiceIdAsync(Guid.NewGuid()));
        }
    }
}

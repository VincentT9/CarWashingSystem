using BusinessLayer.Dtos.Admin;
using BusinessLayer.Service;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessLayer.Tests.Admin
{
    public class AdminUserServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;

        public AdminUserServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task UpdateUserStatusAsync_ValidUser_UpdatesStatusToInactive()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var userId = Guid.NewGuid();
            var role = new Role { RoleID = Guid.NewGuid(), RoleName = "Customer" };

            var user = new User
            {
                UserID = userId,
                Username = "activeuser",
                FullName = "Active User",
                Email = "active@example.com",
                PasswordHash = "hash",
                RoleID = role.RoleID,
                Role = role,
                Status = UserStatusEnum.Active
            };

            context.Roles.Add(role);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = new AdminUserService(context);

            var request = new UpdateUserStatusRequestDto
            {
                Status = UserStatusEnum.Inactive
            };

            var result = await service.UpdateUserStatusAsync(userId, request);

            Assert.NotNull(result);
            Assert.Equal(UserStatusEnum.Inactive, result.Status);

            var updatedUserInDb = await context.Users.FirstAsync(u => u.UserID == userId);
            Assert.Equal(UserStatusEnum.Inactive, updatedUserInDb.Status);
        }

        [Fact]
        public async Task UpdateUserStatusAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var service = new AdminUserService(context);

            var request = new UpdateUserStatusRequestDto
            {
                Status = UserStatusEnum.Inactive
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateUserStatusAsync(Guid.NewGuid(), request));
        }
    }
}

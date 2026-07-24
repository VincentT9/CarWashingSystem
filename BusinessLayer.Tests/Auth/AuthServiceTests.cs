// AutoWash Pro Test Suite
using BusinessLayer.Dtos.Auth;
using BusinessLayer.Helpers;
using BusinessLayer.IService;
using BusinessLayer.Service;
using BusinessLayer.Validators;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BusinessLayer.Tests.Auth
{
    public class AuthServiceTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly JwtSettings _jwtSettings;
        private readonly Mock<IEmailService> _emailServiceMock;

        public AuthServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _jwtSettings = new JwtSettings
            {
                SecretKey = "SuperSecretTestKeyThatIsAtLeast32CharactersLong!",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                AccessTokenExpirationMinutes = 60
            };

            _emailServiceMock = new Mock<IEmailService>();
        }

        private AuthService CreateService(ApplicationDbContext dbContext)
        {
            var options = Options.Create(_jwtSettings);
            return new AuthService(
                dbContext,
                options,
                _emailServiceMock.Object,
                new RegisterRequestValidator(),
                new LoginRequestValidator(),
                new VerifyEmailRequestValidator(),
                new ResendOtpRequestValidator()
            );
        }

        private async Task SeedCustomerRoleAsync(ApplicationDbContext dbContext)
        {
            if (!await dbContext.Roles.AnyAsync(r => r.RoleName == "Customer"))
            {
                dbContext.Roles.Add(new Role
                {
                    RoleID = Guid.NewGuid(),
                    RoleName = "Customer"
                });
                await dbContext.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_CreatesUserAndCustomer()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedCustomerRoleAsync(context);
            var service = CreateService(context);

            var dto = new RegisterRequestDto
            {
                Username = "newcustomer",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                FullName = "New Customer",
                Email = "newcustomer@example.com",
                PhoneNumber = "0988776655"
            };

            var result = await service.RegisterAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("newcustomer", result.Username);
            Assert.Equal("Customer", result.Role);

            var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Username == "newcustomer");
            Assert.NotNull(userInDb);
            Assert.True(userInDb.EmailVerified);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateUsername_ThrowsValidationException()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedCustomerRoleAsync(context);

            var existingUser = new User
            {
                Username = "existinguser",
                Email = "old@example.com",
                FullName = "Existing User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                RoleID = (await context.Roles.FirstAsync()).RoleID
            };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new RegisterRequestDto
            {
                Username = "existinguser",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                FullName = "Duplicate User",
                Email = "unique@example.com",
                PhoneNumber = "0911223344"
            };

            await Assert.ThrowsAsync<ValidationException>(() => service.RegisterAsync(dto));
        }

        [Fact]
        public async Task LoginAsync_InactiveStatus_ThrowsUnauthorizedAccessException()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedCustomerRoleAsync(context);

            var role = await context.Roles.FirstAsync();
            var user = new User
            {
                Username = "inactiveuser",
                Email = "inactive@example.com",
                FullName = "Inactive User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                RoleID = role.RoleID,
                EmailVerified = true,
                Status = UserStatusEnum.Inactive
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var dto = new LoginRequestDto
            {
                Username = "inactiveuser",
                Password = "Password123!"
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(dto));
        }

        [Fact]
        public async Task VerifyEmailAsync_ValidOtp_UpdatesEmailVerified()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedCustomerRoleAsync(context);

            var role = await context.Roles.FirstAsync();
            var user = new User
            {
                Username = "otpuser",
                Email = "otpuser@example.com",
                FullName = "OTP User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                RoleID = role.RoleID,
                EmailVerified = false,
                EmailVerificationToken = "654321",
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15),
                Status = UserStatusEnum.Active
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var verifyDto = new VerifyEmailRequestDto
            {
                Email = "otpuser@example.com",
                OtpCode = "654321"
            };

            await service.VerifyEmailAsync(verifyDto);

            var updatedUser = await context.Users.FirstAsync(u => u.Email == "otpuser@example.com");
            Assert.True(updatedUser.EmailVerified);
            Assert.Null(updatedUser.EmailVerificationToken);
        }

        [Fact]
        public async Task LoginAsync_VerifiedUser_ReturnsAccessToken()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedCustomerRoleAsync(context);

            var role = await context.Roles.FirstAsync();
            var user = new User
            {
                Username = "verifieduser",
                Email = "verified@example.com",
                FullName = "Verified User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                RoleID = role.RoleID,
                Role = role,
                EmailVerified = true,
                Status = UserStatusEnum.Active
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var loginDto = new LoginRequestDto
            {
                Username = "verifieduser",
                Password = "Password123!"
            };

            var response = await service.LoginAsync(loginDto);

            Assert.NotNull(response);
            Assert.Equal("verifieduser", response.Username);
            Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        }
    }
}

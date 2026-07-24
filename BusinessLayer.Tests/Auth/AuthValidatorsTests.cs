using BusinessLayer.Dtos.Auth;
using BusinessLayer.Validators;
using Xunit;

namespace BusinessLayer.Tests.Auth
{
    public class AuthValidatorsTests
    {
        [Fact]
        public void RegisterRequestValidator_ValidPayload_PassesValidation()
        {
            var validator = new RegisterRequestValidator();
            var dto = new RegisterRequestDto
            {
                Username = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                FullName = "Test User",
                Email = "testuser@example.com",
                PhoneNumber = "0912345678"
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void RegisterRequestValidator_InvalidPayload_FailsValidation()
        {
            var validator = new RegisterRequestValidator();
            var dto = new RegisterRequestDto
            {
                Username = "",
                Password = "123",
                ConfirmPassword = "456",
                FullName = "",
                Email = "invalid-email",
                PhoneNumber = ""
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.Username));
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.Password));
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.ConfirmPassword));
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.Email));
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.PhoneNumber));
        }

        [Fact]
        public void LoginRequestValidator_ValidPayload_PassesValidation()
        {
            var validator = new LoginRequestValidator();
            var dto = new LoginRequestDto
            {
                Username = "validuser",
                Password = "Password123!"
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void LoginRequestValidator_MissingFields_FailsValidation()
        {
            var validator = new LoginRequestValidator();
            var dto = new LoginRequestDto
            {
                Username = "",
                Password = ""
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public void VerifyEmailRequestValidator_ValidOtp_PassesValidation()
        {
            var validator = new VerifyEmailRequestValidator();
            var dto = new VerifyEmailRequestDto
            {
                Email = "user@example.com",
                OtpCode = "123456"
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }

        [Fact]
        public void VerifyEmailRequestValidator_InvalidOtp_FailsValidation()
        {
            var validator = new VerifyEmailRequestValidator();
            var dto = new VerifyEmailRequestDto
            {
                Email = "user@example.com",
                OtpCode = "abc12"
            };

            var result = validator.Validate(dto);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(VerifyEmailRequestDto.OtpCode));
        }

        [Fact]
        public void ResendOtpRequestValidator_ValidEmail_PassesValidation()
        {
            var validator = new ResendOtpRequestValidator();
            var dto = new ResendOtpRequestDto
            {
                Email = "user@example.com"
            };

            var result = validator.Validate(dto);

            Assert.True(result.IsValid);
        }
    }
}

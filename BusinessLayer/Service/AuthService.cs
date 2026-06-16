using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BusinessLayer.Dtos.Auth;
using BusinessLayer.Helpers;
using BusinessLayer.IService;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BusinessLayer.Service
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;
        private readonly IValidator<RegisterRequestDto> _registerValidator;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<VerifyEmailRequestDto> _verifyEmailValidator;
        private readonly IValidator<ResendOtpRequestDto> _resendOtpValidator;

        public AuthService(
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            IEmailService emailService,
            IValidator<RegisterRequestDto> registerValidator,
            IValidator<LoginRequestDto> loginValidator,
            IValidator<VerifyEmailRequestDto> verifyEmailValidator,
            IValidator<ResendOtpRequestDto> resendOtpValidator)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _verifyEmailValidator = verifyEmailValidator;
            _resendOtpValidator = resendOtpValidator;
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            // 1. Validate request
            var validationResult = await _registerValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // 2. Check duplicate username, email, phone and collect errors
            var validationErrors = new List<FluentValidation.Results.ValidationFailure>();

            var usernameExists = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == request.Username.ToLower());
            if (usernameExists)
                validationErrors.Add(new FluentValidation.Results.ValidationFailure("Username", "Username is already taken."));

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (emailExists)
                validationErrors.Add(new FluentValidation.Results.ValidationFailure("Email", "Email is already registered."));

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var phoneExists = await _context.Users
                    .AnyAsync(u => u.PhoneNumber == request.PhoneNumber);
                if (phoneExists)
                    validationErrors.Add(new FluentValidation.Results.ValidationFailure("PhoneNumber", "Phone number is already registered."));
            }

            if (validationErrors.Any())
                throw new ValidationException(validationErrors);

            // 5. Get Customer role
            var customerRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Customer")
                ?? throw new InvalidOperationException("Customer role not found in the system.");

            // 6. Generate OTP
            var otpCode = GenerateOtp();

            // 7. Create User (email not verified)
            var user = new User
            {
                Username = request.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim().ToLower(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                RoleID = customerRole.RoleID,
                Status = UserStatusEnum.Active,
                EmailVerified = false,
                EmailVerificationToken = otpCode,
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15)
            };

            // 8. Create linked Customer profile
            var customer = new Customer
            {
                UserID = user.UserID,
                CurrentPoints = 0,
                LifetimePoints = 0,
                TotalSpent = 0,
                TotalVisits = 0
            };

            await _context.Users.AddAsync(user);
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            // 9. Send verification email (after save so account exists even if email fails)
            await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, otpCode);

            return new RegisterResponseDto
            {
                UserID = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = customerRole.RoleName,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // 1. Validate request
            var validationResult = await _loginValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // 2. Find user by username or email
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Username.ToLower() == request.Username.ToLower() ||
                    u.Email.ToLower() == request.Username.ToLower());

            if (user == null)
                throw new UnauthorizedAccessException("Invalid username or password.");

            // 3. Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid username or password.");

            // 4. Check email verification
            if (!user.EmailVerified)
                throw new UnauthorizedAccessException("Please verify your email before logging in. Check your inbox for the OTP code.");

            // 5. Check user status
            if (user.Status != UserStatusEnum.Active)
                throw new UnauthorizedAccessException("Your account has been deactivated. Please contact support.");

            // 6. Generate access token
            var accessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
            var accessToken = GenerateAccessToken(user, accessTokenExpiration);

            return new LoginResponseDto
            {
                UserID = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.RoleName,
                AccessToken = accessToken,
                AccessTokenExpiration = accessTokenExpiration
            };
        }

        public async Task VerifyEmailAsync(VerifyEmailRequestDto request)
        {
            // 1. Validate request
            var validationResult = await _verifyEmailValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // 2. Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
                throw new KeyNotFoundException("No account found with this email.");

            // 3. Check if already verified
            if (user.EmailVerified)
                throw new InvalidOperationException("Email is already verified.");

            // 4. Check token expiry
            if (user.EmailVerificationTokenExpiry == null || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("OTP code has expired. Please request a new one.");

            // 5. Verify OTP
            if (user.EmailVerificationToken != request.OtpCode)
                throw new InvalidOperationException("Invalid OTP code.");

            // 6. Mark email as verified and clear token
            user.EmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task ResendOtpAsync(ResendOtpRequestDto request)
        {
            // 1. Validate request
            var validationResult = await _resendOtpValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // 2. Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (user == null)
                throw new KeyNotFoundException("No account found with this email.");

            // 3. Check if already verified
            if (user.EmailVerified)
                throw new InvalidOperationException("Email is already verified.");

            // 4. Rate limit: don't allow resend if last OTP hasn't expired and was sent less than 1 minute ago
            if (user.EmailVerificationTokenExpiry != null
                && user.EmailVerificationTokenExpiry > DateTime.UtcNow.AddMinutes(14))
            {
                throw new InvalidOperationException("Please wait at least 1 minute before requesting a new OTP.");
            }

            // 5. Generate new OTP and update
            var otpCode = GenerateOtp();
            user.EmailVerificationToken = otpCode;
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // 6. Send email
            await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, otpCode);
        }

        // ─── Private helpers ──────────────────────────────────────────

        private static string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        private string GenerateAccessToken(User user, DateTime expiration)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.RoleName),
                new("FullName", user.FullName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

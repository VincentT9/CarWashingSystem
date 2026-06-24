using BusinessLayer.Dtos.Admin;
using BusinessLayer.IService;
using DataAccessLayer.Context;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Service
{
    public class AdminUserService : IAdminUserService
    {
        private readonly ApplicationDbContext _context;

        public AdminUserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserSummaryDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId)
                ?? throw new KeyNotFoundException("User not found.");

            user.Status = request.Status;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return new UserSummaryDto
            {
                UserID = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.RoleName,
                Status = user.Status,
                EmailVerified = user.EmailVerified,
                CreatedAt = user.CreatedAt
            };
        }
    }
}

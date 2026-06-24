using BusinessLayer.Dtos.Admin;

namespace BusinessLayer.IService
{
    public interface IAdminUserService
    {
        Task<UserSummaryDto> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequestDto request);
    }
}

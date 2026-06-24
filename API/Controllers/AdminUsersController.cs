using BusinessLayer.Dtos.Admin;
using BusinessLayer.Dtos.Common;
using BusinessLayer.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        [HttpPut("{userId:guid}/status")]
        [ProducesResponseType(typeof(ApiResponse<UserSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateUserStatusRequestDto request)
        {
            var result = await _adminUserService.UpdateUserStatusAsync(userId, request);
            return Ok(ApiResponse<UserSummaryDto>.Ok(result));
        }
    }
}

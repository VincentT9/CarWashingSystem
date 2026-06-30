using BusinessLayer.Dtos.Common;
using BusinessLayer.Dtos.History;
using BusinessLayer.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/wash-histories")]
    public class WashHistoriesController : ControllerBase
    {
        private readonly IWashHistoryService _washHistoryService;

        public WashHistoriesController(IWashHistoryService washHistoryService)
        {
            _washHistoryService = washHistoryService;
        }

        [HttpGet("me")]
        [Authorize(Policy = "CustomerOnly")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<WashHistoryListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _washHistoryService.GetMyHistoryAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<WashHistoryListItemDto>>.Ok(result));
        }

        [HttpGet("me/{washHistoryId:guid}")]
        [Authorize(Policy = "CustomerOnly")]
        [ProducesResponseType(typeof(ApiResponse<WashHistoryDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyHistoryDetail(Guid washHistoryId)
        {
            var result = await _washHistoryService.GetMyHistoryDetailAsync(washHistoryId);
            return Ok(ApiResponse<WashHistoryDetailDto>.Ok(result));
        }

        [HttpGet("customer/{customerId:guid}")]
        [Authorize(Policy = "StaffOrAdmin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<WashHistoryListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetHistoryByCustomerId(
            Guid customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _washHistoryService.GetHistoryByCustomerIdAsync(customerId, page, pageSize);
            return Ok(ApiResponse<PagedResult<WashHistoryListItemDto>>.Ok(result));
        }
    }
}

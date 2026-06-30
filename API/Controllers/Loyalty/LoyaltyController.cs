using BusinessLayer.Dtos.Loyalty;
using BusinessLayer.Dtos.Operations;
using BusinessLayer.IService.Loyalty;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Loyalty
{
    [ApiController]
    [Route("api/loyalty")]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _loyaltyService;

        public LoyaltyController(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService;
        }

        [HttpGet("settings")]
        public async Task<ActionResult<LoyaltySettingsResponse>> GetSettings()
        {
            return Ok(await _loyaltyService.GetSettingsAsync());
        }

        [HttpGet("tiers")]
        public async Task<ActionResult<PagedResult<LoyaltyTierResponse>>> GetTiers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeInactive = false)
        {
            return Ok(await _loyaltyService.GetTiersAsync(page, pageSize, includeInactive));
        }

        [HttpGet("tiers/{id:guid}")]
        public async Task<ActionResult> GetTier(Guid id)
        {
            return FromResult(await _loyaltyService.GetTierAsync(id));
        }

        [HttpPost("tiers")]
        public async Task<ActionResult> CreateTier(CreateLoyaltyTierRequest request)
        {
            return FromResult(await _loyaltyService.CreateTierAsync(request));
        }

        [HttpPut("tiers/{id:guid}")]
        public async Task<ActionResult> UpdateTier(Guid id, UpdateLoyaltyTierRequest request)
        {
            return FromResult(await _loyaltyService.UpdateTierAsync(id, request));
        }

        [HttpDelete("tiers/{id:guid}")]
        public async Task<ActionResult> DeleteTier(Guid id)
        {
            return FromResult(await _loyaltyService.DeleteTierAsync(id));
        }

        [HttpGet("customers/{customerId:guid}/points/balance")]
        public async Task<ActionResult> GetPointBalance(Guid customerId)
        {
            return FromResult(await _loyaltyService.GetPointBalanceAsync(customerId));
        }

        [HttpGet("customers/{customerId:guid}/points/history")]
        public async Task<ActionResult<PagedResult<PointTransactionResponse>>> GetPointHistory(
            Guid customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return Ok(await _loyaltyService.GetPointHistoryAsync(customerId, page, pageSize));
        }

        [HttpGet("wash-history")]
        public async Task<ActionResult<PagedResult<WashHistoryResponse>>> GetWashHistory(
            [FromQuery] Guid? customerId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return Ok(await _loyaltyService.GetWashHistoryAsync(customerId, page, pageSize));
        }

        [HttpGet("rewards")]
        public async Task<ActionResult<PagedResult<RewardResponse>>> GetRewards(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeInactive = false)
        {
            return Ok(await _loyaltyService.GetRewardsAsync(page, pageSize, includeInactive));
        }

        [HttpGet("rewards/{id:guid}")]
        public async Task<ActionResult> GetReward(Guid id)
        {
            return FromResult(await _loyaltyService.GetRewardAsync(id));
        }

        [HttpPost("rewards")]
        public async Task<ActionResult> CreateReward(CreateRewardRequest request)
        {
            return FromResult(await _loyaltyService.CreateRewardAsync(request));
        }

        [HttpPut("rewards/{id:guid}")]
        public async Task<ActionResult> UpdateReward(Guid id, UpdateRewardRequest request)
        {
            return FromResult(await _loyaltyService.UpdateRewardAsync(id, request));
        }

        [HttpDelete("rewards/{id:guid}")]
        public async Task<ActionResult> DeleteReward(Guid id)
        {
            return FromResult(await _loyaltyService.DeleteRewardAsync(id));
        }

        [HttpPost("rewards/{id:guid}/redeem")]
        public async Task<ActionResult> RedeemReward(Guid id, RedeemRewardRequest request)
        {
            if (request.CustomerId == Guid.Empty)
            {
                return BadRequest("CustomerId is required.");
            }

            return FromResult(await _loyaltyService.RedeemRewardAsync(id, request));
        }

        [HttpPost("customers/{customerId:guid}/tier/evaluate")]
        public async Task<ActionResult> EvaluateTier(Guid customerId)
        {
            return FromResult(await _loyaltyService.EvaluateTierAsync(customerId));
        }

        [HttpGet("customers/{customerId:guid}/tier/history")]
        public async Task<ActionResult<PagedResult<TierHistoryResponse>>> GetTierHistory(
            Guid customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return Ok(await _loyaltyService.GetTierHistoryAsync(customerId, page, pageSize));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<LoyaltyDashboardResponse>> GetDashboard(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            return Ok(await _loyaltyService.GetDashboardAsync(fromDate, toDate));
        }

        [HttpGet("promotions")]
        public async Task<ActionResult<PagedResult<PromotionResponse>>> GetPromotions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeInactive = false)
        {
            return Ok(await _loyaltyService.GetPromotionsAsync(page, pageSize, includeInactive));
        }

        [HttpGet("promotions/{id:guid}")]
        public async Task<ActionResult> GetPromotion(Guid id)
        {
            return FromResult(await _loyaltyService.GetPromotionAsync(id));
        }

        [HttpPost("promotions")]
        public async Task<ActionResult> CreatePromotion(CreatePromotionRequest request)
        {
            return FromResult(await _loyaltyService.CreatePromotionAsync(request));
        }

        [HttpPut("promotions/{id:guid}")]
        public async Task<ActionResult> UpdatePromotion(Guid id, UpdatePromotionRequest request)
        {
            return FromResult(await _loyaltyService.UpdatePromotionAsync(id, request));
        }

        [HttpDelete("promotions/{id:guid}")]
        public async Task<ActionResult> DeletePromotion(Guid id)
        {
            return FromResult(await _loyaltyService.DeletePromotionAsync(id));
        }

        [HttpPost("promotions/{id:guid}/send")]
        public async Task<ActionResult> SendPromotion(Guid id, SendPromotionRequest request)
        {
            return FromResult(await _loyaltyService.SendPromotionAsync(id, request));
        }

        [HttpPost("promotions/{id:guid}/apply")]
        public async Task<ActionResult> ApplyPromotion(Guid id, ApplyPromotionRequest request)
        {
            if (request.CustomerId == Guid.Empty)
            {
                return BadRequest("CustomerId is required.");
            }

            return FromResult(await _loyaltyService.ApplyPromotionAsync(id, request));
        }

        private ActionResult FromResult<T>(OperationResult<T> result)
        {
            if (result.Succeeded)
            {
                return StatusCode(result.StatusCode, result.Data);
            }

            return Problem(
                title: result.StatusCode == 400 ? "Validation Error" : "Request Error",
                detail: result.Error,
                statusCode: result.StatusCode);
        }

    }
}

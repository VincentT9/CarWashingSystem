using BusinessLayer.Dtos.Loyalty;
using BusinessLayer.Dtos.Operations;

namespace BusinessLayer.IService.Loyalty
{
    public interface ILoyaltyService : ILoyaltyTierQuery
    {
        Task<LoyaltySettingsResponse> GetSettingsAsync();
        Task<OperationResult<LoyaltyTierResponse>> CreateTierAsync(CreateLoyaltyTierRequest request);
        Task<OperationResult<LoyaltyTierResponse>> UpdateTierAsync(Guid id, UpdateLoyaltyTierRequest request);
        Task<OperationResult<bool>> DeleteTierAsync(Guid id);

        Task<OperationResult<PointBalanceResponse>> GetPointBalanceAsync(Guid customerId);
        Task<PagedResult<PointTransactionResponse>> GetPointHistoryAsync(Guid customerId, int page, int pageSize);
        Task<PagedResult<WashHistoryResponse>> GetWashHistoryAsync(Guid? customerId, int page, int pageSize);

        Task<PagedResult<RewardResponse>> GetRewardsAsync(int page, int pageSize, bool includeInactive);
        Task<OperationResult<RewardResponse>> GetRewardAsync(Guid id);
        Task<OperationResult<RewardResponse>> CreateRewardAsync(CreateRewardRequest request);
        Task<OperationResult<RewardResponse>> UpdateRewardAsync(Guid id, UpdateRewardRequest request);
        Task<OperationResult<bool>> DeleteRewardAsync(Guid id);
        Task<OperationResult<RewardRedemptionResponse>> RedeemRewardAsync(Guid rewardId, RedeemRewardRequest request);

        Task<PagedResult<PromotionResponse>> GetPromotionsAsync(int page, int pageSize, bool includeInactive);
        Task<OperationResult<PromotionResponse>> GetPromotionAsync(Guid id);
        Task<OperationResult<PromotionResponse>> CreatePromotionAsync(CreatePromotionRequest request);
        Task<OperationResult<PromotionResponse>> UpdatePromotionAsync(Guid id, UpdatePromotionRequest request);
        Task<OperationResult<bool>> DeletePromotionAsync(Guid id);
        Task<OperationResult<PromotionDeliveryResponse>> SendPromotionAsync(Guid promotionId, SendPromotionRequest request);
        Task<OperationResult<ApplyPromotionResponse>> ApplyPromotionAsync(Guid promotionId, ApplyPromotionRequest request);

        Task<OperationResult<TierEvaluationResponse>> EvaluateTierAsync(Guid customerId);
        Task<PagedResult<TierHistoryResponse>> GetTierHistoryAsync(Guid customerId, int page, int pageSize);
        Task<LoyaltyDashboardResponse> GetDashboardAsync(DateTime? fromDate, DateTime? toDate);
    }
}

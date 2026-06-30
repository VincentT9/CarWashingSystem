using BusinessLayer.Dtos.Loyalty;
using BusinessLayer.Dtos.Operations;
using BusinessLayer.IService;
using BusinessLayer.IService.Loyalty;
using BusinessLayer.IService.Operations;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Service.Loyalty
{
    public class LoyaltyService : ILoyaltyService, IWashCompletionService
    {
        private const decimal PointEarnRateAmount = 10000m;
        private const int PointEarnRatePoints = 1;
        private const int PointExpiryMonths = 12;

        private readonly ApplicationDbContext _context;
        private readonly IBehavioralLogWriter _behavioralLogWriter;

        public LoyaltyService(ApplicationDbContext context, IBehavioralLogWriter behavioralLogWriter)
        {
            _context = context;
            _behavioralLogWriter = behavioralLogWriter;
        }

        public Task<LoyaltySettingsResponse> GetSettingsAsync()
        {
            return Task.FromResult(new LoyaltySettingsResponse
            {
                PointEarnRateAmount = PointEarnRateAmount,
                PointEarnRatePoints = PointEarnRatePoints,
                PointExpiryMonths = PointExpiryMonths,
                EarnRule = $"{PointEarnRatePoints} point per {PointEarnRateAmount:N0} VND, multiplied by current tier."
            });
        }

        public async Task<PagedResult<LoyaltyTierResponse>> GetTiersAsync(int page, int pageSize, bool includeInactive)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.LoyaltyTiers.AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(tier => tier.Status == LoyaltyTierStatusEnum.Active);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(tier => tier.TierRank)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(tier => MapTier(tier))
                .ToListAsync();

            return new PagedResult<LoyaltyTierResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        public async Task<OperationResult<LoyaltyTierResponse>> GetTierAsync(Guid id)
        {
            var tier = await _context.LoyaltyTiers.AsNoTracking().FirstOrDefaultAsync(item => item.TierID == id);
            return tier is null
                ? OperationResult<LoyaltyTierResponse>.Failure("Loyalty tier not found.", 404)
                : OperationResult<LoyaltyTierResponse>.Success(MapTier(tier));
        }

        public async Task<OperationResult<LoyaltyTierResponse>> CreateTierAsync(CreateLoyaltyTierRequest request)
        {
            var validation = await ValidateTierRequestAsync(request, null);
            if (validation is not null)
            {
                return OperationResult<LoyaltyTierResponse>.Failure(validation, 400);
            }

            var tier = new LoyaltyTier();
            ApplyTierRequest(tier, request);

            _context.LoyaltyTiers.Add(tier);
            await _context.SaveChangesAsync();

            return OperationResult<LoyaltyTierResponse>.Success(MapTier(tier), 201);
        }

        public async Task<OperationResult<LoyaltyTierResponse>> UpdateTierAsync(Guid id, UpdateLoyaltyTierRequest request)
        {
            var tier = await _context.LoyaltyTiers.FirstOrDefaultAsync(item => item.TierID == id);
            if (tier is null)
            {
                return OperationResult<LoyaltyTierResponse>.Failure("Loyalty tier not found.", 404);
            }

            var validation = await ValidateTierRequestAsync(request, id);
            if (validation is not null)
            {
                return OperationResult<LoyaltyTierResponse>.Failure(validation, 400);
            }

            ApplyTierRequest(tier, request);
            await _context.SaveChangesAsync();

            return OperationResult<LoyaltyTierResponse>.Success(MapTier(tier));
        }

        public async Task<OperationResult<bool>> DeleteTierAsync(Guid id)
        {
            var tier = await _context.LoyaltyTiers.FirstOrDefaultAsync(item => item.TierID == id);
            if (tier is null)
            {
                return OperationResult<bool>.Failure("Loyalty tier not found.", 404);
            }

            tier.Status = LoyaltyTierStatusEnum.Inactive;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Success(true);
        }

        public async Task<OperationResult<PointBalanceResponse>> GetPointBalanceAsync(Guid customerId)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .Include(item => item.Tier)
                .FirstOrDefaultAsync(item => item.CustomerID == customerId);

            return customer is null
                ? OperationResult<PointBalanceResponse>.Failure("Customer not found.", 404)
                : OperationResult<PointBalanceResponse>.Success(MapBalance(customer));
        }

        public async Task<PagedResult<PointTransactionResponse>> GetPointHistoryAsync(Guid customerId, int page, int pageSize)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.LoyaltyPointTransactions
                .AsNoTracking()
                .Where(transaction => transaction.CustomerID == customerId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(transaction => transaction.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(transaction => MapPointTransaction(transaction))
                .ToListAsync();

            return new PagedResult<PointTransactionResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        public async Task<PagedResult<WashHistoryResponse>> GetWashHistoryAsync(Guid? customerId, int page, int pageSize)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.WashHistories.AsNoTracking().Include(history => history.Booking).AsQueryable();
            if (customerId.HasValue)
            {
                query = query.Where(history => history.Booking.CustomerID == customerId.Value);
            }

            var total = await query.CountAsync();
            var histories = await query
                .OrderByDescending(history => history.WashDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<WashHistoryResponse>
            {
                Items = histories.Select(MapWashHistory).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<PagedResult<RewardResponse>> GetRewardsAsync(int page, int pageSize, bool includeInactive)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.Rewards.AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(reward => reward.Status == RewardStatusEnum.Active);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(reward => reward.PointsRequired)
                .ThenBy(reward => reward.RewardName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(reward => MapReward(reward))
                .ToListAsync();

            return new PagedResult<RewardResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        public async Task<OperationResult<RewardResponse>> GetRewardAsync(Guid id)
        {
            var reward = await _context.Rewards.AsNoTracking().FirstOrDefaultAsync(item => item.RewardID == id);
            return reward is null
                ? OperationResult<RewardResponse>.Failure("Reward not found.", 404)
                : OperationResult<RewardResponse>.Success(MapReward(reward));
        }

        public async Task<OperationResult<RewardResponse>> CreateRewardAsync(CreateRewardRequest request)
        {
            var validation = await ValidateRewardRequestAsync(request, null);
            if (validation is not null)
            {
                return OperationResult<RewardResponse>.Failure(validation, 400);
            }

            var reward = new Reward();
            ApplyRewardRequest(reward, request);

            _context.Rewards.Add(reward);
            await _context.SaveChangesAsync();

            return OperationResult<RewardResponse>.Success(MapReward(reward), 201);
        }

        public async Task<OperationResult<RewardResponse>> UpdateRewardAsync(Guid id, UpdateRewardRequest request)
        {
            var reward = await _context.Rewards.FirstOrDefaultAsync(item => item.RewardID == id);
            if (reward is null)
            {
                return OperationResult<RewardResponse>.Failure("Reward not found.", 404);
            }

            var validation = await ValidateRewardRequestAsync(request, id);
            if (validation is not null)
            {
                return OperationResult<RewardResponse>.Failure(validation, 400);
            }

            ApplyRewardRequest(reward, request);
            await _context.SaveChangesAsync();

            return OperationResult<RewardResponse>.Success(MapReward(reward));
        }

        public async Task<OperationResult<bool>> DeleteRewardAsync(Guid id)
        {
            var reward = await _context.Rewards.FirstOrDefaultAsync(item => item.RewardID == id);
            if (reward is null)
            {
                return OperationResult<bool>.Failure("Reward not found.", 404);
            }

            reward.Status = RewardStatusEnum.Archived;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Success(true);
        }

        public async Task<OperationResult<RewardRedemptionResponse>> RedeemRewardAsync(Guid rewardId, RedeemRewardRequest request)
        {
            if (request.CustomerId == Guid.Empty)
            {
                return OperationResult<RewardRedemptionResponse>.Failure("CustomerId is required.", 400);
            }

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await _context.RewardRedemptions
                    .AsNoTracking()
                    .Include(redemption => redemption.PointTransactions)
                    .FirstOrDefaultAsync(redemption => redemption.PointTransactions.Any(transaction => transaction.IdempotencyKey == request.IdempotencyKey));

                if (existing is not null)
                {
                    return OperationResult<RewardRedemptionResponse>.Success(MapRedemption(existing));
                }
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(item => item.CustomerID == request.CustomerId);
            if (customer is null)
            {
                return OperationResult<RewardRedemptionResponse>.Failure("Customer not found.", 404);
            }

            var reward = await _context.Rewards.FirstOrDefaultAsync(item => item.RewardID == rewardId);
            if (reward is null || reward.Status != RewardStatusEnum.Active)
            {
                return OperationResult<RewardRedemptionResponse>.Failure("Active reward not found.", 404);
            }

            var now = DateTime.UtcNow;
            if ((reward.ValidFrom.HasValue && reward.ValidFrom.Value > now) || (reward.ValidTo.HasValue && reward.ValidTo.Value < now))
            {
                return OperationResult<RewardRedemptionResponse>.Failure("Reward is not valid at this time.", 400);
            }

            if (reward.UsageLimitPerCustomer.HasValue)
            {
                var usedCount = await _context.RewardRedemptions.CountAsync(redemption =>
                    redemption.CustomerID == request.CustomerId &&
                    redemption.RewardID == rewardId &&
                    redemption.Status != RewardRedemptionStatusEnum.Cancelled);

                if (usedCount >= reward.UsageLimitPerCustomer.Value)
                {
                    return OperationResult<RewardRedemptionResponse>.Failure("Reward usage limit reached for this customer.", 400);
                }
            }

            if (customer.CurrentPoints < reward.PointsRequired)
            {
                return OperationResult<RewardRedemptionResponse>.Failure("Customer does not have enough points.", 400);
            }

            var earningTransactions = await _context.LoyaltyPointTransactions
                .Where(transaction =>
                    transaction.CustomerID == request.CustomerId &&
                    transaction.TransactionType == PointTransactionTypeEnum.Earn &&
                    transaction.RemainingPoints > 0 &&
                    (!transaction.ExpiryDate.HasValue || transaction.ExpiryDate.Value > now))
                .OrderBy(transaction => transaction.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(transaction => transaction.CreatedAt)
                .ToListAsync();

            var pointsToSpend = reward.PointsRequired;
            var redemption = new RewardRedemption
            {
                CustomerID = request.CustomerId,
                RewardID = rewardId,
                BookingID = request.BookingId,
                PointsSpent = reward.PointsRequired,
                Status = RewardRedemptionStatusEnum.Reserved,
                ExpiresAt = reward.ValidTo
            };

            _context.RewardRedemptions.Add(redemption);

            foreach (var earningTransaction in earningTransactions)
            {
                if (pointsToSpend <= 0)
                {
                    break;
                }

                var pointsFromTransaction = Math.Min(earningTransaction.RemainingPoints, pointsToSpend);
                earningTransaction.RemainingPoints -= pointsFromTransaction;
                pointsToSpend -= pointsFromTransaction;

                customer.CurrentPoints -= pointsFromTransaction;

                _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
                {
                    CustomerID = request.CustomerId,
                    BookingID = request.BookingId,
                    RedemptionID = redemption.RedemptionID,
                    ReferenceTransactionID = earningTransaction.TransactionID,
                    Points = -pointsFromTransaction,
                    OriginalPoints = 0,
                    RemainingPoints = 0,
                    BalanceAfter = customer.CurrentPoints,
                    TransactionType = PointTransactionTypeEnum.Redeem,
                    IdempotencyKey = NormalizeOptional(request.IdempotencyKey),
                    Description = $"Redeemed reward {reward.RewardName}"
                });
            }

            if (pointsToSpend > 0)
            {
                return OperationResult<RewardRedemptionResponse>.Failure("Customer does not have enough unexpired points.", 400);
            }

            customer.UpdatedAt = now;
            await _context.SaveChangesAsync();

            return OperationResult<RewardRedemptionResponse>.Success(MapRedemption(redemption), 201);
        }

        public async Task<PagedResult<PromotionResponse>> GetPromotionsAsync(int page, int pageSize, bool includeInactive)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.Promotions
                .AsNoTracking()
                .Include(promotion => promotion.PromotionServices)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(promotion => promotion.Status == PromotionStatusEnum.Active);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(promotion => promotion.Priority)
                .ThenBy(promotion => promotion.PromotionName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(promotion => MapPromotion(promotion))
                .ToListAsync();

            return new PagedResult<PromotionResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        public async Task<OperationResult<PromotionResponse>> GetPromotionAsync(Guid id)
        {
            var promotion = await _context.Promotions
                .AsNoTracking()
                .Include(item => item.PromotionServices)
                .FirstOrDefaultAsync(item => item.PromotionID == id);

            return promotion is null
                ? OperationResult<PromotionResponse>.Failure("Promotion not found.", 404)
                : OperationResult<PromotionResponse>.Success(MapPromotion(promotion));
        }

        public async Task<OperationResult<PromotionResponse>> CreatePromotionAsync(CreatePromotionRequest request)
        {
            var validation = await ValidatePromotionRequestAsync(request, null);
            if (validation is not null)
            {
                return OperationResult<PromotionResponse>.Failure(validation, 400);
            }

            var promotion = new Promotion();
            ApplyPromotionRequest(promotion, request);
            AddPromotionServices(promotion, request.ServiceIds);

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            return OperationResult<PromotionResponse>.Success(MapPromotion(promotion), 201);
        }

        public async Task<OperationResult<PromotionResponse>> UpdatePromotionAsync(Guid id, UpdatePromotionRequest request)
        {
            var promotion = await _context.Promotions
                .Include(item => item.PromotionServices)
                .FirstOrDefaultAsync(item => item.PromotionID == id);
            if (promotion is null)
            {
                return OperationResult<PromotionResponse>.Failure("Promotion not found.", 404);
            }

            var validation = await ValidatePromotionRequestAsync(request, id);
            if (validation is not null)
            {
                return OperationResult<PromotionResponse>.Failure(validation, 400);
            }

            ApplyPromotionRequest(promotion, request);
            promotion.PromotionServices.Clear();
            AddPromotionServices(promotion, request.ServiceIds);

            await _context.SaveChangesAsync();
            return OperationResult<PromotionResponse>.Success(MapPromotion(promotion));
        }

        public async Task<OperationResult<bool>> DeletePromotionAsync(Guid id)
        {
            var promotion = await _context.Promotions.FirstOrDefaultAsync(item => item.PromotionID == id);
            if (promotion is null)
            {
                return OperationResult<bool>.Failure("Promotion not found.", 404);
            }

            promotion.Status = PromotionStatusEnum.Disabled;
            await _context.SaveChangesAsync();
            return OperationResult<bool>.Success(true);
        }

        public async Task<OperationResult<PromotionDeliveryResponse>> SendPromotionAsync(Guid promotionId, SendPromotionRequest request)
        {
            var promotion = await _context.Promotions.FirstOrDefaultAsync(item => item.PromotionID == promotionId);
            if (promotion is null)
            {
                return OperationResult<PromotionDeliveryResponse>.Failure("Promotion not found.", 404);
            }

            var distinctCustomerIds = request.CustomerIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (distinctCustomerIds.Count == 0)
            {
                return OperationResult<PromotionDeliveryResponse>.Failure("At least one customer is required.", 400);
            }

            var existingCustomerIds = await _context.Customers
                .Where(customer => distinctCustomerIds.Contains(customer.CustomerID))
                .Select(customer => customer.CustomerID)
                .ToListAsync();

            var alreadySent = await _context.PromotionCustomers
                .Where(item => item.PromotionID == promotionId && distinctCustomerIds.Contains(item.CustomerID))
                .Select(item => item.CustomerID)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var sent = 0;
            foreach (var customerId in existingCustomerIds.Except(alreadySent))
            {
                _context.PromotionCustomers.Add(new PromotionCustomer
                {
                    PromotionID = promotionId,
                    CustomerID = customerId,
                    SentAt = now,
                    ExpiresAt = request.ExpiresAt ?? promotion.EndDate
                });
                sent++;
            }

            await _context.SaveChangesAsync();

            return OperationResult<PromotionDeliveryResponse>.Success(new PromotionDeliveryResponse
            {
                PromotionId = promotionId,
                SentCount = sent,
                SkippedCount = distinctCustomerIds.Count - sent
            });
        }

        public async Task<OperationResult<ApplyPromotionResponse>> ApplyPromotionAsync(Guid promotionId, ApplyPromotionRequest request)
        {
            if (request.BookingId == Guid.Empty || request.CustomerId == Guid.Empty)
            {
                return OperationResult<ApplyPromotionResponse>.Failure("BookingId and CustomerId are required.", 400);
            }

            var booking = await _context.Bookings
                .Include(item => item.Customer)
                .ThenInclude(customer => customer.Tier)
                .Include(item => item.BookingDetails)
                .Include(item => item.BookingPromotions)
                .FirstOrDefaultAsync(item => item.BookingID == request.BookingId);
            if (booking is null || booking.CustomerID != request.CustomerId)
            {
                return OperationResult<ApplyPromotionResponse>.Failure("Booking not found for this customer.", 404);
            }

            if (booking.BookingStatus != BookingStatusEnum.Pending)
            {
                return OperationResult<ApplyPromotionResponse>.Failure("Promotion can only be applied to pending bookings.", 400);
            }

            if (booking.BookingPromotions.Any(item => item.PromotionID == promotionId))
            {
                return OperationResult<ApplyPromotionResponse>.Failure("Promotion already applied to this booking.", 409);
            }

            var promotion = await _context.Promotions
                .Include(item => item.PromotionServices)
                .Include(item => item.PromotionCustomers)
                .FirstOrDefaultAsync(item => item.PromotionID == promotionId);
            if (promotion is null)
            {
                return OperationResult<ApplyPromotionResponse>.Failure("Promotion not found.", 404);
            }

            var eligibilityError = await ValidatePromotionEligibilityAsync(promotion, booking, request.Code);
            if (eligibilityError is not null)
            {
                return OperationResult<ApplyPromotionResponse>.Failure(eligibilityError, 400);
            }

            var before = booking.EstimatedTotalAmount;
            var discount = CalculatePromotionDiscount(promotion, before);
            var bonusPoints = promotion.PromotionType == PromotionTypeEnum.BonusPoints ? promotion.BonusPoints : 0;

            booking.BookingPromotions.Add(new BookingPromotion
            {
                BookingID = booking.BookingID,
                PromotionID = promotion.PromotionID,
                DiscountAmount = discount,
                BonusPoints = bonusPoints
            });
            booking.EstimatedTotalAmount = Math.Max(0, before - discount);
            booking.UpdatedAt = DateTime.UtcNow;

            var sentPromotion = promotion.PromotionCustomers.FirstOrDefault(item => item.CustomerID == booking.CustomerID);
            if (sentPromotion is not null)
            {
                sentPromotion.UsageCount += 1;
                sentPromotion.IsUsed = true;
                sentPromotion.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await _behavioralLogWriter.WriteAsync(new BehavioralLogWriteRequest
            {
                CustomerId = booking.CustomerID,
                BookingId = booking.BookingID,
                ServiceId = booking.BookingDetails.FirstOrDefault()?.ServiceID,
                PromotionId = promotion.PromotionID,
                ActionType = BehavioralActionTypeEnum.ViewPromotion,
                PromotionUsed = true,
                SpendingAmount = booking.EstimatedTotalAmount,
                Notes = "Promotion applied"
            });

            return OperationResult<ApplyPromotionResponse>.Success(new ApplyPromotionResponse
            {
                BookingId = booking.BookingID,
                PromotionId = promotion.PromotionID,
                DiscountAmount = discount,
                BonusPoints = bonusPoints,
                TotalBeforeDiscount = before,
                TotalAfterDiscount = booking.EstimatedTotalAmount
            });
        }

        public async Task<OperationResult<TierEvaluationResponse>> EvaluateTierAsync(Guid customerId)
        {
            var customer = await _context.Customers.Include(item => item.Tier).FirstOrDefaultAsync(item => item.CustomerID == customerId);
            if (customer is null)
            {
                return OperationResult<TierEvaluationResponse>.Failure("Customer not found.", 404);
            }

            var result = await EvaluateAndApplyTierAsync(customer, DateTime.UtcNow, TierChangeReasonEnum.ManualAdjustment);
            await _context.SaveChangesAsync();

            return OperationResult<TierEvaluationResponse>.Success(result);
        }

        public async Task<PagedResult<TierHistoryResponse>> GetTierHistoryAsync(Guid customerId, int page, int pageSize)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.CustomerTierHistories
                .AsNoTracking()
                .Where(history => history.CustomerID == customerId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(history => history.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(history => MapTierHistory(history))
                .ToListAsync();

            return new PagedResult<TierHistoryResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        public async Task<LoyaltyDashboardResponse> GetDashboardAsync(DateTime? fromDate, DateTime? toDate)
        {
            var start = fromDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var end = toDate ?? DateTime.UtcNow;

            return new LoyaltyDashboardResponse
            {
                ActiveCustomers = await _context.Customers.CountAsync(),
                ActiveRewards = await _context.Rewards.CountAsync(reward => reward.Status == RewardStatusEnum.Active),
                PointsIssued = await _context.LoyaltyPointTransactions
                    .Where(transaction => transaction.TransactionType == PointTransactionTypeEnum.Earn && transaction.CreatedAt >= start && transaction.CreatedAt <= end)
                    .SumAsync(transaction => (int?)transaction.Points) ?? 0,
                PointsRedeemed = Math.Abs(await _context.LoyaltyPointTransactions
                    .Where(transaction => transaction.TransactionType == PointTransactionTypeEnum.Redeem && transaction.CreatedAt >= start && transaction.CreatedAt <= end)
                    .SumAsync(transaction => (int?)transaction.Points) ?? 0),
                Revenue = await _context.WashHistories
                    .Where(history => history.WashDate >= start && history.WashDate <= end)
                    .SumAsync(history => (decimal?)history.FinalAmount) ?? 0,
                CompletedWashes = await _context.WashHistories.CountAsync(history => history.WashDate >= start && history.WashDate <= end)
            };
        }

        public async Task CompleteWashAsync(WashCompletionPayload payload)
        {
            var existingHistory = await _context.WashHistories.FirstOrDefaultAsync(history => history.BookingID == payload.BookingId);
            if (existingHistory is not null)
            {
                return;
            }

            var customer = await _context.Customers
                .Include(item => item.Tier)
                .FirstOrDefaultAsync(item => item.CustomerID == payload.CustomerId);

            if (customer is null)
            {
                return;
            }

            var idempotencyKey = $"wash:{payload.BookingId}:earn";
            var existingEarn = await _context.LoyaltyPointTransactions.AnyAsync(transaction => transaction.IdempotencyKey == idempotencyKey);
            if (existingEarn)
            {
                return;
            }

            var multiplier = customer.Tier?.PointMultiplier > 0 ? customer.Tier.PointMultiplier : 1m;
            var appliedPromotions = await _context.BookingPromotions
                .Where(item => item.BookingID == payload.BookingId)
                .ToListAsync();
            var discountAmount = appliedPromotions.Sum(item => item.DiscountAmount);
            var bonusPoints = appliedPromotions.Sum(item => item.BonusPoints);
            var pointsEarned = (int)Math.Floor(payload.Amount / PointEarnRateAmount * PointEarnRatePoints * multiplier) + bonusPoints;
            var completedAt = payload.CompletedAt == default ? DateTime.UtcNow : payload.CompletedAt;

            var history = new WashHistory
            {
                BookingID = payload.BookingId,
                WashDate = completedAt,
                ActualTotalAmount = payload.Amount + discountAmount,
                DiscountAmount = discountAmount,
                FinalAmount = payload.Amount,
                PointsEarned = pointsEarned,
                RewardUsed = discountAmount
            };

            _context.WashHistories.Add(history);

            customer.TotalSpent += payload.Amount;
            customer.TotalVisits += 1;
            customer.LastVisitDate = completedAt;
            customer.UpdatedAt = completedAt;

            if (pointsEarned > 0)
            {
                customer.CurrentPoints += pointsEarned;
                customer.LifetimePoints += pointsEarned;

                _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
                {
                    CustomerID = customer.CustomerID,
                    BookingID = payload.BookingId,
                    WashHistoryID = history.WashHistoryID,
                    Points = pointsEarned,
                    OriginalPoints = pointsEarned,
                    RemainingPoints = pointsEarned,
                    BalanceAfter = customer.CurrentPoints,
                    TransactionType = PointTransactionTypeEnum.Earn,
                    ExpiryDate = completedAt.AddMonths(PointExpiryMonths),
                    IdempotencyKey = idempotencyKey,
                    Description = "Points earned from completed wash"
                });
            }

            _context.BehavioralLogs.Add(new BehavioralLog
            {
                CustomerID = payload.CustomerId,
                BookingID = payload.BookingId,
                ServiceID = payload.ServiceId,
                ActionType = BehavioralActionTypeEnum.Book,
                ActionTime = completedAt,
                PointsChanged = pointsEarned,
                SpendingAmount = payload.Amount,
                RewardUsed = discountAmount,
                PromotionUsed = appliedPromotions.Count > 0,
                Notes = "Wash completed"
            });

            await EvaluateAndApplyTierAsync(customer, completedAt, TierChangeReasonEnum.MonthlyReview);
        }

        private async Task<TierEvaluationResponse> EvaluateAndApplyTierAsync(Customer customer, DateTime now, TierChangeReasonEnum reason)
        {
            var activeTiers = await _context.LoyaltyTiers
                .Where(tier => tier.Status == LoyaltyTierStatusEnum.Active)
                .OrderByDescending(tier => tier.TierRank)
                .ToListAsync();

            var qualifiedTier = activeTiers.FirstOrDefault(tier => Qualifies(customer, tier)) ??
                                activeTiers.OrderBy(tier => tier.TierRank).FirstOrDefault();

            if (qualifiedTier is null)
            {
                return new TierEvaluationResponse
                {
                    CustomerId = customer.CustomerID,
                    PreviousTierId = customer.TierID,
                    CurrentTierId = Guid.Empty,
                    QualifiedSpent = customer.TotalSpent,
                    QualifiedVisits = customer.TotalVisits,
                    Changed = false
                };
            }

            var previousTierId = customer.TierID;
            var changed = previousTierId != qualifiedTier.TierID;
            if (changed)
            {
                customer.TierID = qualifiedTier.TierID;
                customer.CurrentTierSince = now;
                customer.NextTierReviewAt = now.AddMonths(Math.Max(1, qualifiedTier.QualificationPeriodMonths));

                _context.CustomerTierHistories.Add(new CustomerTierHistory
                {
                    CustomerID = customer.CustomerID,
                    PreviousTierID = previousTierId,
                    NewTierID = qualifiedTier.TierID,
                    ReviewPeriodStart = now.AddMonths(-Math.Max(1, qualifiedTier.QualificationPeriodMonths)),
                    ReviewPeriodEnd = now,
                    QualifiedSpent = customer.TotalSpent,
                    QualifiedVisits = customer.TotalVisits,
                    ChangeReason = previousTierId.HasValue ? reason : TierChangeReasonEnum.InitialAssignment,
                    Notes = "Tier evaluated from loyalty activity."
                });
            }

            return new TierEvaluationResponse
            {
                CustomerId = customer.CustomerID,
                PreviousTierId = previousTierId,
                CurrentTierId = qualifiedTier.TierID,
                CurrentTierName = qualifiedTier.TierName,
                QualifiedSpent = customer.TotalSpent,
                QualifiedVisits = customer.TotalVisits,
                Changed = changed
            };
        }

        private static bool Qualifies(Customer customer, LoyaltyTier tier)
        {
            var spentQualified = customer.TotalSpent >= tier.MinSpent;
            var visitsQualified = customer.TotalVisits >= tier.MinVisits;
            return tier.QualificationMode == TierQualificationModeEnum.AnyCondition
                ? spentQualified || visitsQualified
                : spentQualified && visitsQualified;
        }

        private async Task<string?> ValidateTierRequestAsync(CreateLoyaltyTierRequest request, Guid? existingId)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return "Name is required.";
            }

            if (request.Rank < 1)
            {
                return "Rank must be greater than 0.";
            }

            if (request.MinSpent < 0 || request.MinVisits < 0)
            {
                return "MinSpent and MinVisits must be greater than or equal to 0.";
            }

            if (request.PointMultiplier <= 0 || request.BookingWindowDays < 1 || request.QualificationPeriodMonths < 1)
            {
                return "PointMultiplier, BookingWindowDays and QualificationPeriodMonths must be greater than 0.";
            }

            if (!Enum.TryParse<TierQualificationModeEnum>(request.QualificationMode, true, out _))
            {
                return "QualificationMode must be AllConditions or AnyCondition.";
            }

            var normalizedName = request.Name.Trim().ToLower();
            if (await _context.LoyaltyTiers.AnyAsync(tier => tier.TierID != existingId && tier.TierName.ToLower() == normalizedName))
            {
                return "Tier name already exists.";
            }

            if (await _context.LoyaltyTiers.AnyAsync(tier => tier.TierID != existingId && tier.TierRank == request.Rank))
            {
                return "Tier rank already exists.";
            }

            return null;
        }

        private async Task<string?> ValidateRewardRequestAsync(CreateRewardRequest request, Guid? existingId)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return "Name is required.";
            }

            if (request.PointsRequired <= 0)
            {
                return "PointsRequired must be greater than 0.";
            }

            if (request.Value < 0)
            {
                return "Value must be greater than or equal to 0.";
            }

            if (request.ValidFrom.HasValue && request.ValidTo.HasValue && request.ValidTo <= request.ValidFrom)
            {
                return "ValidTo must be later than ValidFrom.";
            }

            if (request.UsageLimitPerCustomer.HasValue && request.UsageLimitPerCustomer <= 0)
            {
                return "UsageLimitPerCustomer must be greater than 0.";
            }

            if (!Enum.TryParse<RewardTypeEnum>(request.Type, true, out _))
            {
                return "Type must be FixedDiscount, PercentageDiscount, FreeService or AddOnService.";
            }

            if (request.ServiceId.HasValue && !await _context.Services.AnyAsync(service => service.ServiceID == request.ServiceId.Value))
            {
                return "Service not found.";
            }

            var normalizedName = request.Name.Trim().ToLower();
            if (await _context.Rewards.AnyAsync(reward => reward.RewardID != existingId && reward.RewardName.ToLower() == normalizedName))
            {
                return "Reward name already exists.";
            }

            return null;
        }

        private static void ApplyTierRequest(LoyaltyTier tier, CreateLoyaltyTierRequest request)
        {
            tier.TierName = request.Name.Trim();
            tier.TierRank = request.Rank;
            tier.MinSpent = request.MinSpent;
            tier.MinVisits = request.MinVisits;
            tier.QualificationPeriodMonths = request.QualificationPeriodMonths;
            tier.QualificationMode = Enum.Parse<TierQualificationModeEnum>(request.QualificationMode, true);
            tier.BookingWindowDays = request.BookingWindowDays;
            tier.PriorityLevel = request.PriorityLevel;
            tier.PointMultiplier = request.PointMultiplier;
            tier.TierBenefits = NormalizeOptional(request.Benefits);
            tier.Status = request.IsActive ? LoyaltyTierStatusEnum.Active : LoyaltyTierStatusEnum.Inactive;
        }

        private static void ApplyRewardRequest(Reward reward, CreateRewardRequest request)
        {
            reward.RewardName = request.Name.Trim();
            reward.Description = NormalizeOptional(request.Description);
            reward.RewardType = Enum.Parse<RewardTypeEnum>(request.Type, true);
            reward.PointsRequired = request.PointsRequired;
            reward.RewardValue = request.Value;
            reward.ServiceID = request.ServiceId;
            reward.ValidFrom = request.ValidFrom;
            reward.ValidTo = request.ValidTo;
            reward.UsageLimitPerCustomer = request.UsageLimitPerCustomer;
            reward.Status = request.IsActive ? RewardStatusEnum.Active : RewardStatusEnum.Inactive;
        }

        private async Task<string?> ValidatePromotionRequestAsync(CreatePromotionRequest request, Guid? existingId)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return "Name is required.";
            }

            if (!Enum.TryParse<PromotionTypeEnum>(request.Type, true, out _))
            {
                return "Type must be PercentageDiscount, FixedDiscount, FreeService or BonusPoints.";
            }

            if (request.Value < 0 || request.MinimumSpend < 0 || request.BonusPoints < 0)
            {
                return "Value, MinimumSpend and BonusPoints must be greater than or equal to 0.";
            }

            if (request.EndDate <= request.StartDate)
            {
                return "EndDate must be later than StartDate.";
            }

            if (request.TotalUsageLimit.HasValue && request.TotalUsageLimit <= 0)
            {
                return "TotalUsageLimit must be greater than 0.";
            }

            if (request.UsageLimitPerCustomer.HasValue && request.UsageLimitPerCustomer <= 0)
            {
                return "UsageLimitPerCustomer must be greater than 0.";
            }

            var normalizedCode = NormalizeOptional(request.Code);
            if (normalizedCode is not null &&
                await _context.Promotions.AnyAsync(promotion => promotion.PromotionID != existingId && promotion.PromotionCode == normalizedCode))
            {
                return "Promotion code already exists.";
            }

            if (request.MinTierId.HasValue && !await _context.LoyaltyTiers.AnyAsync(tier => tier.TierID == request.MinTierId.Value))
            {
                return "Minimum tier not found.";
            }

            if (request.FreeServiceId.HasValue && !await _context.Services.AnyAsync(service => service.ServiceID == request.FreeServiceId.Value))
            {
                return "Free service not found.";
            }

            var serviceIds = request.ServiceIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (serviceIds.Count > 0)
            {
                var existingServiceCount = await _context.Services.CountAsync(service => serviceIds.Contains(service.ServiceID));
                if (existingServiceCount != serviceIds.Count)
                {
                    return "One or more services were not found.";
                }
            }

            return null;
        }

        private static void ApplyPromotionRequest(Promotion promotion, CreatePromotionRequest request)
        {
            promotion.PromotionName = request.Name.Trim();
            promotion.PromotionCode = NormalizeOptional(request.Code);
            promotion.Description = NormalizeOptional(request.Description);
            promotion.PromotionType = Enum.Parse<PromotionTypeEnum>(request.Type, true);
            promotion.PromotionValue = request.Value;
            promotion.MaxDiscountAmount = request.MaxDiscountAmount;
            promotion.BonusPoints = request.BonusPoints;
            promotion.FreeServiceID = request.FreeServiceId;
            promotion.MinimumSpend = request.MinimumSpend;
            promotion.StartDate = request.StartDate;
            promotion.EndDate = request.EndDate;
            promotion.MinTierID = request.MinTierId;
            promotion.TotalUsageLimit = request.TotalUsageLimit;
            promotion.UsageLimitPerCustomer = request.UsageLimitPerCustomer;
            promotion.Priority = request.Priority;
            promotion.IsStackable = request.IsStackable;
            promotion.Status = request.IsActive ? PromotionStatusEnum.Active : PromotionStatusEnum.Disabled;
        }

        private static void AddPromotionServices(Promotion promotion, IEnumerable<Guid> serviceIds)
        {
            foreach (var serviceId in serviceIds.Where(id => id != Guid.Empty).Distinct())
            {
                promotion.PromotionServices.Add(new PromotionService
                {
                    PromotionID = promotion.PromotionID,
                    ServiceID = serviceId
                });
            }
        }

        private async Task<string?> ValidatePromotionEligibilityAsync(Promotion promotion, Booking booking, string? code)
        {
            var now = DateTime.UtcNow;
            if (promotion.Status != PromotionStatusEnum.Active || promotion.StartDate > now || promotion.EndDate < now)
            {
                return "Promotion is not active.";
            }

            if (!string.IsNullOrWhiteSpace(promotion.PromotionCode) &&
                !string.Equals(promotion.PromotionCode, NormalizeOptional(code), StringComparison.OrdinalIgnoreCase))
            {
                return "Promotion code is invalid.";
            }

            if (promotion.MinimumSpend > booking.EstimatedTotalAmount)
            {
                return "Booking amount does not meet promotion minimum spend.";
            }

            var serviceId = booking.BookingDetails.FirstOrDefault()?.ServiceID;
            if (promotion.PromotionServices.Count > 0 &&
                (!serviceId.HasValue || promotion.PromotionServices.All(item => item.ServiceID != serviceId.Value)))
            {
                return "Promotion does not apply to this service.";
            }

            if (promotion.MinTierID.HasValue && booking.Customer.Tier is not null)
            {
                var minTier = await _context.LoyaltyTiers.AsNoTracking().FirstOrDefaultAsync(tier => tier.TierID == promotion.MinTierID.Value);
                if (minTier is not null && booking.Customer.Tier.TierRank < minTier.TierRank)
                {
                    return "Customer tier does not qualify for this promotion.";
                }
            }
            else if (promotion.MinTierID.HasValue)
            {
                return "Customer tier does not qualify for this promotion.";
            }

            if (promotion.TotalUsageLimit.HasValue)
            {
                var totalUsage = await _context.BookingPromotions.CountAsync(item => item.PromotionID == promotion.PromotionID);
                if (totalUsage >= promotion.TotalUsageLimit.Value)
                {
                    return "Promotion usage limit reached.";
                }
            }

            if (promotion.UsageLimitPerCustomer.HasValue)
            {
                var customerUsage = await _context.BookingPromotions
                    .CountAsync(item => item.PromotionID == promotion.PromotionID && item.Booking.CustomerID == booking.CustomerID);
                if (customerUsage >= promotion.UsageLimitPerCustomer.Value)
                {
                    return "Promotion usage limit reached for this customer.";
                }
            }

            var sentPromotion = promotion.PromotionCustomers.FirstOrDefault(item => item.CustomerID == booking.CustomerID);
            if (sentPromotion is not null && sentPromotion.ExpiresAt.HasValue && sentPromotion.ExpiresAt.Value < now)
            {
                return "Promotion delivery expired for this customer.";
            }

            return null;
        }

        private static decimal CalculatePromotionDiscount(Promotion promotion, decimal amount)
        {
            var discount = promotion.PromotionType switch
            {
                PromotionTypeEnum.PercentageDiscount => amount * promotion.PromotionValue / 100m,
                PromotionTypeEnum.FixedDiscount => promotion.PromotionValue,
                _ => 0m
            };

            if (promotion.MaxDiscountAmount.HasValue)
            {
                discount = Math.Min(discount, promotion.MaxDiscountAmount.Value);
            }

            return Math.Min(amount, Math.Max(0, discount));
        }

        private static LoyaltyTierResponse MapTier(LoyaltyTier tier)
        {
            return new LoyaltyTierResponse
            {
                Id = tier.TierID,
                Name = tier.TierName,
                Rank = tier.TierRank,
                MinSpent = tier.MinSpent,
                MinVisits = tier.MinVisits,
                QualificationPeriodMonths = tier.QualificationPeriodMonths,
                QualificationMode = tier.QualificationMode.ToString(),
                BookingWindowDays = tier.BookingWindowDays,
                PriorityLevel = tier.PriorityLevel,
                PointMultiplier = tier.PointMultiplier,
                Benefits = tier.TierBenefits,
                Status = tier.Status.ToString()
            };
        }

        private static PromotionResponse MapPromotion(Promotion promotion)
        {
            return new PromotionResponse
            {
                Id = promotion.PromotionID,
                Name = promotion.PromotionName,
                Code = promotion.PromotionCode,
                Description = promotion.Description,
                Type = promotion.PromotionType.ToString(),
                Value = promotion.PromotionValue,
                MaxDiscountAmount = promotion.MaxDiscountAmount,
                BonusPoints = promotion.BonusPoints,
                FreeServiceId = promotion.FreeServiceID,
                MinimumSpend = promotion.MinimumSpend,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                MinTierId = promotion.MinTierID,
                TotalUsageLimit = promotion.TotalUsageLimit,
                UsageLimitPerCustomer = promotion.UsageLimitPerCustomer,
                Priority = promotion.Priority,
                IsStackable = promotion.IsStackable,
                Status = promotion.Status.ToString(),
                CreatedAt = promotion.CreatedAt,
                ServiceIds = promotion.PromotionServices.Select(item => item.ServiceID).ToList()
            };
        }

        private static PointBalanceResponse MapBalance(Customer customer)
        {
            return new PointBalanceResponse
            {
                CustomerId = customer.CustomerID,
                CurrentPoints = customer.CurrentPoints,
                LifetimePoints = customer.LifetimePoints,
                CurrentTier = customer.Tier?.TierName,
                TotalSpent = customer.TotalSpent,
                TotalVisits = customer.TotalVisits
            };
        }

        private static PointTransactionResponse MapPointTransaction(LoyaltyPointTransaction transaction)
        {
            return new PointTransactionResponse
            {
                Id = transaction.TransactionID,
                CustomerId = transaction.CustomerID,
                BookingId = transaction.BookingID,
                WashHistoryId = transaction.WashHistoryID,
                RedemptionId = transaction.RedemptionID,
                Points = transaction.Points,
                OriginalPoints = transaction.OriginalPoints,
                RemainingPoints = transaction.RemainingPoints,
                BalanceAfter = transaction.BalanceAfter,
                Type = transaction.TransactionType.ToString(),
                ExpiryDate = transaction.ExpiryDate,
                IdempotencyKey = transaction.IdempotencyKey,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt
            };
        }

        private static WashHistoryResponse MapWashHistory(WashHistory history)
        {
            return new WashHistoryResponse
            {
                Id = history.WashHistoryID,
                BookingId = history.BookingID,
                WashDate = history.WashDate,
                ActualTotalAmount = history.ActualTotalAmount,
                DiscountAmount = history.DiscountAmount,
                FinalAmount = history.FinalAmount,
                PointsEarned = history.PointsEarned,
                RewardUsed = history.RewardUsed,
                CustomerRating = history.CustomerRating,
                Feedback = history.Feedback,
                CreatedAt = history.CreatedAt
            };
        }

        private static RewardResponse MapReward(Reward reward)
        {
            return new RewardResponse
            {
                Id = reward.RewardID,
                Name = reward.RewardName,
                Description = reward.Description,
                Type = reward.RewardType.ToString(),
                PointsRequired = reward.PointsRequired,
                Value = reward.RewardValue,
                ServiceId = reward.ServiceID,
                ValidFrom = reward.ValidFrom,
                ValidTo = reward.ValidTo,
                UsageLimitPerCustomer = reward.UsageLimitPerCustomer,
                Status = reward.Status.ToString(),
                CreatedAt = reward.CreatedAt
            };
        }

        private static RewardRedemptionResponse MapRedemption(RewardRedemption redemption)
        {
            return new RewardRedemptionResponse
            {
                Id = redemption.RedemptionID,
                CustomerId = redemption.CustomerID,
                RewardId = redemption.RewardID,
                BookingId = redemption.BookingID,
                PointsSpent = redemption.PointsSpent,
                Status = redemption.Status.ToString(),
                RedeemedAt = redemption.RedeemedAt,
                ExpiresAt = redemption.ExpiresAt,
                UsedAt = redemption.UsedAt
            };
        }

        private static TierHistoryResponse MapTierHistory(CustomerTierHistory history)
        {
            return new TierHistoryResponse
            {
                Id = history.CustomerTierHistoryID,
                CustomerId = history.CustomerID,
                PreviousTierId = history.PreviousTierID,
                NewTierId = history.NewTierID,
                ReviewPeriodStart = history.ReviewPeriodStart,
                ReviewPeriodEnd = history.ReviewPeriodEnd,
                QualifiedSpent = history.QualifiedSpent,
                QualifiedVisits = history.QualifiedVisits,
                ChangeReason = history.ChangeReason.ToString(),
                ChangedAt = history.ChangedAt,
                Notes = history.Notes
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int NormalizePageSize(int pageSize)
        {
            return pageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => pageSize
            };
        }
    }
}

namespace BusinessLayer.Dtos.Loyalty
{
    public class PromotionResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int BonusPoints { get; set; }
        public Guid? FreeServiceId { get; set; }
        public decimal MinimumSpend { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? MinTierId { get; set; }
        public int? TotalUsageLimit { get; set; }
        public int? UsageLimitPerCustomer { get; set; }
        public int Priority { get; set; }
        public bool IsStackable { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public IReadOnlyList<Guid> ServiceIds { get; set; } = Array.Empty<Guid>();
    }

    public class CreatePromotionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; } = "FixedDiscount";
        public decimal Value { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int BonusPoints { get; set; }
        public Guid? FreeServiceId { get; set; }
        public decimal MinimumSpend { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? MinTierId { get; set; }
        public int? TotalUsageLimit { get; set; }
        public int? UsageLimitPerCustomer { get; set; }
        public int Priority { get; set; }
        public bool IsStackable { get; set; }
        public bool IsActive { get; set; } = true;
        public IReadOnlyList<Guid> ServiceIds { get; set; } = Array.Empty<Guid>();
    }

    public class UpdatePromotionRequest : CreatePromotionRequest
    {
    }

    public class SendPromotionRequest
    {
        public IReadOnlyList<Guid> CustomerIds { get; set; } = Array.Empty<Guid>();
        public DateTime? ExpiresAt { get; set; }
    }

    public class PromotionDeliveryResponse
    {
        public Guid PromotionId { get; set; }
        public int SentCount { get; set; }
        public int SkippedCount { get; set; }
    }

    public class ApplyPromotionRequest
    {
        public Guid BookingId { get; set; }
        public Guid CustomerId { get; set; }
        public string? Code { get; set; }
    }

    public class ApplyPromotionResponse
    {
        public Guid BookingId { get; set; }
        public Guid PromotionId { get; set; }
        public decimal DiscountAmount { get; set; }
        public int BonusPoints { get; set; }
        public decimal TotalBeforeDiscount { get; set; }
        public decimal TotalAfterDiscount { get; set; }
    }
}

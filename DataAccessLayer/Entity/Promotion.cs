using System;
using System.Collections.Generic;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entity
{
    public class Promotion
    {
        public Promotion()
        {
            PromotionID = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            PromotionCustomers = new HashSet<PromotionCustomer>();
        }

        public Guid PromotionID { get; set; }
        public string PromotionName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal MinimumSpend { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Guid? MinTierID { get; set; }
        public PromotionStatusEnum Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual LoyaltyTier? MinTier { get; set; }
        public virtual ICollection<PromotionCustomer> PromotionCustomers { get; set; }
    }
}

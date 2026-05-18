using System;
using System.Collections.Generic;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entity
{
    public class LoyaltyTier
    {
        public LoyaltyTier()
        {
            TierID = Guid.NewGuid();
            Promotions = new HashSet<Promotion>();
            Customers = new HashSet<Customer>();
        }

        public Guid TierID { get; set; }
        public string TierName { get; set; } = null!;
        public decimal MinSpent { get; set; }
        public int MinVisits { get; set; }
        public int BookingWindowDays { get; set; }
        public int PriorityLevel { get; set; }
        public decimal PointMultiplier { get; set; }
        public string? TierBenefits { get; set; }
        public LoyaltyTierStatusEnum Status { get; set; }

        public virtual ICollection<Promotion> Promotions { get; set; }
        public virtual ICollection<Customer> Customers { get; set; }
    }
}

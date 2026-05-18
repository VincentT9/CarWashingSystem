using System;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entity
{
    public class LoyaltyPointTransaction
    {
        public LoyaltyPointTransaction()
        {
            TransactionID = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public Guid TransactionID { get; set; }
        public Guid CustomerID { get; set; }
        public int Points { get; set; }
        public PointTransactionTypeEnum TransactionType { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Customer Customer { get; set; } = null!;
    }
}

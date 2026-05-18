using System;
using System.Collections.Generic;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entity
{
    public class Booking
    {
        public Booking()
        {
            BookingID = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            BookingDetails = new HashSet<BookingDetail>();
            WashHistories = new HashSet<WashHistory>();
            Payments = new HashSet<Payment>();
            BehavioralLogs = new HashSet<BehavioralLog>();
        }

        public Guid BookingID { get; set; }
        public Guid CustomerID { get; set; }
        public Guid VehicleID { get; set; }
        public Guid BranchID { get; set; }
        public DateTime BookingDate { get; set; }
        public BookingStatusEnum BookingStatus { get; set; }
        public int QueuePriority { get; set; }
        public decimal EstimatedTotalAmount { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Customer Customer { get; set; } = null!;
        public virtual Vehicle Vehicle { get; set; } = null!;
        public virtual Branch Branch { get; set; } = null!;
        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
        public virtual ICollection<WashHistory> WashHistories { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<BehavioralLog> BehavioralLogs { get; set; }
    }
}

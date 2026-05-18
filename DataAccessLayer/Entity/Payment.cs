using System;
using DataAccessLayer.Enums;

namespace DataAccessLayer.Entity
{
    public class Payment
    {
        public Payment()
        {
            PaymentID = Guid.NewGuid();
        }

        public Guid PaymentID { get; set; }
        public Guid BookingID { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethodEnum PaymentMethod { get; set; }
        public PaymentStatusEnum PaymentStatus { get; set; }
        public DateTime? PaidAt { get; set; }

        public virtual Booking Booking { get; set; } = null!;
    }
}

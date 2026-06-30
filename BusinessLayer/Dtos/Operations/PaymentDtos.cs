namespace BusinessLayer.Dtos.Operations
{
    public class CreatePaymentRequest
    {
        public Guid BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    public class MarkPaymentPaidRequest
    {
        public string? ReferenceNumber { get; set; }
        public string? Note { get; set; }
    }

    public class VoidPaymentRequest
    {
        public string? Note { get; set; }
    }

    public class PaymentResponse
    {
        public Guid Id { get; set; }
        public Guid BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Note { get; set; }
    }

    public class PaymentListItemResponse : PaymentResponse
    {
    }
}

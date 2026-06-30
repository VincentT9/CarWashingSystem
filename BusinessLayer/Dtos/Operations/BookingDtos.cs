namespace BusinessLayer.Dtos.Operations
{
    public class CreateBookingRequest
    {
        public Guid VehicleId { get; set; }
        public Guid BranchId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime BookingStartTime { get; set; }
        public string? Note { get; set; }
    }

    public class CancelBookingRequest
    {
        public string? Reason { get; set; }
    }

    public class BookingResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public Guid VehicleId { get; set; }
        public Guid BranchId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid? WashBayId { get; set; }
        public DateTime BookingStartTime { get; set; }
        public DateTime BookingEndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string ServiceNameSnapshot { get; set; } = string.Empty;
        public int DurationMinutesSnapshot { get; set; }
        public decimal PriceSnapshot { get; set; }
        public string? TierSnapshot { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }
    }

    public class BookingListItemResponse : BookingResponse
    {
    }

    public class BookingDetailResponse : BookingResponse
    {
        public string BranchName { get; set; } = string.Empty;
        public string? WashBayName { get; set; }
    }
}

namespace BusinessLayer.Dtos.History
{
    public class WashHistoryListItemDto
    {
        public Guid WashHistoryID { get; set; }
        public Guid BookingID { get; set; }
        public DateTime WashDate { get; set; }
        public decimal FinalAmount { get; set; }
        public int PointsEarned { get; set; }
        public int? CustomerRating { get; set; }
        public string? VehiclePlate { get; set; }
        public string? BranchName { get; set; }
        public List<string> Services { get; set; } = [];
    }

    public class WashHistoryDetailDto
    {
        public Guid WashHistoryID { get; set; }
        public Guid BookingID { get; set; }
        public DateTime WashDate { get; set; }
        public decimal ActualTotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public int PointsEarned { get; set; }
        public decimal RewardUsed { get; set; }
        public int? CustomerRating { get; set; }
        public string? Feedback { get; set; }
        public string? VehiclePlate { get; set; }
        public string? BranchName { get; set; }
        public List<WashHistoryServiceDto> Services { get; set; } = [];
    }

    public class WashHistoryServiceDto
    {
        public Guid ServiceID { get; set; }
        public string ServiceName { get; set; } = null!;
        public decimal Price { get; set; }
    }
}

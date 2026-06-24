namespace BusinessLayer.Dtos.AI
{
    public class AiChatRequestDto
    {
        public string Message { get; set; } = null!;
        public string? ConversationId { get; set; }
    }

    public class AiChatResponseDto
    {
        public string Reply { get; set; } = null!;
        public string ConversationId { get; set; } = null!;
        public bool IsFallback { get; set; }
        public string Source { get; set; } = null!;
    }

    public class AiSuggestServicesRequestDto
    {
        public string? VehicleType { get; set; }
        public string? Preference { get; set; }
    }

    public class AiSuggestedServiceDto
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Reason { get; set; }
    }

    public class AiSuggestServicesResponseDto
    {
        public List<AiSuggestedServiceDto> Suggestions { get; set; } = [];
        public string Summary { get; set; } = null!;
        public bool IsFallback { get; set; }
        public string Source { get; set; } = null!;
    }

    public class AiAdminChatRequestDto
    {
        public string Message { get; set; } = null!;
        public string? ConversationId { get; set; }
    }

    public class CustomerAiContextDto
    {
        public string CustomerName { get; set; } = null!;
        public int CurrentPoints { get; set; }
        public string? TierName { get; set; }
        public int TotalVisits { get; set; }
        public decimal TotalSpent { get; set; }
        public List<string> Vehicles { get; set; } = [];
        public List<string> Perks { get; set; } = [];
        public List<CustomerWashHistoryContextDto> RecentWashes { get; set; } = [];
        public List<ServiceCatalogItemDto> AvailableServices { get; set; } = [];
    }

    public class CustomerWashHistoryContextDto
    {
        public DateTime WashDate { get; set; }
        public string? Services { get; set; }
        public decimal FinalAmount { get; set; }
    }

    public class AdminAiContextDto
    {
        public int TotalCustomers { get; set; }
        public int TotalActiveUsers { get; set; }
        public int TotalVehicles { get; set; }
        public int TotalBookingsToday { get; set; }
        public List<TierSummaryDto> TierDistribution { get; set; } = [];
        public List<ServiceCatalogItemDto> ActiveServices { get; set; } = [];
    }

    public class TierSummaryDto
    {
        public string TierName { get; set; } = null!;
        public int CustomerCount { get; set; }
    }

    public class ServiceCatalogItemDto
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }
}

using BusinessLayer.Dtos.Common;
using BusinessLayer.Dtos.History;

namespace BusinessLayer.IService
{
    public interface IWashHistoryService
    {
        Task<PagedResult<WashHistoryListItemDto>> GetMyHistoryAsync(int page, int pageSize);
        Task<PagedResult<WashHistoryListItemDto>> GetHistoryByCustomerIdAsync(Guid customerId, int page, int pageSize);
        Task<WashHistoryDetailDto> GetMyHistoryDetailAsync(Guid washHistoryId);
    }
}

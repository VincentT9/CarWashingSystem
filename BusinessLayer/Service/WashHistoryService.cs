using BusinessLayer.Dtos.Common;
using BusinessLayer.Dtos.History;
using BusinessLayer.IService;
using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Service
{
    public class WashHistoryService : IWashHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentCustomerService _currentCustomer;

        public WashHistoryService(ApplicationDbContext context, ICurrentCustomerService currentCustomer)
        {
            _context = context;
            _currentCustomer = currentCustomer;
        }

        public async Task<PagedResult<WashHistoryListItemDto>> GetMyHistoryAsync(int page, int pageSize)
        {
            var customerId = await _currentCustomer.GetCurrentCustomerIdAsync();
            return await GetHistoryByCustomerIdAsync(customerId, page, pageSize);
        }

        public async Task<PagedResult<WashHistoryListItemDto>> GetHistoryByCustomerIdAsync(Guid customerId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var query = _context.WashHistories
                .Include(w => w.Booking)
                    .ThenInclude(b => b.Vehicle)
                .Include(w => w.Booking)
                    .ThenInclude(b => b.Branch)
                .Include(w => w.Booking)
                    .ThenInclude(b => b.BookingDetails)
                        .ThenInclude(d => d.Service)
                .Where(w => w.Booking.CustomerID == customerId)
                .OrderByDescending(w => w.WashDate);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(w => new WashHistoryListItemDto
                {
                    WashHistoryID = w.WashHistoryID,
                    BookingID = w.BookingID,
                    WashDate = w.WashDate,
                    FinalAmount = w.FinalAmount,
                    PointsEarned = w.PointsEarned,
                    CustomerRating = w.CustomerRating,
                    VehiclePlate = w.Booking.Vehicle.LicensePlate,
                    BranchName = w.Booking.Branch.BranchName,
                    Services = w.Booking.BookingDetails.Select(d => d.Service.ServiceName).ToList()
                })
                .ToListAsync();

            return new PagedResult<WashHistoryListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<WashHistoryDetailDto> GetMyHistoryDetailAsync(Guid washHistoryId)
        {
            var customerId = await _currentCustomer.GetCurrentCustomerIdAsync();

            var history = await _context.WashHistories
                .Include(w => w.Booking)
                    .ThenInclude(b => b.Vehicle)
                .Include(w => w.Booking)
                    .ThenInclude(b => b.Branch)
                .Include(w => w.Booking)
                    .ThenInclude(b => b.BookingDetails)
                        .ThenInclude(d => d.Service)
                .FirstOrDefaultAsync(w => w.WashHistoryID == washHistoryId && w.Booking.CustomerID == customerId)
                ?? throw new KeyNotFoundException("Wash history not found.");

            return new WashHistoryDetailDto
            {
                WashHistoryID = history.WashHistoryID,
                BookingID = history.BookingID,
                WashDate = history.WashDate,
                ActualTotalAmount = history.ActualTotalAmount,
                DiscountAmount = history.DiscountAmount,
                FinalAmount = history.FinalAmount,
                PointsEarned = history.PointsEarned,
                RewardUsed = history.RewardUsed,
                CustomerRating = history.CustomerRating,
                Feedback = history.Feedback,
                VehiclePlate = history.Booking.Vehicle.LicensePlate,
                BranchName = history.Booking.Branch.BranchName,
                Services = history.Booking.BookingDetails.Select(d => new WashHistoryServiceDto
                {
                    ServiceID = d.ServiceID,
                    ServiceName = d.Service.ServiceName,
                    Price = d.Service.Price
                }).ToList()
            };
        }
    }
}

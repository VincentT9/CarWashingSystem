using BusinessLayer.Dtos.Customer;
using BusinessLayer.IService;
using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Service
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentCustomerService _currentCustomer;

        public CustomerService(ApplicationDbContext context, ICurrentCustomerService currentCustomer)
        {
            _context = context;
            _currentCustomer = currentCustomer;
        }

        public async Task<CustomerProfileDto> GetMyProfileAsync()
        {
            var customerId = await _currentCustomer.GetCurrentCustomerIdAsync();
            return await GetProfileByCustomerIdAsync(customerId);
        }

        public async Task<CustomerProfileDto> GetProfileByCustomerIdAsync(Guid customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .Include(c => c.Tier)
                .FirstOrDefaultAsync(c => c.CustomerID == customerId)
                ?? throw new KeyNotFoundException("Customer not found.");

            var perks = customer.TierID.HasValue
                ? await _context.TierBenefits
                    .Where(b => b.TierID == customer.TierID && b.IsActive)
                    .Select(b => b.BenefitName)
                    .ToListAsync()
                : [];

            return MapToDto(customer, perks);
        }

        private static CustomerProfileDto MapToDto(DataAccessLayer.Entity.Customer customer, List<string> perks) => new()
        {
            CustomerID = customer.CustomerID,
            UserID = customer.UserID,
            Username = customer.User.Username,
            FullName = customer.User.FullName,
            Email = customer.User.Email,
            PhoneNumber = customer.User.PhoneNumber,
            CurrentPoints = customer.CurrentPoints,
            LifetimePoints = customer.LifetimePoints,
            TotalSpent = customer.TotalSpent,
            TotalVisits = customer.TotalVisits,
            LastVisitDate = customer.LastVisitDate,
            TierID = customer.TierID,
            TierName = customer.Tier?.TierName,
            TierRank = customer.Tier?.TierRank,
            CurrentTierSince = customer.CurrentTierSince,
            CreatedAt = customer.CreatedAt,
            TierPerks = perks
        };
    }
}

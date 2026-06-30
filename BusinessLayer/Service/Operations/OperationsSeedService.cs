using BusinessLayer.IService.Operations;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;
using ServiceEntity = DataAccessLayer.Entity.Service;

namespace BusinessLayer.Service.Operations
{
    public class OperationsSeedService : IOperationsSeedService
    {
        private readonly ApplicationDbContext _context;

        public OperationsSeedService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            var basicService = await EnsureServiceAsync("Basic Wash", "Exterior basic wash", 80000m, 30);
            await EnsureServiceAsync("Premium Wash", "Exterior and interior premium wash", 150000m, 60);
            await EnsureServiceAsync("Interior Cleaning", "Interior vacuum and surface cleaning", 120000m, 45);
            await EnsureServiceAsync("Engine Cleaning", "Engine bay cleaning", 200000m, 90);

            var district1 = await EnsureBranchAsync("AutoWash District 1", "District 1, Ho Chi Minh City", "0900000001");
            var district9 = await EnsureBranchAsync("AutoWash District 9", "District 9, Ho Chi Minh City", "0900000009");

            await EnsureWashBayAsync(district1.BranchID, "Bay 01");
            await EnsureWashBayAsync(district1.BranchID, "Bay 02");
            await EnsureWashBayAsync(district9.BranchID, "Bay 03");

            await _context.SaveChangesAsync();

            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync();
            var vehicle = customer is null
                ? null
                : await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(item => item.CustomerID == customer.CustomerID);

            if (customer is not null && vehicle is not null)
            {
                await EnsureDemoBookingAsync(customer.CustomerID, vehicle.VehicleID, district1.BranchID, basicService.ServiceID, BookingStatusEnum.Pending, DateTime.UtcNow.AddDays(1).Date.AddHours(8));
                await EnsureDemoBookingAsync(customer.CustomerID, vehicle.VehicleID, district1.BranchID, basicService.ServiceID, BookingStatusEnum.Confirmed, DateTime.UtcNow.AddDays(1).Date.AddHours(10));
                await EnsureDemoBookingAsync(customer.CustomerID, vehicle.VehicleID, district9.BranchID, basicService.ServiceID, BookingStatusEnum.InProgress, DateTime.UtcNow.AddDays(1).Date.AddHours(13));
                await EnsureDemoBookingAsync(customer.CustomerID, vehicle.VehicleID, district9.BranchID, basicService.ServiceID, BookingStatusEnum.Completed, DateTime.UtcNow.AddDays(-1).Date.AddHours(9));
                await EnsureDemoBookingAsync(customer.CustomerID, vehicle.VehicleID, district1.BranchID, basicService.ServiceID, BookingStatusEnum.Cancelled, DateTime.UtcNow.AddDays(2).Date.AddHours(15));
            }

            await _context.SaveChangesAsync();
        }

        private async Task<ServiceEntity> EnsureServiceAsync(string name, string description, decimal price, int minutes)
        {
            var service = await _context.Services.FirstOrDefaultAsync(item => item.ServiceName == name);
            if (service is not null)
            {
                return service;
            }

            service = new ServiceEntity
            {
                ServiceName = name,
                Description = description,
                Price = price,
                EstimatedDuration = TimeSpan.FromMinutes(minutes),
                Status = ServiceStatusEnum.Active
            };

            _context.Services.Add(service);
            return service;
        }

        private async Task<Branch> EnsureBranchAsync(string name, string address, string phone)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(item => item.BranchName == name);
            if (branch is not null)
            {
                return branch;
            }

            branch = new Branch
            {
                BranchName = name,
                Address = address,
                PhoneNumber = phone,
                OpenTime = new TimeSpan(7, 0, 0),
                CloseTime = new TimeSpan(20, 0, 0),
                Status = BranchStatusEnum.Open
            };

            _context.Branches.Add(branch);
            return branch;
        }

        private async Task EnsureWashBayAsync(Guid branchId, string name)
        {
            var exists = await _context.WashBays.AnyAsync(item => item.BranchID == branchId && item.BayName == name);
            if (exists)
            {
                return;
            }

            _context.WashBays.Add(new WashBay
            {
                BranchID = branchId,
                BayName = name,
                Status = WashBayStatusEnum.Active
            });
        }

        private async Task EnsureDemoBookingAsync(Guid customerId, Guid vehicleId, Guid branchId, Guid serviceId, BookingStatusEnum status, DateTime start)
        {
            var exists = await _context.Bookings.AnyAsync(item =>
                item.CustomerID == customerId &&
                item.BranchID == branchId &&
                item.BookingStatus == status &&
                item.ScheduledStart == start);

            if (exists)
            {
                return;
            }

            var service = await _context.Services.AsNoTracking().FirstAsync(item => item.ServiceID == serviceId);
            var washBay = await _context.WashBays.AsNoTracking().FirstAsync(item => item.BranchID == branchId && item.Status == WashBayStatusEnum.Active);
            var end = start.Add(service.EstimatedDuration ?? TimeSpan.FromMinutes(30));

            var booking = new Booking
            {
                CustomerID = customerId,
                VehicleID = vehicleId,
                BranchID = branchId,
                WashBayID = washBay.WashBayID,
                ScheduledStart = start,
                ScheduledEnd = end,
                BookingStatus = status,
                EstimatedTotalAmount = service.Price,
                QueuePriority = 0,
                CompletedAt = status == BookingStatusEnum.Completed ? end : null,
                CancelledAt = status == BookingStatusEnum.Cancelled ? DateTime.UtcNow : null
            };

            booking.BookingDetails.Add(new BookingDetail
            {
                BookingID = booking.BookingID,
                ServiceID = service.ServiceID,
                Quantity = 1,
                UnitPrice = service.Price
            });

            booking.Payments.Add(new Payment
            {
                BookingID = booking.BookingID,
                Amount = service.Price,
                PaymentMethod = PaymentMethodEnum.Cash,
                PaymentStatus = status == BookingStatusEnum.Completed ? PaymentStatusEnum.Paid : PaymentStatusEnum.Pending,
                PaidAt = status == BookingStatusEnum.Completed ? DateTime.UtcNow : null
            });

            _context.Bookings.Add(booking);
        }
    }
}

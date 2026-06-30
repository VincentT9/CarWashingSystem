using BusinessLayer.Dtos.Operations;
using BusinessLayer.IService;
using BusinessLayer.IService.Operations;
using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;
using ServiceEntity = DataAccessLayer.Entity.Service;

namespace BusinessLayer.Service.Operations
{
    public class OperationsService : IOperationsService, IBookingReadService
    {
        private static readonly BookingStatusEnum[] ActiveBookingStatuses =
        {
            BookingStatusEnum.Pending,
            BookingStatusEnum.Confirmed,
            BookingStatusEnum.InProgress
        };

        private readonly ApplicationDbContext _context;
        private readonly IWashCompletionService _washCompletionService;
        private readonly IBehavioralLogWriter _behavioralLogWriter;

        public OperationsService(
            ApplicationDbContext context,
            IWashCompletionService washCompletionService,
            IBehavioralLogWriter behavioralLogWriter)
        {
            _context = context;
            _washCompletionService = washCompletionService;
            _behavioralLogWriter = behavioralLogWriter;
        }

        public async Task<PagedResult<ServiceListItemResponse>> GetServicesAsync(int page, int pageSize, bool includeInactive)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.Services.AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(service => service.Status == ServiceStatusEnum.Active);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(service => service.ServiceName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(service => new ServiceListItemResponse
                {
                    Id = service.ServiceID,
                    Name = service.ServiceName,
                    Description = service.Description,
                    Price = service.Price,
                    DurationMinutes = ToMinutes(service.EstimatedDuration),
                    IsActive = service.Status == ServiceStatusEnum.Active
                })
                .ToListAsync();

            return new PagedResult<ServiceListItemResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        public async Task<OperationResult<ServiceResponse>> GetServiceAsync(Guid id)
        {
            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(item => item.ServiceID == id);
            return service is null
                ? OperationResult<ServiceResponse>.Failure("Service not found.", 404)
                : OperationResult<ServiceResponse>.Success(MapService(service));
        }

        public async Task<OperationResult<ServiceResponse>> CreateServiceAsync(CreateServiceRequest request)
        {
            var validation = ValidateServiceRequest(request.Name, request.Description, request.Price, request.DurationMinutes);
            if (validation is not null)
            {
                return OperationResult<ServiceResponse>.Failure(validation, 400);
            }

            var normalizedName = request.Name.Trim();
            var nameExists = await _context.Services.AnyAsync(service => service.ServiceName.ToLower() == normalizedName.ToLower());
            if (nameExists)
            {
                return OperationResult<ServiceResponse>.Failure("Service name already exists.", 409);
            }

            var service = new ServiceEntity
            {
                ServiceName = normalizedName,
                Description = NormalizeOptional(request.Description),
                Price = request.Price,
                EstimatedDuration = TimeSpan.FromMinutes(request.DurationMinutes),
                Status = ServiceStatusEnum.Active
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return OperationResult<ServiceResponse>.Success(MapService(service), 201);
        }

        public async Task<OperationResult<ServiceResponse>> UpdateServiceAsync(Guid id, UpdateServiceRequest request)
        {
            var validation = ValidateServiceRequest(request.Name, request.Description, request.Price, request.DurationMinutes);
            if (validation is not null)
            {
                return OperationResult<ServiceResponse>.Failure(validation, 400);
            }

            var service = await _context.Services.FirstOrDefaultAsync(item => item.ServiceID == id);
            if (service is null)
            {
                return OperationResult<ServiceResponse>.Failure("Service not found.", 404);
            }

            var normalizedName = request.Name.Trim();
            var nameExists = await _context.Services.AnyAsync(item => item.ServiceID != id && item.ServiceName.ToLower() == normalizedName.ToLower());
            if (nameExists)
            {
                return OperationResult<ServiceResponse>.Failure("Service name already exists.", 409);
            }

            service.ServiceName = normalizedName;
            service.Description = NormalizeOptional(request.Description);
            service.Price = request.Price;
            service.EstimatedDuration = TimeSpan.FromMinutes(request.DurationMinutes);
            service.Status = request.IsActive ? ServiceStatusEnum.Active : ServiceStatusEnum.Inactive;

            await _context.SaveChangesAsync();
            return OperationResult<ServiceResponse>.Success(MapService(service));
        }

        public async Task<OperationResult<bool>> DeleteServiceAsync(Guid id)
        {
            var service = await _context.Services.FirstOrDefaultAsync(item => item.ServiceID == id);
            if (service is null)
            {
                return OperationResult<bool>.Failure("Service not found.", 404);
            }

            var hasBookings = await _context.BookingDetails.AnyAsync(detail => detail.ServiceID == id);
            service.Status = hasBookings ? ServiceStatusEnum.Archived : ServiceStatusEnum.Inactive;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Success(true);
        }

        public async Task<PagedResult<BranchListItemResponse>> GetBranchesAsync(int page, int pageSize, bool includeInactive)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.Branches.AsNoTracking();
            if (!includeInactive)
            {
                query = query.Where(branch => branch.Status == BranchStatusEnum.Open);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(branch => branch.BranchName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(branch => new BranchListItemResponse
                {
                    Id = branch.BranchID,
                    Name = branch.BranchName,
                    Address = branch.Address,
                    Phone = branch.PhoneNumber,
                    OpenTime = branch.OpenTime,
                    CloseTime = branch.CloseTime,
                    IsActive = branch.Status == BranchStatusEnum.Open
                })
                .ToListAsync();

            return new PagedResult<BranchListItemResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        public async Task<OperationResult<BranchResponse>> GetBranchAsync(Guid id)
        {
            var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(item => item.BranchID == id);
            return branch is null
                ? OperationResult<BranchResponse>.Failure("Branch not found.", 404)
                : OperationResult<BranchResponse>.Success(MapBranch(branch));
        }

        public async Task<OperationResult<BranchResponse>> CreateBranchAsync(CreateBranchRequest request)
        {
            var validation = ValidateBranchRequest(request.Name, request.Address, request.OpenTime, request.CloseTime);
            if (validation is not null)
            {
                return OperationResult<BranchResponse>.Failure(validation, 400);
            }

            var normalizedName = request.Name.Trim();
            var nameExists = await _context.Branches.AnyAsync(branch => branch.BranchName.ToLower() == normalizedName.ToLower());
            if (nameExists)
            {
                return OperationResult<BranchResponse>.Failure("Branch name already exists.", 409);
            }

            var branch = new Branch
            {
                BranchName = normalizedName,
                Address = request.Address.Trim(),
                PhoneNumber = NormalizeOptional(request.Phone),
                OpenTime = request.OpenTime,
                CloseTime = request.CloseTime,
                Status = BranchStatusEnum.Open
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            return OperationResult<BranchResponse>.Success(MapBranch(branch), 201);
        }

        public async Task<OperationResult<BranchResponse>> UpdateBranchAsync(Guid id, UpdateBranchRequest request)
        {
            var validation = ValidateBranchRequest(request.Name, request.Address, request.OpenTime, request.CloseTime);
            if (validation is not null)
            {
                return OperationResult<BranchResponse>.Failure(validation, 400);
            }

            var branch = await _context.Branches.FirstOrDefaultAsync(item => item.BranchID == id);
            if (branch is null)
            {
                return OperationResult<BranchResponse>.Failure("Branch not found.", 404);
            }

            var normalizedName = request.Name.Trim();
            var nameExists = await _context.Branches.AnyAsync(item => item.BranchID != id && item.BranchName.ToLower() == normalizedName.ToLower());
            if (nameExists)
            {
                return OperationResult<BranchResponse>.Failure("Branch name already exists.", 409);
            }

            branch.BranchName = normalizedName;
            branch.Address = request.Address.Trim();
            branch.PhoneNumber = NormalizeOptional(request.Phone);
            branch.OpenTime = request.OpenTime;
            branch.CloseTime = request.CloseTime;
            branch.Status = request.IsActive ? BranchStatusEnum.Open : BranchStatusEnum.Closed;

            await _context.SaveChangesAsync();
            return OperationResult<BranchResponse>.Success(MapBranch(branch));
        }

        public async Task<OperationResult<bool>> DeleteBranchAsync(Guid id)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(item => item.BranchID == id);
            if (branch is null)
            {
                return OperationResult<bool>.Failure("Branch not found.", 404);
            }

            branch.Status = BranchStatusEnum.Closed;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Success(true);
        }

        public async Task<PagedResult<WashBayListItemResponse>> GetWashBaysAsync(int page, int pageSize, Guid? branchId, bool includeInactive)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.WashBays.AsNoTracking();
            if (branchId.HasValue)
            {
                query = query.Where(washBay => washBay.BranchID == branchId.Value);
            }

            if (!includeInactive)
            {
                query = query.Where(washBay => washBay.Status == WashBayStatusEnum.Active);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(washBay => washBay.BayName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(washBay => new WashBayListItemResponse
                {
                    Id = washBay.WashBayID,
                    BranchId = washBay.BranchID,
                    Name = washBay.BayName,
                    IsActive = washBay.Status == WashBayStatusEnum.Active
                })
                .ToListAsync();

            return new PagedResult<WashBayListItemResponse> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        public async Task<OperationResult<WashBayResponse>> GetWashBayAsync(Guid id)
        {
            var washBay = await _context.WashBays.AsNoTracking().FirstOrDefaultAsync(item => item.WashBayID == id);
            return washBay is null
                ? OperationResult<WashBayResponse>.Failure("Wash bay not found.", 404)
                : OperationResult<WashBayResponse>.Success(MapWashBay(washBay));
        }

        public async Task<OperationResult<WashBayResponse>> CreateWashBayAsync(CreateWashBayRequest request)
        {
            var validation = await ValidateWashBayRequestAsync(request.BranchId, request.Name);
            if (validation is not null)
            {
                return OperationResult<WashBayResponse>.Failure(validation, 400);
            }

            var normalizedName = request.Name.Trim();
            var nameExists = await _context.WashBays.AnyAsync(washBay => washBay.BranchID == request.BranchId && washBay.BayName.ToLower() == normalizedName.ToLower());
            if (nameExists)
            {
                return OperationResult<WashBayResponse>.Failure("Wash bay name already exists in this branch.", 409);
            }

            var washBay = new WashBay
            {
                BranchID = request.BranchId,
                BayName = normalizedName,
                Status = WashBayStatusEnum.Active
            };

            _context.WashBays.Add(washBay);
            await _context.SaveChangesAsync();

            return OperationResult<WashBayResponse>.Success(MapWashBay(washBay), 201);
        }

        public async Task<OperationResult<WashBayResponse>> UpdateWashBayAsync(Guid id, UpdateWashBayRequest request)
        {
            var validation = await ValidateWashBayRequestAsync(request.BranchId, request.Name);
            if (validation is not null)
            {
                return OperationResult<WashBayResponse>.Failure(validation, 400);
            }

            var washBay = await _context.WashBays.FirstOrDefaultAsync(item => item.WashBayID == id);
            if (washBay is null)
            {
                return OperationResult<WashBayResponse>.Failure("Wash bay not found.", 404);
            }

            var normalizedName = request.Name.Trim();
            var nameExists = await _context.WashBays.AnyAsync(item =>
                item.WashBayID != id &&
                item.BranchID == request.BranchId &&
                item.BayName.ToLower() == normalizedName.ToLower());
            if (nameExists)
            {
                return OperationResult<WashBayResponse>.Failure("Wash bay name already exists in this branch.", 409);
            }

            washBay.BranchID = request.BranchId;
            washBay.BayName = normalizedName;
            washBay.Status = request.IsActive ? WashBayStatusEnum.Active : WashBayStatusEnum.Inactive;

            await _context.SaveChangesAsync();
            return OperationResult<WashBayResponse>.Success(MapWashBay(washBay));
        }

        public async Task<OperationResult<bool>> DeleteWashBayAsync(Guid id)
        {
            var washBay = await _context.WashBays.FirstOrDefaultAsync(item => item.WashBayID == id);
            if (washBay is null)
            {
                return OperationResult<bool>.Failure("Wash bay not found.", 404);
            }

            washBay.Status = WashBayStatusEnum.Inactive;
            await _context.SaveChangesAsync();

            return OperationResult<bool>.Success(true);
        }

        public async Task<PagedResult<BookingListItemResponse>> GetBookingsAsync(
            Guid? currentCustomerId,
            bool isAdmin,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            Guid? branchId,
            int page,
            int pageSize)
        {
            page = NormalizePage(page);
            pageSize = NormalizePageSize(pageSize);

            var query = _context.Bookings
                .AsNoTracking()
                .Include(booking => booking.BookingDetails)
                .ThenInclude(detail => detail.Service)
                .Include(booking => booking.TierSnapshot)
                .AsQueryable();

            if (!isAdmin)
            {
                if (!currentCustomerId.HasValue)
                {
                    return new PagedResult<BookingListItemResponse> { Items = Array.Empty<BookingListItemResponse>(), Page = page, PageSize = pageSize };
                }

                query = query.Where(booking => booking.CustomerID == currentCustomerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatusEnum>(status, true, out var parsedStatus))
            {
                query = query.Where(booking => booking.BookingStatus == parsedStatus);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(booking => booking.ScheduledStart >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(booking => booking.ScheduledStart <= toDate.Value);
            }

            if (branchId.HasValue)
            {
                query = query.Where(booking => booking.BranchID == branchId.Value);
            }

            var total = await query.CountAsync();
            var bookings = await query
                .OrderByDescending(booking => booking.ScheduledStart)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<BookingListItemResponse>
            {
                Items = bookings.Select(MapBookingListItem).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<OperationResult<BookingDetailResponse>> GetBookingAsync(Guid id, Guid? currentCustomerId, bool isAdmin)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(item => item.Branch)
                .Include(item => item.WashBay)
                .Include(item => item.BookingDetails)
                .ThenInclude(detail => detail.Service)
                .Include(item => item.TierSnapshot)
                .FirstOrDefaultAsync(item => item.BookingID == id);

            if (booking is null)
            {
                return OperationResult<BookingDetailResponse>.Failure("Booking not found.", 404);
            }

            if (!isAdmin && (!currentCustomerId.HasValue || booking.CustomerID != currentCustomerId.Value))
            {
                return OperationResult<BookingDetailResponse>.Failure("You are not allowed to access this booking.", 403);
            }

            return OperationResult<BookingDetailResponse>.Success(MapBookingDetail(booking));
        }

        public async Task<OperationResult<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, Guid? currentCustomerId)
        {
            if (!currentCustomerId.HasValue)
            {
                return OperationResult<BookingResponse>.Failure("Current customer is required.", 401);
            }

            if (request.VehicleId == Guid.Empty || request.BranchId == Guid.Empty || request.ServiceId == Guid.Empty)
            {
                return OperationResult<BookingResponse>.Failure("VehicleId, BranchId and ServiceId are required.", 400);
            }

            if (request.BookingStartTime <= DateTime.UtcNow)
            {
                return OperationResult<BookingResponse>.Failure("Booking time must be in the future.", 400);
            }

            var vehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(item => item.VehicleID == request.VehicleId);
            if (vehicle is null || vehicle.CustomerID != currentCustomerId.Value)
            {
                return OperationResult<BookingResponse>.Failure("Vehicle does not belong to the current customer.", 403);
            }

            var service = await _context.Services.AsNoTracking().FirstOrDefaultAsync(item => item.ServiceID == request.ServiceId);
            if (service is null || service.Status != ServiceStatusEnum.Active)
            {
                return OperationResult<BookingResponse>.Failure("Service not found or inactive.", 400);
            }

            var duration = service.EstimatedDuration ?? TimeSpan.Zero;
            if (duration.TotalMinutes <= 0)
            {
                return OperationResult<BookingResponse>.Failure("Service duration must be greater than 0.", 400);
            }

            var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(item => item.BranchID == request.BranchId);
            if (branch is null || branch.Status != BranchStatusEnum.Open)
            {
                return OperationResult<BookingResponse>.Failure("Branch not found or inactive.", 400);
            }

            var start = request.BookingStartTime;
            var end = start.Add(duration);
            if (!IsInsideOperatingHours(branch, start, end))
            {
                return OperationResult<BookingResponse>.Failure("Booking time must be within branch operating hours.", 400);
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .Include(item => item.Tier)
                .FirstOrDefaultAsync(item => item.CustomerID == currentCustomerId.Value);
            if (customer is null)
            {
                return OperationResult<BookingResponse>.Failure("Current customer not found.", 401);
            }

            if (customer.Tier is not null && customer.Tier.BookingWindowDays > 0 && start > DateTime.UtcNow.AddDays(customer.Tier.BookingWindowDays))
            {
                return OperationResult<BookingResponse>.Failure("Booking time exceeds current tier booking window.", 400);
            }

            var washBay = await FindAvailableWashBayAsync(request.BranchId, start, end);
            if (washBay is null)
            {
                return OperationResult<BookingResponse>.Failure("No available wash bay for selected time window.", 409);
            }

            var booking = new Booking
            {
                CustomerID = currentCustomerId.Value,
                VehicleID = request.VehicleId,
                BranchID = request.BranchId,
                WashBayID = washBay.WashBayID,
                TierIDSnapshot = customer.TierID,
                ScheduledStart = start,
                ScheduledEnd = end,
                BookingStatus = BookingStatusEnum.Pending,
                QueuePriority = customer.Tier?.PriorityLevel ?? 0,
                EstimatedTotalAmount = service.Price,
                Notes = NormalizeOptional(request.Note)
            };

            booking.BookingDetails.Add(new BookingDetail
            {
                BookingID = booking.BookingID,
                ServiceID = service.ServiceID,
                Quantity = 1,
                UnitPrice = service.Price
            });

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            await _behavioralLogWriter.WriteAsync(new BehavioralLogWriteRequest
            {
                CustomerId = booking.CustomerID,
                BookingId = booking.BookingID,
                ServiceId = service.ServiceID,
                ActionType = BehavioralActionTypeEnum.Book,
                SpendingAmount = booking.EstimatedTotalAmount,
                Notes = "Booking created"
            });

            booking.BookingDetails.First().Service = service;
            booking.TierSnapshot = customer.Tier;

            return OperationResult<BookingResponse>.Success(MapBooking(booking), 201);
        }

        public async Task<OperationResult<BookingResponse>> CancelBookingAsync(Guid id, CancelBookingRequest request, Guid? currentCustomerId, bool isAdmin)
        {
            var booking = await LoadBookingAsync(id);
            if (booking is null)
            {
                return OperationResult<BookingResponse>.Failure("Booking not found.", 404);
            }

            if (!isAdmin && (!currentCustomerId.HasValue || booking.CustomerID != currentCustomerId.Value))
            {
                return OperationResult<BookingResponse>.Failure("You are not allowed to cancel this booking.", 403);
            }

            if (booking.BookingStatus == BookingStatusEnum.Completed)
            {
                return OperationResult<BookingResponse>.Failure("Completed booking cannot be cancelled.", 400);
            }

            if (await _context.Payments.AnyAsync(payment => payment.BookingID == id && payment.PaymentStatus == PaymentStatusEnum.Paid))
            {
                return OperationResult<BookingResponse>.Failure("Paid booking cannot be cancelled.", 400);
            }

            booking.BookingStatus = BookingStatusEnum.Cancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.CancellationReason = NormalizeOptional(request.Reason);
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _behavioralLogWriter.WriteAsync(new BehavioralLogWriteRequest
            {
                CustomerId = booking.CustomerID,
                BookingId = booking.BookingID,
                ServiceId = booking.BookingDetails.FirstOrDefault()?.ServiceID,
                ActionType = BehavioralActionTypeEnum.CancelBooking,
                Notes = booking.CancellationReason
            });
            return OperationResult<BookingResponse>.Success(MapBooking(booking));
        }

        public Task<OperationResult<BookingResponse>> ConfirmBookingAsync(Guid id)
        {
            return TransitionBookingAsync(id, BookingStatusEnum.Pending, BookingStatusEnum.Confirmed, "Only pending bookings can be confirmed.");
        }

        public async Task<OperationResult<BookingResponse>> StartBookingAsync(Guid id)
        {
            var result = await TransitionBookingAsync(id, BookingStatusEnum.Confirmed, BookingStatusEnum.InProgress, "Only confirmed bookings can be started.");
            if (result.Succeeded && result.Data is not null)
            {
                var booking = await _context.Bookings.FirstAsync(item => item.BookingID == id);
                booking.StartedAt ??= DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return result;
        }

        public async Task<OperationResult<BookingResponse>> CompleteBookingAsync(Guid id)
        {
            var booking = await LoadBookingAsync(id);
            if (booking is null)
            {
                return OperationResult<BookingResponse>.Failure("Booking not found.", 404);
            }

            if (booking.BookingStatus == BookingStatusEnum.Completed)
            {
                return OperationResult<BookingResponse>.Success(MapBooking(booking));
            }

            if (booking.BookingStatus != BookingStatusEnum.InProgress)
            {
                return OperationResult<BookingResponse>.Failure("Only in-progress bookings can be completed.", 400);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var completedAt = DateTime.UtcNow;
                booking.BookingStatus = BookingStatusEnum.Completed;
                booking.CompletedAt = completedAt;
                booking.UpdatedAt = completedAt;

                await _washCompletionService.CompleteWashAsync(new WashCompletionPayload
                {
                    BookingId = booking.BookingID,
                    CustomerId = booking.CustomerID,
                    VehicleId = booking.VehicleID,
                    ServiceId = booking.BookingDetails.First().ServiceID,
                    BranchId = booking.BranchID,
                    Amount = booking.EstimatedTotalAmount,
                    CompletedAt = completedAt
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return OperationResult<BookingResponse>.Success(MapBooking(booking));
        }

        public async Task<OperationResult<BookingReadSnapshot>> GetSnapshotAsync(Guid bookingId)
        {
            var booking = await _context.Bookings
                .AsNoTracking()
                .Include(item => item.BookingDetails)
                .ThenInclude(detail => detail.Service)
                .FirstOrDefaultAsync(item => item.BookingID == bookingId);

            if (booking is null)
            {
                return OperationResult<BookingReadSnapshot>.Failure("Booking not found.", 404);
            }

            var detail = booking.BookingDetails.FirstOrDefault();
            return OperationResult<BookingReadSnapshot>.Success(new BookingReadSnapshot
            {
                BookingId = booking.BookingID,
                CustomerId = booking.CustomerID,
                VehicleId = booking.VehicleID,
                BranchId = booking.BranchID,
                ServiceId = detail?.ServiceID ?? Guid.Empty,
                ScheduledStart = booking.ScheduledStart,
                ScheduledEnd = booking.ScheduledEnd,
                Status = booking.BookingStatus.ToString(),
                TotalAmount = booking.EstimatedTotalAmount,
                ServiceName = detail?.Service.ServiceName ?? string.Empty
            });
        }

        public async Task<OperationResult<PaymentResponse>> GetPaymentAsync(Guid id)
        {
            var payment = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(item => item.PaymentID == id);
            return payment is null
                ? OperationResult<PaymentResponse>.Failure("Payment not found.", 404)
                : OperationResult<PaymentResponse>.Success(MapPayment(payment));
        }

        public async Task<OperationResult<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request)
        {
            if (request.BookingId == Guid.Empty)
            {
                return OperationResult<PaymentResponse>.Failure("BookingId is required.", 400);
            }

            if (request.Amount <= 0)
            {
                return OperationResult<PaymentResponse>.Failure("Amount must be greater than 0.", 400);
            }

            if (!TryParsePaymentMethod(request.Method, out var method))
            {
                return OperationResult<PaymentResponse>.Failure("Payment method must be Cash, Card or BankTransfer.", 400);
            }

            var booking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(item => item.BookingID == request.BookingId);
            if (booking is null)
            {
                return OperationResult<PaymentResponse>.Failure("Booking not found.", 404);
            }

            if (booking.BookingStatus == BookingStatusEnum.Cancelled)
            {
                return OperationResult<PaymentResponse>.Failure("Cannot create payment for cancelled booking.", 400);
            }

            if (request.Amount != booking.EstimatedTotalAmount)
            {
                return OperationResult<PaymentResponse>.Failure("Payment amount must equal booking total amount.", 400);
            }

            var payment = new Payment
            {
                BookingID = request.BookingId,
                Amount = request.Amount,
                PaymentMethod = method,
                PaymentStatus = PaymentStatusEnum.Pending,
                Notes = NormalizeOptional(request.Note)
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return OperationResult<PaymentResponse>.Success(MapPayment(payment), 201);
        }

        public async Task<OperationResult<PaymentResponse>> MarkPaymentPaidAsync(Guid id, MarkPaymentPaidRequest request)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(item => item.PaymentID == id);
            if (payment is null)
            {
                return OperationResult<PaymentResponse>.Failure("Payment not found.", 404);
            }

            if (payment.PaymentStatus == PaymentStatusEnum.Voided)
            {
                return OperationResult<PaymentResponse>.Failure("Voided payment cannot be marked paid.", 400);
            }

            if (payment.PaymentStatus != PaymentStatusEnum.Pending)
            {
                return OperationResult<PaymentResponse>.Failure("Only pending payment can be marked paid.", 400);
            }

            payment.PaymentStatus = PaymentStatusEnum.Paid;
            payment.PaidAt = DateTime.UtcNow;
            payment.ReferenceNumber = NormalizeOptional(request.ReferenceNumber);
            payment.Notes = MergeNotes(payment.Notes, request.Note);

            await _context.SaveChangesAsync();
            return OperationResult<PaymentResponse>.Success(MapPayment(payment));
        }

        public async Task<OperationResult<PaymentResponse>> VoidPaymentAsync(Guid id, VoidPaymentRequest request)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(item => item.PaymentID == id);
            if (payment is null)
            {
                return OperationResult<PaymentResponse>.Failure("Payment not found.", 404);
            }

            if (payment.PaymentStatus == PaymentStatusEnum.Paid)
            {
                return OperationResult<PaymentResponse>.Failure("Paid payment cannot be voided.", 400);
            }

            if (payment.PaymentStatus != PaymentStatusEnum.Pending)
            {
                return OperationResult<PaymentResponse>.Failure("Only pending payment can be voided.", 400);
            }

            payment.PaymentStatus = PaymentStatusEnum.Voided;
            payment.Notes = MergeNotes(payment.Notes, request.Note);

            await _context.SaveChangesAsync();
            return OperationResult<PaymentResponse>.Success(MapPayment(payment));
        }

        private async Task<OperationResult<BookingResponse>> TransitionBookingAsync(Guid id, BookingStatusEnum expected, BookingStatusEnum next, string invalidMessage)
        {
            var booking = await LoadBookingAsync(id);
            if (booking is null)
            {
                return OperationResult<BookingResponse>.Failure("Booking not found.", 404);
            }

            if (booking.BookingStatus != expected)
            {
                return OperationResult<BookingResponse>.Failure(invalidMessage, 400);
            }

            booking.BookingStatus = next;
            booking.UpdatedAt = DateTime.UtcNow;
            if (next == BookingStatusEnum.InProgress)
            {
                booking.StartedAt ??= DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return OperationResult<BookingResponse>.Success(MapBooking(booking));
        }

        private async Task<Booking?> LoadBookingAsync(Guid id)
        {
            return await _context.Bookings
                .Include(item => item.BookingDetails)
                .ThenInclude(detail => detail.Service)
                .Include(item => item.TierSnapshot)
                .FirstOrDefaultAsync(item => item.BookingID == id);
        }

        private async Task<WashBay?> FindAvailableWashBayAsync(Guid branchId, DateTime start, DateTime end)
        {
            var washBays = await _context.WashBays
                .Where(item => item.BranchID == branchId && item.Status == WashBayStatusEnum.Active)
                .OrderBy(item => item.BayName)
                .ToListAsync();

            foreach (var washBay in washBays)
            {
                var overlaps = await _context.Bookings.AnyAsync(booking =>
                    booking.WashBayID == washBay.WashBayID &&
                    ActiveBookingStatuses.Contains(booking.BookingStatus) &&
                    booking.ScheduledStart < end &&
                    start < booking.ScheduledEnd);

                if (!overlaps)
                {
                    return washBay;
                }
            }

            return null;
        }

        private static string? ValidateServiceRequest(string name, string? description, decimal price, int durationMinutes)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Name is required.";
            }

            if (name.Trim().Length > 100)
            {
                return "Name must be at most 100 characters.";
            }

            if (price <= 0)
            {
                return "Price must be greater than 0.";
            }

            if (durationMinutes is < 10 or > 240)
            {
                return "DurationMinutes must be between 10 and 240.";
            }

            if (description?.Length > 500)
            {
                return "Description must be at most 500 characters.";
            }

            return null;
        }

        private static string? ValidateBranchRequest(string name, string address, TimeSpan openTime, TimeSpan closeTime)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Name is required.";
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                return "Address is required.";
            }

            if (openTime >= closeTime)
            {
                return "OpenTime must be earlier than CloseTime.";
            }

            return null;
        }

        private async Task<string?> ValidateWashBayRequestAsync(Guid branchId, string name)
        {
            if (branchId == Guid.Empty)
            {
                return "BranchId is required.";
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return "Name is required.";
            }

            var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(item => item.BranchID == branchId);
            if (branch is null)
            {
                return "Branch not found.";
            }

            if (branch.Status != BranchStatusEnum.Open)
            {
                return "Cannot create wash bay for inactive branch.";
            }

            return null;
        }

        private static bool IsInsideOperatingHours(Branch branch, DateTime start, DateTime end)
        {
            if (!branch.OpenTime.HasValue || !branch.CloseTime.HasValue)
            {
                return true;
            }

            return start.TimeOfDay >= branch.OpenTime.Value && end.TimeOfDay <= branch.CloseTime.Value;
        }

        private static ServiceResponse MapService(ServiceEntity service)
        {
            return new ServiceResponse
            {
                Id = service.ServiceID,
                Name = service.ServiceName,
                Description = service.Description,
                Price = service.Price,
                DurationMinutes = ToMinutes(service.EstimatedDuration),
                IsActive = service.Status == ServiceStatusEnum.Active
            };
        }

        private static BranchResponse MapBranch(Branch branch)
        {
            return new BranchResponse
            {
                Id = branch.BranchID,
                Name = branch.BranchName,
                Address = branch.Address,
                Phone = branch.PhoneNumber,
                OpenTime = branch.OpenTime,
                CloseTime = branch.CloseTime,
                IsActive = branch.Status == BranchStatusEnum.Open
            };
        }

        private static WashBayResponse MapWashBay(WashBay washBay)
        {
            return new WashBayResponse
            {
                Id = washBay.WashBayID,
                BranchId = washBay.BranchID,
                Name = washBay.BayName,
                IsActive = washBay.Status == WashBayStatusEnum.Active
            };
        }

        private static BookingListItemResponse MapBookingListItem(Booking booking)
        {
            var mapped = MapBooking(booking);
            return new BookingListItemResponse
            {
                Id = mapped.Id,
                CustomerId = mapped.CustomerId,
                VehicleId = mapped.VehicleId,
                BranchId = mapped.BranchId,
                ServiceId = mapped.ServiceId,
                WashBayId = mapped.WashBayId,
                BookingStartTime = mapped.BookingStartTime,
                BookingEndTime = mapped.BookingEndTime,
                Status = mapped.Status,
                TotalAmount = mapped.TotalAmount,
                ServiceNameSnapshot = mapped.ServiceNameSnapshot,
                DurationMinutesSnapshot = mapped.DurationMinutesSnapshot,
                PriceSnapshot = mapped.PriceSnapshot,
                TierSnapshot = mapped.TierSnapshot,
                CreatedAt = mapped.CreatedAt,
                Note = mapped.Note
            };
        }

        private static BookingDetailResponse MapBookingDetail(Booking booking)
        {
            var mapped = MapBooking(booking);
            return new BookingDetailResponse
            {
                Id = mapped.Id,
                CustomerId = mapped.CustomerId,
                VehicleId = mapped.VehicleId,
                BranchId = mapped.BranchId,
                ServiceId = mapped.ServiceId,
                WashBayId = mapped.WashBayId,
                BookingStartTime = mapped.BookingStartTime,
                BookingEndTime = mapped.BookingEndTime,
                Status = mapped.Status,
                TotalAmount = mapped.TotalAmount,
                ServiceNameSnapshot = mapped.ServiceNameSnapshot,
                DurationMinutesSnapshot = mapped.DurationMinutesSnapshot,
                PriceSnapshot = mapped.PriceSnapshot,
                TierSnapshot = mapped.TierSnapshot,
                CreatedAt = mapped.CreatedAt,
                Note = mapped.Note,
                BranchName = booking.Branch.BranchName,
                WashBayName = booking.WashBay?.BayName
            };
        }

        private static BookingResponse MapBooking(Booking booking)
        {
            var detail = booking.BookingDetails.FirstOrDefault();
            return new BookingResponse
            {
                Id = booking.BookingID,
                CustomerId = booking.CustomerID,
                VehicleId = booking.VehicleID,
                BranchId = booking.BranchID,
                ServiceId = detail?.ServiceID ?? Guid.Empty,
                WashBayId = booking.WashBayID,
                BookingStartTime = booking.ScheduledStart,
                BookingEndTime = booking.ScheduledEnd,
                Status = booking.BookingStatus.ToString(),
                TotalAmount = booking.EstimatedTotalAmount,
                ServiceNameSnapshot = detail?.Service.ServiceName ?? string.Empty,
                DurationMinutesSnapshot = ToMinutes(detail?.Service.EstimatedDuration),
                PriceSnapshot = detail?.UnitPrice ?? booking.EstimatedTotalAmount,
                TierSnapshot = booking.TierSnapshot?.TierName,
                CreatedAt = booking.CreatedAt,
                Note = booking.Notes
            };
        }

        private static PaymentResponse MapPayment(Payment payment)
        {
            return new PaymentResponse
            {
                Id = payment.PaymentID,
                BookingId = payment.BookingID,
                Amount = payment.Amount,
                Method = payment.PaymentMethod.ToString(),
                Status = payment.PaymentStatus.ToString(),
                CreatedAt = payment.RecordedAt,
                PaidAt = payment.PaidAt,
                ReferenceNumber = payment.ReferenceNumber,
                Note = payment.Notes
            };
        }

        private static int ToMinutes(TimeSpan? duration)
        {
            return duration.HasValue ? (int)Math.Round(duration.Value.TotalMinutes) : 0;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int NormalizePageSize(int pageSize)
        {
            return pageSize switch
            {
                < 1 => 20,
                > 100 => 100,
                _ => pageSize
            };
        }

        private static bool TryParsePaymentMethod(string value, out PaymentMethodEnum method)
        {
            method = PaymentMethodEnum.Cash;
            if (string.Equals(value, "Cash", StringComparison.OrdinalIgnoreCase))
            {
                method = PaymentMethodEnum.Cash;
                return true;
            }

            if (string.Equals(value, "Card", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "CardAtCounter", StringComparison.OrdinalIgnoreCase))
            {
                method = PaymentMethodEnum.CardAtCounter;
                return true;
            }

            if (string.Equals(value, "BankTransfer", StringComparison.OrdinalIgnoreCase))
            {
                method = PaymentMethodEnum.BankTransfer;
                return true;
            }

            return false;
        }

        private static string? MergeNotes(string? existing, string? addition)
        {
            var normalizedAddition = NormalizeOptional(addition);
            if (normalizedAddition is null)
            {
                return existing;
            }

            return string.IsNullOrWhiteSpace(existing) ? normalizedAddition : $"{existing} | {normalizedAddition}";
        }
    }
}

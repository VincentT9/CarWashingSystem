using BusinessLayer.Dtos.Operations;

namespace BusinessLayer.IService.Operations
{
    public interface IOperationsService
    {
        Task<PagedResult<ServiceListItemResponse>> GetServicesAsync(int page, int pageSize, bool includeInactive);
        Task<OperationResult<ServiceResponse>> GetServiceAsync(Guid id);
        Task<OperationResult<ServiceResponse>> CreateServiceAsync(CreateServiceRequest request);
        Task<OperationResult<ServiceResponse>> UpdateServiceAsync(Guid id, UpdateServiceRequest request);
        Task<OperationResult<bool>> DeleteServiceAsync(Guid id);

        Task<PagedResult<BranchListItemResponse>> GetBranchesAsync(int page, int pageSize, bool includeInactive);
        Task<OperationResult<BranchResponse>> GetBranchAsync(Guid id);
        Task<OperationResult<BranchResponse>> CreateBranchAsync(CreateBranchRequest request);
        Task<OperationResult<BranchResponse>> UpdateBranchAsync(Guid id, UpdateBranchRequest request);
        Task<OperationResult<bool>> DeleteBranchAsync(Guid id);

        Task<PagedResult<WashBayListItemResponse>> GetWashBaysAsync(int page, int pageSize, Guid? branchId, bool includeInactive);
        Task<OperationResult<WashBayResponse>> GetWashBayAsync(Guid id);
        Task<OperationResult<WashBayResponse>> CreateWashBayAsync(CreateWashBayRequest request);
        Task<OperationResult<WashBayResponse>> UpdateWashBayAsync(Guid id, UpdateWashBayRequest request);
        Task<OperationResult<bool>> DeleteWashBayAsync(Guid id);

        Task<PagedResult<BookingListItemResponse>> GetBookingsAsync(
            Guid? currentCustomerId,
            bool isAdmin,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            Guid? branchId,
            int page,
            int pageSize);
        Task<OperationResult<BookingDetailResponse>> GetBookingAsync(Guid id, Guid? currentCustomerId, bool isAdmin);
        Task<OperationResult<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, Guid? currentCustomerId);
        Task<OperationResult<BookingResponse>> CancelBookingAsync(Guid id, CancelBookingRequest request, Guid? currentCustomerId, bool isAdmin);
        Task<OperationResult<BookingResponse>> ConfirmBookingAsync(Guid id);
        Task<OperationResult<BookingResponse>> StartBookingAsync(Guid id);
        Task<OperationResult<BookingResponse>> CompleteBookingAsync(Guid id);

        Task<OperationResult<PaymentResponse>> GetPaymentAsync(Guid id);
        Task<OperationResult<PaymentResponse>> CreatePaymentAsync(CreatePaymentRequest request);
        Task<OperationResult<PaymentResponse>> MarkPaymentPaidAsync(Guid id, MarkPaymentPaidRequest request);
        Task<OperationResult<PaymentResponse>> VoidPaymentAsync(Guid id, VoidPaymentRequest request);
    }
}

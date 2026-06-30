using BusinessLayer.Dtos.Operations;
using BusinessLayer.IService.Operations;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Operations
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingsController : OperationsControllerBase
    {
        private readonly IOperationsService _operationsService;

        public BookingsController(IOperationsService operationsService)
        {
            _operationsService = operationsService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<BookingListItemResponse>>> GetBookings(
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] Guid? branchId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return Ok(await _operationsService.GetBookingsAsync(CurrentCustomerId(), IsAdmin(), status, fromDate, toDate, branchId, page, pageSize));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetBooking(Guid id)
        {
            return FromResult(await _operationsService.GetBookingAsync(id, CurrentCustomerId(), IsAdmin()));
        }

        [HttpPost]
        public async Task<ActionResult> CreateBooking(CreateBookingRequest request)
        {
            return FromResult(await _operationsService.CreateBookingAsync(request, CurrentCustomerId()));
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<ActionResult> CancelBooking(Guid id, CancelBookingRequest request)
        {
            return FromResult(await _operationsService.CancelBookingAsync(id, request, CurrentCustomerId(), IsAdmin()));
        }

        [HttpPost("{id:guid}/confirm")]
        public async Task<ActionResult> ConfirmBooking(Guid id)
        {
            return FromResult(await _operationsService.ConfirmBookingAsync(id));
        }

        [HttpPost("{id:guid}/start")]
        public async Task<ActionResult> StartBooking(Guid id)
        {
            return FromResult(await _operationsService.StartBookingAsync(id));
        }

        [HttpPost("{id:guid}/complete")]
        public async Task<ActionResult> CompleteBooking(Guid id)
        {
            return FromResult(await _operationsService.CompleteBookingAsync(id));
        }
    }
}

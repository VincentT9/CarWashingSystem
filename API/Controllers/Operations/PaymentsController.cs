using BusinessLayer.Dtos.Operations;
using BusinessLayer.IService.Operations;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Operations
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : OperationsControllerBase
    {
        private readonly IOperationsService _operationsService;

        public PaymentsController(IOperationsService operationsService)
        {
            _operationsService = operationsService;
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetPayment(Guid id)
        {
            return FromResult(await _operationsService.GetPaymentAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult> CreatePayment(CreatePaymentRequest request)
        {
            return FromResult(await _operationsService.CreatePaymentAsync(request));
        }

        [HttpPost("{id:guid}/paid")]
        public async Task<ActionResult> MarkPaymentPaid(Guid id, MarkPaymentPaidRequest request)
        {
            return FromResult(await _operationsService.MarkPaymentPaidAsync(id, request));
        }

        [HttpPost("{id:guid}/void")]
        public async Task<ActionResult> VoidPayment(Guid id, VoidPaymentRequest request)
        {
            return FromResult(await _operationsService.VoidPaymentAsync(id, request));
        }
    }
}

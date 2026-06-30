using BusinessLayer.Dtos.Operations;
using BusinessLayer.IService.Operations;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Operations
{
    [ApiController]
    [Route("api/wash-bays")]
    public class WashBaysController : OperationsControllerBase
    {
        private readonly IOperationsService _operationsService;

        public WashBaysController(IOperationsService operationsService)
        {
            _operationsService = operationsService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<WashBayListItemResponse>>> GetWashBays(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? branchId = null,
            [FromQuery] bool includeInactive = false)
        {
            return Ok(await _operationsService.GetWashBaysAsync(page, pageSize, branchId, includeInactive));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult> GetWashBay(Guid id)
        {
            return FromResult(await _operationsService.GetWashBayAsync(id));
        }

        [HttpPost]
        public async Task<ActionResult> CreateWashBay(CreateWashBayRequest request)
        {
            return FromResult(await _operationsService.CreateWashBayAsync(request));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> UpdateWashBay(Guid id, UpdateWashBayRequest request)
        {
            return FromResult(await _operationsService.UpdateWashBayAsync(id, request));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteWashBay(Guid id)
        {
            return FromResult(await _operationsService.DeleteWashBayAsync(id));
        }
    }
}

using BusinessLayer.IService.Operations;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Operations
{
    [ApiController]
    [Route("api/operations/seed")]
    public class OperationsSeedController : ControllerBase
    {
        private readonly IOperationsSeedService _seedService;

        public OperationsSeedController(IOperationsSeedService seedService)
        {
            _seedService = seedService;
        }

        [HttpPost]
        public async Task<IActionResult> SeedOperations()
        {
            await _seedService.SeedAsync();
            return Ok(new { message = "Operations seed data is ready." });
        }
    }
}

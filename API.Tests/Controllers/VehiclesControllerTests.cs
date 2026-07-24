using System.Net;
using Xunit;

namespace API.Tests.Controllers
{
    public class VehiclesControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public VehiclesControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetMyVehicles_WithoutToken_ReturnsUnauthorized401()
        {
            var response = await _client.GetAsync("/api/vehicles/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}

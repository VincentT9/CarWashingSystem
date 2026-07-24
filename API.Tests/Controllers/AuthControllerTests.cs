using System.Net;
using System.Net.Http.Json;
using BusinessLayer.Dtos.Auth;
using Xunit;

namespace API.Tests.Controllers
{
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AdminOnly_WithoutToken_ReturnsUnauthorized401()
        {
            var response = await _client.GetAsync("/api/Auth/admin-only");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CustomerOnly_WithoutToken_ReturnsUnauthorized401()
        {
            var response = await _client.GetAsync("/api/Auth/customer-only");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Register_InvalidPayload_ReturnsBadRequestOrServerError()
        {
            var request = new RegisterRequestDto
            {
                Username = "",
                Password = "123",
                Email = "invalid-email",
                PhoneNumber = ""
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/register", request);

            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }
}

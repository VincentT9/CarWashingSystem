using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var dbName = "InMemoryApiTestDb_" + Guid.NewGuid().ToString();
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();

                if (!db.Roles.Any())
                {
                    db.Roles.AddRange(
                        new Role { RoleID = Guid.Parse("11111111-1111-1111-1111-111111111111"), RoleName = "Admin" },
                        new Role { RoleID = Guid.Parse("22222222-2222-2222-2222-222222222222"), RoleName = "Staff" },
                        new Role { RoleID = Guid.Parse("33333333-3333-3333-3333-333333333333"), RoleName = "Customer" }
                    );
                    db.SaveChanges();
                }
            });
        }
    }
}

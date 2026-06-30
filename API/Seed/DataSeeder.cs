using DataAccessLayer.Context;
using DataAccessLayer.Entity;
using DataAccessLayer.Enums;
using Microsoft.EntityFrameworkCore;

namespace API.Seed
{
    public static class DataSeeder
    {
        public static readonly Guid BronzeTierId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static readonly Guid SilverTierId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static readonly Guid GoldTierId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static readonly Guid PlatinumTierId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        public static readonly Guid AdminUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        public static readonly Guid StaffUserId = Guid.Parse("22222222-3333-4444-5555-666666666661");
        public static readonly Guid DemoCustomer1UserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        public static readonly Guid DemoCustomer2UserId = Guid.Parse("11111111-2222-3333-4444-555555555551");
        public static readonly Guid DemoBronzeUserId = Guid.Parse("33333333-4444-5555-6666-777777777771");
        public static readonly Guid DemoPlatinumUserId = Guid.Parse("44444444-5555-6666-7777-888888888881");

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await SeedTiersAsync(context);
            await SeedServicesAsync(context);
            await SeedBranchAsync(context);
            await SeedUsersAndCustomersAsync(context);
            await context.SaveChangesAsync();
            await SeedDemoWashHistoryAsync(context);
            await SeedBehavioralLogsAsync(context);
            await context.SaveChangesAsync();
        }

        private static async Task SeedTiersAsync(ApplicationDbContext context)
        {
            if (await context.LoyaltyTiers.AnyAsync()) return;

            var tiers = new[]
            {
                new LoyaltyTier { TierID = BronzeTierId, TierName = "Bronze", TierRank = 1, MinSpent = 0, MinVisits = 0, BookingWindowDays = 3, PriorityLevel = 1, PointMultiplier = 1.0m, Status = LoyaltyTierStatusEnum.Active, QualificationMode = TierQualificationModeEnum.AllConditions },
                new LoyaltyTier { TierID = SilverTierId, TierName = "Silver", TierRank = 2, MinSpent = 500000, MinVisits = 3, BookingWindowDays = 5, PriorityLevel = 2, PointMultiplier = 1.2m, Status = LoyaltyTierStatusEnum.Active, QualificationMode = TierQualificationModeEnum.AllConditions },
                new LoyaltyTier { TierID = GoldTierId, TierName = "Gold", TierRank = 3, MinSpent = 2000000, MinVisits = 10, BookingWindowDays = 7, PriorityLevel = 3, PointMultiplier = 1.5m, Status = LoyaltyTierStatusEnum.Active, QualificationMode = TierQualificationModeEnum.AllConditions },
                new LoyaltyTier { TierID = PlatinumTierId, TierName = "Platinum", TierRank = 4, MinSpent = 5000000, MinVisits = 25, BookingWindowDays = 14, PriorityLevel = 4, PointMultiplier = 2.0m, Status = LoyaltyTierStatusEnum.Active, QualificationMode = TierQualificationModeEnum.AllConditions }
            };

            await context.LoyaltyTiers.AddRangeAsync(tiers);

            await context.TierBenefits.AddRangeAsync(
                new TierBenefit { TierID = SilverTierId, BenefitName = "5% discount on basic wash", BenefitType = TierBenefitTypeEnum.PercentageDiscount, BenefitValue = 5, IsAutoApplied = true, IsActive = true },
                new TierBenefit { TierID = GoldTierId, BenefitName = "Free interior vacuum monthly", BenefitType = TierBenefitTypeEnum.FreeService, BenefitValue = 1, MonthlyLimit = 1, IsAutoApplied = true, IsActive = true },
                new TierBenefit { TierID = PlatinumTierId, BenefitName = "Priority queue", BenefitType = TierBenefitTypeEnum.BonusPointMultiplier, BenefitValue = 1, IsAutoApplied = true, IsActive = true }
            );
        }

        private static async Task SeedServicesAsync(ApplicationDbContext context)
        {
            if (await context.Services.AnyAsync()) return;

            await context.Services.AddRangeAsync(
                new Service { ServiceName = "Basic Wash", Description = "Exterior wash and dry", Price = 80000, EstimatedDuration = TimeSpan.FromMinutes(20), Status = ServiceStatusEnum.Active },
                new Service { ServiceName = "Premium Wash", Description = "Exterior wash, wax, tire shine", Price = 150000, EstimatedDuration = TimeSpan.FromMinutes(35), Status = ServiceStatusEnum.Active },
                new Service { ServiceName = "Interior Clean", Description = "Vacuum and dashboard wipe", Price = 120000, EstimatedDuration = TimeSpan.FromMinutes(30), Status = ServiceStatusEnum.Active },
                new Service { ServiceName = "Full Detail", Description = "Complete interior and exterior detail", Price = 350000, EstimatedDuration = TimeSpan.FromMinutes(90), Status = ServiceStatusEnum.Active }
            );
        }

        private static Guid _branchId;

        private static async Task SeedBranchAsync(ApplicationDbContext context)
        {
            if (await context.Branches.AnyAsync()) return;

            var branch = new Branch
            {
                BranchName = "AutoWash Pro - District 1",
                Address = "123 Nguyen Hue, District 1, HCMC",
                PhoneNumber = "+84901234567",
                OpenTime = TimeSpan.FromHours(7),
                CloseTime = TimeSpan.FromHours(21),
                Status = BranchStatusEnum.Open
            };
            _branchId = branch.BranchID;

            var washBay = new WashBay { BranchID = branch.BranchID, BayName = "Bay 1", Status = WashBayStatusEnum.Active };

            await context.Branches.AddAsync(branch);
            await context.WashBays.AddAsync(washBay);
        }

        private static async Task SeedUsersAndCustomersAsync(ApplicationDbContext context)
        {
            var adminRole = await context.Roles.FirstAsync(r => r.RoleName == "Admin");
            var staffRole = await context.Roles.FirstAsync(r => r.RoleName == "Staff");
            var customerRole = await context.Roles.FirstAsync(r => r.RoleName == "Customer");

            if (!await context.Users.AnyAsync(u => u.Username == "admin"))
            {
                await context.Users.AddAsync(new User
                {
                    UserID = AdminUserId,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    FullName = "System Administrator",
                    Email = "admin@autowashpro.local",
                    RoleID = adminRole.RoleID,
                    Status = UserStatusEnum.Active,
                    EmailVerified = true
                });
            }

            if (!await context.Users.AnyAsync(u => u.Username == "staff"))
            {
                await context.Users.AddAsync(new User
                {
                    UserID = StaffUserId,
                    Username = "staff",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                    FullName = "Demo Staff",
                    Email = "staff@autowashpro.local",
                    RoleID = staffRole.RoleID,
                    Status = UserStatusEnum.Active,
                    EmailVerified = true
                });
            }

            if (!await context.Users.AnyAsync(u => u.Username == "demo_customer"))
            {
                var customer1User = new User
                {
                    UserID = DemoCustomer1UserId,
                    Username = "demo_customer",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                    FullName = "Nguyen Van A",
                    Email = "demo.customer@autowashpro.local",
                    PhoneNumber = "+84987654321",
                    RoleID = customerRole.RoleID,
                    Status = UserStatusEnum.Active,
                    EmailVerified = true
                };

                var customer1 = new Customer
                {
                    UserID = customer1User.UserID,
                    TierID = SilverTierId,
                    CurrentPoints = 450,
                    LifetimePoints = 1200,
                    TotalSpent = 850000,
                    TotalVisits = 5,
                    LastVisitDate = DateTime.UtcNow.AddDays(-7),
                    CurrentTierSince = DateTime.UtcNow.AddMonths(-2)
                };

                await context.Users.AddAsync(customer1User);
                await context.Customers.AddAsync(customer1);
                await context.Vehicles.AddAsync(new Vehicle
                {
                    CustomerID = customer1.CustomerID,
                    LicensePlate = "51A12345",
                    Brand = "Toyota",
                    Model = "Vios",
                    VehicleType = VehicleTypeEnum.Sedan,
                    Color = "White",
                    Status = VehicleStatusEnum.Active
                });
            }

            if (!await context.Users.AnyAsync(u => u.Username == "demo_vip"))
            {
                var customer2User = new User
                {
                    UserID = DemoCustomer2UserId,
                    Username = "demo_vip",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                    FullName = "Tran Thi B",
                    Email = "demo.vip@autowashpro.local",
                    PhoneNumber = "+84912345678",
                    RoleID = customerRole.RoleID,
                    Status = UserStatusEnum.Active,
                    EmailVerified = true
                };

                var customer2 = new Customer
                {
                    UserID = customer2User.UserID,
                    TierID = GoldTierId,
                    CurrentPoints = 2100,
                    LifetimePoints = 5000,
                    TotalSpent = 3200000,
                    TotalVisits = 18,
                    LastVisitDate = DateTime.UtcNow.AddDays(-2),
                    CurrentTierSince = DateTime.UtcNow.AddMonths(-6)
                };

                await context.Users.AddAsync(customer2User);
                await context.Customers.AddAsync(customer2);
                await context.Vehicles.AddAsync(new Vehicle
                {
                    CustomerID = customer2.CustomerID,
                    LicensePlate = "30B67890",
                    Brand = "Hyundai",
                    Model = "Santa Fe",
                    VehicleType = VehicleTypeEnum.SUV,
                    Color = "Black",
                    Status = VehicleStatusEnum.Active
                });
            }

            if (!await context.Users.AnyAsync(u => u.Username == "demo_bronze"))
            {
                var bronzeUser = new User
                {
                    UserID = DemoBronzeUserId,
                    Username = "demo_bronze",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                    FullName = "Le Van C",
                    Email = "demo.bronze@autowashpro.local",
                    PhoneNumber = "+84901112233",
                    RoleID = customerRole.RoleID,
                    Status = UserStatusEnum.Active,
                    EmailVerified = true
                };

                var bronzeCustomer = new Customer
                {
                    UserID = bronzeUser.UserID,
                    TierID = BronzeTierId,
                    CurrentPoints = 50,
                    LifetimePoints = 50,
                    TotalSpent = 80000,
                    TotalVisits = 1,
                    LastVisitDate = DateTime.UtcNow.AddDays(-30),
                    CurrentTierSince = DateTime.UtcNow.AddMonths(-1)
                };

                await context.Users.AddAsync(bronzeUser);
                await context.Customers.AddAsync(bronzeCustomer);
                await context.Vehicles.AddAsync(new Vehicle
                {
                    CustomerID = bronzeCustomer.CustomerID,
                    LicensePlate = "59C11111",
                    Brand = "Kia",
                    Model = "Morning",
                    VehicleType = VehicleTypeEnum.Hatchback,
                    Color = "Red",
                    Status = VehicleStatusEnum.Active
                });
            }

            if (!await context.Users.AnyAsync(u => u.Username == "demo_platinum"))
            {
                var platinumUser = new User
                {
                    UserID = DemoPlatinumUserId,
                    Username = "demo_platinum",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                    FullName = "Pham Thi D",
                    Email = "demo.platinum@autowashpro.local",
                    PhoneNumber = "+84909998877",
                    RoleID = customerRole.RoleID,
                    Status = UserStatusEnum.Active,
                    EmailVerified = true
                };

                var platinumCustomer = new Customer
                {
                    UserID = platinumUser.UserID,
                    TierID = PlatinumTierId,
                    CurrentPoints = 8500,
                    LifetimePoints = 15000,
                    TotalSpent = 7200000,
                    TotalVisits = 32,
                    LastVisitDate = DateTime.UtcNow.AddDays(-1),
                    CurrentTierSince = DateTime.UtcNow.AddMonths(-12)
                };

                await context.Users.AddAsync(platinumUser);
                await context.Customers.AddAsync(platinumCustomer);
                await context.Vehicles.AddAsync(new Vehicle
                {
                    CustomerID = platinumCustomer.CustomerID,
                    LicensePlate = "51F99999",
                    Brand = "Mercedes-Benz",
                    Model = "GLC",
                    VehicleType = VehicleTypeEnum.SUV,
                    Color = "Silver",
                    Status = VehicleStatusEnum.Active
                });
            }
        }

        private static async Task SeedDemoWashHistoryAsync(ApplicationDbContext context)
        {
            if (await context.WashHistories.AnyAsync()) return;

            var customer = await context.Customers
                .Include(c => c.User)
                .Include(c => c.Vehicles)
                .FirstAsync(c => c.User.Username == "demo_customer");
            var services = await context.Services.OrderBy(s => s.Price).Take(2).ToListAsync();
            var branch = await context.Branches.FirstAsync();
            var washBay = await context.WashBays.FirstAsync();

            var booking = new Booking
            {
                CustomerID = customer.CustomerID,
                VehicleID = customer.Vehicles.First().VehicleID,
                BranchID = branch.BranchID,
                WashBayID = washBay.WashBayID,
                TierIDSnapshot = customer.TierID,
                ScheduledStart = DateTime.UtcNow.AddDays(-7),
                ScheduledEnd = DateTime.UtcNow.AddDays(-7).AddHours(1),
                BookingStatus = BookingStatusEnum.Completed,
                QueuePriority = 2,
                EstimatedTotalAmount = services.Sum(s => s.Price),
                CompletedAt = DateTime.UtcNow.AddDays(-7).AddMinutes(45)
            };

            await context.Bookings.AddAsync(booking);
            await context.BookingDetails.AddRangeAsync(services.Select(s => new BookingDetail
            {
                BookingID = booking.BookingID,
                ServiceID = s.ServiceID,
                Quantity = 1,
                UnitPrice = s.Price
            }));

            await context.WashHistories.AddAsync(new WashHistory
            {
                BookingID = booking.BookingID,
                WashDate = DateTime.UtcNow.AddDays(-7),
                ActualTotalAmount = services.Sum(s => s.Price),
                DiscountAmount = 0,
                FinalAmount = services.Sum(s => s.Price),
                PointsEarned = 85,
                CustomerRating = 5,
                Feedback = "Great service!"
            });
        }

        private static async Task SeedBehavioralLogsAsync(ApplicationDbContext context)
        {
            if (await context.BehavioralLogs.AnyAsync()) return;

            var customers = await context.Customers
                .Include(c => c.User)
                .Where(c => c.User.Username == "demo_customer" || c.User.Username == "demo_vip")
                .ToListAsync();
            var services = await context.Services.Take(2).ToListAsync();

            if (customers.Count == 0 || services.Count == 0) return;

            var logs = new List<BehavioralLog>
            {
                new()
                {
                    CustomerID = customers[0].CustomerID,
                    ServiceID = services[0].ServiceID,
                    ActionType = BehavioralActionTypeEnum.ViewPromotion,
                    PointsChanged = 0,
                    SpendingAmount = 0,
                    Notes = "Viewed summer promotion"
                },
                new()
                {
                    CustomerID = customers[0].CustomerID,
                    ServiceID = services[1].ServiceID,
                    ActionType = BehavioralActionTypeEnum.Book,
                    PointsChanged = 10,
                    SpendingAmount = 150000,
                    Notes = "Booked Premium Wash"
                },
                new()
                {
                    CustomerID = customers.Count > 1 ? customers[1].CustomerID : customers[0].CustomerID,
                    ActionType = BehavioralActionTypeEnum.LeaveFeedback,
                    PointsChanged = 5,
                    SpendingAmount = 0,
                    Notes = "Left 5-star feedback"
                }
            };

            await context.BehavioralLogs.AddRangeAsync(logs);
        }
    }
}

BEGIN;

INSERT INTO "Roles" ("RoleID", "RoleName")
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Admin'),
    ('22222222-2222-2222-2222-222222222222', 'Staff'),
    ('33333333-3333-3333-3333-333333333333', 'Customer')
ON CONFLICT ("RoleID") DO UPDATE SET
    "RoleName" = EXCLUDED."RoleName";

INSERT INTO "LoyaltyTiers" ("TierID", "TierName", "TierRank", "MinSpent", "MinVisits", "QualificationPeriodMonths", "QualificationMode", "BookingWindowDays", "PriorityLevel", "PointMultiplier", "TierBenefits", "Status")
VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Bronze', 1, 0, 0, 12, 1, 3, 1, 1.00, 'Standard booking and point earning', 1),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Silver', 2, 500000, 3, 12, 1, 5, 2, 1.20, '5% discount on basic wash', 1),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'Gold', 3, 2000000, 10, 12, 1, 7, 3, 1.50, 'Free monthly interior vacuum and priority booking', 1),
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'Platinum', 4, 5000000, 25, 12, 1, 14, 4, 2.00, 'Priority queue, premium support, and double points', 1)
ON CONFLICT ("TierName") DO UPDATE SET
    "TierRank" = EXCLUDED."TierRank",
    "MinSpent" = EXCLUDED."MinSpent",
    "MinVisits" = EXCLUDED."MinVisits",
    "QualificationPeriodMonths" = EXCLUDED."QualificationPeriodMonths",
    "QualificationMode" = EXCLUDED."QualificationMode",
    "BookingWindowDays" = EXCLUDED."BookingWindowDays",
    "PriorityLevel" = EXCLUDED."PriorityLevel",
    "PointMultiplier" = EXCLUDED."PointMultiplier",
    "TierBenefits" = EXCLUDED."TierBenefits",
    "Status" = EXCLUDED."Status";

INSERT INTO "Services" ("ServiceID", "ServiceName", "Description", "Price", "EstimatedDuration", "Status")
VALUES
    ('10000000-0000-0000-0000-000000000001', 'Basic Wash', 'Exterior foam wash, rinse, and dry', 80000, INTERVAL '20 minutes', 1),
    ('10000000-0000-0000-0000-000000000002', 'Premium Wash', 'Exterior wash, wax, tire shine, and glass cleaning', 150000, INTERVAL '35 minutes', 1),
    ('10000000-0000-0000-0000-000000000003', 'Interior Clean', 'Vacuum, dashboard wipe, seat surface cleaning, and deodorizing', 120000, INTERVAL '30 minutes', 1),
    ('10000000-0000-0000-0000-000000000004', 'Full Detail', 'Complete interior and exterior detail package', 350000, INTERVAL '90 minutes', 1)
ON CONFLICT ("ServiceName") DO UPDATE SET
    "Description" = EXCLUDED."Description",
    "Price" = EXCLUDED."Price",
    "EstimatedDuration" = EXCLUDED."EstimatedDuration",
    "Status" = EXCLUDED."Status";

INSERT INTO "Branches" ("BranchID", "BranchName", "Address", "PhoneNumber", "OpenTime", "CloseTime", "Status")
VALUES
    ('20000000-0000-0000-0000-000000000001', 'AutoWash Pro - District 1', '123 Nguyen Hue, District 1, Ho Chi Minh City', '+84901234567', INTERVAL '07:00:00', INTERVAL '21:00:00', 1),
    ('20000000-0000-0000-0000-000000000002', 'AutoWash Pro - Thu Duc', '45 Vo Van Ngan, Thu Duc, Ho Chi Minh City', '+84907654321', INTERVAL '07:30:00', INTERVAL '20:30:00', 1)
ON CONFLICT ("BranchName") DO UPDATE SET
    "Address" = EXCLUDED."Address",
    "PhoneNumber" = EXCLUDED."PhoneNumber",
    "OpenTime" = EXCLUDED."OpenTime",
    "CloseTime" = EXCLUDED."CloseTime",
    "Status" = EXCLUDED."Status";

INSERT INTO "WashBays" ("WashBayID", "BranchID", "BayName", "Status")
VALUES
    ('21000000-0000-0000-0000-000000000001', (SELECT "BranchID" FROM "Branches" WHERE "BranchName" = 'AutoWash Pro - District 1'), 'Bay 1', 1),
    ('21000000-0000-0000-0000-000000000002', (SELECT "BranchID" FROM "Branches" WHERE "BranchName" = 'AutoWash Pro - District 1'), 'Bay 2', 1),
    ('21000000-0000-0000-0000-000000000003', (SELECT "BranchID" FROM "Branches" WHERE "BranchName" = 'AutoWash Pro - Thu Duc'), 'Bay 1', 1)
ON CONFLICT ("BranchID", "BayName") DO UPDATE SET "Status" = EXCLUDED."Status";

INSERT INTO "TierBenefits" ("TierBenefitID", "TierID", "ServiceID", "BenefitName", "BenefitType", "BenefitValue", "MonthlyLimit", "IsAutoApplied", "IsActive")
VALUES
    ('22000000-0000-0000-0000-000000000001', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Basic Wash'), '5% discount on Basic Wash', 2, 5, NULL, TRUE, TRUE),
    ('22000000-0000-0000-0000-000000000002', 'cccccccc-cccc-cccc-cccc-cccccccccccc', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Interior Clean'), 'Free Interior Clean monthly', 3, 1, 1, TRUE, TRUE),
    ('22000000-0000-0000-0000-000000000003', 'dddddddd-dddd-dddd-dddd-dddddddddddd', NULL, 'Double loyalty points', 4, 2, NULL, TRUE, TRUE)
ON CONFLICT ("TierID", "BenefitName") DO UPDATE SET
    "ServiceID" = EXCLUDED."ServiceID",
    "BenefitType" = EXCLUDED."BenefitType",
    "BenefitValue" = EXCLUDED."BenefitValue",
    "MonthlyLimit" = EXCLUDED."MonthlyLimit",
    "IsAutoApplied" = EXCLUDED."IsAutoApplied",
    "IsActive" = EXCLUDED."IsActive";

INSERT INTO "Users" ("UserID", "Username", "PasswordHash", "FullName", "Email", "PhoneNumber", "RoleID", "Status", "CreatedAt", "EmailVerified")
VALUES
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'admin', '$2a$11$aPg0xU7XLWFBBBd9kFSeged5kk0bi1lS0yvs8iI7QPX.94xCf7rhK', 'System Administrator', 'admin@autowashpro.local', NULL, '11111111-1111-1111-1111-111111111111', 1, '2026-07-16T00:00:00Z', TRUE),
    ('22222222-3333-4444-5555-666666666661', 'staff', '$2a$11$k4atw0V6czLeDhh1Bw0ajOREZ4ll9uV4CJAjxHeb7bEcAhSqqM.Gi', 'Demo Staff', 'staff@autowashpro.local', '+84908887766', '22222222-2222-2222-2222-222222222222', 1, '2026-07-16T00:00:00Z', TRUE),
    ('ffffffff-ffff-ffff-ffff-ffffffffffff', 'demo_customer', '$2a$11$vBu9Y8kSY97wnV6us0W.nuICS/OzO6udTDhuXWr8JJmc4w.k4YBHW', 'Nguyen Van A', 'demo.customer@autowashpro.local', '+84987654321', '33333333-3333-3333-3333-333333333333', 1, '2026-07-16T00:00:00Z', TRUE),
    ('11111111-2222-3333-4444-555555555551', 'demo_vip', '$2a$11$SHYNNaUn6ZSr6uozu8jTY.K1A8Oyw0HuXOaRjAMd.xHvS5Ch/ipDC', 'Tran Thi B', 'demo.vip@autowashpro.local', '+84912345678', '33333333-3333-3333-3333-333333333333', 1, '2026-07-16T00:00:00Z', TRUE),
    ('33333333-4444-5555-6666-777777777771', 'demo_bronze', '$2a$11$3bj8X9bCorL7zRE0Vv/mReS61w3YK6Dpoc0MKJAABg6YdlVQxmUKy', 'Le Van C', 'demo.bronze@autowashpro.local', '+84901112233', '33333333-3333-3333-3333-333333333333', 1, '2026-07-16T00:00:00Z', TRUE),
    ('44444444-5555-6666-7777-888888888881', 'demo_platinum', '$2a$11$dmeB3xO.tbFWGXRvRLJKrOyDoJGIUbTHuhycQ4PA9qduFTvY2HQKe', 'Pham Thi D', 'demo.platinum@autowashpro.local', '+84909998877', '33333333-3333-3333-3333-333333333333', 1, '2026-07-16T00:00:00Z', TRUE)
ON CONFLICT ("Username") DO UPDATE SET
    "PasswordHash" = EXCLUDED."PasswordHash",
    "FullName" = EXCLUDED."FullName",
    "Email" = EXCLUDED."Email",
    "PhoneNumber" = EXCLUDED."PhoneNumber",
    "RoleID" = EXCLUDED."RoleID",
    "Status" = EXCLUDED."Status",
    "EmailVerified" = EXCLUDED."EmailVerified";

INSERT INTO "Customers" ("CustomerID", "UserID", "TierID", "CurrentPoints", "LifetimePoints", "TotalSpent", "TotalVisits", "LastVisitDate", "CurrentTierSince", "CreatedAt", "Version")
VALUES
    ('30000000-0000-0000-0000-000000000001', 'ffffffff-ffff-ffff-ffff-ffffffffffff', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 450, 1200, 850000, 5, '2026-07-09T00:00:00Z', '2026-05-16T00:00:00Z', '2026-07-16T00:00:00Z', 0),
    ('30000000-0000-0000-0000-000000000002', '11111111-2222-3333-4444-555555555551', 'cccccccc-cccc-cccc-cccc-cccccccccccc', 2100, 5000, 3200000, 18, '2026-07-14T00:00:00Z', '2026-01-16T00:00:00Z', '2026-07-16T00:00:00Z', 0),
    ('30000000-0000-0000-0000-000000000003', '33333333-4444-5555-6666-777777777771', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 50, 50, 80000, 1, '2026-06-16T00:00:00Z', '2026-06-16T00:00:00Z', '2026-07-16T00:00:00Z', 0),
    ('30000000-0000-0000-0000-000000000004', '44444444-5555-6666-7777-888888888881', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 8500, 15000, 7200000, 32, '2026-07-15T00:00:00Z', '2025-07-16T00:00:00Z', '2026-07-16T00:00:00Z', 0)
ON CONFLICT ("UserID") DO UPDATE SET
    "TierID" = EXCLUDED."TierID",
    "CurrentPoints" = EXCLUDED."CurrentPoints",
    "LifetimePoints" = EXCLUDED."LifetimePoints",
    "TotalSpent" = EXCLUDED."TotalSpent",
    "TotalVisits" = EXCLUDED."TotalVisits",
    "LastVisitDate" = EXCLUDED."LastVisitDate",
    "CurrentTierSince" = EXCLUDED."CurrentTierSince";

INSERT INTO "Vehicles" ("VehicleID", "CustomerID", "LicensePlate", "VehicleType", "Brand", "Model", "Color", "Status", "CreatedAt")
VALUES
    ('40000000-0000-0000-0000-000000000001', (SELECT c."CustomerID" FROM "Customers" c JOIN "Users" u ON u."UserID" = c."UserID" WHERE u."Username" = 'demo_customer'), '51A12345', 'Sedan', 'Toyota', 'Vios', 'White', 1, '2026-07-16T00:00:00Z'),
    ('40000000-0000-0000-0000-000000000002', (SELECT c."CustomerID" FROM "Customers" c JOIN "Users" u ON u."UserID" = c."UserID" WHERE u."Username" = 'demo_vip'), '30B67890', 'SUV', 'Hyundai', 'Santa Fe', 'Black', 1, '2026-07-16T00:00:00Z'),
    ('40000000-0000-0000-0000-000000000003', (SELECT c."CustomerID" FROM "Customers" c JOIN "Users" u ON u."UserID" = c."UserID" WHERE u."Username" = 'demo_bronze'), '59C11111', 'Hatchback', 'Kia', 'Morning', 'Red', 1, '2026-07-16T00:00:00Z'),
    ('40000000-0000-0000-0000-000000000004', (SELECT c."CustomerID" FROM "Customers" c JOIN "Users" u ON u."UserID" = c."UserID" WHERE u."Username" = 'demo_platinum'), '51F99999', 'SUV', 'Mercedes-Benz', 'GLC', 'Silver', 1, '2026-07-16T00:00:00Z')
ON CONFLICT ("LicensePlate") DO UPDATE SET
    "CustomerID" = EXCLUDED."CustomerID",
    "VehicleType" = EXCLUDED."VehicleType",
    "Brand" = EXCLUDED."Brand",
    "Model" = EXCLUDED."Model",
    "Color" = EXCLUDED."Color",
    "Status" = EXCLUDED."Status";

INSERT INTO "Promotions" ("PromotionID", "PromotionName", "PromotionCode", "Description", "PromotionType", "PromotionValue", "MaxDiscountAmount", "BonusPoints", "FreeServiceID", "MinimumSpend", "StartDate", "EndDate", "MinTierID", "TotalUsageLimit", "UsageLimitPerCustomer", "Priority", "IsStackable", "Status", "CreatedAt")
VALUES
    ('50000000-0000-0000-0000-000000000001', 'July Premium Wash Discount', 'JULY15', '15% discount for Premium Wash bookings in July', 1, 15, 50000, 0, NULL, 100000, '2026-07-01T00:00:00Z', '2026-07-31T23:59:59Z', NULL, 500, 1, 10, FALSE, 2, '2026-07-16T00:00:00Z'),
    ('50000000-0000-0000-0000-000000000002', 'Gold Member Bonus Points', 'GOLDPOINTS', 'Gold and Platinum members earn bonus points on full detail', 4, 0, NULL, 200, NULL, 300000, '2026-07-01T00:00:00Z', '2026-08-31T23:59:59Z', 'cccccccc-cccc-cccc-cccc-cccccccccccc', 200, 2, 8, TRUE, 2, '2026-07-16T00:00:00Z'),
    ('50000000-0000-0000-0000-000000000003', 'New Customer Welcome 20K', 'WELCOME20K', 'Fixed 20,000 VND discount for first test booking', 2, 20000, 20000, 0, NULL, 80000, '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', NULL, 1000, 1, 9, FALSE, 2, '2026-07-16T00:00:00Z'),
    ('50000000-0000-0000-0000-000000000004', 'Basic Wash Flash Sale', 'BASIC10', '10% discount for Basic Wash bookings', 1, 10, 20000, 0, NULL, 80000, '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', NULL, 1000, 3, 7, FALSE, 2, '2026-07-16T00:00:00Z'),
    ('50000000-0000-0000-0000-000000000005', 'Interior Clean 30K Off', 'INTERIOR30K', 'Fixed 30,000 VND discount for Interior Clean', 2, 30000, 30000, 0, NULL, 120000, '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', NULL, 500, 2, 7, FALSE, 2, '2026-07-16T00:00:00Z'),
    ('50000000-0000-0000-0000-000000000006', 'Silver Weekend Bonus', 'SILVER100', 'Silver and above members earn 100 bonus points', 4, 0, NULL, 100, NULL, 150000, '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 500, 2, 6, TRUE, 2, '2026-07-16T00:00:00Z'),
    ('50000000-0000-0000-0000-000000000007', 'Platinum VIP Detail Discount', 'VIP25', '25% discount for Platinum members on Full Detail', 1, 25, 120000, 0, NULL, 300000, '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', 'dddddddd-dddd-dddd-dddd-dddddddddddd', 100, 2, 10, FALSE, 2, '2026-07-16T00:00:00Z'),
    ('50000000-0000-0000-0000-000000000008', 'Free Basic Wash Add-on', 'FREEBASIC', 'Free Basic Wash promotion for high value bookings', 3, 80000, 80000, 0, (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Basic Wash'), 300000, '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', NULL, 200, 1, 8, FALSE, 2, '2026-07-16T00:00:00Z'),
    ('50000000-0000-0000-0000-000000000009', 'Thu Duc Branch Opening', 'THUDUC20', '20% discount for bookings at Thu Duc branch services', 1, 20, 60000, 0, NULL, 100000, '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', NULL, 300, 2, 8, FALSE, 2, '2026-07-16T00:00:00Z'),
    ('50000000-0000-0000-0000-000000000010', 'Full Detail 100K Off', 'DETAIL100K', 'Fixed 100,000 VND discount for Full Detail', 2, 100000, 100000, 0, NULL, 350000, '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', NULL, 300, 1, 9, FALSE, 2, '2026-07-16T00:00:00Z')
ON CONFLICT ("PromotionCode") WHERE "PromotionCode" IS NOT NULL DO UPDATE SET
    "PromotionName" = EXCLUDED."PromotionName",
    "Description" = EXCLUDED."Description",
    "PromotionType" = EXCLUDED."PromotionType",
    "PromotionValue" = EXCLUDED."PromotionValue",
    "MaxDiscountAmount" = EXCLUDED."MaxDiscountAmount",
    "BonusPoints" = EXCLUDED."BonusPoints",
    "MinimumSpend" = EXCLUDED."MinimumSpend",
    "StartDate" = EXCLUDED."StartDate",
    "EndDate" = EXCLUDED."EndDate",
    "MinTierID" = EXCLUDED."MinTierID",
    "TotalUsageLimit" = EXCLUDED."TotalUsageLimit",
    "UsageLimitPerCustomer" = EXCLUDED."UsageLimitPerCustomer",
    "Priority" = EXCLUDED."Priority",
    "IsStackable" = EXCLUDED."IsStackable",
    "Status" = EXCLUDED."Status";

INSERT INTO "PromotionServices" ("PromotionID", "ServiceID")
VALUES
    ('50000000-0000-0000-0000-000000000001', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Premium Wash')),
    ('50000000-0000-0000-0000-000000000002', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Full Detail')),
    ('50000000-0000-0000-0000-000000000003', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Basic Wash')),
    ('50000000-0000-0000-0000-000000000003', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Premium Wash')),
    ('50000000-0000-0000-0000-000000000004', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Basic Wash')),
    ('50000000-0000-0000-0000-000000000005', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Interior Clean')),
    ('50000000-0000-0000-0000-000000000006', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Premium Wash')),
    ('50000000-0000-0000-0000-000000000006', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Full Detail')),
    ('50000000-0000-0000-0000-000000000007', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Full Detail')),
    ('50000000-0000-0000-0000-000000000008', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Full Detail')),
    ('50000000-0000-0000-0000-000000000009', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Premium Wash')),
    ('50000000-0000-0000-0000-000000000009', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Interior Clean')),
    ('50000000-0000-0000-0000-000000000010', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Full Detail'))
ON CONFLICT ("PromotionID", "ServiceID") DO NOTHING;

INSERT INTO "Rewards" ("RewardID", "RewardName", "Description", "RewardType", "PointsRequired", "RewardValue", "ServiceID", "ValidFrom", "ValidTo", "UsageLimitPerCustomer", "Status", "CreatedAt")
VALUES
    ('60000000-0000-0000-0000-000000000001', '80K Wash Voucher', 'Redeem points for an 80,000 VND wash discount', 1, 800, 80000, NULL, '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', 3, 1, '2026-07-16T00:00:00Z'),
    ('60000000-0000-0000-0000-000000000002', 'Free Basic Wash', 'Redeem points for one free Basic Wash', 3, 1200, 80000, (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Basic Wash'), '2026-07-01T00:00:00Z', '2026-12-31T23:59:59Z', 2, 1, '2026-07-16T00:00:00Z')
ON CONFLICT ("RewardName") DO UPDATE SET
    "Description" = EXCLUDED."Description",
    "RewardType" = EXCLUDED."RewardType",
    "PointsRequired" = EXCLUDED."PointsRequired",
    "RewardValue" = EXCLUDED."RewardValue",
    "ServiceID" = EXCLUDED."ServiceID",
    "ValidFrom" = EXCLUDED."ValidFrom",
    "ValidTo" = EXCLUDED."ValidTo",
    "UsageLimitPerCustomer" = EXCLUDED."UsageLimitPerCustomer",
    "Status" = EXCLUDED."Status";

INSERT INTO "Bookings" ("BookingID", "CustomerID", "VehicleID", "BranchID", "WashBayID", "TierIDSnapshot", "ScheduledStart", "ScheduledEnd", "BookingStatus", "QueuePriority", "EstimatedTotalAmount", "Notes", "CompletedAt", "CreatedAt", "Version")
VALUES
    ('70000000-0000-0000-0000-000000000001', (SELECT c."CustomerID" FROM "Customers" c JOIN "Users" u ON u."UserID" = c."UserID" WHERE u."Username" = 'demo_customer'), (SELECT "VehicleID" FROM "Vehicles" WHERE "LicensePlate" = '51A12345'), (SELECT "BranchID" FROM "Branches" WHERE "BranchName" = 'AutoWash Pro - District 1'), (SELECT wb."WashBayID" FROM "WashBays" wb JOIN "Branches" b ON b."BranchID" = wb."BranchID" WHERE b."BranchName" = 'AutoWash Pro - District 1' AND wb."BayName" = 'Bay 1'), 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '2026-07-09T09:00:00Z', '2026-07-09T10:00:00Z', 4, 2, 200000, 'Completed customer booking imported from SQL seed', '2026-07-09T09:45:00Z', '2026-07-09T08:30:00Z', 0)
ON CONFLICT ("BookingID") DO UPDATE SET
    "BookingStatus" = EXCLUDED."BookingStatus",
    "EstimatedTotalAmount" = EXCLUDED."EstimatedTotalAmount",
    "Notes" = EXCLUDED."Notes",
    "CompletedAt" = EXCLUDED."CompletedAt";

INSERT INTO "BookingDetails" ("BookingDetailID", "BookingID", "ServiceID", "Quantity", "UnitPrice")
VALUES
    ('71000000-0000-0000-0000-000000000001', '70000000-0000-0000-0000-000000000001', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Basic Wash'), 1, 80000),
    ('71000000-0000-0000-0000-000000000002', '70000000-0000-0000-0000-000000000001', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Interior Clean'), 1, 120000)
ON CONFLICT ("BookingID", "ServiceID") DO UPDATE SET
    "Quantity" = EXCLUDED."Quantity",
    "UnitPrice" = EXCLUDED."UnitPrice";

INSERT INTO "WashHistories" ("WashHistoryID", "BookingID", "WashDate", "ActualTotalAmount", "DiscountAmount", "FinalAmount", "PointsEarned", "RewardUsed", "CustomerRating", "Feedback", "CreatedAt")
VALUES
    ('72000000-0000-0000-0000-000000000001', '70000000-0000-0000-0000-000000000001', '2026-07-09T09:45:00Z', 200000, 0, 200000, 85, 0, 5, 'Great service!', '2026-07-09T10:00:00Z')
ON CONFLICT ("BookingID") DO UPDATE SET
    "ActualTotalAmount" = EXCLUDED."ActualTotalAmount",
    "DiscountAmount" = EXCLUDED."DiscountAmount",
    "FinalAmount" = EXCLUDED."FinalAmount",
    "PointsEarned" = EXCLUDED."PointsEarned",
    "RewardUsed" = EXCLUDED."RewardUsed",
    "CustomerRating" = EXCLUDED."CustomerRating",
    "Feedback" = EXCLUDED."Feedback";

INSERT INTO "Payments" ("PaymentID", "BookingID", "Amount", "PaymentMethod", "PaymentStatus", "PaidAt", "RecordedAt", "ReferenceNumber", "Notes")
VALUES
    ('73000000-0000-0000-0000-000000000001', '70000000-0000-0000-0000-000000000001', 200000, 4, 2, '2026-07-09T09:50:00Z', '2026-07-09T09:50:00Z', 'EWALLET-20260709-0001', 'Paid at counter')
ON CONFLICT ("PaymentID") DO UPDATE SET
    "Amount" = EXCLUDED."Amount",
    "PaymentMethod" = EXCLUDED."PaymentMethod",
    "PaymentStatus" = EXCLUDED."PaymentStatus",
    "PaidAt" = EXCLUDED."PaidAt",
    "ReferenceNumber" = EXCLUDED."ReferenceNumber",
    "Notes" = EXCLUDED."Notes";

INSERT INTO "BehavioralLogs" ("LogID", "CustomerID", "BookingID", "ServiceID", "PromotionID", "SessionID", "ActionType", "ActionTime", "PointsChanged", "SpendingAmount", "RewardUsed", "PromotionUsed", "MetadataJson", "Notes")
VALUES
    ('80000000-0000-0000-0000-000000000001', (SELECT c."CustomerID" FROM "Customers" c JOIN "Users" u ON u."UserID" = c."UserID" WHERE u."Username" = 'demo_customer'), NULL, (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Premium Wash'), '50000000-0000-0000-0000-000000000001', 'seed-session-001', 1, '2026-07-08T08:00:00Z', 0, 0, 0, FALSE, '{"source":"sql-seed"}', 'Viewed July Premium Wash Discount'),
    ('80000000-0000-0000-0000-000000000002', (SELECT c."CustomerID" FROM "Customers" c JOIN "Users" u ON u."UserID" = c."UserID" WHERE u."Username" = 'demo_customer'), '70000000-0000-0000-0000-000000000001', (SELECT "ServiceID" FROM "Services" WHERE "ServiceName" = 'Basic Wash'), NULL, 'seed-session-001', 2, '2026-07-09T08:30:00Z', 10, 200000, 0, FALSE, '{"source":"sql-seed"}', 'Booked Basic Wash and Interior Clean'),
    ('80000000-0000-0000-0000-000000000003', (SELECT c."CustomerID" FROM "Customers" c JOIN "Users" u ON u."UserID" = c."UserID" WHERE u."Username" = 'demo_vip'), NULL, NULL, NULL, 'seed-session-002', 4, '2026-07-10T10:00:00Z', 5, 0, 0, FALSE, '{"source":"sql-seed"}', 'Left 5-star feedback')
ON CONFLICT ("LogID") DO UPDATE SET
    "CustomerID" = EXCLUDED."CustomerID",
    "BookingID" = EXCLUDED."BookingID",
    "ServiceID" = EXCLUDED."ServiceID",
    "PromotionID" = EXCLUDED."PromotionID",
    "ActionType" = EXCLUDED."ActionType",
    "PointsChanged" = EXCLUDED."PointsChanged",
    "SpendingAmount" = EXCLUDED."SpendingAmount",
    "RewardUsed" = EXCLUDED."RewardUsed",
    "PromotionUsed" = EXCLUDED."PromotionUsed",
    "MetadataJson" = EXCLUDED."MetadataJson",
    "Notes" = EXCLUDED."Notes";

COMMIT;

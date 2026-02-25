using HallApp.Core.Entities;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.ReviewEntities;
using HallApp.Core.Entities.VendorEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HallApp.Infrastructure.Data.Seed;

public class SeedAll
{
    public static async Task SeedAllData(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        DataContext context,
        IHostEnvironment env,
        ILogger logger)
    {
        // ---------------------------------------------------------------
        // PRODUCTION GUARD: Never seed test data outside Development
        // ---------------------------------------------------------------
        if (!env.IsDevelopment())
        {
            logger?.LogInformation("Skipping seed data - not in Development environment (current: {Environment})", env.EnvironmentName);
            return;
        }

        // First seed users, roles, organizations, halls, and hall manager entities
        await Seed.SeedInformation(userManager, roleManager, context, env, logger);

        // Seed vendors (types, details, locations, hours, service items, org linkage, vendor managers)
        Console.WriteLine("[SeedAll] Starting vendor data seeding...");
        await SeedVendors.SeedVendorData(context, userManager);
        await context.SaveChangesAsync();
        Console.WriteLine("[SeedAll] Vendor data seeding completed.");

        // Seed dependent entities in correct order
        await SeedCustomers(context);
        await SeedAddresses(context);
        await SeedBookings(context);
        await SeedVendorBookings(context);
        await SeedVendorBookingServices(context);
        await SeedInvoices(context);
        await SeedReviews(context);

        Console.WriteLine("[SeedAll] Successfully seeded all comprehensive test data!");
    }

    // ===================================================================
    // CUSTOMERS
    // ===================================================================

    private static async Task SeedCustomers(DataContext context)
    {
        if (await context.Customers.AnyAsync())
            return;

        try
        {
            var customerUsernames = new[]
            {
                "mohammed.rashid", "fatima.ahmed", "ali.hassan", "customer.demo",
                "layla.omar", "omar.khalid", "noor.sultan", "yousef.tamimi",
                "amira.fahad", "sultan.harbi", "hana.qahtani", "bandar.otaibi",
                "maha.anazi", "tariq.shammari", "reem.dosari", "faisal.mutairi"
            };

            var customerUsers = await context.Users
                .Where(u => customerUsernames.Contains(u.UserName))
                .OrderBy(u => u.Id)
                .ToListAsync();

            if (!customerUsers.Any())
            {
                Console.WriteLine("[SeedAll] No customer users found, skipping customer entity creation");
                return;
            }

            var random = new Random(42); // Fixed seed for reproducibility
            foreach (var user in customerUsers)
            {
                context.Customers.Add(new Customer
                {
                    AppUserId = user.Id,
                    NumberOfOrders = random.Next(0, 8),
                    SelectedAddressId = 0,
                    CreditMoney = random.Next(0, 500),
                    Confirmed = true,
                    Active = true,
                    Created = DateTime.UtcNow.AddDays(-random.Next(30, 365)),
                    Updated = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedAll] Created {customerUsers.Count} customer entities");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedAll] Error seeding customers: {ex.Message}");
        }
    }

    // ===================================================================
    // ADDRESSES
    // ===================================================================

    private static async Task SeedAddresses(DataContext context)
    {
        if (await context.Addresses.AnyAsync())
            return;

        try
        {
            var customers = await context.Customers.OrderBy(c => c.Id).ToListAsync();
            if (!customers.Any()) return;

            var addressData = new[]
            {
                ("Al Hamra District, King Abdullah Road", "Jeddah", "Makkah Region", "21452"),
                ("Al Olaya District, King Fahd Road", "Riyadh", "Riyadh Region", "12211"),
                ("Al Aziziyah District, Ibrahim Al Khalil Road", "Makkah", "Makkah Region", "24232"),
                ("Al Khalidiyah District, Prince Abdulmajeed Road", "Madinah", "Madinah Region", "42312"),
                ("Corniche District, King Saud Street", "Dammam", "Eastern Region", "31411"),
                ("Al Rawdah District, Tahlia Street", "Jeddah", "Makkah Region", "21472"),
                ("Al Worood District, Olaya Street", "Riyadh", "Riyadh Region", "12251"),
                ("Al Malaz District, King Abdullah Road", "Riyadh", "Riyadh Region", "12641"),
                ("Al Nakheel District, Northern Ring Road", "Riyadh", "Riyadh Region", "12382"),
                ("Al Salamah District, Prince Sultan Street", "Jeddah", "Makkah Region", "21442"),
                ("Al Sulimaniyah District, Al-Dabab Street", "Riyadh", "Riyadh Region", "12234"),
                ("Al Narjis District, Prince Mohammed Road", "Riyadh", "Riyadh Region", "13326"),
                ("Al Faisaliyah District, King Fahd Road", "Riyadh", "Riyadh Region", "12212"),
                ("Al Shati District, Prince Sultan Road", "Jeddah", "Makkah Region", "21453"),
                ("Al Andalus District, Heraa Street", "Jeddah", "Makkah Region", "21461"),
                ("Al Rabwah District, Makkah Road", "Riyadh", "Riyadh Region", "12816")
            };

            var addresses = new List<Address>();
            for (int i = 0; i < customers.Count; i++)
            {
                var (street, city, state, zip) = addressData[i % addressData.Length];
                addresses.Add(new Address
                {
                    Street = street,
                    City = city,
                    State = state,
                    ZipCode = zip,
                    IsMain = true,
                    CustomerId = customers[i].Id
                });

                // Some customers have a second address
                if (i % 3 == 0 && i + 1 < addressData.Length)
                {
                    var alt = addressData[(i + 5) % addressData.Length];
                    addresses.Add(new Address
                    {
                        Street = alt.Item1,
                        City = alt.Item2,
                        State = alt.Item3,
                        ZipCode = alt.Item4,
                        IsMain = false,
                        CustomerId = customers[i].Id
                    });
                }
            }

            await context.Addresses.AddRangeAsync(addresses);
            await context.SaveChangesAsync();

            // Update SelectedAddressId on each customer to point to their main address
            var allAddresses = await context.Addresses.ToListAsync();
            foreach (var customer in customers)
            {
                var mainAddr = allAddresses.FirstOrDefault(a => a.CustomerId == customer.Id && a.IsMain);
                if (mainAddr != null)
                {
                    customer.SelectedAddressId = mainAddr.Id;
                }
            }
            await context.SaveChangesAsync();

            Console.WriteLine($"[SeedAll] Created {addresses.Count} addresses for {customers.Count} customers");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedAll] Error seeding addresses: {ex.Message}");
        }
    }

    // ===================================================================
    // BOOKINGS (comprehensive: 25+ bookings across all statuses)
    // ===================================================================

    private static async Task SeedBookings(DataContext context)
    {
        if (await context.Bookings.AnyAsync())
            return;

        try
        {
            var customers = await context.Customers.OrderBy(c => c.Id).ToListAsync();
            var halls = await context.Halls.OrderBy(h => h.ID).ToListAsync();

            if (!customers.Any() || !halls.Any())
            {
                Console.WriteLine("[SeedAll] No customers or halls found, skipping booking creation");
                return;
            }

            var now = DateTime.UtcNow;
            var bookings = new List<Booking>();

            // Helper to pick customer/hall by index wrapping
            Customer cust(int i) => customers[i % customers.Count];
            int hallId(int i) => halls[i % halls.Count].ID;

            // ---- COMPLETED bookings (past events, paid) ----
            bookings.Add(new Booking
            {
                HallId = hallId(0), CustomerId = cust(0).Id,
                PaymentMethod = "Credit Card", Coupon = "WELCOME10",
                Status = "Completed", Comments = "Beautiful wedding celebration. Everything was perfect.",
                VisitDate = now.AddDays(-90), IsVisitCompleted = true, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-120),
                EventDate = now.AddDays(-90), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 350, GenderPreference = 2,
                HallCost = 15000m, VendorServicesCost = 8000m, Subtotal = 23000m,
                DiscountAmount = 2300m, TaxRate = 0.15m, TaxAmount = 3105m, TotalAmount = 23805m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-120),
                CreatedAt = now.AddDays(-120), UpdatedAt = now.AddDays(-90)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(1), CustomerId = cust(1).Id,
                PaymentMethod = "Bank Transfer", Coupon = "",
                Status = "Completed", Comments = "Corporate gala dinner went smoothly. Great venue.",
                VisitDate = now.AddDays(-60), IsVisitCompleted = true, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-90),
                EventDate = now.AddDays(-60), StartTime = TimeSpan.FromHours(19), EndTime = TimeSpan.FromHours(23),
                EventType = "Corporate", GuestCount = 200, GenderPreference = 0,
                HallCost = 18000m, VendorServicesCost = 5000m, Subtotal = 23000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 3450m, TotalAmount = 26450m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-90),
                CreatedAt = now.AddDays(-90), UpdatedAt = now.AddDays(-60)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(3), CustomerId = cust(2).Id,
                PaymentMethod = "Credit Card", Coupon = "VIP2025",
                Status = "Completed", Comments = "Engagement party was lovely. Ladies section was beautifully decorated.",
                VisitDate = now.AddDays(-45), IsVisitCompleted = true, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-75),
                EventDate = now.AddDays(-45), StartTime = TimeSpan.FromHours(17), EndTime = TimeSpan.FromHours(22),
                EventType = "Engagement", GuestCount = 180, GenderPreference = 1,
                HallCost = 7500m, VendorServicesCost = 4500m, Subtotal = 12000m,
                DiscountAmount = 600m, TaxRate = 0.15m, TaxAmount = 1710m, TotalAmount = 13110m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-75),
                CreatedAt = now.AddDays(-75), UpdatedAt = now.AddDays(-45)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(5), CustomerId = cust(3).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Completed", Comments = "Anniversary celebration at the elite palace. Exceeded expectations.",
                VisitDate = now.AddDays(-30), IsVisitCompleted = true, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-60),
                EventDate = now.AddDays(-30), StartTime = TimeSpan.FromHours(20), EndTime = TimeSpan.FromHours(1),
                EventType = "Anniversary", GuestCount = 100, GenderPreference = 2,
                HallCost = 22000m, VendorServicesCost = 10000m, Subtotal = 32000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 4800m, TotalAmount = 36800m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-60),
                CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-30)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(7), CustomerId = cust(4).Id,
                PaymentMethod = "Bank Transfer", Coupon = "",
                Status = "Completed", Comments = "Baby shower was a delight. Garden setting was perfect.",
                VisitDate = now.AddDays(-20), IsVisitCompleted = true, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-50),
                EventDate = now.AddDays(-20), StartTime = TimeSpan.FromHours(15), EndTime = TimeSpan.FromHours(20),
                EventType = "Baby Shower", GuestCount = 80, GenderPreference = 1,
                HallCost = 6500m, VendorServicesCost = 3000m, Subtotal = 9500m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 1425m, TotalAmount = 10925m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-50),
                CreatedAt = now.AddDays(-50), UpdatedAt = now.AddDays(-20)
            });

            // ---- CONFIRMED bookings (upcoming events, paid) ----
            bookings.Add(new Booking
            {
                HallId = hallId(0), CustomerId = cust(5).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Confirmed", Comments = "Wedding celebration with 400 guests. Need extra seating.",
                VisitDate = now.AddDays(30), IsVisitCompleted = false, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-14),
                EventDate = now.AddDays(30), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 400, GenderPreference = 2,
                HallCost = 20000m, VendorServicesCost = 12000m, Subtotal = 32000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 4800m, TotalAmount = 36800m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-14),
                CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-7)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(2), CustomerId = cust(6).Id,
                PaymentMethod = "Bank Transfer", Coupon = "EARLYBIRD",
                Status = "Confirmed", Comments = "Traditional men-only wedding. Mandi feast required.",
                VisitDate = now.AddDays(45), IsVisitCompleted = false, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-21),
                EventDate = now.AddDays(45), StartTime = TimeSpan.FromHours(19), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 250, GenderPreference = 0,
                HallCost = 4500m, VendorServicesCost = 6000m, Subtotal = 10500m,
                DiscountAmount = 1050m, TaxRate = 0.15m, TaxAmount = 1417.50m, TotalAmount = 10867.50m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-20),
                CreatedAt = now.AddDays(-21), UpdatedAt = now.AddDays(-10)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(8), CustomerId = cust(7).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Confirmed", Comments = "Skyline ballroom booking for a graduation party.",
                VisitDate = now.AddDays(60), IsVisitCompleted = false, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-10),
                EventDate = now.AddDays(60), StartTime = TimeSpan.FromHours(17), EndTime = TimeSpan.FromHours(22),
                EventType = "Graduation", GuestCount = 150, GenderPreference = 2,
                HallCost = 13000m, VendorServicesCost = 5000m, Subtotal = 18000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 2700m, TotalAmount = 20700m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-9),
                CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-5)
            });

            // ---- READY FOR PAYMENT bookings ----
            bookings.Add(new Booking
            {
                HallId = hallId(4), CustomerId = cust(8).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "ReadyForPayment", Comments = "All vendor approvals received. Ready to pay.",
                VisitDate = now.AddDays(50), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-7),
                EventDate = now.AddDays(50), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 220, GenderPreference = 2,
                HallCost = 6000m, VendorServicesCost = 4500m, Subtotal = 10500m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 1575m, TotalAmount = 12075m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-1)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(6), CustomerId = cust(9).Id,
                PaymentMethod = "Bank Transfer", Coupon = "SPRING2026",
                Status = "ReadyForPayment", Comments = "Engagement approved by hall and vendors. Awaiting payment.",
                VisitDate = now.AddDays(40), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-5),
                EventDate = now.AddDays(40), StartTime = TimeSpan.FromHours(17), EndTime = TimeSpan.FromHours(22),
                EventType = "Engagement", GuestCount = 150, GenderPreference = 1,
                HallCost = 6000m, VendorServicesCost = 3500m, Subtotal = 9500m,
                DiscountAmount = 950m, TaxRate = 0.15m, TaxAmount = 1282.50m, TotalAmount = 9832.50m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-1)
            });

            // ---- VENDORS APPROVING bookings ----
            bookings.Add(new Booking
            {
                HallId = hallId(1), CustomerId = cust(10).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "VendorsApproving", Comments = "Hall approved. Waiting for catering and photography vendors.",
                VisitDate = now.AddDays(55), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-4),
                EventDate = now.AddDays(55), StartTime = TimeSpan.FromHours(19), EndTime = TimeSpan.FromHours(1),
                EventType = "Wedding", GuestCount = 300, GenderPreference = 2,
                HallCost = 25000m, VendorServicesCost = 15000m, Subtotal = 40000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 6000m, TotalAmount = 46000m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now.AddDays(-4), UpdatedAt = now.AddDays(-2)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(5), CustomerId = cust(11).Id,
                PaymentMethod = "Credit Card", Coupon = "VIP2026",
                Status = "VendorsApproving", Comments = "Premium venue. Waiting for decoration and entertainment vendors.",
                VisitDate = now.AddDays(70), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-3),
                EventDate = now.AddDays(70), StartTime = TimeSpan.FromHours(20), EndTime = TimeSpan.FromHours(2),
                EventType = "Wedding", GuestCount = 500, GenderPreference = 2,
                HallCost = 30000m, VendorServicesCost = 20000m, Subtotal = 50000m,
                DiscountAmount = 5000m, TaxRate = 0.15m, TaxAmount = 6750m, TotalAmount = 51750m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-1)
            });

            // ---- HALL APPROVED bookings ----
            bookings.Add(new Booking
            {
                HallId = hallId(3), CustomerId = cust(12).Id,
                PaymentMethod = "Bank Transfer", Coupon = "",
                Status = "HallApproved", Comments = "Hall manager approved. Selecting vendors next.",
                VisitDate = now.AddDays(65), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-3),
                EventDate = now.AddDays(65), StartTime = TimeSpan.FromHours(17), EndTime = TimeSpan.FromHours(22),
                EventType = "Engagement", GuestCount = 120, GenderPreference = 1,
                HallCost = 7500m, VendorServicesCost = 3000m, Subtotal = 10500m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 1575m, TotalAmount = 12075m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-2)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(9), CustomerId = cust(0).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "HallApproved", Comments = "Pearl Palace approved. Beautiful seaside venue.",
                VisitDate = now.AddDays(75), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-2),
                EventDate = now.AddDays(75), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 280, GenderPreference = 2,
                HallCost = 22000m, VendorServicesCost = 8000m, Subtotal = 30000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 4500m, TotalAmount = 34500m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-1)
            });

            // ---- PENDING bookings (awaiting hall approval) ----
            bookings.Add(new Booking
            {
                HallId = hallId(0), CustomerId = cust(13).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Pending", Comments = "Requesting Qasr Al Ahlam for a large wedding. Awaiting approval.",
                VisitDate = now.AddDays(80), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-1),
                EventDate = now.AddDays(80), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 600, GenderPreference = 2,
                HallCost = 20000m, VendorServicesCost = 0m, Subtotal = 20000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 3000m, TotalAmount = 23000m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(6), CustomerId = cust(14).Id,
                PaymentMethod = "Bank Transfer", Coupon = "",
                Status = "Pending", Comments = "Corporate conference at Rawdat Al Zahra. Need projector and mic.",
                VisitDate = now.AddDays(90), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now,
                EventDate = now.AddDays(90), StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(17),
                EventType = "Corporate", GuestCount = 100, GenderPreference = 1,
                HallCost = 4000m, VendorServicesCost = 0m, Subtotal = 4000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 600m, TotalAmount = 4600m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now, UpdatedAt = now
            });

            bookings.Add(new Booking
            {
                HallId = hallId(4), CustomerId = cust(15).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Pending", Comments = "Birthday party for 80 guests at Al Firdaws.",
                VisitDate = now.AddDays(35), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now,
                EventDate = now.AddDays(35), StartTime = TimeSpan.FromHours(16), EndTime = TimeSpan.FromHours(21),
                EventType = "Birthday", GuestCount = 80, GenderPreference = 2,
                HallCost = 3500m, VendorServicesCost = 0m, Subtotal = 3500m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 525m, TotalAmount = 4025m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now, UpdatedAt = now
            });

            bookings.Add(new Booking
            {
                HallId = hallId(8), CustomerId = cust(4).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Pending", Comments = "Retirement celebration. Panoramic views requested.",
                VisitDate = now.AddDays(100), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now,
                EventDate = now.AddDays(100), StartTime = TimeSpan.FromHours(19), EndTime = TimeSpan.FromHours(23),
                EventType = "Retirement", GuestCount = 120, GenderPreference = 0,
                HallCost = 10000m, VendorServicesCost = 0m, Subtotal = 10000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 1500m, TotalAmount = 11500m,
                Currency = "SAR", PaymentStatus = "Pending",
                CreatedAt = now, UpdatedAt = now
            });

            // ---- CANCELLED bookings ----
            bookings.Add(new Booking
            {
                HallId = hallId(1), CustomerId = cust(3).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Cancelled", Comments = "Customer cancelled due to schedule conflict.",
                VisitDate = now.AddDays(15), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-30),
                EventDate = now.AddDays(15), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 250, GenderPreference = 2,
                HallCost = 18000m, VendorServicesCost = 7000m, Subtotal = 25000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 3750m, TotalAmount = 28750m,
                Currency = "SAR", PaymentStatus = "Refunded", PaidAt = now.AddDays(-30),
                CreatedAt = now.AddDays(-30), UpdatedAt = now.AddDays(-15)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(7), CustomerId = cust(6).Id,
                PaymentMethod = "Bank Transfer", Coupon = "",
                Status = "Cancelled", Comments = "Venue no longer available for the requested date.",
                VisitDate = now.AddDays(25), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-20),
                EventDate = now.AddDays(25), StartTime = TimeSpan.FromHours(17), EndTime = TimeSpan.FromHours(22),
                EventType = "Engagement", GuestCount = 100, GenderPreference = 2,
                HallCost = 9000m, VendorServicesCost = 3000m, Subtotal = 12000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 1800m, TotalAmount = 13800m,
                Currency = "SAR", PaymentStatus = "Refunded", PaidAt = now.AddDays(-20),
                CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-10)
            });

            // ---- PAID bookings (payment completed, event upcoming) ----
            bookings.Add(new Booking
            {
                HallId = hallId(10 % halls.Count), CustomerId = cust(1).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Paid", Comments = "Payment received. Looking forward to the event at the Jewel.",
                VisitDate = now.AddDays(20), IsVisitCompleted = false, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-25),
                EventDate = now.AddDays(20), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 350, GenderPreference = 2,
                HallCost = 28000m, VendorServicesCost = 15000m, Subtotal = 43000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 6450m, TotalAmount = 49450m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-24),
                CreatedAt = now.AddDays(-25), UpdatedAt = now.AddDays(-5)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(11 % halls.Count), CustomerId = cust(5).Id,
                PaymentMethod = "Bank Transfer", Coupon = "",
                Status = "Paid", Comments = "Garden party paid in full. Outdoor setup confirmed.",
                VisitDate = now.AddDays(10), IsVisitCompleted = false, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-15),
                EventDate = now.AddDays(10), StartTime = TimeSpan.FromHours(16), EndTime = TimeSpan.FromHours(21),
                EventType = "Birthday", GuestCount = 60, GenderPreference = 2,
                HallCost = 5000m, VendorServicesCost = 2000m, Subtotal = 7000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 1050m, TotalAmount = 8050m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-14),
                CreatedAt = now.AddDays(-15), UpdatedAt = now.AddDays(-3)
            });

            // ---- Additional bookings for variety ----
            bookings.Add(new Booking
            {
                HallId = hallId(12 % halls.Count), CustomerId = cust(8).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Confirmed", Comments = "Beach-view wedding at Al Sharq Palace. Excited!",
                VisitDate = now.AddDays(45), IsVisitCompleted = false, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-10),
                EventDate = now.AddDays(45), StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 400, GenderPreference = 2,
                HallCost = 24000m, VendorServicesCost = 12000m, Subtotal = 36000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 5400m, TotalAmount = 41400m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-9),
                CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-5)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(2), CustomerId = cust(9).Id,
                PaymentMethod = "Cash", Coupon = "",
                Status = "Completed", Comments = "Aqeeqah ceremony. Traditional and warm atmosphere.",
                VisitDate = now.AddDays(-15), IsVisitCompleted = true, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-45),
                EventDate = now.AddDays(-15), StartTime = TimeSpan.FromHours(19), EndTime = TimeSpan.FromHours(22),
                EventType = "Aqeeqah", GuestCount = 100, GenderPreference = 0,
                HallCost = 3000m, VendorServicesCost = 2500m, Subtotal = 5500m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 825m, TotalAmount = 6325m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-45),
                CreatedAt = now.AddDays(-45), UpdatedAt = now.AddDays(-15)
            });

            bookings.Add(new Booking
            {
                HallId = hallId(3), CustomerId = cust(10).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "Confirmed", Comments = "Bridal shower at Layali Al Ward. All-female event.",
                VisitDate = now.AddDays(25), IsVisitCompleted = false, IsBookingConfirmed = true,
                BookingDate = now.AddDays(-7),
                EventDate = now.AddDays(25), StartTime = TimeSpan.FromHours(15), EndTime = TimeSpan.FromHours(21),
                EventType = "Bridal Shower", GuestCount = 80, GenderPreference = 1,
                HallCost = 5000m, VendorServicesCost = 3500m, Subtotal = 8500m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 1275m, TotalAmount = 9775m,
                Currency = "SAR", PaymentStatus = "Completed", PaidAt = now.AddDays(-6),
                CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-3)
            });

            // ---- Payment Failed booking ----
            bookings.Add(new Booking
            {
                HallId = hallId(1), CustomerId = cust(12).Id,
                PaymentMethod = "Credit Card", Coupon = "",
                Status = "ReadyForPayment", Comments = "Payment attempt failed. Card declined. Please retry.",
                VisitDate = now.AddDays(55), IsVisitCompleted = false, IsBookingConfirmed = false,
                BookingDate = now.AddDays(-5),
                EventDate = now.AddDays(55), StartTime = TimeSpan.FromHours(19), EndTime = TimeSpan.FromHours(23),
                EventType = "Wedding", GuestCount = 250, GenderPreference = 2,
                HallCost = 18000m, VendorServicesCost = 8000m, Subtotal = 26000m,
                DiscountAmount = 0m, TaxRate = 0.15m, TaxAmount = 3900m, TotalAmount = 29900m,
                Currency = "SAR", PaymentStatus = "Failed",
                CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-2)
            });

            await context.Bookings.AddRangeAsync(bookings);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedAll] Created {bookings.Count} bookings across all statuses and dates");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedAll] Error seeding bookings: {ex.Message}");
            Console.WriteLine($"[SeedAll] Stack trace: {ex.StackTrace}");
        }
    }

    // ===================================================================
    // VENDOR BOOKINGS
    // ===================================================================

    private static async Task SeedVendorBookings(DataContext context)
    {
        if (await context.VendorBookings.AnyAsync())
            return;

        try
        {
            var bookings = await context.Bookings.OrderBy(b => b.Id).ToListAsync();
            var vendors = await context.Vendors.Include(v => v.VendorType).OrderBy(v => v.Id).ToListAsync();

            if (!bookings.Any() || !vendors.Any())
            {
                Console.WriteLine("[SeedAll] No bookings or vendors found, skipping vendor bookings");
                return;
            }

            // Group vendors by type for realistic assignment
            var cateringVendors = vendors.Where(v => v.VendorType?.Name == "Catering Service").ToList();
            var photoVendors = vendors.Where(v => v.VendorType?.Name == "Photography").ToList();
            var decorVendors = vendors.Where(v => v.VendorType?.Name == "Decoration").ToList();
            var entertainVendors = vendors.Where(v => v.VendorType?.Name == "Entertainment").ToList();
            var transportVendors = vendors.Where(v => v.VendorType?.Name == "Transportation").ToList();

            var vendorBookings = new List<VendorBooking>();
            var random = new Random(42);

            // Only assign vendors to bookings that are not Pending (they need hall approval first)
            var eligibleBookings = bookings
                .Where(b => b.Status != "Pending")
                .ToList();

            foreach (var booking in eligibleBookings)
            {
                var eventDate = booking.EventDate;
                var isPaid = booking.PaymentStatus == "Completed";
                var isCancelled = booking.Status == "Cancelled";

                // Catering vendor for most bookings
                if (cateringVendors.Any())
                {
                    var caterer = cateringVendors[random.Next(cateringVendors.Count)];
                    var amount = booking.GuestCount * (random.Next(70, 160));
                    vendorBookings.Add(new VendorBooking
                    {
                        BookingId = booking.Id,
                        VendorId = caterer.Id,
                        Status = isCancelled ? "Cancelled" : (isPaid ? "Confirmed" : "Pending"),
                        StartTime = eventDate.Date.AddHours(booking.StartTime.Hours - 2),
                        EndTime = eventDate.Date.AddHours(booking.EndTime.Hours),
                        TotalAmount = amount,
                        Notes = $"Catering for {booking.GuestCount} guests - {booking.EventType}",
                        IsPaid = isPaid,
                        PaidAt = isPaid ? booking.PaidAt : null,
                        PaymentMethod = isPaid ? booking.PaymentMethod : "",
                        PaymentStatus = isPaid ? "Completed" : (isCancelled ? "Refunded" : "Pending"),
                        CancellationReason = isCancelled ? "Booking cancelled by customer" : "",
                        CancelledAt = isCancelled ? booking.UpdatedAt : null,
                        ApprovedAt = !isCancelled ? booking.BookingDate.AddDays(1) : null,
                        ServiceDate = eventDate,
                        ServiceType = "Catering",
                        CreatedAt = booking.CreatedAt,
                        UpdatedAt = booking.UpdatedAt,
                        Created = booking.CreatedAt,
                        IsActive = !isCancelled
                    });
                }

                // Photography vendor for wedding/engagement bookings
                if (photoVendors.Any() && (booking.EventType == "Wedding" || booking.EventType == "Engagement" || booking.EventType == "Bridal Shower"))
                {
                    var photographer = photoVendors[random.Next(photoVendors.Count)];
                    var amount = random.Next(6000, 12000);
                    vendorBookings.Add(new VendorBooking
                    {
                        BookingId = booking.Id,
                        VendorId = photographer.Id,
                        Status = isCancelled ? "Cancelled" : (isPaid ? "Confirmed" : "Pending"),
                        StartTime = eventDate.Date.AddHours(booking.StartTime.Hours - 1),
                        EndTime = eventDate.Date.AddHours(booking.EndTime.Hours + 1),
                        TotalAmount = amount,
                        Notes = $"Full event photography coverage - {booking.EventType}",
                        IsPaid = isPaid,
                        PaidAt = isPaid ? booking.PaidAt : null,
                        PaymentMethod = isPaid ? booking.PaymentMethod : "",
                        PaymentStatus = isPaid ? "Completed" : (isCancelled ? "Refunded" : "Pending"),
                        CancellationReason = isCancelled ? "Booking cancelled" : "",
                        CancelledAt = isCancelled ? booking.UpdatedAt : null,
                        ApprovedAt = !isCancelled ? booking.BookingDate.AddDays(1) : null,
                        ServiceDate = eventDate,
                        ServiceType = "Photography",
                        CreatedAt = booking.CreatedAt,
                        UpdatedAt = booking.UpdatedAt,
                        Created = booking.CreatedAt,
                        IsActive = !isCancelled
                    });
                }

                // Decoration vendor for most bookings
                if (decorVendors.Any() && random.Next(100) > 30)
                {
                    var decorator = decorVendors[random.Next(decorVendors.Count)];
                    var amount = random.Next(5000, 15000);
                    vendorBookings.Add(new VendorBooking
                    {
                        BookingId = booking.Id,
                        VendorId = decorator.Id,
                        Status = isCancelled ? "Cancelled" : (isPaid ? "Confirmed" : "Pending"),
                        StartTime = eventDate.Date.AddHours(8), // Setup starts early
                        EndTime = eventDate.Date.AddHours(booking.EndTime.Hours + 2), // Teardown after
                        TotalAmount = amount,
                        Notes = $"Full venue decoration - {booking.EventType} theme",
                        IsPaid = isPaid,
                        PaidAt = isPaid ? booking.PaidAt : null,
                        PaymentMethod = isPaid ? booking.PaymentMethod : "",
                        PaymentStatus = isPaid ? "Completed" : (isCancelled ? "Refunded" : "Pending"),
                        CancellationReason = isCancelled ? "Booking cancelled" : "",
                        CancelledAt = isCancelled ? booking.UpdatedAt : null,
                        ApprovedAt = !isCancelled ? booking.BookingDate.AddDays(2) : null,
                        ServiceDate = eventDate,
                        ServiceType = "Decoration",
                        CreatedAt = booking.CreatedAt,
                        UpdatedAt = booking.UpdatedAt,
                        Created = booking.CreatedAt,
                        IsActive = !isCancelled
                    });
                }

                // Entertainment for large weddings only
                if (entertainVendors.Any() && booking.EventType == "Wedding" && booking.GuestCount >= 200)
                {
                    var entertainer = entertainVendors[random.Next(entertainVendors.Count)];
                    var amount = random.Next(5000, 10000);
                    vendorBookings.Add(new VendorBooking
                    {
                        BookingId = booking.Id,
                        VendorId = entertainer.Id,
                        Status = isCancelled ? "Cancelled" : (isPaid ? "Confirmed" : "Pending"),
                        StartTime = eventDate.Date.AddHours(booking.StartTime.Hours),
                        EndTime = eventDate.Date.AddHours(booking.EndTime.Hours),
                        TotalAmount = amount,
                        Notes = "Live entertainment and DJ services",
                        IsPaid = isPaid,
                        PaidAt = isPaid ? booking.PaidAt : null,
                        PaymentMethod = isPaid ? booking.PaymentMethod : "",
                        PaymentStatus = isPaid ? "Completed" : (isCancelled ? "Refunded" : "Pending"),
                        CancellationReason = isCancelled ? "Booking cancelled" : "",
                        CancelledAt = isCancelled ? booking.UpdatedAt : null,
                        ApprovedAt = !isCancelled ? booking.BookingDate.AddDays(2) : null,
                        ServiceDate = eventDate,
                        ServiceType = "Entertainment",
                        CreatedAt = booking.CreatedAt,
                        UpdatedAt = booking.UpdatedAt,
                        Created = booking.CreatedAt,
                        IsActive = !isCancelled
                    });
                }
            }

            if (vendorBookings.Any())
            {
                await context.VendorBookings.AddRangeAsync(vendorBookings);
                await context.SaveChangesAsync();
                Console.WriteLine($"[SeedAll] Created {vendorBookings.Count} vendor bookings across {eligibleBookings.Count} bookings");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedAll] Error seeding vendor bookings: {ex.Message}");
            Console.WriteLine($"[SeedAll] Stack trace: {ex.StackTrace}");
        }
    }

    // ===================================================================
    // VENDOR BOOKING SERVICES (line items linking VendorBooking -> ServiceItem)
    // ===================================================================

    private static async Task SeedVendorBookingServices(DataContext context)
    {
        if (await context.VendorBookingServices.AnyAsync())
            return;

        try
        {
            var vendorBookings = await context.VendorBookings
                .Include(vb => vb.Vendor)
                .OrderBy(vb => vb.Id)
                .ToListAsync();

            var serviceItems = await context.ServiceItems.ToListAsync();

            if (!vendorBookings.Any() || !serviceItems.Any())
            {
                Console.WriteLine("[SeedAll] No vendor bookings or service items, skipping VendorBookingService");
                return;
            }

            var vbServices = new List<VendorBookingService>();

            foreach (var vb in vendorBookings)
            {
                // Find service items for this vendor
                var vendorItems = serviceItems.Where(si => si.VendorId == vb.VendorId).ToList();
                if (!vendorItems.Any()) continue;

                // Pick 1-2 service items per vendor booking
                var selectedItems = vendorItems.Take(Math.Min(2, vendorItems.Count)).ToList();
                var remainingAmount = vb.TotalAmount;

                for (int i = 0; i < selectedItems.Count; i++)
                {
                    var item = selectedItems[i];
                    var isLast = (i == selectedItems.Count - 1);
                    var qty = i == 0 ? 1 : 1;
                    var unitPrice = isLast ? remainingAmount : Math.Min(item.Price, remainingAmount * 0.6m);
                    var totalPrice = unitPrice * qty;

                    if (totalPrice <= 0) continue;

                    vbServices.Add(new VendorBookingService
                    {
                        VendorBookingId = vb.Id,
                        ServiceItemId = item.Id,
                        Quantity = qty,
                        SpecialInstructions = "",
                        UnitPrice = unitPrice,
                        TotalPrice = totalPrice,
                        CreatedAt = vb.CreatedAt,
                        UpdatedAt = vb.UpdatedAt,
                        Created = vb.Created,
                        IsActive = vb.IsActive
                    });

                    remainingAmount -= totalPrice;
                }
            }

            if (vbServices.Any())
            {
                await context.VendorBookingServices.AddRangeAsync(vbServices);
                await context.SaveChangesAsync();
                Console.WriteLine($"[SeedAll] Created {vbServices.Count} vendor booking service line items");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedAll] Error seeding vendor booking services: {ex.Message}");
        }
    }

    // ===================================================================
    // INVOICES (for Completed and Paid bookings with full ZATCA fields)
    // ===================================================================

    private static async Task SeedInvoices(DataContext context)
    {
        if (await context.Invoices.AnyAsync())
            return;

        try
        {
            // Generate invoices for bookings that are Completed, Confirmed, or Paid with payment completed
            var paidBookings = await context.Bookings
                .Include(b => b.Customer)
                    .ThenInclude(c => c!.AppUser)
                .Include(b => b.Hall)
                    .ThenInclude(h => h!.Organization)
                .Where(b => (b.Status == "Completed" || b.Status == "Confirmed" || b.Status == "Paid")
                            && b.PaymentStatus == "Completed")
                .OrderBy(b => b.Id)
                .ToListAsync();

            if (!paidBookings.Any())
            {
                Console.WriteLine("[SeedAll] No paid bookings found, skipping invoice creation");
                return;
            }

            var invoices = new List<Invoice>();
            var invoiceCounter = 1;
            var year = DateTime.UtcNow.Year;

            foreach (var booking in paidBookings)
            {
                var org = booking.Hall?.Organization;
                var customer = booking.Customer;
                var appUser = customer?.AppUser;

                if (customer == null || appUser == null || booking.Hall == null) continue;

                var sellerName = org?.LegalName ?? org?.Name ?? booking.Hall.Name;
                var sellerVat = org?.VatNumber ?? booking.Hall.Vat.ToString();
                var sellerCr = org?.CommercialRegistrationNumber ?? booking.Hall.CommercialRegistration.ToString();
                var sellerAddr = org != null
                    ? $"{org.BuildingNumber} {org.StreetName}, {org.District}"
                    : booking.Hall.Location?.Address ?? "";
                var sellerCity = org?.City ?? booking.Hall.Location?.City ?? "";

                var invoiceNumber = $"INV-{year}-{invoiceCounter:D6}";
                var zatcaUuid = Guid.NewGuid().ToString();

                var subtotalBeforeTax = booking.Subtotal - booking.DiscountAmount;
                var taxAmount = subtotalBeforeTax * 0.15m;
                var totalWithTax = subtotalBeforeTax + taxAmount;

                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    InvoiceType = booking.TotalAmount > 375000m ? "Standard" : "Simplified",
                    InvoiceDate = booking.PaidAt ?? booking.BookingDate,
                    SupplyDate = booking.EventDate,
                    BookingId = booking.Id,
                    CustomerId = customer.Id,
                    HallId = booking.HallId,

                    // Seller
                    SellerName = sellerName,
                    SellerVatNumber = sellerVat,
                    SellerCommercialRegistrationNumber = sellerCr,
                    SellerAddress = sellerAddr,
                    SellerBuildingNumber = org?.BuildingNumber ?? "",
                    SellerStreetName = org?.StreetName ?? "",
                    SellerDistrict = org?.District ?? "",
                    SellerCity = sellerCity,
                    SellerPostalCode = org?.PostalCode ?? "",
                    SellerCountryCode = "SA",

                    // Buyer
                    BuyerName = $"{appUser.FirstName} {appUser.LastName}",
                    BuyerVatNumber = "",
                    BuyerAddress = "",
                    BuyerCity = sellerCity,
                    BuyerPostalCode = "",
                    BuyerCountryCode = "SA",

                    // Financials
                    SubtotalBeforeTax = subtotalBeforeTax,
                    TaxableAmount = subtotalBeforeTax,
                    TaxRate = 15.00m,
                    TaxAmount = taxAmount,
                    DiscountAmount = booking.DiscountAmount,
                    TotalAmountWithTax = totalWithTax,
                    Currency = "SAR",

                    // Payment
                    PaymentMethod = booking.PaymentMethod,
                    PaymentStatus = "Paid",
                    PaymentDate = booking.PaidAt,
                    PaymentReference = $"PAY-{year}-{invoiceCounter:D6}",

                    // Notes
                    Notes = $"Invoice for {booking.EventType} event on {booking.EventDate:yyyy-MM-dd} at {booking.Hall.Name}.",
                    Terms = "Payment due within 30 days of invoice date. All amounts in SAR.",

                    // ZATCA
                    ZATCA_UUID = zatcaUuid,
                    ZATCA_Status = booking.Status == "Completed" ? "Submitted" : "NotSubmitted",
                    ZATCA_SubmittedAt = booking.Status == "Completed" ? booking.PaidAt?.AddDays(1) : null,
                    ZATCA_Response = booking.Status == "Completed" ? "Accepted" : "",
                    InvoiceHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{invoiceNumber}:{sellerVat}:{totalWithTax}")),
                    QRCode = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                        $"1|{sellerName}|2|{sellerVat}|3|{(booking.PaidAt ?? DateTime.UtcNow):yyyy-MM-ddTHH:mm:ssZ}|4|{totalWithTax:F2}|5|{taxAmount:F2}")),

                    // Audit
                    CreatedAt = booking.PaidAt ?? DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "System",
                    IsCancelled = false,

                    // Line items added below
                    LineItems = new List<InvoiceLineItem>()
                };

                // --- Line Items ---
                var lineNumber = 1;

                // Hall rental line
                invoice.LineItems.Add(new InvoiceLineItem
                {
                    LineNumber = lineNumber++,
                    Description = $"Hall Rental - {booking.Hall.Name} ({booking.EventType})",
                    ItemCode = $"HALL-{booking.HallId:D4}",
                    Quantity = 1,
                    Unit = "Service",
                    UnitPrice = booking.HallCost,
                    DiscountAmount = booking.DiscountAmount,
                    SubtotalBeforeTax = booking.HallCost - booking.DiscountAmount,
                    TaxRate = 15.00m,
                    TaxAmount = (booking.HallCost - booking.DiscountAmount) * 0.15m,
                    TotalAmount = (booking.HallCost - booking.DiscountAmount) * 1.15m,
                    TaxCategory = "Standard"
                });

                // Vendor services line (aggregated)
                if (booking.VendorServicesCost > 0)
                {
                    invoice.LineItems.Add(new InvoiceLineItem
                    {
                        LineNumber = lineNumber++,
                        Description = "Vendor Services (Catering, Photography, Decoration, etc.)",
                        ItemCode = "VENDOR-SERVICES",
                        Quantity = 1,
                        Unit = "Service",
                        UnitPrice = booking.VendorServicesCost,
                        DiscountAmount = 0,
                        SubtotalBeforeTax = booking.VendorServicesCost,
                        TaxRate = 15.00m,
                        TaxAmount = booking.VendorServicesCost * 0.15m,
                        TotalAmount = booking.VendorServicesCost * 1.15m,
                        TaxCategory = "Standard"
                    });
                }

                invoices.Add(invoice);
                invoiceCounter++;
            }

            if (invoices.Any())
            {
                await context.Invoices.AddRangeAsync(invoices);
                await context.SaveChangesAsync();
                Console.WriteLine($"[SeedAll] Created {invoices.Count} invoices with {invoices.Sum(i => i.LineItems.Count)} line items");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedAll] Error seeding invoices: {ex.Message}");
            Console.WriteLine($"[SeedAll] Stack trace: {ex.StackTrace}");
        }
    }

    // ===================================================================
    // REVIEWS (realistic reviews for completed bookings)
    // ===================================================================

    private static async Task SeedReviews(DataContext context)
    {
        if (await context.Reviews.AnyAsync())
            return;

        try
        {
            var completedBookings = await context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.VendorBookings)
                .Where(b => b.Status == "Completed")
                .OrderBy(b => b.Id)
                .ToListAsync();

            if (!completedBookings.Any())
            {
                Console.WriteLine("[SeedAll] No completed bookings, skipping reviews");
                return;
            }

            var reviews = new List<Review>();
            var now = DateTime.UtcNow;

            // Hall reviews data
            var hallReviewData = new[]
            {
                (5, "Absolutely stunning venue! The crystal chandeliers and marble columns took our breath away. The staff went above and beyond to make our wedding day perfect. The separate sections for men and women were equally beautiful.", "Thank you for your wonderful feedback! We are honored to have been part of your special day."),
                (4, "Great venue with beautiful decor. The ballroom was spacious and well-maintained. Only minor issue was the parking situation which got a bit crowded. Overall a wonderful experience.", "Thank you for your feedback. We are working on improving our parking facilities."),
                (5, "The best wedding hall in the city! The team was incredibly professional from start to finish. The sound system was excellent and the lighting created a magical atmosphere.", "We appreciate your kind words! Our team works hard to deliver the best experience."),
                (4, "Very nice hall with good capacity. The location was easy to find and the staff was helpful. The decoration options in the packages are good value for money.", null),
                (3, "Decent venue but the air conditioning struggled during our summer event. The food area could use some updating. Staff was friendly though.", "We apologize for the AC issue. We have since upgraded our HVAC system for better climate control."),
                (5, "Perfect venue for our engagement party! The ladies section was absolutely gorgeous with the flower walls and soft lighting. Highly recommend!", null),
                (4, "Good experience overall. The hall looked exactly like the photos. Catering coordination could be improved but everything else was great.", "Thank you! We have streamlined our catering coordination process based on feedback like yours."),
                (5, "We hosted our corporate gala here and it was phenomenal. The panoramic city views from the ballroom were spectacular. Will definitely return.", null)
            };

            // Vendor reviews data
            var vendorReviewData = new[]
            {
                (5, "Royal Catering exceeded all expectations! The Mandi was perfectly spiced and the live cooking stations were a hit with all our guests. Professional service throughout."),
                (4, "Capture Moments did an amazing job with our wedding photos. The drone shots of the venue were breathtaking. Only wish we got the album a bit sooner."),
                (5, "Elegant Decor transformed our hall into a fairy tale setting. The flower walls and crystal centerpieces were stunning. Worth every riyal."),
                (4, "Melody Entertainment kept the energy high all night. The DJ was excellent and the traditional Samer dance troupe was a wonderful surprise for our guests."),
                (5, "Al Safwa Transportation provided a beautiful Rolls-Royce for our wedding. The chauffeur was professional and the car decoration was perfect."),
                (4, "Noor Floral Design created beautiful arrangements. The bridal bouquet was exactly what I envisioned. Fresh flowers throughout the venue."),
                (5, "Arabian Nights Catering served the most authentic Kabsa I have ever tasted at a wedding. Our guests could not stop talking about the food!"),
                (3, "Gourmet Delight provided good food but the dessert bar ran out a bit early for our 300-guest event. Communication could be improved.")
            };

            var reviewIndex = 0;

            foreach (var booking in completedBookings)
            {
                if (booking.Customer == null) continue;

                // Hall review
                var (hallRating, hallContent, hallResponse) = hallReviewData[reviewIndex % hallReviewData.Length];
                reviews.Add(new Review
                {
                    Rating = hallRating,
                    Content = hallContent,
                    Created = booking.EventDate.AddDays(2), // Review 2 days after event
                    Updated = booking.EventDate.AddDays(2),
                    IsApproved = true,
                    IsFlagged = false,
                    ManagerResponse = hallResponse,
                    ManagerResponseDate = hallResponse != null ? booking.EventDate.AddDays(5) : null,
                    CustomerId = booking.Customer.Id,
                    HallId = booking.HallId,
                    VendorId = null
                });

                // Vendor reviews for completed bookings with vendor bookings
                if (booking.VendorBookings != null)
                {
                    foreach (var vb in booking.VendorBookings.Take(2)) // Max 2 vendor reviews per booking
                    {
                        var (vendorRating, vendorContent) = vendorReviewData[reviewIndex % vendorReviewData.Length];
                        reviews.Add(new Review
                        {
                            Rating = vendorRating,
                            Content = vendorContent,
                            Created = booking.EventDate.AddDays(3),
                            Updated = booking.EventDate.AddDays(3),
                            IsApproved = true,
                            IsFlagged = false,
                            CustomerId = booking.Customer.Id,
                            HallId = booking.HallId,
                            VendorId = vb.VendorId
                        });
                    }
                }

                reviewIndex++;
            }

            if (reviews.Any())
            {
                await context.Reviews.AddRangeAsync(reviews);
                await context.SaveChangesAsync();
                Console.WriteLine($"[SeedAll] Created {reviews.Count} reviews (halls and vendors)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedAll] Error seeding reviews: {ex.Message}");
        }
    }
}

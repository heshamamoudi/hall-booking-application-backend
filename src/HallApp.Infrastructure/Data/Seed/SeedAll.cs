using HallApp.Core.Entities;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.VendorEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HallApp.Infrastructure.Data.Seed;

public class ServiceItemSeed
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int VendorId { get; set; }
    public int VendorTypeId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int SortOrder { get; set; }
}

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

        // First seed users and roles
        await Seed.SeedInformation(userManager, roleManager, context, env, logger);

        // Then add our own seeding logic in the correct order.
        // SeedVendors now handles vendor types, vendors, locations, business hours,
        // and service items internally -- no need for separate calls.
        Console.WriteLine("Starting vendor data seeding...");
        await SeedVendors.SeedVendorData(context, userManager);

        // Ensure all vendor data is saved before proceeding
        await context.SaveChangesAsync();
        Console.WriteLine("Vendor data seeding completed, proceeding with dependent data...");

        await SeedCustomers(context);
        await SeedBookings(context);
        
        // Only seed vendor bookings after vendors and bookings exist
        try {
            await SeedVendorBookings(context);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error seeding vendor bookings: {ex.Message}");
            // Continue even if vendor bookings fail
        }
        
        Console.WriteLine("Successfully seeded all data!");
    }
    
    private static async Task SeedVendorBusinessHours(DataContext context)
    {
        if (await context.VendorBusinessHours.AnyAsync())
            return;
            
        try
        {
            // Get all vendors first
            var vendors = await context.Vendors.ToListAsync();
            if (!vendors.Any())
            {
                Console.WriteLine("No vendors found, skipping business hours seeding");
                return;
            }

            var businessHours = new List<VendorBusinessHour>();
            
            foreach (var vendor in vendors)
            {
                // Create standard business hours for each vendor (Monday-Saturday)
                for (int day = 1; day <= 6; day++) // Monday to Saturday
                {
                    businessHours.Add(new VendorBusinessHour
                    {
                        VendorId = vendor.Id,
                        DayOfWeek = (DayOfWeek)day,
                        IsClosed = false,
                        OpenTime = TimeSpan.FromHours(9),  // 9:00 AM
                        CloseTime = TimeSpan.FromHours(18) // 6:00 PM
                    });
                }
                
                // Sunday closed for most vendors
                businessHours.Add(new VendorBusinessHour
                {
                    VendorId = vendor.Id,
                    DayOfWeek = DayOfWeek.Sunday,
                    IsClosed = true,
                    OpenTime = TimeSpan.Zero,
                    CloseTime = TimeSpan.Zero
                });
            }

            await context.VendorBusinessHours.AddRangeAsync(businessHours);
            await context.SaveChangesAsync();
            Console.WriteLine($"Seeded business hours for {vendors.Count} vendors");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding vendor business hours: {ex.Message}");
        }
    }
    
    private static async Task SeedServiceItems(DataContext context)
    {
        if (await context.ServiceItems.AnyAsync())
            return;
            
        try
        {
            // Read service items from JSON file - direct absolute path
            var jsonFilePath = Seed.GetSeedDataPath("serviceItems.json");
            
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"Service items JSON file not found at: {jsonFilePath}");
                return;
            }
            
            var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
            var serviceItemsFromJson = JsonSerializer.Deserialize<List<ServiceItemSeed>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (serviceItemsFromJson == null || !serviceItemsFromJson.Any())
            {
                Console.WriteLine("No service items found in JSON file.");
                return;
            }
            
            // Get existing vendors to map VendorIds properly
            var vendors = await context.Vendors.ToListAsync();
            if (!vendors.Any())
            {
                Console.WriteLine("No vendors found. Skipping service items seeding.");
                return;
            }

            // Convert JSON data to ServiceItem entities with proper VendorId mapping
            var serviceItems = new List<ServiceItem>();
            foreach (var item in serviceItemsFromJson)
            {
                // Find vendor by VendorTypeId instead of hardcoded VendorId
                var vendor = vendors.FirstOrDefault(v => v.VendorTypeId == item.VendorTypeId);
                if (vendor != null)
                {
                    serviceItems.Add(new ServiceItem
                    {
                        Name = item.Name,
                        Description = item.Description,
                        ServiceType = item.ServiceType,
                        Price = item.Price,
                        VendorId = vendor.Id, // Use actual vendor ID
                        VendorTypeId = item.VendorTypeId,
                        ImageUrl = item.ImageUrl,
                        IsAvailable = item.IsAvailable,
                        SortOrder = item.SortOrder,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            
            if (serviceItems.Any())
            {
                await context.ServiceItems.AddRangeAsync(serviceItems);
                await context.SaveChangesAsync();
                Console.WriteLine($"Seeded {serviceItems.Count} service items");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding service items: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
    
    /// <summary>
    /// Creates Customer entities for each AppUser that has the Customer role.
    /// Maps by looking up actual customer AppUser IDs instead of using hardcoded
    /// JSON values (which may not match actual database IDs).
    /// </summary>
    private static async Task SeedCustomers(DataContext context)
    {
        if (await context.Customers.AnyAsync())
            return;

        try
        {
            // Find customer users by their known usernames (seeded in Seed.cs)
            var customerUsernames = new[] { "mohammed.rashid", "fatima.ahmed", "ali.hassan", "customer.demo" };
            var customerUsers = await context.Users
                .Where(u => customerUsernames.Contains(u.UserName))
                .OrderBy(u => u.Id)
                .ToListAsync();

            if (!customerUsers.Any())
            {
                Console.WriteLine("[SeedAll] No customer users found, skipping customer entity creation");
                return;
            }

            foreach (var user in customerUsers)
            {
                context.Customers.Add(new Customer
                {
                    AppUserId = user.Id,
                    NumberOfOrders = 0,
                    SelectedAddressId = 0,
                    CreditMoney = 0,
                    Confirmed = true,
                    Active = true,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedAll] Created {customerUsers.Count} customer entities mapped to actual user IDs");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedAll] Error seeding customers: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates booking entities mapped to actual Customer IDs and Hall IDs.
    /// No longer reads from JSON (which had hardcoded FK values that may not exist).
    /// </summary>
    private static async Task SeedBookings(DataContext context)
    {
        if (await context.Bookings.AnyAsync())
            return;

        try
        {
            var customers = await context.Customers.OrderBy(c => c.Id).ToListAsync();
            var halls = await context.Halls.OrderBy(h => h.ID).ToListAsync();

            if (!customers.Any())
            {
                Console.WriteLine("[SeedAll] No customers found, skipping booking creation");
                return;
            }

            if (!halls.Any())
            {
                Console.WriteLine("[SeedAll] No halls found, skipping booking creation");
                return;
            }

            var bookings = new List<Booking>
            {
                new Booking
                {
                    HallId = halls[0].ID,
                    CustomerId = customers[0].Id,
                    PaymentMethod = "Credit Card",
                    Coupon = "WELCOME10",
                    Status = "Paid",
                    Comments = "Need extra chairs and tables for 150 guests",
                    VisitDate = DateTime.SpecifyKind(new DateTime(2026, 6, 15, 18, 0, 0), DateTimeKind.Utc),
                    IsVisitCompleted = false,
                    IsBookingConfirmed = true,
                    BookingDate = DateTime.SpecifyKind(new DateTime(2026, 1, 10, 10, 30, 0), DateTimeKind.Utc),
                    EventDate = DateTime.SpecifyKind(new DateTime(2026, 6, 15, 18, 0, 0), DateTimeKind.Utc),
                    StartTime = TimeSpan.FromHours(18),
                    EndTime = TimeSpan.FromHours(23),
                    EventType = "Wedding",
                    GuestCount = 150,
                    GenderPreference = 2, // Both
                    HallCost = 15000m,
                    VendorServicesCost = 3000m,
                    Subtotal = 18000m,
                    DiscountAmount = 1800m,
                    TaxRate = 0.15m,
                    TaxAmount = 2430m,
                    TotalAmount = 18630m,
                    Currency = "SAR",
                    PaymentStatus = "Completed",
                    PaidAt = DateTime.UtcNow,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Booking
                {
                    HallId = halls.Count > 1 ? halls[1].ID : halls[0].ID,
                    CustomerId = customers.Count > 1 ? customers[1].Id : customers[0].Id,
                    PaymentMethod = "Bank Transfer",
                    Coupon = "",
                    Status = "HallApproved",
                    Comments = "Wedding reception for 200 guests - need vendor approval",
                    VisitDate = DateTime.SpecifyKind(new DateTime(2026, 7, 20, 19, 0, 0), DateTimeKind.Utc),
                    IsVisitCompleted = false,
                    IsBookingConfirmed = false,
                    BookingDate = DateTime.SpecifyKind(new DateTime(2026, 1, 11, 14, 15, 0), DateTimeKind.Utc),
                    EventDate = DateTime.SpecifyKind(new DateTime(2026, 7, 20, 19, 0, 0), DateTimeKind.Utc),
                    StartTime = TimeSpan.FromHours(19),
                    EndTime = TimeSpan.FromHours(1), // 1 AM next day
                    EventType = "Wedding",
                    GuestCount = 200,
                    GenderPreference = 2,
                    HallCost = 18000m,
                    VendorServicesCost = 4000m,
                    Subtotal = 22000m,
                    DiscountAmount = 0m,
                    TaxRate = 0.15m,
                    TaxAmount = 3300m,
                    TotalAmount = 25300m,
                    Currency = "SAR",
                    PaymentStatus = "Pending",
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Booking
                {
                    HallId = halls[0].ID,
                    CustomerId = customers.Count > 2 ? customers[2].Id : customers[0].Id,
                    PaymentMethod = "Cash",
                    Coupon = "",
                    Status = "Pending",
                    Comments = "Corporate event - awaiting hall manager approval",
                    VisitDate = DateTime.SpecifyKind(new DateTime(2026, 8, 10, 9, 0, 0), DateTimeKind.Utc),
                    IsVisitCompleted = false,
                    IsBookingConfirmed = false,
                    BookingDate = DateTime.SpecifyKind(new DateTime(2026, 1, 12, 11, 45, 0), DateTimeKind.Utc),
                    EventDate = DateTime.SpecifyKind(new DateTime(2026, 8, 10, 9, 0, 0), DateTimeKind.Utc),
                    StartTime = TimeSpan.FromHours(9),
                    EndTime = TimeSpan.FromHours(17),
                    EventType = "Corporate",
                    GuestCount = 120,
                    GenderPreference = 0, // Male
                    HallCost = 8000m,
                    VendorServicesCost = 3000m,
                    Subtotal = 11000m,
                    DiscountAmount = 0m,
                    TaxRate = 0.15m,
                    TaxAmount = 1650m,
                    TotalAmount = 12650m,
                    Currency = "SAR",
                    PaymentStatus = "Pending",
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Booking
                {
                    HallId = halls.Count > 1 ? halls[1].ID : halls[0].ID,
                    CustomerId = customers[0].Id,
                    PaymentMethod = "Credit Card",
                    Coupon = "VIP2026",
                    Status = "VendorsApproving",
                    Comments = "VIP wedding - waiting for all vendor confirmations",
                    VisitDate = DateTime.SpecifyKind(new DateTime(2026, 9, 5, 20, 0, 0), DateTimeKind.Utc),
                    IsVisitCompleted = false,
                    IsBookingConfirmed = false,
                    BookingDate = DateTime.SpecifyKind(new DateTime(2026, 1, 13, 16, 0, 0), DateTimeKind.Utc),
                    EventDate = DateTime.SpecifyKind(new DateTime(2026, 9, 5, 20, 0, 0), DateTimeKind.Utc),
                    StartTime = TimeSpan.FromHours(20),
                    EndTime = TimeSpan.FromHours(2), // 2 AM next day
                    EventType = "Wedding",
                    GuestCount = 300,
                    GenderPreference = 2,
                    HallCost = 25000m,
                    VendorServicesCost = 5000m,
                    Subtotal = 30000m,
                    DiscountAmount = 1500m,
                    TaxRate = 0.15m,
                    TaxAmount = 4275m,
                    TotalAmount = 32775m,
                    Currency = "SAR",
                    PaymentStatus = "Pending",
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Booking
                {
                    HallId = halls[0].ID,
                    CustomerId = customers.Count > 1 ? customers[1].Id : customers[0].Id,
                    PaymentMethod = "Bank Transfer",
                    Coupon = "",
                    Status = "ReadyForPayment",
                    Comments = "All approvals received - ready to proceed with payment",
                    VisitDate = DateTime.SpecifyKind(new DateTime(2026, 10, 12, 17, 0, 0), DateTimeKind.Utc),
                    IsVisitCompleted = false,
                    IsBookingConfirmed = false,
                    BookingDate = DateTime.SpecifyKind(new DateTime(2026, 1, 14, 9, 20, 0), DateTimeKind.Utc),
                    EventDate = DateTime.SpecifyKind(new DateTime(2026, 10, 12, 17, 0, 0), DateTimeKind.Utc),
                    StartTime = TimeSpan.FromHours(17),
                    EndTime = TimeSpan.FromHours(23),
                    EventType = "Engagement",
                    GuestCount = 180,
                    GenderPreference = 1, // Female
                    HallCost = 12500m,
                    VendorServicesCost = 3500m,
                    Subtotal = 16000m,
                    DiscountAmount = 0m,
                    TaxRate = 0.15m,
                    TaxAmount = 2400m,
                    TotalAmount = 18400m,
                    Currency = "SAR",
                    PaymentStatus = "Pending",
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Bookings.AddRangeAsync(bookings);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedAll] Created {bookings.Count} bookings with valid Hall and Customer FK references");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedAll] Error seeding bookings: {ex.Message}");
            Console.WriteLine($"[SeedAll] Stack trace: {ex.StackTrace}");
        }
    }
    
    private static async Task SeedVendorBookings(DataContext context)
    {
        if (await context.VendorBookings.AnyAsync())
            return;
                
        try
        {
            // Get all bookings and vendors
            var bookings = await context.Bookings.ToListAsync();
            var vendors = await context.Vendors.ToListAsync();
                
            if (!bookings.Any() || !vendors.Any())
            {
                Console.WriteLine("No bookings or vendors found. Skipping vendor bookings seeding.");
                return;
            }
                
            var vendorBookings = new List<VendorBooking>();
            var random = new Random();
                
            // Create some vendor bookings by assigning vendors to existing bookings
            foreach (var booking in bookings)
            {
                // Assign 1-3 vendors to each booking
                int numVendors = random.Next(1, Math.Min(4, vendors.Count + 1));
                var selectedVendors = vendors.OrderBy(x => random.Next()).Take(numVendors).ToList();
                    
                foreach (var vendor in selectedVendors)
                {
                    var startTime = DateTime.UtcNow.Date.AddHours(random.Next(10, 16)); // Random start time between 10 AM and 4 PM
                    var endTime = startTime.AddHours(random.Next(3, 6)); // 3-6 hours duration
                        
                    vendorBookings.Add(new VendorBooking
                    {
                        BookingId = booking.Id,
                        VendorId = vendor.Id,
                        Status = "Confirmed",
                        StartTime = startTime,
                        EndTime = endTime,
                        TotalAmount = random.Next(3000, 15000),
                        Notes = "Auto-generated booking",
                        IsPaid = true,
                        PaidAt = DateTime.UtcNow,
                        PaymentMethod = "Credit Card",
                        PaymentStatus = "Completed",
                        Created = DateTime.UtcNow
                    });
                }
            }

            if (vendorBookings.Any())
            {
                await context.VendorBookings.AddRangeAsync(vendorBookings);
                await context.SaveChangesAsync();
                Console.WriteLine($"Seeded {vendorBookings.Count} vendor bookings");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding vendor bookings: {ex.Message}");
        }
    }
}

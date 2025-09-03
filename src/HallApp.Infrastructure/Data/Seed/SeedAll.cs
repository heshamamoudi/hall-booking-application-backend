using HallApp.Core.Entities;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.VendorEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    public static async Task SeedAllData(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, DataContext context)
    {
        // First seed users and roles
        await Seed.SeedInformation(userManager, roleManager, context);
        
        // Then add our own seeding logic in the correct order
        // Use the SeedVendors class to handle both vendor types and vendors
        Console.WriteLine("Starting vendor data seeding...");
        await SeedVendors.SeedVendorData(context);
        
        // Ensure all vendor data is saved before proceeding
        await context.SaveChangesAsync();
        Console.WriteLine("Vendor data seeding completed, proceeding with dependent data...");
        
        await SeedVendorBusinessHours(context);
        
        // Only seed service items after vendors are confirmed to exist
        Console.WriteLine("Starting service items seeding...");
        await SeedServiceItems(context);
        
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
            var jsonFilePath = "/Users/heshamamoudi/Application Development/Hesham/HallAppBackend/Data/serviceItems.json";
            
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
    
    private static async Task SeedCustomers(DataContext context)
    {
        if (await context.Customers.AnyAsync())
            return;
            
        try
        {
            // Read customers from JSON file
            var customersPath = "/Users/heshamamoudi/Application Development/Hesham/HallAppBackend/Data/customers.json";
            
            if (!File.Exists(customersPath))
            {
                Console.WriteLine($"Customers JSON file not found at: {customersPath}");
                return;
            }
            
            var customersData = await File.ReadAllTextAsync(customersPath);
            var customersFromJson = JsonSerializer.Deserialize<List<Customer>>(customersData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (customersFromJson == null || !customersFromJson.Any())
            {
                Console.WriteLine("No customers found in JSON file.");
                return;
            }

            // Get existing AppUsers to map properly
            var appUsers = await context.Users.ToListAsync();
            
            foreach (var customer in customersFromJson)
            {
                // Set timestamps and add to context
                customer.Created = DateTime.UtcNow;
                customer.Updated = DateTime.UtcNow;
                context.Customers.Add(customer);
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"Seeded {customersFromJson.Count} customers from JSON");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding customers: {ex.Message}");
        }
    }
    
    private static async Task SeedBookings(DataContext context)
    {
        if (await context.Bookings.AnyAsync())
            return;
                
        try
        {
            // Use direct absolute path
            var bookingsPath = "/Users/heshamamoudi/Application Development/Hesham/HallAppBackend/Data/bookings.json";
            var bookingsData = await File.ReadAllTextAsync(bookingsPath);
                
            var bookings = JsonSerializer.Deserialize<List<Booking>>(bookingsData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
                
            if (bookings != null && bookings.Any())
            {
                // Get existing customers to map properly
                var customers = await context.Customers.OrderBy(c => c.Id).ToListAsync();
                
                for (int i = 0; i < bookings.Count && i < customers.Count; i++)
                {
                    var booking = bookings[i];
                    var customer = customers[i];
                    
                    // Map booking to actual customer ID
                    booking.CustomerId = customer.Id;
                    booking.Created = DateTime.UtcNow;
                    booking.Updated = DateTime.UtcNow;
                    context.Bookings.Add(booking);
                }
                
                await context.SaveChangesAsync();
                Console.WriteLine($"Seeded {Math.Min(bookings.Count, customers.Count)} bookings mapped to existing customers");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding bookings: {ex.Message}");
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
                    var startTime = DateTime.Today.AddHours(random.Next(10, 16)); // Random start time between 10 AM and 4 PM
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

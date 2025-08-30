using HallApp.Core.Entities.VendorEntities;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Seed;

public class SeedVendorTypes
{
    public static async Task SeedVendorTypesData(DataContext context)
    {
        // Check if any vendor types already exist
        if (await context.VendorTypes.AnyAsync())
            return;

        // Create sample vendor types
        var vendorTypes = new List<VendorType>
        {
            new VendorType
            {
                Name = "Catering Service",
                Description = "Food and beverage services for events",
                RequiresHallBooking = true,
                AllowsMultipleBookings = true,
                MaxSimultaneousBookings = 3,
                RequiresTimeSlot = true,
                DefaultDuration = 180, // 3 hours in minutes
                SortOrder = 1,
                IsActive = true
            },
            new VendorType
            {
                Name = "Photography",
                Description = "Professional photography services",
                RequiresHallBooking = true,
                AllowsMultipleBookings = false,
                MaxSimultaneousBookings = 1,
                RequiresTimeSlot = true,
                DefaultDuration = 240, // 4 hours in minutes
                SortOrder = 2,
                IsActive = true
            },
            new VendorType
            {
                Name = "Decoration",
                Description = "Event decoration services",
                RequiresHallBooking = true,
                AllowsMultipleBookings = false,
                MaxSimultaneousBookings = 1,
                RequiresTimeSlot = true,
                DefaultDuration = 120, // 2 hours in minutes
                SortOrder = 3,
                IsActive = true
            }
        };

        // Add to the context
        await context.VendorTypes.AddRangeAsync(vendorTypes);
        
        // Save changes
        await context.SaveChangesAsync();
    }
}

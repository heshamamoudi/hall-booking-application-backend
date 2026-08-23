using HallApp.Core.Entities.VendorEntities;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Seed;

public class SeedVendorTypes
{
    /// <summary>
    /// The vendor categories the platform ships with. A vendor is distinguished from
    /// a hall only by its type, so this list is core reference data rather than demo
    /// data and is seeded in every environment.
    ///
    /// Seeding is idempotent PER TYPE, not all-or-nothing. The previous version
    /// returned early if any vendor type existed at all, which meant a partially
    /// populated table could never be completed - and SeedVendors, which needs all
    /// five by name, would then give up with "Missing vendor types, cannot seed
    /// vendors" and leave the whole demo dataset without a single vendor.
    /// </summary>
    private static List<VendorType> Catalogue() => new()
    {
        new VendorType
        {
            Name = "Catering Service",
            Description = "Provides food and beverage services for weddings, corporate events, and celebrations. Includes menu planning, preparation, and on-site serving.",
            RequiresHallBooking = true,
            AllowsMultipleBookings = false,
            MaxSimultaneousBookings = 1,
            RequiresTimeSlot = true,
            DefaultDuration = 360, // 6 hours
            SortOrder = 1,
            IsActive = true
        },
        new VendorType
        {
            Name = "Photography",
            Description = "Professional photography and videography services for events. Covers pre-event, ceremony, and post-event photography.",
            RequiresHallBooking = true,
            AllowsMultipleBookings = true,
            MaxSimultaneousBookings = 2,
            RequiresTimeSlot = true,
            DefaultDuration = 480, // 8 hours
            SortOrder = 2,
            IsActive = true
        },
        new VendorType
        {
            Name = "Decoration",
            Description = "Event decoration and setup services including floral arrangements, lighting, drapery, and theme design.",
            RequiresHallBooking = true,
            AllowsMultipleBookings = false,
            MaxSimultaneousBookings = 1,
            RequiresTimeSlot = true,
            DefaultDuration = 720, // 12 hours (setup + event)
            SortOrder = 3,
            IsActive = true
        },
        new VendorType
        {
            Name = "Entertainment",
            Description = "Music, performances, DJ services, and entertainment acts for events and celebrations.",
            RequiresHallBooking = true,
            AllowsMultipleBookings = true,
            MaxSimultaneousBookings = 3,
            RequiresTimeSlot = true,
            DefaultDuration = 240, // 4 hours
            SortOrder = 4,
            IsActive = true
        },
        new VendorType
        {
            Name = "Transportation",
            Description = "Vehicle rental, limousine, and transportation services for events. Includes bridal car decoration.",
            RequiresHallBooking = false,
            AllowsMultipleBookings = true,
            MaxSimultaneousBookings = 5,
            RequiresTimeSlot = false,
            DefaultDuration = 120, // 2 hours
            SortOrder = 5,
            IsActive = true
        },
        new VendorType
        {
            Name = "Restaurant",
            Description = "Restaurants offering event menus, private dining rooms, and on-premise celebrations.",
            RequiresHallBooking = false,
            AllowsMultipleBookings = true,
            MaxSimultaneousBookings = 3,
            RequiresTimeSlot = true,
            DefaultDuration = 240, // 4 hours
            SortOrder = 6,
            IsActive = true
        }
    };

    public static async Task SeedVendorTypesData(DataContext context)
    {
        var existingNames = await context.VendorTypes
            .Select(vt => vt.Name)
            .ToListAsync();

        var missing = Catalogue()
            .Where(vt => !existingNames.Contains(vt.Name))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        await context.VendorTypes.AddRangeAsync(missing);
        await context.SaveChangesAsync();

        Console.WriteLine(
            $"[Seed] Created {missing.Count} missing vendor types: {string.Join(", ", missing.Select(v => v.Name))}");
    }
}

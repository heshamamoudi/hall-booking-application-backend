using HallApp.Core.Constants;
using HallApp.Core.Entities;
using HallApp.Core.Entities.VendorEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

#nullable enable

namespace HallApp.Infrastructure.Data.Seed;

public class SeedVendors
{
    public static async Task SeedVendorData(DataContext context, UserManager<AppUser>? userManager = null)
    {
        try
        {
            Console.WriteLine("[SeedVendors] Starting vendor seeding...");

            // 1. Seed vendor types
            await SeedVendorTypes(context);

            // 2. Seed vendors with full details
            await SeedVendorsWithDetails(context);

            // 3. Link vendors to organizations
            await LinkVendorsToOrganizations(context);

            // 4. Create VendorManager entities for team members
            if (userManager != null && !await context.VendorManagers.AnyAsync())
            {
                await CreateVendorManagerEntities(context, userManager);
            }
            else if (userManager == null)
            {
                Console.WriteLine("[SeedVendors] UserManager not provided, skipping vendor manager creation");
            }
            else
            {
                Console.WriteLine("[SeedVendors] Vendor managers already exist, skipping creation");
            }

            Console.WriteLine("[SeedVendors] Vendor seeding completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedVendors] Error in vendor seeding: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    // ===================================================================
    // VENDOR TYPES
    // ===================================================================

    private static async Task SeedVendorTypes(DataContext context)
    {
        if (await context.VendorTypes.AnyAsync())
        {
            Console.WriteLine("[SeedVendors] Vendor types already exist, skipping");
            return;
        }

        var vendorTypes = new List<VendorType>
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
            }
        };

        await context.VendorTypes.AddRangeAsync(vendorTypes);
        await context.SaveChangesAsync();
        Console.WriteLine($"[SeedVendors] Seeded {vendorTypes.Count} vendor types");
    }

    // ===================================================================
    // VENDORS WITH FULL DETAILS
    // ===================================================================

    private static async Task SeedVendorsWithDetails(DataContext context)
    {
        if (await context.Vendors.AnyAsync())
        {
            Console.WriteLine("[SeedVendors] Vendors already exist, skipping");
            return;
        }

        // Load vendor types to get their IDs
        var cateringType = await context.VendorTypes.FirstOrDefaultAsync(vt => vt.Name == "Catering Service");
        var photographyType = await context.VendorTypes.FirstOrDefaultAsync(vt => vt.Name == "Photography");
        var decorationType = await context.VendorTypes.FirstOrDefaultAsync(vt => vt.Name == "Decoration");
        var entertainmentType = await context.VendorTypes.FirstOrDefaultAsync(vt => vt.Name == "Entertainment");
        var transportationType = await context.VendorTypes.FirstOrDefaultAsync(vt => vt.Name == "Transportation");

        if (cateringType == null || photographyType == null || decorationType == null ||
            entertainmentType == null || transportationType == null)
        {
            Console.WriteLine("[SeedVendors] Missing vendor types, cannot seed vendors");
            return;
        }

        var vendors = GetVendorDefinitions(
            cateringType, photographyType, decorationType,
            entertainmentType, transportationType);

        context.Vendors.AddRange(vendors);
        await context.SaveChangesAsync();
        Console.WriteLine($"[SeedVendors] Seeded {vendors.Count} vendors with full details");

        // Seed locations for all vendors
        await SeedVendorLocations(context, vendors);

        // Seed business hours for all vendors
        await SeedVendorBusinessHours(context, vendors);

        // Seed service items for all vendors
        await SeedVendorServiceItems(context, vendors);
    }

    private static List<Vendor> GetVendorDefinitions(
        VendorType cateringType,
        VendorType photographyType,
        VendorType decorationType,
        VendorType entertainmentType,
        VendorType transportationType)
    {
        // ---- Vendor 1: Elegant Decor (Decoration) ----
        // Belongs to "Luxury Decor & Design" org (noura.dosari)
        var elegantDecor = new Vendor
        {
            Name = "Elegant Decor",
            Description = "Elegant Decor is Jeddah's leading event decoration company with over 15 years of experience transforming venues into breathtaking spaces. Specializing in luxury wedding decoration, we offer bespoke floral arrangements, designer drapery, custom lighting, and themed decor that brings your vision to life. Our team of 20+ designers has decorated over 3,000 events across Saudi Arabia.",
            Email = "contact@elegantdecor.com",
            Phone = "+966501234567",
            WhatsApp = "+966501234567",
            Website = "https://elegantdecor.sa",
            LogoUrl = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?w=200",
            CoverImageUrl = "https://images.unsplash.com/photo-1519225421980-715cb0215aed?w=1200",
            CommercialRegistrationNumber = "4030567890",
            VatNumber = "300567890100003",
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            IsFeatured = true,
            IsPremium = true,
            HasSpecialOffer = false,
            Rating = 4.7,
            ReviewCount = 124,
            VendorTypeId = decorationType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ---- Vendor 2: Royal Catering (Catering) ----
        // Belongs to "Elite Catering & Events" org (khalid.otaibi)
        var royalCatering = new Vendor
        {
            Name = "Royal Catering",
            Description = "Royal Catering is the premier luxury catering service in Saudi Arabia, renowned for its exceptional cuisine and impeccable service. With over 20 years of experience, we specialize in traditional Saudi, Lebanese, and international cuisine for weddings, corporate galas, and royal banquets. Our team of 15 chefs, including 3 Michelin-trained experts, prepare fresh, halal-certified dishes using locally sourced ingredients. We serve 200-2000 guests per event.",
            Email = "info@royalcatering.sa",
            Phone = "+966512345678",
            WhatsApp = "+966512345678",
            Website = "https://royalcatering.sa",
            LogoUrl = "https://images.unsplash.com/photo-1555244162-803834f70033?w=200",
            CoverImageUrl = "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=1200",
            CommercialRegistrationNumber = "1010890123",
            VatNumber = "300890123400003",
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            IsFeatured = true,
            IsPremium = true,
            HasSpecialOffer = true,
            Rating = 4.8,
            ReviewCount = 156,
            VendorTypeId = cateringType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ---- Vendor 3: Gourmet Delight Catering (Catering) ----
        // Belongs to "Elite Catering & Events" org (khalid.otaibi)
        var gourmetDelight = new Vendor
        {
            Name = "Gourmet Delight Catering",
            Description = "Gourmet Delight Catering brings international fine dining to your events. Our executive chef trained at Le Cordon Bleu, and our menu features French, Italian, Asian fusion, and traditional Arabian cuisine. We specialize in multi-course plated dinners, live cooking stations, and artisanal dessert bars. Serving events of 50-800 guests with white-glove service.",
            Email = "info@gourmetdelight.sa",
            Phone = "+966523456789",
            WhatsApp = "+966523456789",
            Website = "https://gourmetdelight.sa",
            LogoUrl = "https://images.unsplash.com/photo-1528605248644-14dd04022da1?w=200",
            CoverImageUrl = "https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?w=1200",
            CommercialRegistrationNumber = "1010901234",
            VatNumber = "300901234500003",
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            IsFeatured = false,
            IsPremium = true,
            HasSpecialOffer = true,
            Rating = 4.6,
            ReviewCount = 108,
            VendorTypeId = cateringType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ---- Vendor 4: Capture Moments Photography (Photography) ----
        // Belongs to "Premium Vendors Co." org (vendorowner.demo)
        var capturePhotography = new Vendor
        {
            Name = "Capture Moments Photography",
            Description = "Capture Moments is Saudi Arabia's award-winning photography studio, specializing in wedding, engagement, and event photography. Our team of 8 photographers and 4 videographers use state-of-the-art equipment including cinema-grade cameras and drone technology. We offer pre-wedding shoots, same-day edits, cinematic highlight reels, and premium photo albums. Gender-segregated photography available with dedicated female photography team.",
            Email = "info@capturemoments.sa",
            Phone = "+966534567890",
            WhatsApp = "+966534567890",
            Website = "https://capturemoments.sa",
            LogoUrl = "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=200",
            CoverImageUrl = "https://images.unsplash.com/photo-1554048612-b6a482bc67e5?w=1200",
            CommercialRegistrationNumber = "1010012345",
            VatNumber = "301012345600003",
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            IsFeatured = true,
            IsPremium = false,
            HasSpecialOffer = false,
            Rating = 4.9,
            ReviewCount = 87,
            VendorTypeId = photographyType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ---- Vendor 5: Melody Entertainment (Entertainment) ----
        // Belongs to "Premium Vendors Co." org (vendorowner.demo)
        var melodyEntertainment = new Vendor
        {
            Name = "Melody Entertainment",
            Description = "Melody Entertainment is the go-to entertainment provider for weddings and celebrations across Saudi Arabia. We offer professional DJ services, live oud and keyboard players, traditional Samer and Dabke dance troupes, and fireworks displays. Our team has entertained at over 5,000 events. We provide gender-appropriate entertainment with dedicated female-only performers for women's celebrations.",
            Email = "bookings@melodyent.sa",
            Phone = "+966545678901",
            WhatsApp = "+966545678901",
            Website = "https://melodyent.sa",
            LogoUrl = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=200",
            CoverImageUrl = "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?w=1200",
            CommercialRegistrationNumber = "1010123456",
            VatNumber = "301123456700003",
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            IsFeatured = false,
            IsPremium = false,
            HasSpecialOffer = true,
            Rating = 4.5,
            ReviewCount = 65,
            VendorTypeId = entertainmentType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ---- Vendor 6: Al Safwa Transportation (Transportation) ----
        // Belongs to "Luxury Decor & Design" org (noura.dosari)
        var safwaTransport = new Vendor
        {
            Name = "Al Safwa Transportation",
            Description = "Al Safwa Transportation offers luxury vehicle services for weddings and VIP events across Saudi Arabia. Our fleet includes Rolls-Royce Phantoms, Mercedes S-Class, decorated bridal cars, and vintage classics. We provide professional chauffeurs in formal attire, red carpet service, and custom car decoration with fresh flowers and ribbons. Airport transfers and multi-vehicle convoys available.",
            Email = "reservations@alsafwa.sa",
            Phone = "+966556789012",
            WhatsApp = "+966556789012",
            Website = "https://alsafwa-transport.sa",
            LogoUrl = "https://images.unsplash.com/photo-1549317661-bd32c8ce0afa?w=200",
            CoverImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=1200",
            CommercialRegistrationNumber = "1010234567",
            VatNumber = "301234567800003",
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            IsFeatured = true,
            IsPremium = true,
            HasSpecialOffer = false,
            Rating = 4.4,
            ReviewCount = 52,
            VendorTypeId = transportationType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ---- Vendor 7: Lens Studio Photography (Photography) ----
        // Belongs to "Elite Catering & Events" org (khalid.otaibi)
        var lensStudio = new Vendor
        {
            Name = "Lens Studio Photography",
            Description = "Lens Studio is a boutique photography studio in Riyadh specializing in artistic wedding storytelling. We blend photojournalistic candid captures with cinematic posed portraits. Our signature style has been featured in Arabian Bride Magazine. Services include drone aerial photography, 360-degree photo booths, instant photo printing stations, and luxury leather-bound albums. Female-only photography team available.",
            Email = "hello@lensstudio.sa",
            Phone = "+966567890123",
            WhatsApp = "+966567890123",
            Website = "https://lensstudio.sa",
            LogoUrl = "https://images.unsplash.com/photo-1606216794079-73f85bbd57d5?w=200",
            CoverImageUrl = "https://images.unsplash.com/photo-1537633552985-df8429e8048b?w=1200",
            CommercialRegistrationNumber = "1010345678",
            VatNumber = "301345678900003",
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            IsFeatured = false,
            IsPremium = true,
            HasSpecialOffer = true,
            Rating = 4.7,
            ReviewCount = 73,
            VendorTypeId = photographyType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ---- Vendor 8: Noor Floral Design (Decoration) ----
        // Belongs to "Luxury Decor & Design" org (noura.dosari)
        var noorFloral = new Vendor
        {
            Name = "Noor Floral Design",
            Description = "Noor Floral Design creates stunning floral masterpieces for weddings and events in the Makkah and Madinah regions. We import premium roses from Ecuador, orchids from Thailand, and exotic blooms from around the world. Our signature offerings include cascading flower walls, suspended floral chandeliers, aisle arrangements, and bridal bouquets. We use eco-friendly foam-free techniques and offer same-day delivery.",
            Email = "flowers@noorfloral.sa",
            Phone = "+966578901234",
            WhatsApp = "+966578901234",
            Website = "https://noorfloral.sa",
            LogoUrl = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?w=200",
            CoverImageUrl = "https://images.unsplash.com/photo-1490750967868-88aa4f44baee?w=1200",
            CommercialRegistrationNumber = "4030456789",
            VatNumber = "301456789000003",
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            IsFeatured = true,
            IsPremium = false,
            HasSpecialOffer = false,
            Rating = 4.6,
            ReviewCount = 91,
            VendorTypeId = decorationType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // ---- Vendor 9: Arabian Nights Catering (Catering) ----
        // Belongs to "Premium Vendors Co." org (vendorowner.demo)
        var arabianNightsCatering = new Vendor
        {
            Name = "Arabian Nights Catering",
            Description = "Arabian Nights Catering specializes in authentic traditional Saudi and Arabian cuisine for weddings and celebrations. We are famous for our whole-lamb Mandi, Kabsa, and Harees. Our open-fire grilling stations and live bread-making (Tannour) add theatrical flair to your event. We source all our spices from Souq Al-Balad and serve 100-1500 guests with traditional copper serving ware.",
            Email = "orders@arabiannights.sa",
            Phone = "+966589012345",
            WhatsApp = "+966589012345",
            Website = "https://arabiannightscatering.sa",
            LogoUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=200",
            CoverImageUrl = "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=1200",
            CommercialRegistrationNumber = "4030567891",
            VatNumber = "301567890100003",
            IsActive = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            IsFeatured = false,
            IsPremium = false,
            HasSpecialOffer = true,
            Rating = 4.3,
            ReviewCount = 145,
            VendorTypeId = cateringType.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return
        [
            elegantDecor, royalCatering, gourmetDelight,
            capturePhotography, melodyEntertainment, safwaTransport,
            lensStudio, noorFloral, arabianNightsCatering
        ];
    }

    // ===================================================================
    // VENDOR LOCATIONS
    // ===================================================================

    private static async Task SeedVendorLocations(DataContext context, List<Vendor> vendors)
    {
        try
        {
            var locations = new List<VendorLocation>();

            // Get vendors by name from DB (they now have IDs)
            var savedVendors = await context.Vendors.OrderBy(v => v.Id).ToListAsync();

            foreach (var vendor in savedVendors)
            {
                var location = vendor.Name switch
                {
                    "Elegant Decor" => new VendorLocation
                    {
                        VendorId = vendor.Id,
                        Address = "Al Rawdah District, Tahlia Street, Building 45",
                        City = "Jeddah",
                        State = "Makkah Region",
                        PostalCode = "21472",
                        Country = "SA",
                        Latitude = 21.5200,
                        Longitude = 39.1620,
                        IsPrimary = true,
                        IsActive = true
                    },
                    "Royal Catering" => new VendorLocation
                    {
                        VendorId = vendor.Id,
                        Address = "Al Olaya District, King Fahd Road, Tower 12, Floor 3",
                        City = "Riyadh",
                        State = "Riyadh Region",
                        PostalCode = "12211",
                        Country = "SA",
                        Latitude = 24.7116,
                        Longitude = 46.6741,
                        IsPrimary = true,
                        IsActive = true
                    },
                    "Gourmet Delight Catering" => new VendorLocation
                    {
                        VendorId = vendor.Id,
                        Address = "Al Salamah District, Prince Sultan Street, Villa 78",
                        City = "Jeddah",
                        State = "Makkah Region",
                        PostalCode = "21442",
                        Country = "SA",
                        Latitude = 21.5433,
                        Longitude = 39.1727,
                        IsPrimary = true,
                        IsActive = true
                    },
                    "Capture Moments Photography" => new VendorLocation
                    {
                        VendorId = vendor.Id,
                        Address = "Al Worood District, Olaya Street, Studio 22",
                        City = "Riyadh",
                        State = "Riyadh Region",
                        PostalCode = "12251",
                        Country = "SA",
                        Latitude = 24.6900,
                        Longitude = 46.6800,
                        IsPrimary = true,
                        IsActive = true
                    },
                    "Melody Entertainment" => new VendorLocation
                    {
                        VendorId = vendor.Id,
                        Address = "Al Malaz District, King Abdullah Road, Suite 15",
                        City = "Riyadh",
                        State = "Riyadh Region",
                        PostalCode = "12641",
                        Country = "SA",
                        Latitude = 24.7000,
                        Longitude = 46.7100,
                        IsPrimary = true,
                        IsActive = true
                    },
                    "Al Safwa Transportation" => new VendorLocation
                    {
                        VendorId = vendor.Id,
                        Address = "Al Hamra District, King Abdullah Road, Showroom 5",
                        City = "Jeddah",
                        State = "Makkah Region",
                        PostalCode = "21488",
                        Country = "SA",
                        Latitude = 21.5430,
                        Longitude = 39.1725,
                        IsPrimary = true,
                        IsActive = true
                    },
                    "Lens Studio Photography" => new VendorLocation
                    {
                        VendorId = vendor.Id,
                        Address = "Al Nakheel District, Northern Ring Road, Plaza 8, Unit 3A",
                        City = "Riyadh",
                        State = "Riyadh Region",
                        PostalCode = "12382",
                        Country = "SA",
                        Latitude = 24.7200,
                        Longitude = 46.6600,
                        IsPrimary = true,
                        IsActive = true
                    },
                    "Noor Floral Design" => new VendorLocation
                    {
                        VendorId = vendor.Id,
                        Address = "Al Aziziyah District, Ibrahim Al Khalil Road, Shop 34",
                        City = "Makkah",
                        State = "Makkah Region",
                        PostalCode = "24232",
                        Country = "SA",
                        Latitude = 21.4225,
                        Longitude = 39.8261,
                        IsPrimary = true,
                        IsActive = true
                    },
                    "Arabian Nights Catering" => new VendorLocation
                    {
                        VendorId = vendor.Id,
                        Address = "Al Khalidiyah District, Prince Abdulmajeed Road, Building 12",
                        City = "Madinah",
                        State = "Madinah Region",
                        PostalCode = "42312",
                        Country = "SA",
                        Latitude = 24.4709,
                        Longitude = 39.6122,
                        IsPrimary = true,
                        IsActive = true
                    },
                    _ => null
                };

                if (location != null)
                {
                    locations.Add(location);
                }
            }

            if (locations.Count > 0)
            {
                await context.VendorLocations.AddRangeAsync(locations);
                await context.SaveChangesAsync();
                Console.WriteLine($"[SeedVendors] Seeded {locations.Count} vendor locations");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedVendors] Error seeding vendor locations: {ex.Message}");
        }
    }

    // ===================================================================
    // VENDOR BUSINESS HOURS
    // ===================================================================

    private static async Task SeedVendorBusinessHours(DataContext context, List<Vendor> vendors)
    {
        if (await context.VendorBusinessHours.AnyAsync())
        {
            Console.WriteLine("[SeedVendors] Business hours already exist, skipping");
            return;
        }

        try
        {
            var savedVendors = await context.Vendors.ToListAsync();
            var businessHours = new List<VendorBusinessHour>();

            foreach (var vendor in savedVendors)
            {
                // Most vendors: Saturday-Thursday open, Friday closed (Saudi weekend)
                // Transportation: open every day
                var isFridayClosed = vendor.Name != "Al Safwa Transportation";
                // Catering vendors have extended hours
                var isCatering = vendor.VendorTypeId == savedVendors
                    .FirstOrDefault(v => v.Name == "Royal Catering")?.VendorTypeId;

                for (int day = 0; day < 7; day++)
                {
                    var dayOfWeek = (DayOfWeek)day;

                    if (dayOfWeek == DayOfWeek.Friday && isFridayClosed)
                    {
                        businessHours.Add(new VendorBusinessHour
                        {
                            VendorId = vendor.Id,
                            DayOfWeek = dayOfWeek,
                            IsClosed = true,
                            OpenTime = TimeSpan.Zero,
                            CloseTime = TimeSpan.Zero,
                            SpecialNote = "Closed for Friday prayers"
                        });
                    }
                    else
                    {
                        var openTime = isCatering == true ? TimeSpan.FromHours(8) : TimeSpan.FromHours(9);
                        var closeTime = isCatering == true ? TimeSpan.FromHours(22) : TimeSpan.FromHours(21);

                        // Entertainment vendors have later hours
                        if (vendor.Name == "Melody Entertainment")
                        {
                            openTime = TimeSpan.FromHours(14);
                            closeTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));
                        }

                        businessHours.Add(new VendorBusinessHour
                        {
                            VendorId = vendor.Id,
                            DayOfWeek = dayOfWeek,
                            IsClosed = false,
                            OpenTime = openTime,
                            CloseTime = closeTime,
                            SpecialNote = dayOfWeek == DayOfWeek.Saturday ? "Weekend - Extended hours available" : ""
                        });
                    }
                }
            }

            await context.VendorBusinessHours.AddRangeAsync(businessHours);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedVendors] Seeded {businessHours.Count} business hour entries for {savedVendors.Count} vendors");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedVendors] Error seeding business hours: {ex.Message}");
        }
    }

    // ===================================================================
    // VENDOR SERVICE ITEMS
    // ===================================================================

    private static async Task SeedVendorServiceItems(DataContext context, List<Vendor> vendors)
    {
        if (await context.ServiceItems.AnyAsync())
        {
            Console.WriteLine("[SeedVendors] Service items already exist, skipping");
            return;
        }

        try
        {
            var savedVendors = await context.Vendors.Include(v => v.VendorType).ToListAsync();
            var serviceItems = new List<ServiceItem>();

            foreach (var vendor in savedVendors)
            {
                var items = vendor.Name switch
                {
                    // DECORATION VENDORS
                    "Elegant Decor" => new List<ServiceItem>
                    {
                        new ServiceItem
                        {
                            Name = "Luxury Wedding Decoration",
                            Description = "Complete luxury wedding decoration including designer drapery, premium floral arrangements, crystal chandeliers, custom stage design, and kosha setup. Includes setup and teardown.",
                            ServiceType = "Wedding",
                            Price = 12000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 1
                        },
                        new ServiceItem
                        {
                            Name = "Corporate Event Decor",
                            Description = "Professional decoration for corporate events, conferences, and gala dinners. Includes branded elements, stage design, table settings, and ambient lighting.",
                            ServiceType = "Corporate",
                            Price = 8000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1519225421980-715cb0215aed?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 2
                        },
                        new ServiceItem
                        {
                            Name = "Flower Wall Backdrop",
                            Description = "Stunning flower wall backdrop (3m x 3m) with premium roses, hydrangeas, and greenery. Perfect for photos and stage backgrounds.",
                            ServiceType = "AddOn",
                            Price = 3500m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 3
                        },
                        new ServiceItem
                        {
                            Name = "Table Centerpiece Package (20 tables)",
                            Description = "Elegant floral centerpieces for 20 tables. Each centerpiece includes a tall crystal vase with premium flowers and LED candle accents.",
                            ServiceType = "AddOn",
                            Price = 4000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1507504031003-b417219a0fde?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 4
                        }
                    },

                    // CATERING VENDORS
                    "Royal Catering" => new List<ServiceItem>
                    {
                        new ServiceItem
                        {
                            Name = "Premium Wedding Package (per person)",
                            Description = "Complete wedding catering for 200+ guests: appetizers, 3 live stations (Mandi, Kebab, Seafood), international buffet with 12 dishes, dessert bar, Arabic coffee service, and premium beverages.",
                            ServiceType = "Wedding",
                            Price = 150m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1555244162-803834f70033?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 1
                        },
                        new ServiceItem
                        {
                            Name = "Corporate Event Catering (per person)",
                            Description = "Professional corporate catering: welcome refreshments, seated 3-course lunch or dinner, coffee breaks with pastries, and custom branded desserts.",
                            ServiceType = "Corporate",
                            Price = 100m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 2
                        },
                        new ServiceItem
                        {
                            Name = "VIP Gala Dinner (per person)",
                            Description = "Ultra-premium 5-course plated dinner with wagyu beef, fresh lobster, truffle risotto, and custom dessert tasting. Includes dedicated waiter per 6 guests.",
                            ServiceType = "Premium",
                            Price = 250m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 3
                        },
                        new ServiceItem
                        {
                            Name = "Arabic Coffee & Dates Station",
                            Description = "Traditional Arabic coffee station with premium Hasawi dates, Saudi chocolates, and incense welcome ceremony. Serves up to 500 guests.",
                            ServiceType = "AddOn",
                            Price = 2000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 4
                        }
                    },
                    "Gourmet Delight Catering" => new List<ServiceItem>
                    {
                        new ServiceItem
                        {
                            Name = "International Buffet (per person)",
                            Description = "Gourmet international buffet featuring French, Italian, and Asian fusion cuisine. Includes 8 main dishes, salad bar, bread station, and dessert selection.",
                            ServiceType = "Premium",
                            Price = 120m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1528605248644-14dd04022da1?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 1
                        },
                        new ServiceItem
                        {
                            Name = "Live Cooking Station Package",
                            Description = "3 live cooking stations with chefs: pasta station, sushi bar, and grilled meats. Includes equipment, chefs, and ingredients for up to 300 guests.",
                            ServiceType = "AddOn",
                            Price = 5000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 2
                        },
                        new ServiceItem
                        {
                            Name = "Artisanal Dessert Bar",
                            Description = "Premium dessert station with French patisserie, chocolate fountain, macarons, mini cakes, and traditional Middle Eastern sweets. Serves 200 guests.",
                            ServiceType = "AddOn",
                            Price = 3500m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1551024506-0bccd828d307?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 3
                        }
                    },
                    "Arabian Nights Catering" => new List<ServiceItem>
                    {
                        new ServiceItem
                        {
                            Name = "Traditional Mandi Feast (per person)",
                            Description = "Authentic whole-lamb Mandi rice feast with traditional spices. Served on large communal platters with raita, salads, and Arabian pickles. Minimum 50 guests.",
                            ServiceType = "Traditional",
                            Price = 80m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 1
                        },
                        new ServiceItem
                        {
                            Name = "Kabsa Banquet (per person)",
                            Description = "Classic Saudi Kabsa with saffron rice and slow-cooked lamb. Includes Jareesh, Harees, and assorted appetizers. Traditional copper serving ware.",
                            ServiceType = "Traditional",
                            Price = 70m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 2
                        },
                        new ServiceItem
                        {
                            Name = "Open-Fire Grilling Station",
                            Description = "Live open-fire grilling station with whole lamb, kebabs, and grilled vegetables. Includes chef, equipment, and charcoal. Serves up to 200 guests.",
                            ServiceType = "AddOn",
                            Price = 4500m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 3
                        }
                    },

                    // PHOTOGRAPHY VENDORS
                    "Capture Moments Photography" => new List<ServiceItem>
                    {
                        new ServiceItem
                        {
                            Name = "Full Wedding Coverage",
                            Description = "Complete wedding day photography: 2 photographers, 8+ hours coverage, 500+ edited photos, pre-ceremony prep shots, ceremony, reception, and group portraits. Includes online gallery and USB drive.",
                            ServiceType = "Wedding",
                            Price = 8000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 1
                        },
                        new ServiceItem
                        {
                            Name = "Cinematic Wedding Film",
                            Description = "Full cinematic videography: 2 videographers, drone footage, same-day highlight edit (3 min), full ceremony edit, and 15-minute cinematic film. 4K resolution.",
                            ServiceType = "Wedding",
                            Price = 10000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1554048612-b6a482bc67e5?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 2
                        },
                        new ServiceItem
                        {
                            Name = "Event Photography (per hour)",
                            Description = "Professional event photography with 1 photographer. Includes edited photos delivered within 5 business days. Minimum 3 hours.",
                            ServiceType = "Event",
                            Price = 800m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1537633552985-df8429e8048b?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 3
                        },
                        new ServiceItem
                        {
                            Name = "Premium Photo Album",
                            Description = "Luxury leather-bound photo album with 30 pages of premium prints. Custom design layout with gold foil embossing.",
                            ServiceType = "AddOn",
                            Price = 2500m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1606216794079-73f85bbd57d5?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 4
                        }
                    },
                    "Lens Studio Photography" => new List<ServiceItem>
                    {
                        new ServiceItem
                        {
                            Name = "Artistic Wedding Storytelling",
                            Description = "Our signature photojournalistic wedding coverage: 2 photographers capturing candid moments and artistic portraits. 10+ hours, 600+ photos, drone aerial shots included.",
                            ServiceType = "Wedding",
                            Price = 9500m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1606216794079-73f85bbd57d5?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 1
                        },
                        new ServiceItem
                        {
                            Name = "360-Degree Photo Booth",
                            Description = "Interactive 360-degree photo booth with instant sharing to social media. Includes operator, props, and custom overlay branding. 4-hour rental.",
                            ServiceType = "AddOn",
                            Price = 3000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1537633552985-df8429e8048b?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 2
                        },
                        new ServiceItem
                        {
                            Name = "Pre-Wedding Shoot",
                            Description = "Outdoor pre-wedding photoshoot at premium locations in Riyadh. 3 hours, 100+ edited photos, 2 outfit changes. Hair and makeup coordination available.",
                            ServiceType = "PreWedding",
                            Price = 4000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 3
                        }
                    },

                    // ENTERTAINMENT VENDORS
                    "Melody Entertainment" => new List<ServiceItem>
                    {
                        new ServiceItem
                        {
                            Name = "DJ & Sound System Package",
                            Description = "Professional DJ with premium sound system (JBL/Bose), lighting rig, and fog machine. 5 hours of music including Arabic, khaleeji, and international tracks. Includes wireless microphones for speeches.",
                            ServiceType = "Entertainment",
                            Price = 6000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 1
                        },
                        new ServiceItem
                        {
                            Name = "Live Band Performance",
                            Description = "5-piece live band performing Arabic classics, khaleeji songs, and modern hits. 4 hours of live music with professional sound equipment included.",
                            ServiceType = "Entertainment",
                            Price = 9000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 2
                        },
                        new ServiceItem
                        {
                            Name = "Traditional Samer Dance Troupe",
                            Description = "10-member traditional Saudi Samer dance troupe with drummers. Authentic ceremonial performance for the groom's entrance. 2 hours.",
                            ServiceType = "Traditional",
                            Price = 5000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 3
                        },
                        new ServiceItem
                        {
                            Name = "Fireworks Display",
                            Description = "Professional 5-minute fireworks display to close your celebration. Includes safety team and coordination with venue. Subject to venue approval.",
                            ServiceType = "AddOn",
                            Price = 8000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1498931299472-f7a63a5a1cfa?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 4
                        }
                    },

                    // TRANSPORTATION VENDORS
                    "Al Safwa Transportation" => new List<ServiceItem>
                    {
                        new ServiceItem
                        {
                            Name = "Bridal Rolls-Royce Phantom",
                            Description = "White Rolls-Royce Phantom with chauffeur in formal attire. Includes custom floral decoration, red carpet, and 4-hour rental. Complimentary champagne glasses and bottled water.",
                            ServiceType = "Wedding",
                            Price = 5000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1549317661-bd32c8ce0afa?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 1
                        },
                        new ServiceItem
                        {
                            Name = "Mercedes S-Class VIP",
                            Description = "Black Mercedes S-Class with professional chauffeur. 4-hour rental with complimentary water and tissues. Ideal for VIP guests and groom transport.",
                            ServiceType = "VIP",
                            Price = 2500m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1503376780353-7e6692767b70?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 2
                        },
                        new ServiceItem
                        {
                            Name = "Guest Shuttle Service (per bus)",
                            Description = "Luxury 30-seat shuttle bus for guest transportation between venue and hotel. Includes driver and 6-hour service. Multiple trip coordination available.",
                            ServiceType = "Group",
                            Price = 3000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 3
                        },
                        new ServiceItem
                        {
                            Name = "Vintage Classic Car",
                            Description = "Restored 1960s vintage convertible for wedding photos and short rides. Includes decoration and 2-hour rental. Perfect for outdoor photo sessions.",
                            ServiceType = "Photography",
                            Price = 3500m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1549317661-bd32c8ce0afa?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 4
                        }
                    },

                    // FLORAL DECORATION VENDOR
                    "Noor Floral Design" => new List<ServiceItem>
                    {
                        new ServiceItem
                        {
                            Name = "Bridal Bouquet & Boutonniere",
                            Description = "Luxury hand-tied bridal bouquet with premium roses, orchids, and trailing greenery. Includes matching groom's boutonniere and 2 bridesmaid posies.",
                            ServiceType = "Wedding",
                            Price = 1500m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 1
                        },
                        new ServiceItem
                        {
                            Name = "Full Venue Floral Package",
                            Description = "Complete venue floral decoration: entrance arch, aisle arrangements, stage florals, 20 table centerpieces, and suspended floral chandelier. Premium imported flowers.",
                            ServiceType = "Wedding",
                            Price = 15000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1490750967868-88aa4f44baee?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 2
                        },
                        new ServiceItem
                        {
                            Name = "Flower Wall Installation",
                            Description = "Stunning flower wall (4m x 3m) with fresh roses, peonies, and hydrangeas. Setup and removal included. Available in white, blush, or custom color schemes.",
                            ServiceType = "AddOn",
                            Price = 5000m,
                            VendorId = vendor.Id,
                            VendorTypeId = vendor.VendorTypeId,
                            ImageUrl = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?w=600",
                            IsAvailable = true,
                            IsActive = true,
                            SortOrder = 3
                        }
                    },

                    _ => new List<ServiceItem>()
                };

                serviceItems.AddRange(items);
            }

            if (serviceItems.Count > 0)
            {
                await context.ServiceItems.AddRangeAsync(serviceItems);
                await context.SaveChangesAsync();
                Console.WriteLine($"[SeedVendors] Seeded {serviceItems.Count} service items across {savedVendors.Count} vendors");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedVendors] Error seeding service items: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    // ===================================================================
    // LINK VENDORS TO ORGANIZATIONS
    // ===================================================================

    /// <summary>
    /// Links vendors to their respective VendorManagement organizations.
    /// Distribution:
    ///   - Vendors 1, 6, 8 -> "Luxury Decor & Design" (noura.dosari)
    ///   - Vendors 2, 3, 7 -> "Elite Catering & Events" (khalid.otaibi)
    ///   - Vendors 4, 5, 9 -> "Premium Vendors Co." (vendorowner.demo)
    /// </summary>
    private static async Task LinkVendorsToOrganizations(DataContext context)
    {
        try
        {
            var vendorOrgs = await context.Organizations
                .Where(o => o.Type == "VendorManagement" && o.IsActive)
                .OrderBy(o => o.Id)
                .ToListAsync();

            if (vendorOrgs.Count == 0)
            {
                Console.WriteLine("[SeedVendors] No vendor organizations found, skipping vendor-org linkage");
                return;
            }

            var unlinkedVendors = await context.Vendors
                .Where(v => v.OrganizationId == null)
                .OrderBy(v => v.Id)
                .ToListAsync();

            if (unlinkedVendors.Count == 0)
            {
                Console.WriteLine("[SeedVendors] All vendors already linked to organizations");
                return;
            }

            // Distribute vendors round-robin among vendor organizations
            for (int i = 0; i < unlinkedVendors.Count; i++)
            {
                var org = vendorOrgs[i % vendorOrgs.Count];
                unlinkedVendors[i].OrganizationId = org.Id;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedVendors] Linked {unlinkedVendors.Count} vendors to {vendorOrgs.Count} vendor organizations");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedVendors] Error linking vendors to organizations: {ex.Message}");
        }
    }

    // ===================================================================
    // VENDOR MANAGER ENTITIES (team members only)
    // ===================================================================

    /// <summary>
    /// Creates VendorManager entities for team members ONLY (AppRoles.VendorManager).
    /// Organization owners (AppRoles.VendorOrganizationManager) do NOT get VendorManager
    /// entities -- they access vendors through their Organization -> Vendor relationship.
    /// </summary>
    private static async Task CreateVendorManagerEntities(DataContext context, UserManager<AppUser> userManager)
    {
        try
        {
            Console.WriteLine("[SeedVendors] Creating vendor manager entities (team members only)...");

            var vendors = await context.Vendors.OrderBy(v => v.Id).ToListAsync();
            if (vendors.Count < 2)
            {
                Console.WriteLine("[SeedVendors] Not enough vendors to assign to vendor managers");
                return;
            }

            var vendorManagerDemo = await userManager.FindByNameAsync("vendormanager.demo");

            // Create VendorManager entity ONLY for the team member (vendormanager@example.com).
            // Organization owners (khalid.otaibi, noura.dosari, vendorowner.demo) do NOT get
            // VendorManager entities -- they access vendors via Organization ownership.
            if (vendorManagerDemo != null)
            {
                // Assign first 2 vendors so this team member can manage them
                var teamMemberVendors = vendors.Take(2).ToList();
                var teamMemberManager = new VendorManager
                {
                    AppUserId = vendorManagerDemo.Id,
                    CreatedAt = DateTime.UtcNow,
                    Vendors = teamMemberVendors
                };
                context.VendorManagers.Add(teamMemberManager);
                Console.WriteLine($"[SeedVendors] Created VendorManager entity for team member (vendormanager@example.com) with {teamMemberVendors.Count} vendors");
            }

            await context.SaveChangesAsync();
            Console.WriteLine("[SeedVendors] VendorManager entities seeded successfully (team members only)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeedVendors] Error creating vendor managers: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}

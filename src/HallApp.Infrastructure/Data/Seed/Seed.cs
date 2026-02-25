using System.Text.Json;
using HallApp.Core.Constants;
using HallApp.Core.Entities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.ChamperEntities.ContactEntities;
using HallApp.Core.Entities.ChamperEntities.LocationEntities;
using HallApp.Core.Entities.ChamperEntities.MediaEntities;
using HallApp.Core.Entities.ChamperEntities.PackageEntities;
using HallApp.Core.Entities.ChamperEntities.ServiceEntities;
using HallApp.Core.Entities.VendorEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HallApp.Infrastructure.Data.Seed;

public class Seed
{
    /// <summary>
    /// Resolves the path to a seed data JSON file in the Data/ directory at the repo root.
    /// </summary>
    public static string GetSeedDataPath(string filename)
    {
        // Data/ folder is at the repo root: hall-booking-application-backend/Data/
        // dotnet run CWD = src/HallApp.Web, BaseDirectory = src/HallApp.Web/bin/Debug/net8.0/
        var cwd = Directory.GetCurrentDirectory();
        var baseDir = AppContext.BaseDirectory;

        var candidates = new[]
        {
            // From CWD (src/HallApp.Web) -> go up 2 levels to repo root
            Path.GetFullPath(Path.Combine(cwd, "..", "..", "Data", filename)),
            // From BaseDirectory (bin/Debug/net8.0/) -> go up 5 levels to repo root
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "Data", filename)),
            // From CWD if running from repo root
            Path.GetFullPath(Path.Combine(cwd, "Data", filename)),
            // From BaseDirectory if published
            Path.GetFullPath(Path.Combine(baseDir, "Data", filename)),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"[Seed] Found seed file: {path}");
                return path;
            }
        }

        // Fallback: return the first candidate (will fail gracefully later)
        Console.WriteLine($"[Seed] WARNING: Seed file '{filename}' not found in any known location");
        return candidates[0];
    }

    public static async Task SeedInformation(
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

        Console.WriteLine("[Seed] Starting database seeding...");

        // ---------------------------------------------------------------
        // 1. Seed roles (idempotent -- only creates missing roles)
        // ---------------------------------------------------------------
        await SeedRoles(roleManager);

        // ---------------------------------------------------------------
        // 2. Seed users -- skip everything below if users already exist
        // ---------------------------------------------------------------
        if (await userManager.Users.AnyAsync())
        {
            Console.WriteLine("[Seed] Users already exist, skipping user creation");

            // Even if users exist, ensure organizations + halls are seeded
            await SeedOrganizationsForExistingUsers(userManager, context);
            await SeedHalls(context);
            return;
        }

        // -- Admin ---
        await SeedAdminUser(userManager);

        // -- Customers ---
        await SeedCustomerUsers(userManager);

        // -- HallOrganizationManager users (organization owners for halls) ---
        await SeedHallOrganizationManagerUsers(userManager);

        // -- VendorOrganizationManager users (organization owners for vendors) ---
        await SeedVendorOrganizationManagerUsers(userManager);

        // -- HallManager team member (assigned to a hall organization) ---
        await SeedHallManagerTeamMemberUser(userManager);

        // -- VendorManager team member (assigned to a vendor organization) ---
        await SeedVendorManagerTeamMemberUser(userManager);

        Console.WriteLine("[Seed] All users created successfully");

        // ---------------------------------------------------------------
        // 3. Create Organizations + OrganizationMembers (NO hall linkage yet)
        // ---------------------------------------------------------------
        await SeedOrganizationsAndMembers(userManager, context);

        // ---------------------------------------------------------------
        // 4. Create Halls programmatically, linked to their organizations
        // ---------------------------------------------------------------
        await SeedHalls(context);

        // ---------------------------------------------------------------
        // 5. Create HallManager entities (the join table between user and halls)
        // ---------------------------------------------------------------
        await SeedHallManagerEntities(userManager, context);
    }

    // ===================================================================
    // ROLES
    // ===================================================================

    private static async Task SeedRoles(RoleManager<AppRole> roleManager)
    {
        var roles = new List<AppRole>
        {
            new AppRole { Name = AppRoles.Admin },
            new AppRole { Name = AppRoles.Moderator },
            new AppRole { Name = AppRoles.HallOrganizationManager },
            new AppRole { Name = AppRoles.VendorOrganizationManager },
            new AppRole { Name = AppRoles.HallManager },
            new AppRole { Name = AppRoles.VendorManager },
            new AppRole { Name = AppRoles.Customer },
        };

        var createdCount = 0;
        foreach (var role in roles)
        {
            // Only create role if it doesn't already exist (idempotent)
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                await roleManager.CreateAsync(role);
                createdCount++;
            }
        }

        if (createdCount > 0)
        {
            Console.WriteLine($"[Seed] Created {createdCount} missing roles");
        }
        else
        {
            Console.WriteLine("[Seed] All roles already exist");
        }
    }

    // ===================================================================
    // HALLS (programmatic seeding with valid FK references)
    // ===================================================================

    /// <summary>
    /// Creates halls programmatically with proper organization linkage.
    /// Distribution:
    ///   - Halls 1-3 -> "Grand Hall Events Company" (ahmed.mansour)
    ///   - Halls 4-5 -> "Royal Palace Events" (sara.zahrani)
    ///   - Halls 6-9 -> "Elite Halls Management" (hallowner.demo)
    /// This ensures every hall has a valid OrganizationId FK.
    /// </summary>
    private static async Task SeedHalls(DataContext context)
    {
        if (await context.Halls.AnyAsync())
        {
            Console.WriteLine("[Seed] Halls already exist, skipping hall creation");
            return;
        }

        // Load hall organizations by name to get their IDs
        var grandHallOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Name == "Grand Hall Events Company");
        var royalPalaceOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Name == "Royal Palace Events");
        var eliteHallsOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Name == "Elite Halls Management");
        var diamondHallsOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Name == "Diamond Halls Group");

        if (grandHallOrg == null || royalPalaceOrg == null || eliteHallsOrg == null)
        {
            var missing = new List<string>();
            if (grandHallOrg == null) missing.Add("Grand Hall Events Company");
            if (royalPalaceOrg == null) missing.Add("Royal Palace Events");
            if (eliteHallsOrg == null) missing.Add("Elite Halls Management");
            Console.WriteLine($"[Seed] Missing hall organizations ({string.Join(", ", missing)}), skipping hall creation");
            return;
        }

        try
        {
            var hallDefinitions = GetHallDefinitions(grandHallOrg, royalPalaceOrg, eliteHallsOrg, diamondHallsOrg);

            foreach (var hall in hallDefinitions)
            {
                context.Halls.Add(hall);
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"[Seed] Created {hallDefinitions.Count} halls with organization linkage");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seed] ERROR seeding halls: {ex.Message}");
            Console.WriteLine($"[Seed] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Returns the full list of hall definitions with contacts, locations, packages,
    /// services, and media. Each hall is pre-assigned to an organization.
    /// </summary>
    private static List<Hall> GetHallDefinitions(
        Organization? grandHallOrg,
        Organization? royalPalaceOrg,
        Organization? eliteHallsOrg,
        Organization? diamondHallsOrg)
    {
        // ---- Hall 1: Qasr Al Ahlam (Grand Hall Events) ----
        var hall1 = new Hall
        {
            Name = "Qasr Al Ahlam",
            CommercialRegistration = 1010234567,
            Vat = 300123456789,
            IsActive = true,
            IsFeatured = true,
            IsPremium = true,
            HasSpecialOffer = false,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Qasr Al Ahlam, the Palace of Dreams, is Jeddah's most sought-after wedding venue. Featuring soaring crystal chandeliers, hand-carved marble columns, and lush indoor gardens, the venue seamlessly blends traditional Arabian elegance with contemporary luxury. The grand ballroom accommodates up to 800 guests across dedicated male and female sections.",
            BothWeekDays = 15000.0,
            BothWeekEnds = 20000.0,
            MaleWeekDays = 8000.0,
            MaleWeekEnds = 12000.0,
            MaleMin = 200,
            MaleMax = 800,
            MaleActive = true,
            FemaleWeekDays = 8500.0,
            FemaleWeekEnds = 12500.0,
            FemaleMin = 200,
            FemaleMax = 800,
            FemaleActive = true,
            Gender = 3,
            Email = "info@qasralahlam.sa",
            Phone = "+966501234567",
            WhatsApp = "+966501234567",
            Logo = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=100",
            AverageRating = 4.8,
            OrganizationId = grandHallOrg?.Id,
            Location = new Location
            {
                Altitude = 12.0,
                Latitude = 21.543333,
                Longitude = 39.172778,
                City = "Jeddah",
                State = "Makkah Region",
                Address = "Al Hamra District, King Abdullah Road"
            },
            Contacts =
            [
                new Contact { Name = "Fahad Al-Rashidi", Email = "fahad@qasralahlam.sa", Phone = "+966501234567" },
                new Contact { Name = "Noura Al-Shammari", Email = "noura@qasralahlam.sa", Phone = "+966509876543" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=800", index = 1, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1507504031003-b417219a0fde?w=800", index = 2, Gender = 2 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?w=800", index = 3, Gender = 1 }
            ],
            Packages =
            [
                new Package { Name = "Essential", Description = "Standard decoration, basic lighting, sound system, and event coordinator.", Price = 3000.0, IsActive = true },
                new Package { Name = "Silver", Description = "Enhanced floral arrangements, premium lighting, photography coverage.", Price = 5500.0, IsActive = true },
                new Package { Name = "Gold", Description = "Luxury decoration, cinematic videography, live oud player, gourmet dinner.", Price = 8000.0, IsActive = true },
                new Package { Name = "Platinum", Description = "All-inclusive: couture decoration, international chef, drone photography, fireworks.", Price = 12000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Catering", ArabicName = "تموين", IsActive = true },
                new Service { Name = "Photography", ArabicName = "تصوير", IsActive = true },
                new Service { Name = "Decoration", ArabicName = "ديكور", IsActive = true },
                new Service { Name = "Valet Parking", ArabicName = "خدمة صف السيارات", IsActive = true }
            ]
        };

        // ---- Hall 2: Al Mamlaka Hall (Grand Hall Events) ----
        var hall2 = new Hall
        {
            Name = "Al Mamlaka Hall",
            CommercialRegistration = 1010345678,
            Vat = 300234567890,
            IsActive = true,
            IsFeatured = true,
            IsPremium = true,
            HasSpecialOffer = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Al Mamlaka Hall stands as Riyadh's crown jewel of wedding venues. Situated in the prestigious King Abdullah Financial District, this magnificent venue features a 1,000-guest grand ballroom with a retractable ceiling.",
            BothWeekDays = 18000.0,
            BothWeekEnds = 25000.0,
            MaleWeekDays = 10000.0,
            MaleWeekEnds = 15000.0,
            MaleMin = 300,
            MaleMax = 1000,
            MaleActive = true,
            FemaleWeekDays = 10500.0,
            FemaleWeekEnds = 15500.0,
            FemaleMin = 300,
            FemaleMax = 1000,
            FemaleActive = true,
            Gender = 3,
            Email = "info@almamlaka.sa",
            Phone = "+966551234567",
            WhatsApp = "+966551234567",
            Logo = "https://images.unsplash.com/photo-1549488344-1f9b8d2bd1f3?w=100",
            AverageRating = 4.6,
            OrganizationId = grandHallOrg?.Id,
            Location = new Location
            {
                Altitude = 612.0,
                Latitude = 24.711667,
                Longitude = 46.674167,
                City = "Riyadh",
                State = "Riyadh Region",
                Address = "King Abdullah Financial District, KAFD"
            },
            Contacts =
            [
                new Contact { Name = "Sultan Al-Otaibi", Email = "sultan@almamlaka.sa", Phone = "+966551234567" },
                new Contact { Name = "Reem Al-Dosari", Email = "reem@almamlaka.sa", Phone = "+966559876543" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1549488344-1f9b8d2bd1f3?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=800", index = 1, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=800", index = 2, Gender = 2 }
            ],
            Packages =
            [
                new Package { Name = "Classic", Description = "Elegant decoration, professional sound system, basic lighting.", Price = 4000.0, IsActive = true },
                new Package { Name = "Premium", Description = "Enhanced decoration, photography, live music, gourmet dinner.", Price = 7000.0, IsActive = true },
                new Package { Name = "Royal", Description = "Full luxury: couture decoration, international chef, cinematic videography.", Price = 11000.0, IsActive = true },
                new Package { Name = "Imperial", Description = "Ultimate package: fireworks, drone show, celebrity entertainment.", Price = 16000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Gourmet Catering", ArabicName = "تموين فاخر", IsActive = true },
                new Service { Name = "Professional Photography", ArabicName = "تصوير احترافي", IsActive = true },
                new Service { Name = "Luxury Decoration", ArabicName = "ديكور فاخر", IsActive = true },
                new Service { Name = "Live Music", ArabicName = "موسيقى حية", IsActive = true }
            ]
        };

        // ---- Hall 3: Dar Al Uroubah (Grand Hall Events) ----
        var hall3 = new Hall
        {
            Name = "Dar Al Uroubah",
            CommercialRegistration = 4030123456,
            Vat = 300345678901,
            IsActive = true,
            IsFeatured = false,
            IsPremium = false,
            HasSpecialOffer = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Dar Al Uroubah offers an authentic Arabian wedding experience in the holy city of Makkah. This men's-only venue is designed in the traditional Hijazi architectural style.",
            BothWeekDays = 0.0,
            BothWeekEnds = 0.0,
            MaleWeekDays = 3000.0,
            MaleWeekEnds = 4500.0,
            MaleMin = 100,
            MaleMax = 400,
            MaleActive = true,
            FemaleWeekDays = 0.0,
            FemaleWeekEnds = 0.0,
            FemaleMin = 0,
            FemaleMax = 0,
            FemaleActive = false,
            Gender = 1,
            Email = "info@daruroubah.sa",
            Phone = "+966561234567",
            WhatsApp = "+966561234567",
            Logo = "https://images.unsplash.com/photo-1520854221256-17451cc331bf?w=100",
            AverageRating = 4.2,
            OrganizationId = grandHallOrg?.Id,
            Location = new Location
            {
                Altitude = 277.0,
                Latitude = 21.422510,
                Longitude = 39.826168,
                City = "Makkah",
                State = "Makkah Region",
                Address = "Al Aziziyah District, Ibrahim Al Khalil Road"
            },
            Contacts =
            [
                new Contact { Name = "Abdullah Al-Zahrani", Email = "abdullah@daruroubah.sa", Phone = "+966561234567" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1520854221256-17451cc331bf?w=800", index = 0, Gender = 1 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1469371670807-013ccf25f16a?w=800", index = 1, Gender = 1 }
            ],
            Packages =
            [
                new Package { Name = "Standard", Description = "Basic decoration, traditional lighting, sound system.", Price = 1500.0, IsActive = true },
                new Package { Name = "Plus", Description = "Enhanced decoration with Arabian touches, photography, premium catering.", Price = 2500.0, IsActive = true },
                new Package { Name = "Premium", Description = "Full decoration, live oud music, gourmet traditional feast.", Price = 4000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Traditional Catering", ArabicName = "تموين تقليدي", IsActive = true },
                new Service { Name = "Decoration", ArabicName = "ديكور", IsActive = true },
                new Service { Name = "Sound System", ArabicName = "نظام صوتي", IsActive = true }
            ]
        };

        // ---- Hall 4: Layali Al Ward (Royal Palace Events) ----
        var hall4 = new Hall
        {
            Name = "Layali Al Ward",
            CommercialRegistration = 4650234567,
            Vat = 300456789012,
            IsActive = true,
            IsFeatured = true,
            IsPremium = false,
            HasSpecialOffer = false,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Layali Al Ward -- Nights of Roses -- is Madinah's premier ladies-only wedding venue, a sanctuary of feminine elegance and floral beauty.",
            BothWeekDays = 0.0,
            BothWeekEnds = 0.0,
            MaleWeekDays = 0.0,
            MaleWeekEnds = 0.0,
            MaleMin = 0,
            MaleMax = 0,
            MaleActive = false,
            FemaleWeekDays = 5000.0,
            FemaleWeekEnds = 7500.0,
            FemaleMin = 150,
            FemaleMax = 600,
            FemaleActive = true,
            Gender = 2,
            Email = "info@layalialward.sa",
            Phone = "+966571234567",
            WhatsApp = "+966571234567",
            Logo = "https://images.unsplash.com/photo-1522413452208-996ff3f3e740?w=100",
            AverageRating = 4.7,
            OrganizationId = royalPalaceOrg?.Id,
            Location = new Location
            {
                Altitude = 608.0,
                Latitude = 24.470901,
                Longitude = 39.612236,
                City = "Madinah",
                State = "Madinah Region",
                Address = "Al Khalidiyah District, Prince Abdulmajeed Road"
            },
            Contacts =
            [
                new Contact { Name = "Sara Al-Anazi", Email = "sara@layalialward.sa", Phone = "+966571234567" },
                new Contact { Name = "Maha Al-Mutairi", Email = "maha@layalialward.sa", Phone = "+966579876543" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1522413452208-996ff3f3e740?w=800", index = 0, Gender = 2 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1470784790053-6c2f15669781?w=800", index = 1, Gender = 2 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1510076857177-7470076d4098?w=800", index = 2, Gender = 2 }
            ],
            Packages =
            [
                new Package { Name = "Rose", Description = "Elegant floral decoration, basic lighting, event coordination.", Price = 2500.0, IsActive = true },
                new Package { Name = "Orchid", Description = "Premium floral design, photography, bridal preparation suite.", Price = 4500.0, IsActive = true },
                new Package { Name = "Jasmine", Description = "Ultimate bridal experience: luxury decoration, videography, gourmet dinner.", Price = 7000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Catering", ArabicName = "تموين", IsActive = true },
                new Service { Name = "Photography", ArabicName = "تصوير", IsActive = true },
                new Service { Name = "Makeup Artist", ArabicName = "فنانة مكياج", IsActive = true },
                new Service { Name = "Henna Artist", ArabicName = "فنانة حناء", IsActive = true }
            ]
        };

        // ---- Hall 5: Al Firdaws Ballroom (Royal Palace Events) ----
        var hall5 = new Hall
        {
            Name = "Al Firdaws Ballroom",
            CommercialRegistration = 1010456789,
            Vat = 300567890123,
            IsActive = true,
            IsFeatured = false,
            IsPremium = false,
            HasSpecialOffer = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Al Firdaws Ballroom is a welcoming, mid-range venue in central Jeddah perfect for intimate weddings and family celebrations.",
            BothWeekDays = 4500.0,
            BothWeekEnds = 6000.0,
            MaleWeekDays = 2500.0,
            MaleWeekEnds = 3500.0,
            MaleMin = 80,
            MaleMax = 300,
            MaleActive = true,
            FemaleWeekDays = 2800.0,
            FemaleWeekEnds = 3800.0,
            FemaleMin = 80,
            FemaleMax = 300,
            FemaleActive = true,
            Gender = 3,
            Email = "info@alfirdaws.sa",
            Phone = "+966581234567",
            WhatsApp = "+966581234567",
            Logo = "https://images.unsplash.com/photo-1519225421980-715cb0215aed?w=100",
            AverageRating = 4.0,
            OrganizationId = royalPalaceOrg?.Id,
            Location = new Location
            {
                Altitude = 15.0,
                Latitude = 21.485811,
                Longitude = 39.185219,
                City = "Jeddah",
                State = "Makkah Region",
                Address = "Al Andalus District, Prince Sultan Road"
            },
            Contacts =
            [
                new Contact { Name = "Omar Al-Juhani", Email = "omar@alfirdaws.sa", Phone = "+966581234567" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1519225421980-715cb0215aed?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1465495976277-4387d4b0b4c6?w=800", index = 1, Gender = 3 }
            ],
            Packages =
            [
                new Package { Name = "Basic", Description = "Simple decoration, sound system, event coordinator.", Price = 1200.0, IsActive = true },
                new Package { Name = "Standard", Description = "Enhanced decoration, photography, premium sound, basic catering.", Price = 2200.0, IsActive = true },
                new Package { Name = "Deluxe", Description = "Full decoration, photography, DJ, complete catering.", Price = 3500.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Catering", ArabicName = "تموين", IsActive = true },
                new Service { Name = "Decoration", ArabicName = "ديكور", IsActive = true },
                new Service { Name = "Sound System", ArabicName = "نظام صوتي", IsActive = true }
            ]
        };

        // ---- Hall 6: Qasr Al Nokhba (Elite Halls Management) ----
        var hall6 = new Hall
        {
            Name = "Qasr Al Nokhba",
            CommercialRegistration = 1010567890,
            Vat = 300678901234,
            IsActive = true,
            IsFeatured = true,
            IsPremium = true,
            HasSpecialOffer = false,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Qasr Al Nokhba -- the Elite Palace -- represents the pinnacle of luxury wedding venues in Saudi Arabia. Located in Riyadh's exclusive diplomatic quarter.",
            BothWeekDays = 22000.0,
            BothWeekEnds = 30000.0,
            MaleWeekDays = 12000.0,
            MaleWeekEnds = 18000.0,
            MaleMin = 250,
            MaleMax = 900,
            MaleActive = true,
            FemaleWeekDays = 12500.0,
            FemaleWeekEnds = 18500.0,
            FemaleMin = 250,
            FemaleMax = 900,
            FemaleActive = true,
            Gender = 3,
            Email = "info@qasralnokhba.sa",
            Phone = "+966500112233",
            WhatsApp = "+966500112233",
            Logo = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=100",
            AverageRating = 4.9,
            OrganizationId = eliteHallsOrg?.Id,
            Location = new Location
            {
                Altitude = 620.0,
                Latitude = 24.690000,
                Longitude = 46.680000,
                City = "Riyadh",
                State = "Riyadh Region",
                Address = "Diplomatic Quarter, Embassy Row"
            },
            Contacts =
            [
                new Contact { Name = "Lama Al-Faisal", Email = "lama@qasralnokhba.sa", Phone = "+966500445566" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1582268611958-ebfd161ef9cf?w=800", index = 1, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800", index = 2, Gender = 2 }
            ],
            Packages =
            [
                new Package { Name = "Signature", Description = "Premium decoration, photography, luxury sound, personal planner.", Price = 6000.0, IsActive = true },
                new Package { Name = "Elite", Description = "Luxury decoration, cinematic videography, live orchestra, gourmet dinner.", Price = 10000.0, IsActive = true },
                new Package { Name = "Royal Suite", Description = "All-inclusive luxury: Michelin chef dinner, drone photography, bridal wing.", Price = 15000.0, IsActive = true },
                new Package { Name = "Grand Imperial", Description = "The ultimate: fireworks, fountain show, helicopter arrival, celebrity entertainment.", Price = 22000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "International Catering", ArabicName = "تموين عالمي", IsActive = true },
                new Service { Name = "Cinematic Photography", ArabicName = "تصوير سينمائي", IsActive = true },
                new Service { Name = "Couture Decoration", ArabicName = "ديكور راقي", IsActive = true },
                new Service { Name = "Live Orchestra", ArabicName = "أوركسترا حية", IsActive = true }
            ]
        };

        // ---- Hall 7: Rawdat Al Zahra (Elite Halls Management) ----
        var hall7 = new Hall
        {
            Name = "Rawdat Al Zahra",
            CommercialRegistration = 4650345678,
            Vat = 300789012345,
            IsActive = true,
            IsFeatured = false,
            IsPremium = true,
            HasSpecialOffer = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Rawdat Al Zahra -- the Flower Garden -- is an enchanting ladies-only venue nestled in Madinah's most beautiful district.",
            BothWeekDays = 0.0,
            BothWeekEnds = 0.0,
            MaleWeekDays = 0.0,
            MaleWeekEnds = 0.0,
            MaleMin = 0,
            MaleMax = 0,
            MaleActive = false,
            FemaleWeekDays = 4000.0,
            FemaleWeekEnds = 6000.0,
            FemaleMin = 100,
            FemaleMax = 500,
            FemaleActive = true,
            Gender = 2,
            Email = "info@rawdatalzahra.sa",
            Phone = "+966531234567",
            WhatsApp = "+966531234567",
            Logo = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?w=100",
            AverageRating = 4.5,
            OrganizationId = eliteHallsOrg?.Id,
            Location = new Location
            {
                Altitude = 608.0,
                Latitude = 24.467500,
                Longitude = 39.610000,
                City = "Madinah",
                State = "Madinah Region",
                Address = "Al Rawdah District, Abu Bakr Al Siddiq Road"
            },
            Contacts =
            [
                new Contact { Name = "Fatimah Al-Harbi", Email = "fatimah@rawdatalzahra.sa", Phone = "+966531234567" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?w=800", index = 0, Gender = 2 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1490750967868-88aa4f44baee?w=800", index = 1, Gender = 2 }
            ],
            Packages =
            [
                new Package { Name = "Garden", Description = "Beautiful floral decoration, ambient lighting, event coordination.", Price = 2000.0, IsActive = true },
                new Package { Name = "Blossom", Description = "Premium flower walls, photography, bridal preparation room.", Price = 3800.0, IsActive = true },
                new Package { Name = "Paradise", Description = "Full luxury: cascading floral design, cinematic videography, gourmet dinner.", Price = 6000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Catering", ArabicName = "تموين", IsActive = true },
                new Service { Name = "Decoration", ArabicName = "ديكور", IsActive = true },
                new Service { Name = "Photography", ArabicName = "تصوير", IsActive = true },
                new Service { Name = "Floral Arrangements", ArabicName = "تنسيق زهور", IsActive = true }
            ]
        };

        // ---- Hall 8: Crystal Garden Hall (Elite Halls Management) ----
        var hall8 = new Hall
        {
            Name = "Crystal Garden Hall",
            CommercialRegistration = 1010678901,
            Vat = 300901234567,
            IsActive = true,
            IsFeatured = true,
            IsPremium = false,
            HasSpecialOffer = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Crystal Garden Hall brings a fresh, contemporary aesthetic to Jeddah's wedding scene with its stunning glass atrium and tropical plants.",
            BothWeekDays = 6500.0,
            BothWeekEnds = 9000.0,
            MaleWeekDays = 3500.0,
            MaleWeekEnds = 5000.0,
            MaleMin = 120,
            MaleMax = 450,
            MaleActive = true,
            FemaleWeekDays = 3800.0,
            FemaleWeekEnds = 5300.0,
            FemaleMin = 120,
            FemaleMax = 450,
            FemaleActive = true,
            Gender = 3,
            Email = "info@crystalgarden.sa",
            Phone = "+966591234567",
            WhatsApp = "+966591234567",
            Logo = "https://images.unsplash.com/photo-1519741497674-611481863552?w=100",
            AverageRating = 4.3,
            OrganizationId = eliteHallsOrg?.Id,
            Location = new Location
            {
                Altitude = 10.0,
                Latitude = 21.520000,
                Longitude = 39.162000,
                City = "Jeddah",
                State = "Makkah Region",
                Address = "Al Rawdah District, Tahlia Street"
            },
            Contacts =
            [
                new Contact { Name = "Tariq Al-Balawi", Email = "tariq@crystalgarden.sa", Phone = "+966591234567" },
                new Contact { Name = "Dina Al-Otaibi", Email = "dina@crystalgarden.sa", Phone = "+966599876543" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1519741497674-611481863552?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1513278974582-3e1b4c4e3a64?w=800", index = 1, Gender = 3 }
            ],
            Packages =
            [
                new Package { Name = "Crystal", Description = "Modern decoration, professional lighting, sound system.", Price = 2000.0, IsActive = true },
                new Package { Name = "Diamond", Description = "Premium decoration with crystal installations, photography, DJ.", Price = 4000.0, IsActive = true },
                new Package { Name = "Emerald", Description = "Full luxury: garden party setup, cinematic videography, live band.", Price = 6500.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Catering", ArabicName = "تموين", IsActive = true },
                new Service { Name = "Photography", ArabicName = "تصوير", IsActive = true },
                new Service { Name = "Decoration", ArabicName = "ديكور", IsActive = true },
                new Service { Name = "Chocolate Fountain", ArabicName = "نافورة شوكولاتة", IsActive = true }
            ]
        };

        // ---- Hall 9: Al Burj Grand Ballroom (Elite Halls Management) ----
        var hall9 = new Hall
        {
            Name = "Al Burj Grand Ballroom",
            CommercialRegistration = 1010789012,
            Vat = 301012345678,
            IsActive = true,
            IsFeatured = false,
            IsPremium = true,
            HasSpecialOffer = false,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Al Burj Grand Ballroom occupies the top three floors of one of Riyadh's most iconic towers, offering panoramic city views through floor-to-ceiling windows.",
            BothWeekDays = 13000.0,
            BothWeekEnds = 18000.0,
            MaleWeekDays = 7000.0,
            MaleWeekEnds = 10000.0,
            MaleMin = 200,
            MaleMax = 700,
            MaleActive = true,
            FemaleWeekDays = 7500.0,
            FemaleWeekEnds = 10500.0,
            FemaleMin = 200,
            FemaleMax = 700,
            FemaleActive = true,
            Gender = 3,
            Email = "info@alburj.sa",
            Phone = "+966521234567",
            WhatsApp = "+966521234567",
            Logo = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?w=100",
            AverageRating = 4.4,
            OrganizationId = eliteHallsOrg?.Id,
            Location = new Location
            {
                Altitude = 625.0,
                Latitude = 24.700000,
                Longitude = 46.710000,
                City = "Riyadh",
                State = "Riyadh Region",
                Address = "Olaya District, King Fahad Road"
            },
            Contacts =
            [
                new Contact { Name = "Bandar Al-Tamimi", Email = "bandar@alburj.sa", Phone = "+966521234567" },
                new Contact { Name = "Nada Al-Subaie", Email = "nada@alburj.sa", Phone = "+966529876543" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1590381105924-c72589b9ef3f?w=800", index = 1, Gender = 3 }
            ],
            Packages =
            [
                new Package { Name = "Tower", Description = "Elegant decoration, city-view setup, professional sound and lighting.", Price = 3500.0, IsActive = true },
                new Package { Name = "Skyline", Description = "Premium decoration, panoramic photo session, live music, gourmet dinner.", Price = 6000.0, IsActive = true },
                new Package { Name = "Penthouse", Description = "Luxury rooftop experience: terrace dinner, cinematic videography, DJ.", Price = 9000.0, IsActive = true },
                new Package { Name = "Royal Summit", Description = "Ultimate tower experience: full venue buyout, celebrity chef, fireworks.", Price = 13000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Catering", ArabicName = "تموين", IsActive = true },
                new Service { Name = "Photography", ArabicName = "تصوير", IsActive = true },
                new Service { Name = "Decoration", ArabicName = "ديكور", IsActive = true },
                new Service { Name = "Valet Parking", ArabicName = "خدمة صف السيارات", IsActive = true },
                new Service { Name = "Fireworks Display", ArabicName = "عرض ألعاب نارية", IsActive = true }
            ]
        };

        // ---- Hall 10: Qasr Al Lulu (Royal Palace Events) ----
        var hall10 = new Hall
        {
            Name = "Qasr Al Lulu",
            CommercialRegistration = 4650456789,
            Vat = 301123456789,
            IsActive = true,
            IsFeatured = true,
            IsPremium = true,
            HasSpecialOffer = false,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Qasr Al Lulu -- the Pearl Palace -- is a stunning seaside venue in Dammam's Corniche district. With panoramic Arabian Gulf views and an open-air terrace, it offers a unique blend of indoor luxury and outdoor beauty.",
            BothWeekDays = 16000.0,
            BothWeekEnds = 22000.0,
            MaleWeekDays = 9000.0,
            MaleWeekEnds = 13000.0,
            MaleMin = 200,
            MaleMax = 700,
            MaleActive = true,
            FemaleWeekDays = 9500.0,
            FemaleWeekEnds = 13500.0,
            FemaleMin = 200,
            FemaleMax = 700,
            FemaleActive = true,
            Gender = 3,
            Email = "info@qasrallulu.sa",
            Phone = "+966531112233",
            WhatsApp = "+966531112233",
            Logo = "https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=100",
            AverageRating = 4.6,
            OrganizationId = royalPalaceOrg?.Id,
            Location = new Location
            {
                Altitude = 5.0,
                Latitude = 26.432222,
                Longitude = 50.101389,
                City = "Dammam",
                State = "Eastern Region",
                Address = "Corniche District, King Saud Street"
            },
            Contacts =
            [
                new Contact { Name = "Nawaf Al-Dawsari", Email = "nawaf@qasrallulu.sa", Phone = "+966531112233" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1519167758481-83f550bb49b3?w=800", index = 1, Gender = 3 }
            ],
            Packages =
            [
                new Package { Name = "Pearl", Description = "Seaside decoration, professional lighting, sound system.", Price = 4000.0, IsActive = true },
                new Package { Name = "Ocean", Description = "Premium beach-themed decor, photography, live music.", Price = 7500.0, IsActive = true },
                new Package { Name = "Royal Pearl", Description = "All-inclusive seafood dinner, fireworks over the Gulf, VIP service.", Price = 12000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Seafood Catering", ArabicName = "تموين مأكولات بحرية", IsActive = true },
                new Service { Name = "Photography", ArabicName = "تصوير", IsActive = true },
                new Service { Name = "Outdoor Decoration", ArabicName = "ديكور خارجي", IsActive = true }
            ]
        };

        // ---- Hall 11: Al Jawharah Ballroom (Diamond Halls Group) ----
        var hall11 = diamondHallsOrg != null ? new Hall
        {
            Name = "Al Jawharah Ballroom",
            CommercialRegistration = 1010890123,
            Vat = 301234567890,
            IsActive = true,
            IsFeatured = true,
            IsPremium = true,
            HasSpecialOffer = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Al Jawharah -- the Jewel -- is a premium ballroom in Riyadh's newest luxury hotel. Featuring Swarovski crystal installations, marble floors, and a dedicated bridal wing with private salon.",
            BothWeekDays = 20000.0,
            BothWeekEnds = 28000.0,
            MaleWeekDays = 11000.0,
            MaleWeekEnds = 16000.0,
            MaleMin = 250,
            MaleMax = 850,
            MaleActive = true,
            FemaleWeekDays = 11500.0,
            FemaleWeekEnds = 16500.0,
            FemaleMin = 250,
            FemaleMax = 850,
            FemaleActive = true,
            Gender = 3,
            Email = "info@aljawharah.sa",
            Phone = "+966541112233",
            WhatsApp = "+966541112233",
            Logo = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=100",
            AverageRating = 4.8,
            OrganizationId = diamondHallsOrg.Id,
            Location = new Location
            {
                Altitude = 615.0,
                Latitude = 24.725000,
                Longitude = 46.690000,
                City = "Riyadh",
                State = "Riyadh Region",
                Address = "Al-Sulimaniyah District, Al-Dabab Street"
            },
            Contacts =
            [
                new Contact { Name = "Abdulaziz Al-Faisal", Email = "abdulaziz@aljawharah.sa", Phone = "+966541112233" },
                new Contact { Name = "Sarah Al-Enezi", Email = "sarah@aljawharah.sa", Phone = "+966541445566" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1549488344-1f9b8d2bd1f3?w=800", index = 1, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?w=800", index = 2, Gender = 2 }
            ],
            Packages =
            [
                new Package { Name = "Diamond", Description = "Crystal-themed decoration, premium lighting, personal coordinator.", Price = 6000.0, IsActive = true },
                new Package { Name = "Sapphire", Description = "Full luxury decoration, photography, videography, gourmet dinner.", Price = 10000.0, IsActive = true },
                new Package { Name = "Crown Jewel", Description = "All-inclusive: Michelin chef, fireworks, helicopter transfer, celebrity DJ.", Price = 18000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Premium Catering", ArabicName = "تموين فاخر", IsActive = true },
                new Service { Name = "Photography", ArabicName = "تصوير", IsActive = true },
                new Service { Name = "Decoration", ArabicName = "ديكور", IsActive = true },
                new Service { Name = "Valet Parking", ArabicName = "خدمة صف السيارات", IsActive = true },
                new Service { Name = "Bridal Suite", ArabicName = "جناح العروس", IsActive = true }
            ]
        } : null;

        // ---- Hall 12: Nakheel Garden (Diamond Halls Group) ----
        var hall12 = diamondHallsOrg != null ? new Hall
        {
            Name = "Nakheel Garden",
            CommercialRegistration = 1010901234,
            Vat = 301345678901,
            IsActive = true,
            IsFeatured = false,
            IsPremium = false,
            HasSpecialOffer = true,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Nakheel Garden is a charming open-air garden venue perfect for intimate gatherings and engagement parties. Surrounded by date palms and water features, it offers an affordable yet elegant setting.",
            BothWeekDays = 5000.0,
            BothWeekEnds = 7500.0,
            MaleWeekDays = 2800.0,
            MaleWeekEnds = 4000.0,
            MaleMin = 50,
            MaleMax = 250,
            MaleActive = true,
            FemaleWeekDays = 3000.0,
            FemaleWeekEnds = 4200.0,
            FemaleMin = 50,
            FemaleMax = 250,
            FemaleActive = true,
            Gender = 3,
            Email = "info@nakheelgarden.sa",
            Phone = "+966542223344",
            WhatsApp = "+966542223344",
            Logo = "https://images.unsplash.com/photo-1465495976277-4387d4b0b4c6?w=100",
            AverageRating = 4.1,
            OrganizationId = diamondHallsOrg.Id,
            Location = new Location
            {
                Altitude = 610.0,
                Latitude = 24.750000,
                Longitude = 46.720000,
                City = "Riyadh",
                State = "Riyadh Region",
                Address = "Al-Narjis District, Prince Mohammed bin Salman Road"
            },
            Contacts =
            [
                new Contact { Name = "Faris Al-Shahrani", Email = "faris@nakheelgarden.sa", Phone = "+966542223344" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1465495976277-4387d4b0b4c6?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1469371670807-013ccf25f16a?w=800", index = 1, Gender = 3 }
            ],
            Packages =
            [
                new Package { Name = "Garden Party", Description = "Outdoor setup with fairy lights, basic decoration.", Price = 1500.0, IsActive = true },
                new Package { Name = "Palm Oasis", Description = "Enhanced garden decor, photography, light catering.", Price = 3000.0, IsActive = true },
                new Package { Name = "Royal Garden", Description = "Full outdoor luxury: tent, chandeliers, gourmet BBQ.", Price = 5500.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "BBQ Catering", ArabicName = "تموين شواء", IsActive = true },
                new Service { Name = "Garden Decoration", ArabicName = "ديكور حدائق", IsActive = true },
                new Service { Name = "Photography", ArabicName = "تصوير", IsActive = true }
            ]
        } : null;

        // ---- Hall 13: Al Sharq Palace (Diamond Halls Group) ----
        var hall13 = diamondHallsOrg != null ? new Hall
        {
            Name = "Al Sharq Palace",
            CommercialRegistration = 4650567890,
            Vat = 301456789012,
            IsActive = true,
            IsFeatured = true,
            IsPremium = true,
            HasSpecialOffer = false,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,
            Description = "Al Sharq Palace in Khobar is the Eastern Province's finest wedding destination. With breathtaking Gulf views and a private beach, it combines modern Arabian architecture with traditional hospitality.",
            BothWeekDays = 17000.0,
            BothWeekEnds = 24000.0,
            MaleWeekDays = 9500.0,
            MaleWeekEnds = 14000.0,
            MaleMin = 200,
            MaleMax = 800,
            MaleActive = true,
            FemaleWeekDays = 10000.0,
            FemaleWeekEnds = 14500.0,
            FemaleMin = 200,
            FemaleMax = 800,
            FemaleActive = true,
            Gender = 3,
            Email = "info@alsharqpalace.sa",
            Phone = "+966543334455",
            WhatsApp = "+966543334455",
            Logo = "https://images.unsplash.com/photo-1590381105924-c72589b9ef3f?w=100",
            AverageRating = 4.7,
            OrganizationId = diamondHallsOrg.Id,
            Location = new Location
            {
                Altitude = 3.0,
                Latitude = 26.283333,
                Longitude = 50.208333,
                City = "Khobar",
                State = "Eastern Region",
                Address = "Corniche District, Prince Turkey Street"
            },
            Contacts =
            [
                new Contact { Name = "Khaled Al-Dosari", Email = "khaled@alsharqpalace.sa", Phone = "+966543334455" },
                new Contact { Name = "Nouf Al-Rasheed", Email = "nouf@alsharqpalace.sa", Phone = "+966543556677" }
            ],
            MediaFiles =
            [
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1590381105924-c72589b9ef3f?w=800", index = 0, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?w=800", index = 1, Gender = 3 },
                new HallMedia { MediaType = "image", URL = "https://images.unsplash.com/photo-1522413452208-996ff3f3e740?w=800", index = 2, Gender = 2 }
            ],
            Packages =
            [
                new Package { Name = "Gulf View", Description = "Seaside decoration, sound system, event coordination.", Price = 5000.0, IsActive = true },
                new Package { Name = "Sunset", Description = "Premium beach ceremony setup, photography, live entertainment.", Price = 9000.0, IsActive = true },
                new Package { Name = "Sultan of the Sea", Description = "Ultimate: yacht arrival, beach fireworks, international chef, full-day coverage.", Price = 16000.0, IsActive = true }
            ],
            Services =
            [
                new Service { Name = "Seafood Banquet", ArabicName = "مأدبة مأكولات بحرية", IsActive = true },
                new Service { Name = "Beach Photography", ArabicName = "تصوير شاطئي", IsActive = true },
                new Service { Name = "Marine Decoration", ArabicName = "ديكور بحري", IsActive = true },
                new Service { Name = "Yacht Service", ArabicName = "خدمة يخت", IsActive = true }
            ]
        } : null;

        var halls = new List<Hall> { hall1, hall2, hall3, hall4, hall5, hall6, hall7, hall8, hall9, hall10 };
        if (hall11 != null) halls.Add(hall11);
        if (hall12 != null) halls.Add(hall12);
        if (hall13 != null) halls.Add(hall13);
        return halls;
    }

    // ===================================================================
    // ADMIN USER
    // ===================================================================

    private static async Task SeedAdminUser(UserManager<AppUser> userManager)
    {
        var admin = new AppUser
        {
            UserName = "admin",
            PhoneNumber = "597477814",
            Email = "HeshamAmoudi.it@gmail.com",
            FirstName = "Hesham",
            LastName = "Amoudi",
            Gender = "Male",
            DOB = DateTime.SpecifyKind(new DateTime(1990, 1, 1), DateTimeKind.Utc),
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            Active = true
        };

        var createResult = await userManager.CreateAsync(admin, "Pa$$w0rd");
        if (!createResult.Succeeded)
        {
            Console.WriteLine($"[Seed] ERROR creating admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            return;
        }

        var roleResult = await userManager.AddToRolesAsync(admin, [AppRoles.Admin, AppRoles.Moderator]);
        if (!roleResult.Succeeded)
        {
            Console.WriteLine($"[Seed] ERROR adding roles to admin: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            return;
        }

        Console.WriteLine("[Seed] Admin user created (HeshamAmoudi.it@gmail.com -> Admin, Moderator)");
    }

    // ===================================================================
    // CUSTOMER USERS
    // ===================================================================

    private static async Task SeedCustomerUsers(UserManager<AppUser> userManager)
    {
        var customerUsers = new List<AppUser>
        {
            new AppUser
            {
                UserName = "mohammed.rashid",
                PhoneNumber = "+966501234567",
                Email = "mohammed.rashid@example.com",
                FirstName = "Mohammed",
                LastName = "Al-Rashid",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1985, 5, 15), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "fatima.ahmed",
                PhoneNumber = "+966502345678",
                Email = "fatima.ahmed@example.com",
                FirstName = "Fatima",
                LastName = "Ahmed",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1990, 8, 20), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "ali.hassan",
                PhoneNumber = "+966503456789",
                Email = "ali.hassan@example.com",
                FirstName = "Ali",
                LastName = "Hassan",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1988, 3, 10), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            // Dedicated demo customer matching the spec
            new AppUser
            {
                UserName = "customer.demo",
                PhoneNumber = "+966508000001",
                Email = "customer@example.com",
                FirstName = "Customer",
                LastName = "Demo",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1995, 1, 1), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            // Additional customers for comprehensive test data
            new AppUser
            {
                UserName = "layla.omar",
                PhoneNumber = "+966504567890",
                Email = "layla.omar@example.com",
                FirstName = "Layla",
                LastName = "Omar",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1992, 11, 3), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "omar.khalid",
                PhoneNumber = "+966505678901",
                Email = "omar.khalid@example.com",
                FirstName = "Omar",
                LastName = "Al-Khalid",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1983, 7, 22), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "noor.sultan",
                PhoneNumber = "+966506789012",
                Email = "noor.sultan@example.com",
                FirstName = "Noor",
                LastName = "Al-Sultan",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1994, 2, 14), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "yousef.tamimi",
                PhoneNumber = "+966507890123",
                Email = "yousef.tamimi@example.com",
                FirstName = "Yousef",
                LastName = "Al-Tamimi",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1987, 9, 30), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "amira.fahad",
                PhoneNumber = "+966508901234",
                Email = "amira.fahad@example.com",
                FirstName = "Amira",
                LastName = "Al-Fahad",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1996, 4, 18), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "sultan.harbi",
                PhoneNumber = "+966509012345",
                Email = "sultan.harbi@example.com",
                FirstName = "Sultan",
                LastName = "Al-Harbi",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1981, 12, 5), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "hana.qahtani",
                PhoneNumber = "+966510123456",
                Email = "hana.qahtani@example.com",
                FirstName = "Hana",
                LastName = "Al-Qahtani",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1993, 6, 9), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "bandar.otaibi",
                PhoneNumber = "+966511234567",
                Email = "bandar.otaibi@example.com",
                FirstName = "Bandar",
                LastName = "Al-Otaibi",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1986, 1, 25), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "maha.anazi",
                PhoneNumber = "+966512345678",
                Email = "maha.anazi@example.com",
                FirstName = "Maha",
                LastName = "Al-Anazi",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1991, 10, 12), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "tariq.shammari",
                PhoneNumber = "+966513456789",
                Email = "tariq.shammari@example.com",
                FirstName = "Tariq",
                LastName = "Al-Shammari",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1989, 8, 7), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "reem.dosari",
                PhoneNumber = "+966514567890",
                Email = "reem.dosari@example.com",
                FirstName = "Reem",
                LastName = "Al-Dosari",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1997, 3, 28), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "faisal.mutairi",
                PhoneNumber = "+966515678901",
                Email = "faisal.mutairi@example.com",
                FirstName = "Faisal",
                LastName = "Al-Mutairi",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1984, 5, 16), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            }
        };

        foreach (var user in customerUsers)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, AppRoles.Customer);
        }

        Console.WriteLine($"[Seed] Created {customerUsers.Count} customer users");
    }

    // ===================================================================
    // HALL ORGANIZATION MANAGER USERS (organization owners for halls)
    // ===================================================================

    private static async Task SeedHallOrganizationManagerUsers(UserManager<AppUser> userManager)
    {
        var hallOrgManagerUsers = new List<AppUser>
        {
            new AppUser
            {
                UserName = "ahmed.mansour",
                PhoneNumber = "+966504567890",
                Email = "ahmed.mansour@grandhall.com",
                FirstName = "Ahmed",
                LastName = "Al-Mansour",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1980, 4, 12), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "sara.zahrani",
                PhoneNumber = "+966505678901",
                Email = "sara.zahrani@royalpalace.com",
                FirstName = "Sara",
                LastName = "Al-Zahrani",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1987, 9, 25), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            // Dedicated demo hall owner matching the spec
            new AppUser
            {
                UserName = "hallowner.demo",
                PhoneNumber = "+966508100001",
                Email = "hallowner@example.com",
                FirstName = "Hall Owner",
                LastName = "Demo",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1978, 6, 15), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            // 4th hall organization owner for additional halls
            new AppUser
            {
                UserName = "turki.faisal",
                PhoneNumber = "+966508500001",
                Email = "turki.faisal@diamondhalls.sa",
                FirstName = "Turki",
                LastName = "Al-Faisal",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1976, 2, 20), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            }
        };

        foreach (var user in hallOrgManagerUsers)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, AppRoles.HallOrganizationManager);
        }

        Console.WriteLine($"[Seed] Created {hallOrgManagerUsers.Count} HallOrganizationManager users");
    }

    // ===================================================================
    // VENDOR ORGANIZATION MANAGER USERS (organization owners for vendors)
    // ===================================================================

    private static async Task SeedVendorOrganizationManagerUsers(UserManager<AppUser> userManager)
    {
        var vendorOrgManagerUsers = new List<AppUser>
        {
            new AppUser
            {
                UserName = "khalid.otaibi",
                PhoneNumber = "+966506789012",
                Email = "khalid.otaibi@elitecatering.sa",
                FirstName = "Khalid",
                LastName = "Al-Otaibi",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1982, 11, 8), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "noura.dosari",
                PhoneNumber = "+966507890123",
                Email = "noura.dosari@luxurydecor.sa",
                FirstName = "Noura",
                LastName = "Al-Dosari",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1989, 6, 19), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            // Dedicated demo vendor owner matching the spec
            new AppUser
            {
                UserName = "vendorowner.demo",
                PhoneNumber = "+966508200001",
                Email = "vendorowner@example.com",
                FirstName = "Vendor Owner",
                LastName = "Demo",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1975, 3, 20), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            }
        };

        foreach (var user in vendorOrgManagerUsers)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, AppRoles.VendorOrganizationManager);
        }

        Console.WriteLine($"[Seed] Created {vendorOrgManagerUsers.Count} VendorOrganizationManager users");
    }

    // ===================================================================
    // HALL MANAGER TEAM MEMBER (works under a HallOrganizationManager)
    // ===================================================================

    private static async Task SeedHallManagerTeamMemberUser(UserManager<AppUser> userManager)
    {
        var hallManagerUsers = new List<AppUser>
        {
            new AppUser
            {
                UserName = "hallmanager.demo",
                PhoneNumber = "+966508300001",
                Email = "hallmanager@example.com",
                FirstName = "Hall Manager",
                LastName = "Demo",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1992, 7, 10), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "hallmanager.riyadh",
                PhoneNumber = "+966508300002",
                Email = "hallmanager.riyadh@example.com",
                FirstName = "Abdulrahman",
                LastName = "Al-Ghamdi",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1990, 4, 15), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "hallmanager.jeddah",
                PhoneNumber = "+966508300003",
                Email = "hallmanager.jeddah@example.com",
                FirstName = "Maryam",
                LastName = "Al-Juhani",
                Gender = "Female",
                DOB = DateTime.SpecifyKind(new DateTime(1994, 11, 2), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            }
        };

        foreach (var user in hallManagerUsers)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, AppRoles.HallManager);
        }

        Console.WriteLine($"[Seed] Created {hallManagerUsers.Count} HallManager team member users");
    }

    // ===================================================================
    // VENDOR MANAGER TEAM MEMBER (works under a VendorOrganizationManager)
    // ===================================================================

    private static async Task SeedVendorManagerTeamMemberUser(UserManager<AppUser> userManager)
    {
        var vendorManagerUsers = new List<AppUser>
        {
            new AppUser
            {
                UserName = "vendormanager.demo",
                PhoneNumber = "+966508400001",
                Email = "vendormanager@example.com",
                FirstName = "Vendor Manager",
                LastName = "Demo",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1993, 2, 25), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "vendormanager.catering",
                PhoneNumber = "+966508400002",
                Email = "vendormanager.catering@example.com",
                FirstName = "Hassan",
                LastName = "Al-Shehri",
                Gender = "Male",
                DOB = DateTime.SpecifyKind(new DateTime(1991, 8, 18), DateTimeKind.Utc),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            }
        };

        foreach (var user in vendorManagerUsers)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, AppRoles.VendorManager);
        }

        Console.WriteLine($"[Seed] Created {vendorManagerUsers.Count} VendorManager team member users");
    }

    // ===================================================================
    // ORGANIZATIONS, MEMBERS, AND RESOURCE LINKAGE
    // ===================================================================

    private static async Task SeedOrganizationsAndMembers(
        UserManager<AppUser> userManager,
        DataContext context)
    {
        if (await context.Organizations.AnyAsync())
        {
            Console.WriteLine("[Seed] Organizations already exist, skipping organization creation");
            return;
        }

        // NOTE: Halls are NOT created yet at this point. They are seeded AFTER
        // organizations so they can reference valid OrganizationIds directly.
        // Hall linkage happens in SeedHalls(), not here.

        // ------- Hall Organization for Ahmed Al-Mansour -------
        await SeedSingleOrganization(context, userManager,
            ownerUsername: "ahmed.mansour",
            orgName: "Grand Hall Events Company",
            orgType: "HallManagement",
            businessInfo: new Organization
            {
                CommercialRegistrationNumber = "1010234567",
                VatNumber = "300012345600003",
                LegalName = "Grand Hall Events Company Ltd.",
                BuildingNumber = "2345",
                StreetName = "King Fahd Road",
                District = "Al-Olaya",
                City = "Riyadh",
                PostalCode = "12211",
                CountryCode = "SA",
                Email = "ahmed.mansour@grandhall.com",
                Phone = "+966504567890",
                WhatsApp = "+966504567890",
                Website = "https://grandhall.com",
                BankName = "Al Rajhi Bank",
                BankAccountNumber = "1234567890",
                BankIban = "SA0380000000608010167519"
            });

        // ------- Hall Organization for Sara Al-Zahrani -------
        await SeedSingleOrganization(context, userManager,
            ownerUsername: "sara.zahrani",
            orgName: "Royal Palace Events",
            orgType: "HallManagement",
            businessInfo: new Organization
            {
                CommercialRegistrationNumber = "1010345678",
                VatNumber = "300023456700003",
                LegalName = "Royal Palace Events LLC",
                BuildingNumber = "5678",
                StreetName = "Prince Sultan Street",
                District = "Al-Salamah",
                City = "Jeddah",
                PostalCode = "21442",
                CountryCode = "SA",
                Email = "sara.zahrani@royalpalace.com",
                Phone = "+966505678901",
                WhatsApp = "+966505678901",
                BankName = "Saudi National Bank",
                BankAccountNumber = "9876543210",
                BankIban = "SA4420000001234567891234"
            });

        // ------- Vendor Organization for Khalid Al-Otaibi -------
        await SeedSingleOrganization(context, userManager,
            ownerUsername: "khalid.otaibi",
            orgName: "Elite Catering & Events",
            orgType: "VendorManagement",
            businessInfo: new Organization
            {
                CommercialRegistrationNumber = "1010456789",
                VatNumber = "300034567800003",
                LegalName = "Elite Catering & Events Co.",
                BuildingNumber = "1234",
                StreetName = "Tahlia Street",
                District = "Al-Rawdah",
                City = "Jeddah",
                PostalCode = "21472",
                CountryCode = "SA",
                Email = "khalid.otaibi@elitecatering.sa",
                Phone = "+966506789012",
                WhatsApp = "+966506789012",
                Website = "https://elitecatering.sa",
                BankName = "Riyad Bank",
                BankAccountNumber = "5555666677",
                BankIban = "SA8660100012345678901234"
            });

        // ------- Vendor Organization for Noura Al-Dosari -------
        await SeedSingleOrganization(context, userManager,
            ownerUsername: "noura.dosari",
            orgName: "Luxury Decor & Design",
            orgType: "VendorManagement",
            businessInfo: new Organization
            {
                CommercialRegistrationNumber = "1010567890",
                VatNumber = "300045678900003",
                LegalName = "Luxury Decor & Design Est.",
                BuildingNumber = "9012",
                StreetName = "Al-Haram Street",
                District = "Al-Aziziyah",
                City = "Makkah",
                PostalCode = "24232",
                CountryCode = "SA",
                Email = "noura.dosari@luxurydecor.sa",
                Phone = "+966507890123",
                WhatsApp = "+966507890123",
                Website = "https://luxurydecor.sa",
                BankName = "Alinma Bank",
                BankAccountNumber = "8888999900",
                BankIban = "SA0505000068201234567891"
            });

        // ------- Hall Organization for Turki Al-Faisal (4th org) -------
        await SeedSingleOrganization(context, userManager,
            ownerUsername: "turki.faisal",
            orgName: "Diamond Halls Group",
            orgType: "HallManagement",
            businessInfo: new Organization
            {
                CommercialRegistrationNumber = "1010890123",
                VatNumber = "300078901200003",
                LegalName = "Diamond Halls Group Co.",
                BuildingNumber = "1234",
                StreetName = "Al-Dabab Street",
                District = "Al-Sulimaniyah",
                City = "Riyadh",
                PostalCode = "12234",
                CountryCode = "SA",
                Email = "turki.faisal@diamondhalls.sa",
                Phone = "+966508500001",
                WhatsApp = "+966508500001",
                Website = "https://diamondhalls.sa",
                BankName = "Bank Albilad",
                BankAccountNumber = "7777888899",
                BankIban = "SA1215000007777888899001"
            });

        // ------- Primary Demo Organizations (with team member memberships) -------
        await SeedDemoOrganizations(context, userManager);

        Console.WriteLine("[Seed] All organizations and members created successfully");
    }

    /// <summary>
    /// Creates the two primary demo organizations with owner and team member memberships.
    /// Each organization + its members are wrapped in an explicit transaction following
    /// the same pattern as OrganizationService.CreateOrganization.
    /// Halls are NOT linked here -- they are created afterward in SeedHalls().
    /// </summary>
    private static async Task SeedDemoOrganizations(
        DataContext context,
        UserManager<AppUser> userManager)
    {
        // ---- 1. "Elite Halls Management" (HallManagement) ----
        var hallOwner = await userManager.FindByEmailAsync("hallowner@example.com");
        var hallManager = await userManager.FindByEmailAsync("hallmanager@example.com");
        var hallManagerRiyadh = await userManager.FindByEmailAsync("hallmanager.riyadh@example.com");

        if (hallOwner != null)
        {
            await using var txHall = await context.Database.BeginTransactionAsync();
            try
            {
                var eliteHallsOrg = new Organization
                {
                    Name = "Elite Halls Management",
                    Type = "HallManagement",
                    OwnerId = hallOwner.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    CommercialRegistrationNumber = "1010678901",
                    VatNumber = "300056789000003",
                    LegalName = "Elite Halls Management Co. Ltd.",
                    BuildingNumber = "3456",
                    StreetName = "Olaya Street",
                    District = "Al-Worood",
                    City = "Riyadh",
                    PostalCode = "12251",
                    CountryCode = "SA",
                    Email = "hallowner@example.com",
                    Phone = "+966508100001",
                    WhatsApp = "+966508100001",
                    Website = "https://elitehalls.sa",
                    BankName = "Al Rajhi Bank",
                    BankAccountNumber = "1111222233",
                    BankIban = "SA0380000000608011111222",
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow
                };
                context.Organizations.Add(eliteHallsOrg);
                await context.SaveChangesAsync();

                // Owner membership
                context.OrganizationMembers.Add(new OrganizationMember
                {
                    OrganizationId = eliteHallsOrg.Id,
                    AppUserId = hallOwner.Id,
                    Role = "Owner",
                    CanManageTeam = true,
                    CanCreateResources = true,
                    JoinedAt = DateTime.UtcNow
                });

                // Team member: hallmanager@example.com as Manager
                if (hallManager != null)
                {
                    context.OrganizationMembers.Add(new OrganizationMember
                    {
                        OrganizationId = eliteHallsOrg.Id,
                        AppUserId = hallManager.Id,
                        Role = "Manager",
                        CanManageTeam = false,
                        CanCreateResources = true,
                        JoinedAt = DateTime.UtcNow,
                        InvitedBy = hallOwner.Id
                    });
                }

                // Second team member: hallmanager.riyadh@example.com
                if (hallManagerRiyadh != null)
                {
                    context.OrganizationMembers.Add(new OrganizationMember
                    {
                        OrganizationId = eliteHallsOrg.Id,
                        AppUserId = hallManagerRiyadh.Id,
                        Role = "Manager",
                        CanManageTeam = false,
                        CanCreateResources = true,
                        JoinedAt = DateTime.UtcNow,
                        InvitedBy = hallOwner.Id
                    });
                }

                await context.SaveChangesAsync();
                await txHall.CommitAsync();

                Console.WriteLine("[Seed] Created 'Elite Halls Management' for hallowner@example.com");
            }
            catch (Exception ex)
            {
                await txHall.RollbackAsync();
                Console.WriteLine($"[Seed] ERROR creating Elite Halls Management: {ex.Message}");
                Console.WriteLine($"[Seed] Stack trace: {ex.StackTrace}");
            }
        }

        // ---- 2. "Premium Vendors Co." (VendorManagement) ----
        var vendorOwner = await userManager.FindByEmailAsync("vendorowner@example.com");
        var vendorManager = await userManager.FindByEmailAsync("vendormanager@example.com");

        if (vendorOwner != null)
        {
            await using var txVendor = await context.Database.BeginTransactionAsync();
            try
            {
                var premiumVendorsOrg = new Organization
                {
                    Name = "Premium Vendors Co.",
                    Type = "VendorManagement",
                    OwnerId = vendorOwner.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    CommercialRegistrationNumber = "1010789012",
                    VatNumber = "300067890100003",
                    LegalName = "Premium Vendors Trading Co.",
                    BuildingNumber = "7890",
                    StreetName = "King Abdullah Road",
                    District = "Al-Malaz",
                    City = "Riyadh",
                    PostalCode = "12641",
                    CountryCode = "SA",
                    Email = "vendorowner@example.com",
                    Phone = "+966508200001",
                    WhatsApp = "+966508200001",
                    Website = "https://premiumvendors.sa",
                    BankName = "Saudi National Bank",
                    BankAccountNumber = "4444555566",
                    BankIban = "SA4420000004444555566001",
                    IsVerified = true,
                    VerifiedAt = DateTime.UtcNow
                };
                context.Organizations.Add(premiumVendorsOrg);
                await context.SaveChangesAsync();

                // Owner membership
                context.OrganizationMembers.Add(new OrganizationMember
                {
                    OrganizationId = premiumVendorsOrg.Id,
                    AppUserId = vendorOwner.Id,
                    Role = "Owner",
                    CanManageTeam = true,
                    CanCreateResources = true,
                    JoinedAt = DateTime.UtcNow
                });

                // Team member: vendormanager@example.com as Manager
                if (vendorManager != null)
                {
                    context.OrganizationMembers.Add(new OrganizationMember
                    {
                        OrganizationId = premiumVendorsOrg.Id,
                        AppUserId = vendorManager.Id,
                        Role = "Manager",
                        CanManageTeam = false,
                        CanCreateResources = true,
                        JoinedAt = DateTime.UtcNow,
                        InvitedBy = vendorOwner.Id
                    });
                }

                await context.SaveChangesAsync();
                await txVendor.CommitAsync();

                Console.WriteLine("[Seed] Created 'Premium Vendors Co.' for vendorowner@example.com");
            }
            catch (Exception ex)
            {
                await txVendor.RollbackAsync();
                Console.WriteLine($"[Seed] ERROR creating Premium Vendors Co.: {ex.Message}");
                Console.WriteLine($"[Seed] Stack trace: {ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Helper to seed a single organization with its owner membership inside a transaction.
    /// Follows the same transaction pattern as OrganizationService.CreateOrganization.
    /// </summary>
    private static async Task SeedSingleOrganization(
        DataContext context,
        UserManager<AppUser> userManager,
        string ownerUsername,
        string orgName,
        string orgType,
        Organization businessInfo = null)
    {
        var owner = await userManager.FindByNameAsync(ownerUsername);
        if (owner == null)
        {
            Console.WriteLine($"[Seed] User '{ownerUsername}' not found, skipping organization '{orgName}'");
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var org = new Organization
            {
                Name = orgName,
                Type = orgType,
                OwnerId = owner.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Apply business registration info if provided
            if (businessInfo != null)
            {
                org.CommercialRegistrationNumber = businessInfo.CommercialRegistrationNumber;
                org.VatNumber = businessInfo.VatNumber;
                org.LegalName = businessInfo.LegalName;
                org.BuildingNumber = businessInfo.BuildingNumber;
                org.StreetName = businessInfo.StreetName;
                org.District = businessInfo.District;
                org.City = businessInfo.City;
                org.PostalCode = businessInfo.PostalCode;
                org.CountryCode = businessInfo.CountryCode;
                org.Email = businessInfo.Email;
                org.Phone = businessInfo.Phone;
                org.WhatsApp = businessInfo.WhatsApp;
                org.Website = businessInfo.Website;
                org.BankName = businessInfo.BankName;
                org.BankAccountNumber = businessInfo.BankAccountNumber;
                org.BankIban = businessInfo.BankIban;
                org.IsVerified = true;
                org.VerifiedAt = DateTime.UtcNow;
            }

            context.Organizations.Add(org);
            await context.SaveChangesAsync();

            // Owner as OrganizationMember
            context.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = org.Id,
                AppUserId = owner.Id,
                Role = "Owner",
                CanManageTeam = true,
                CanCreateResources = true,
                JoinedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            Console.WriteLine($"[Seed] Created organization '{orgName}' for {ownerUsername}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[Seed] ERROR creating organization '{orgName}': {ex.Message}");
            Console.WriteLine($"[Seed] Stack trace: {ex.StackTrace}");
        }
    }

    // ===================================================================
    // HALL MANAGER ENTITIES (the HallManager table linking users to halls)
    // ===================================================================
    //
    // IMPORTANT: HallManager entities are ONLY for team members (AppRoles.HallManager),
    // NOT for organization owners (AppRoles.HallOrganizationManager).
    // Organization owners access halls through their Organization -> Hall relationship.
    // Team members need a HallManager entity for direct hall assignment.

    private static async Task SeedHallManagerEntities(
        UserManager<AppUser> userManager,
        DataContext context)
    {
        if (await context.HallManagers.AnyAsync())
            return;

        try
        {
            var eliteHallsOrg = await context.Organizations
                .FirstOrDefaultAsync(o => o.Name == "Elite Halls Management");

            if (eliteHallsOrg == null)
            {
                Console.WriteLine("[Seed] Elite Halls Management org not found, skipping HallManager entity creation");
                return;
            }

            var orgHalls = await context.Halls
                .Where(h => h.OrganizationId == eliteHallsOrg.Id)
                .OrderBy(h => h.ID)
                .ToListAsync();

            if (orgHalls.Count == 0)
            {
                Console.WriteLine("[Seed] No halls found in Elite Halls Management org, skipping HallManager entity creation");
                return;
            }

            // HallManager entity for hallmanager.demo -- first 2 halls
            var hallManagerDemo = await userManager.FindByNameAsync("hallmanager.demo");
            if (hallManagerDemo != null)
            {
                var demoHalls = orgHalls.Take(2).ToList();
                var teamMemberHallManager = new HallManager
                {
                    AppUserId = hallManagerDemo.Id,
                    CreatedAt = DateTime.UtcNow,
                    Halls = demoHalls
                };
                context.HallManagers.Add(teamMemberHallManager);
                await context.SaveChangesAsync();

                foreach (var hall in demoHalls)
                {
                    hall.AssignedToHallManagerId = teamMemberHallManager.Id;
                }
                await context.SaveChangesAsync();
                Console.WriteLine($"[Seed] Created HallManager entity for hallmanager.demo with {demoHalls.Count} halls");
            }

            // HallManager entity for hallmanager.riyadh -- remaining halls from Elite
            var hallManagerRiyadh = await userManager.FindByNameAsync("hallmanager.riyadh");
            if (hallManagerRiyadh != null && orgHalls.Count > 2)
            {
                var riyadhHalls = orgHalls.Skip(2).ToList();
                var riyadhManager = new HallManager
                {
                    AppUserId = hallManagerRiyadh.Id,
                    CreatedAt = DateTime.UtcNow,
                    Halls = riyadhHalls
                };
                context.HallManagers.Add(riyadhManager);
                await context.SaveChangesAsync();

                foreach (var hall in riyadhHalls)
                {
                    hall.AssignedToHallManagerId = riyadhManager.Id;
                }
                await context.SaveChangesAsync();
                Console.WriteLine($"[Seed] Created HallManager entity for hallmanager.riyadh with {riyadhHalls.Count} halls");
            }

            // HallManager entity for hallmanager.jeddah -- assigned to Grand Hall Events
            var hallManagerJeddah = await userManager.FindByNameAsync("hallmanager.jeddah");
            var grandHallOrg = await context.Organizations
                .FirstOrDefaultAsync(o => o.Name == "Grand Hall Events Company");
            if (hallManagerJeddah != null && grandHallOrg != null)
            {
                var jeddahHalls = await context.Halls
                    .Where(h => h.OrganizationId == grandHallOrg.Id)
                    .OrderBy(h => h.ID)
                    .Take(2)
                    .ToListAsync();

                if (jeddahHalls.Count > 0)
                {
                    // Add hallmanager.jeddah as a member of Grand Hall Events
                    context.OrganizationMembers.Add(new OrganizationMember
                    {
                        OrganizationId = grandHallOrg.Id,
                        AppUserId = hallManagerJeddah.Id,
                        Role = "Manager",
                        CanManageTeam = false,
                        CanCreateResources = true,
                        JoinedAt = DateTime.UtcNow,
                        InvitedBy = grandHallOrg.OwnerId
                    });

                    var jeddahManager = new HallManager
                    {
                        AppUserId = hallManagerJeddah.Id,
                        CreatedAt = DateTime.UtcNow,
                        Halls = jeddahHalls
                    };
                    context.HallManagers.Add(jeddahManager);
                    await context.SaveChangesAsync();

                    foreach (var hall in jeddahHalls)
                    {
                        hall.AssignedToHallManagerId = jeddahManager.Id;
                    }
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[Seed] Created HallManager entity for hallmanager.jeddah with {jeddahHalls.Count} halls");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seed] ERROR seeding HallManager entities: {ex.Message}");
            Console.WriteLine($"[Seed] Stack trace: {ex.StackTrace}");
        }
    }

    // ===================================================================
    // ORGANIZATIONS FOR EXISTING USERS (idempotent backfill)
    // ===================================================================

    /// <summary>
    /// Ensures that HallOrganizationManager and VendorOrganizationManager users
    /// who exist but lack an Organization get one created. This handles the case
    /// where the database already had users from a previous seed run but did not
    /// yet have the organization seeding logic.
    /// </summary>
    private static async Task SeedOrganizationsForExistingUsers(
        UserManager<AppUser> userManager,
        DataContext context)
    {
        if (await context.Organizations.AnyAsync())
        {
            Console.WriteLine("[Seed] Organizations already exist, skipping backfill");
            return;
        }

        Console.WriteLine("[Seed] Backfilling organizations for existing users...");

        try
        {
            // Find HallOrganizationManager users without organizations
            var hallOrgManagers = await userManager.GetUsersInRoleAsync(AppRoles.HallOrganizationManager);
            var halls = await context.Halls.ToListAsync();
            var hallIndex = 0;

            foreach (var user in hallOrgManagers)
            {
                var hasOrg = await context.Organizations.AnyAsync(o => o.OwnerId == user.Id && o.IsActive);
                if (hasOrg) continue;

                var org = new Organization
                {
                    Name = $"{user.FirstName} {user.LastName} Hall Organization",
                    Type = "HallManagement",
                    OwnerId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                context.Organizations.Add(org);
                await context.SaveChangesAsync();

                context.OrganizationMembers.Add(new OrganizationMember
                {
                    OrganizationId = org.Id,
                    AppUserId = user.Id,
                    Role = "Owner",
                    CanManageTeam = true,
                    CanCreateResources = true,
                    JoinedAt = DateTime.UtcNow
                });

                // Link up to 2 unlinked halls
                var unlinkedHalls = halls.Where(h => h.OrganizationId == null).Skip(hallIndex).Take(2).ToList();
                foreach (var hall in unlinkedHalls)
                {
                    hall.OrganizationId = org.Id;
                    hallIndex++;
                }

                await context.SaveChangesAsync();
                Console.WriteLine($"[Seed] Backfilled organization for {user.UserName}");
            }

            // Find VendorOrganizationManager users without organizations
            var vendorOrgManagers = await userManager.GetUsersInRoleAsync(AppRoles.VendorOrganizationManager);
            foreach (var user in vendorOrgManagers)
            {
                var hasOrg = await context.Organizations.AnyAsync(o => o.OwnerId == user.Id && o.IsActive);
                if (hasOrg) continue;

                var org = new Organization
                {
                    Name = $"{user.FirstName} {user.LastName} Vendor Organization",
                    Type = "VendorManagement",
                    OwnerId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                context.Organizations.Add(org);
                await context.SaveChangesAsync();

                context.OrganizationMembers.Add(new OrganizationMember
                {
                    OrganizationId = org.Id,
                    AppUserId = user.Id,
                    Role = "Owner",
                    CanManageTeam = true,
                    CanCreateResources = true,
                    JoinedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
                Console.WriteLine($"[Seed] Backfilled organization for {user.UserName}");
            }

            Console.WriteLine("[Seed] Organization backfill completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seed] ERROR in organization backfill: {ex.Message}");
        }
    }
}

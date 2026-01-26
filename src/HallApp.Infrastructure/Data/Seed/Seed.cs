using System.Text.Json;
using HallApp.Core.Entities;
using HallApp.Core.Entities.ChamperEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Seed;

public class Seed
{
    public static async Task SeedInformation(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, DataContext context)
    {
        Console.WriteLine("🌱 Starting database seeding...");
        
        // Seed roles - only if they don't exist
        if (await roleManager.Roles.AnyAsync())
        {
            Console.WriteLine("⚠️ Roles already exist, skipping role creation");
        }
        else
        {
            var roles = new List<AppRole>{
            new AppRole{Name = "Customer"},
            new AppRole{Name = "HallEmployee"},
            new AppRole{Name = "HallSuperVisor"},
            new AppRole{Name = "HallManager"},
            new AppRole{Name = "Moderator"},
            new AppRole{Name = "Admin"},
            new AppRole{Name = "VendorManager"},
        };

            foreach (var role in roles)
            {
                await roleManager.CreateAsync(role);
            }
            Console.WriteLine("✅ Roles created successfully");
        }

        // CRITICAL: Seed halls FIRST before creating any users with HallManager role
        if (!await context.Halls.AnyAsync())
        {
            try
            {
                var hallsPath = "/Users/heshamamoudi/Application Development/Hesham/HallAppBackend/Data/halls.json";
                var hallsData = await File.ReadAllTextAsync(hallsPath);
                var halls = JsonSerializer.Deserialize<List<Hall>>(hallsData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (halls != null && halls.Any())
                {
                    foreach (var hall in halls)
                    {
                        context.Halls.Add(hall);
                    }
                    await context.SaveChangesAsync();
                    Console.WriteLine($"✅ Seeded {halls.Count} halls from JSON");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding halls: {ex.Message}");
            }
        }

        // Now seed admin user AFTER halls exist - only if users don't exist
        if (await userManager.Users.AnyAsync())
        {
            Console.WriteLine("⚠️ Users already exist, skipping user creation");
            return;
        }
        
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

        await userManager.CreateAsync(admin, "Pa$$w0rd");
        await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" });
        Console.WriteLine("✅ Admin user created successfully");

        // Seed customer users with proper authentication
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
            }
        };

        foreach (var user in customerUsers)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "Customer");
        }

        // Seed hall manager users
        var hallManagerUsers = new List<AppUser>
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
            }
        };

        foreach (var user in hallManagerUsers)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "HallManager");
        }

        // Seed vendor manager users
        var vendorManagerUsers = new List<AppUser>
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
            }
        };

        foreach (var user in vendorManagerUsers)
        {
            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "VendorManager");
        }

        Console.WriteLine("✅ All users created successfully");

        // Create hall managers (matching VendorManager pattern - one-to-one with AppUser)
        if (!await context.HallManagers.AnyAsync())
        {
            try
            {
                var halls = await context.Halls.ToListAsync();
                var manager1 = await userManager.FindByNameAsync("ahmed.mansour");
                var manager2 = await userManager.FindByNameAsync("sara.zahrani");

                if (halls.Count >= 2 && manager1 != null && manager2 != null && manager1.Active && manager2.Active)
                {
                    var hall1 = halls[0];
                    var hall2 = halls[1];
                    
                    if (hall1.IsActive && hall2.IsActive)
                    {
                        // Create HallManager for Ahmed and assign to Grand Hall
                        var ahmedManager = new HallManager
                        {
                            AppUserId = manager1.Id,
                            CreatedAt = DateTime.UtcNow,
                            Halls = new List<Hall> { hall1 }
                        };
                        context.HallManagers.Add(ahmedManager);

                        // Create HallManager for Sara and assign to Royal Palace
                        var saraManager = new HallManager
                        {
                            AppUserId = manager2.Id,
                            CreatedAt = DateTime.UtcNow,
                            Halls = new List<Hall> { hall2 }
                        };
                        context.HallManagers.Add(saraManager);

                        await context.SaveChangesAsync();
                        Console.WriteLine("✅ Seeded hall managers with hall assignments");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ One or more halls are inactive - skipping hall managers");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Could not create hall managers - Halls: {halls.Count}, Manager1: {manager1 != null}, Manager2: {manager2 != null}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding hall managers: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}

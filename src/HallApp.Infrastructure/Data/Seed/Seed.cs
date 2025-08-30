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
        if (await userManager.Users.AnyAsync())
            return;

        // Seed roles and admin user as before
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

        var admin = new AppUser
        {
            UserName = "admin",
            PhoneNumber = "597477814",
            Email = "HeshamAmoudi.it@gmail.com",
            FirstName = "Hesham",
            LastName = "Amoudi",
            Gender = "Male",
            DOB = new DateTime(1990, 1, 1),
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            Active = true
        };

        await userManager.CreateAsync(admin, "Pa$$w0rd");
        await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator", "HallManager" });

        // Seed customer users with proper authentication
        var customerUsers = new List<AppUser>
        {
            new AppUser
            {
                UserName = "john.doe",
                PhoneNumber = "966512345678",
                Email = "john.doe@example.com",
                FirstName = "John",
                LastName = "Doe",
                Gender = "Male",
                DOB = new DateTime(1985, 5, 15),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "sarah.ahmed",
                PhoneNumber = "966523456789",
                Email = "sarah.ahmed@example.com",
                FirstName = "Sarah",
                LastName = "Ahmed",
                Gender = "Female",
                DOB = new DateTime(1990, 8, 20),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Active = true
            },
            new AppUser
            {
                UserName = "mohammed.ali",
                PhoneNumber = "966534567890",
                Email = "mohammed.ali@example.com",
                FirstName = "Mohammed",
                LastName = "Ali",
                Gender = "Male",
                DOB = new DateTime(1988, 3, 10),
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

        // Seed halls from JSON file
        if (!await context.Halls.AnyAsync())
        {
            try
            {
                // Read the JSON file - direct absolute path
                var hallsPath = "/Users/heshamamoudi/Application Development/Hesham/HallAppBackend/Data/halls.json";
                var hallsData = await File.ReadAllTextAsync(hallsPath);

                // Deserialize the JSON into a list of Hall objects
                var halls = JsonSerializer.Deserialize<List<Hall>>(hallsData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (halls != null && halls.Any())
                {
                    foreach (var hall in halls)
                    {
                        // Add each hall to the context
                        context.Halls.Add(hall);
                    }

                    // Save to the database
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing halls: {ex.Message}");
            }
        }
    }
}

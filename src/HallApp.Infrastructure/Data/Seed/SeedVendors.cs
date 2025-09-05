using HallApp.Core.Entities.VendorEntities;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Seed;

public class SeedVendors
{
    public static async Task SeedVendorData(DataContext context)
    {
        try
        {
            Console.WriteLine("Starting vendor seeding...");
            
            // First ensure we have vendor types
            if (!await context.VendorTypes.AnyAsync())
            {
                var vendorTypes = new List<VendorType>
                {
                    new VendorType { Name = "Catering Service", Description = "Provides food and beverage services for events and celebrations" },
                    new VendorType { Name = "Photography", Description = "Professional photography services for events" },
                    new VendorType { Name = "Decoration", Description = "Event decoration and setup services" },
                    new VendorType { Name = "Entertainment", Description = "Music, performances, and entertainment services" },
                    new VendorType { Name = "Transportation", Description = "Vehicle rental and transportation services" }
                };

                await context.VendorTypes.AddRangeAsync(vendorTypes);
                await context.SaveChangesAsync();
                Console.WriteLine($"Seeded {vendorTypes.Count} vendor types");
            }
            else
            {
                Console.WriteLine("Vendor types already exist, skipping seeding");
            }

            // Check if we have the decoration vendor that the Flutter app expects
            var elegantDecorExists = await context.Vendors
                .AnyAsync(v => v.Name == "Elegant Decor" && 
                              v.Email == "contact@elegantdecor.com");
            
            if (!elegantDecorExists)
            {
                try
                {
                    Console.WriteLine("Creating Elegant Decor vendor...");
                    
                    // First, make sure decoration vendor type exists
                    var decorationType = await context.VendorTypes
                        .FirstOrDefaultAsync(vt => vt.Name == "Decoration");
                    
                    if (decorationType == null)
                    {
                        decorationType = new VendorType 
                        { 
                            Name = "Decoration", 
                            Description = "Event decoration and setup services" 
                        };
                        context.VendorTypes.Add(decorationType);
                        await context.SaveChangesAsync();
                        Console.WriteLine("Created Decoration vendor type");
                    }

                    var elegantDecor = new Vendor
                    {
                        Name = "Elegant Decor",
                        Description = "Professional event decoration services with premium quality materials and creative designs",
                        Email = "contact@elegantdecor.com",
                        Phone = "966501234567",
                        WhatsApp = "966501234567",
                        LogoUrl = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2340&q=80",
                        CoverImageUrl = "https://images.unsplash.com/photo-1519225421980-715cb0215aed?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2340&q=80",
                        IsActive = true,
                        Rating = 4.7f,
                        ReviewCount = 124,
                        VendorTypeId = decorationType.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.Vendors.Add(elegantDecor);
                    await context.SaveChangesAsync();
                    Console.WriteLine("Created Elegant Decor vendor successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating Elegant Decor vendor: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
            else
            {
                Console.WriteLine("Elegant Decor vendor already exists, skipping creation");
            }

            // Check if we need to create additional vendors
            var vendorCount = await context.Vendors.CountAsync();
            if (vendorCount < 5) // We want at least 5 vendors for testing
            {
                try
                {
                    Console.WriteLine("Creating additional vendors...");
                    
                    // Get vendor types to reference
                    var cateringType = await context.VendorTypes.FirstOrDefaultAsync(vt => vt.Name == "Catering Service");
                    var photographyType = await context.VendorTypes.FirstOrDefaultAsync(vt => vt.Name == "Photography");
                    var entertainmentType = await context.VendorTypes.FirstOrDefaultAsync(vt => vt.Name == "Entertainment");
                    
                    if (cateringType == null || photographyType == null || entertainmentType == null)
                    {
                        Console.WriteLine("Required vendor types not found, skipping additional vendors");
                        return;
                    }
                    
                    var royalCatering = new Vendor
                    {
                        Name = "Royal Catering",
                        Description = "Luxury catering service for weddings and corporate events with over 20 years of experience",
                        Email = "info@royalcatering.com",
                        Phone = "966512345678",
                        WhatsApp = "966512345678",
                        LogoUrl = "https://images.unsplash.com/photo-1555244162-803834f70033?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2340&q=80",
                        CoverImageUrl = "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2340&q=80",
                        IsActive = true,
                        Rating = 4.8f,
                        ReviewCount = 156,
                        VendorTypeId = cateringType.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var gourmetCatering = new Vendor
                    {
                        Name = "Gourmet Delight Catering",
                        Description = "Premium gourmet catering with international cuisine options",
                        Email = "info@gourmetdelight.com",
                        Phone = "966523456789",
                        WhatsApp = "966523456789",
                        LogoUrl = "https://images.unsplash.com/photo-1528605248644-14dd04022da1?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2340&q=80",
                        CoverImageUrl = "https://images.unsplash.com/photo-1455619452474-d2be8b1e70cd?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2370&q=80",
                        IsActive = true,
                        Rating = 4.6f,
                        ReviewCount = 108,
                        VendorTypeId = cateringType.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var photographyVendor = new Vendor
                    {
                        Name = "Capture Moments Photography",
                        Description = "Professional photography services for all special occasions",
                        Email = "info@capturemoments.com",
                        Phone = "966534567890",
                        WhatsApp = "966534567890",
                        LogoUrl = "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2338&q=80",
                        CoverImageUrl = "https://images.unsplash.com/photo-1554048612-b6a482bc67e5?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2340&q=80",
                        IsActive = true,
                        Rating = 4.9f,
                        ReviewCount = 87,
                        VendorTypeId = photographyType.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var entertainmentVendor = new Vendor
                    {
                        Name = "Melody Entertainment",
                        Description = "Professional DJ and live music services for all events",
                        Email = "bookings@melodyent.com",
                        Phone = "966545678901",
                        WhatsApp = "966545678901",
                        LogoUrl = "https://images.unsplash.com/photo-1470225620780-dba8ba36b745?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2340&q=80",
                        CoverImageUrl = "https://images.unsplash.com/photo-1429962714451-bb934ecdc4ec?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=2340&q=80",
                        IsActive = true,
                        Rating = 4.5f,
                        ReviewCount = 65,
                        VendorTypeId = entertainmentType.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var vendors = new List<Vendor> { royalCatering, gourmetCatering, photographyVendor, entertainmentVendor };

                    context.Vendors.AddRange(vendors);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"Seeded {vendors.Count} additional vendors");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error seeding additional vendors: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
            else
            {
                Console.WriteLine("Sufficient vendors already exist, skipping creation");
            }
            
            Console.WriteLine("Vendor seeding completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in vendor seeding: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}

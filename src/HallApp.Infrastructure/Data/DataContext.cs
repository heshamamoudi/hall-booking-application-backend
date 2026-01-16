using HallApp.Core.Entities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.NotificationEntities;
using HallApp.Core.Entities.ReviewEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Entities.ChatEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data;

public class DataContext : IdentityDbContext<AppUser, AppRole, int,
    IdentityUserClaim<int>, AppUserRole, IdentityUserLogin<int>,
    IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    // User Entities
    public DbSet<AppUser> AppUsers { get; set; }

    // Vendor Entities
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<VendorManager> VendorManagers { get; set; }
    public DbSet<VendorLocation> VendorLocations { get; set; }
    public DbSet<ServiceItem> ServiceItems { get; set; }
    public DbSet<ServiceItemImage> ServiceItemImages { get; set; }
    public DbSet<VendorBusinessHour> VendorBusinessHours { get; set; }
    public DbSet<VendorBlockedDate> VendorBlockedDates { get; set; }
    public DbSet<VendorType> VendorTypes { get; set; }
    public DbSet<VendorBooking> VendorBookings { get; set; }
    public DbSet<VendorBookingService> VendorBookingServices { get; set; }

    // Hall Entities
    public DbSet<Hall> Halls { get; set; }
    public DbSet<HallManager> HallManagers { get; set; }

    // Customer Entities
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<Favorite> Favorites { get; set; }

    // Reviews Entities
    public DbSet<Review> Reviews { get; set; }

    // Shared Entities
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingPackage> BookingPackages { get; set; }

    // Invoice Entities
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }

    // Payment Entities
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentRefund> PaymentRefunds { get; set; }

    // Chat Entities
    public DbSet<ChatConversation> ChatConversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatStatistics> ChatStatistics { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity configurations
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<AppRole>().ToTable("Roles");
        builder.Entity<AppUserRole>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

        // User role relationships
        builder.Entity<AppUser>()
            .HasMany(ur => ur.UserRoles)
            .WithOne(u => u.User)
            .HasForeignKey(ur => ur.UserId)
            .IsRequired();

        builder.Entity<AppRole>()
            .HasMany(ur => ur.UserRoles)
            .WithOne(u => u.Role)
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired();

        // Vendor relationships
        builder.Entity<Vendor>(entity =>
        {
            entity.HasMany(v => v.Managers)
                .WithMany(m => m.Vendors);

            entity.HasOne(v => v.VendorType)
                .WithMany(t => t.Vendors)
                .HasForeignKey(v => v.VendorTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(v => v.ServiceItems)
                .WithOne(s => s.Vendor)
                .HasForeignKey(s => s.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Location)
                .WithOne(l => l.Vendor)
                .HasForeignKey<VendorLocation>(l => l.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.BusinessHours)
                .WithOne(b => b.Vendor)
                .HasForeignKey(b => b.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.BlockedDates)
                .WithOne(b => b.Vendor)
                .HasForeignKey(b => b.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.Reviews)
                .WithOne(r => r.Vendor)
                .HasForeignKey(r => r.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // VendorManager relationships
        builder.Entity<VendorManager>(entity =>
        {
            entity.HasOne(m => m.AppUser)
                .WithOne()
                .HasForeignKey<VendorManager>(m => m.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ServiceItem relationships
        builder.Entity<ServiceItem>(entity =>
        {
            entity.HasMany(s => s.Images)
                .WithOne(i => i.ServiceItem)
                .HasForeignKey(i => i.ServiceItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(s => s.Price)
                .HasColumnType("decimal(18,2)");

            entity.Property(s => s.DiscountedPrice)
                .HasColumnType("decimal(18,2)");
        });

        // VendorLocation relationships
        builder.Entity<VendorLocation>(entity =>
        {
            entity.HasIndex(l => new { l.VendorId, l.IsPrimary })
                .HasFilter("[IsPrimary] = 1")
                .IsUnique();

            entity.Property(l => l.Latitude)
                .HasColumnType("decimal(9,6)");

            entity.Property(l => l.Longitude)
                .HasColumnType("decimal(9,6)");
        });

        // VendorBusinessHour relationships
        builder.Entity<VendorBusinessHour>(entity =>
        {
            entity.HasIndex(b => new { b.VendorId, b.DayOfWeek })
                .IsUnique();
        });

        // Customer relationships
        builder.Entity<Customer>(entity =>
        {
            entity.HasOne(c => c.AppUser)
                .WithOne()
                .HasForeignKey<Customer>(c => c.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Addresses)
                .WithOne(a => a.Customer)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Reviews)
                .WithOne(r => r.Customer)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Bookings)
                .WithOne(b => b.Customer)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Favorites)
                .WithOne(f => f.Customer)
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HallManager relationships - matching VendorManager pattern
        builder.Entity<HallManager>(entity =>
        {
            entity.HasKey(hm => hm.Id);
            entity.HasOne(hm => hm.AppUser)
                .WithOne()
                .HasForeignKey<HallManager>(hm => hm.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // Configure many-to-many between Hall and HallManager explicitly
        builder.Entity<Hall>()
            .HasMany(h => h.Managers)
            .WithMany(hm => hm.Halls)
            .UsingEntity(j => j.ToTable("HallHallManager"));

        // Notification relationships
        builder.Entity<Notification>()
            .HasOne(n => n.AppUser)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Booking relationships
        builder.Entity<Booking>(entity =>
        {
            // Configure decimal properties with proper precision and scale
            entity.Property(b => b.HallCost)
                .HasColumnType("decimal(18,2)");
            entity.Property(b => b.VendorServicesCost)
                .HasColumnType("decimal(18,2)");
            entity.Property(b => b.Subtotal)
                .HasColumnType("decimal(18,2)");
            entity.Property(b => b.DiscountAmount)
                .HasColumnType("decimal(18,2)");
            entity.Property(b => b.TaxAmount)
                .HasColumnType("decimal(18,2)");
            entity.Property(b => b.TaxRate)
                .HasColumnType("decimal(5,4)"); // For percentage values like 0.15 (15%)
            entity.Property(b => b.TotalAmount)
                .HasColumnType("decimal(18,2)");

            entity.HasOne(b => b.PackageDetails)
                .WithOne(p => p.Booking)
                .HasForeignKey<BookingPackage>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.VendorBookings)
                .WithOne(vb => vb.Booking)
                .HasForeignKey(vb => vb.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VendorBooking relationships
        builder.Entity<VendorBooking>(entity =>
        {
            entity.Property(vb => vb.TotalAmount)
                .HasColumnType("decimal(18,2)");
                
            entity.HasMany(vb => vb.Services)
                .WithOne(s => s.VendorBooking)
                .HasForeignKey(s => s.VendorBookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VendorBookingService relationships
        builder.Entity<VendorBookingService>(entity =>
        {
            entity.Property(s => s.UnitPrice)
                .HasColumnType("decimal(18,2)");
            entity.Property(s => s.TotalPrice)
                .HasColumnType("decimal(18,2)");
                
            entity.HasOne(s => s.ServiceItem)
                .WithMany()
                .HasForeignKey(s => s.ServiceItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Hall decimal precision configuration (only for monetary values)
        builder.Entity<Hall>(entity =>
        {
            entity.Property(h => h.BothWeekDays)
                .HasColumnType("decimal(18,2)");
            entity.Property(h => h.BothWeekEnds)
                .HasColumnType("decimal(18,2)");
            entity.Property(h => h.MaleWeekDays)
                .HasColumnType("decimal(18,2)");
            entity.Property(h => h.MaleWeekEnds)
                .HasColumnType("decimal(18,2)");
            entity.Property(h => h.FemaleWeekDays)
                .HasColumnType("decimal(18,2)");
            entity.Property(h => h.FemaleWeekEnds)
                .HasColumnType("decimal(18,2)");
            // AverageRating is now double - no configuration needed
        });

        // ServiceItem decimal precision configuration (only for monetary values)
        builder.Entity<ServiceItem>(entity =>
        {
            entity.Property(si => si.Price)
                .HasColumnType("decimal(18,2)");
            entity.Property(si => si.DiscountedPrice)
                .HasColumnType("decimal(18,2)");
        });

        // VendorLocation - Latitude/Longitude are now double - no configuration needed

        // Invoice relationships and configurations
        builder.Entity<Invoice>(entity =>
        {
            entity.HasOne(i => i.Booking)
                .WithOne(b => b.Invoice)
                .HasForeignKey<Invoice>(i => i.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Hall)
                .WithMany()
                .HasForeignKey(i => i.HallId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(i => i.LineItems)
                .WithOne(li => li.Invoice)
                .HasForeignKey(li => li.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique invoice number
            entity.HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            // ZATCA UUID index for quick lookup
            entity.HasIndex(i => i.ZATCA_UUID);

            // Decimal precision for monetary values
            entity.Property(i => i.SubtotalBeforeTax)
                .HasColumnType("decimal(18,2)");
            entity.Property(i => i.TaxableAmount)
                .HasColumnType("decimal(18,2)");
            entity.Property(i => i.TaxRate)
                .HasColumnType("decimal(5,2)");
            entity.Property(i => i.TaxAmount)
                .HasColumnType("decimal(18,2)");
            entity.Property(i => i.DiscountAmount)
                .HasColumnType("decimal(18,2)");
            entity.Property(i => i.TotalAmountWithTax)
                .HasColumnType("decimal(18,2)");
        });

        // InvoiceLineItem configuration
        builder.Entity<InvoiceLineItem>(entity =>
        {
            // Decimal precision for monetary values
            entity.Property(li => li.Quantity)
                .HasColumnType("decimal(18,2)");
            entity.Property(li => li.UnitPrice)
                .HasColumnType("decimal(18,2)");
            entity.Property(li => li.DiscountAmount)
                .HasColumnType("decimal(18,2)");
            entity.Property(li => li.SubtotalBeforeTax)
                .HasColumnType("decimal(18,2)");
            entity.Property(li => li.TaxRate)
                .HasColumnType("decimal(5,2)");
            entity.Property(li => li.TaxAmount)
                .HasColumnType("decimal(18,2)");
            entity.Property(li => li.TotalAmount)
                .HasColumnType("decimal(18,2)");
        });

        // Payment relationships and configurations
        builder.Entity<Payment>(entity =>
        {
            entity.HasOne(p => p.Booking)
                .WithMany()
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(p => p.Refunds)
                .WithOne(r => r.Payment)
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique checkout ID
            entity.HasIndex(p => p.CheckoutId)
                .IsUnique();

            // Transaction ID index
            entity.HasIndex(p => p.TransactionId);

            // Decimal precision for monetary values
            entity.Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");
            entity.Property(p => p.RefundAmount)
                .HasColumnType("decimal(18,2)");
        });

        // PaymentRefund configuration
        builder.Entity<PaymentRefund>(entity =>
        {
            entity.HasOne(r => r.RequestedByUser)
                .WithMany()
                .HasForeignKey(r => r.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(r => r.RefundAmount)
                .HasColumnType("decimal(18,2)");
        });

        // Chat relationships and configurations
        builder.Entity<ChatConversation>(entity =>
        {
            entity.HasOne(c => c.Booking)
                .WithMany()
                .HasForeignKey(c => c.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // CreatedBy - the user who created the conversation
            entity.HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Hall context for HallManager conversations
            entity.HasOne(c => c.Hall)
                .WithMany()
                .HasForeignKey(c => c.HallId)
                .OnDelete(DeleteBehavior.SetNull);

            // Vendor context for VendorManager conversations
            entity.HasOne(c => c.Vendor)
                .WithMany()
                .HasForeignKey(c => c.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(c => c.SupportAgent)
                .WithMany()
                .HasForeignKey(c => c.SupportAgentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(c => c.Status);
            entity.HasIndex(c => c.CustomerId);
            entity.HasIndex(c => c.SupportAgentId);
            entity.HasIndex(c => c.CreatedByUserId);
            entity.HasIndex(c => c.CreatedAt);
        });

        // ChatMessage configuration
        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            entity.HasIndex(m => m.ConversationId);
            entity.HasIndex(m => m.SentAt);
            entity.HasIndex(m => m.IsRead);
        });

        // ChatStatistics configuration
        builder.Entity<ChatStatistics>(entity =>
        {
            entity.HasIndex(s => s.Date)
                .IsUnique();
        });
    }
}

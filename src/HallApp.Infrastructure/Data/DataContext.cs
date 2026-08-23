using HallApp.Core.Entities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.GdprEntities;
using HallApp.Core.Entities.NotificationEntities;
using HallApp.Core.Entities.ReviewEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.PaymentEntities;
using HallApp.Core.Entities.ChatEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data;

public class DataContext : IdentityDbContext<AppUser, AppRole, int,
    IdentityUserClaim<int>, AppUserRole, IdentityUserLogin<int>,
    IdentityRoleClaim<int>, IdentityUserToken<int>>, IDataProtectionKeyContext
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

    // Businesses applying to join, and the papers backing each application.
    public DbSet<VendorApplication> VendorApplications { get; set; }
    public DbSet<VendorDocument> VendorDocuments { get; set; }

    // Hall Entities
    public DbSet<Hall> Halls { get; set; }
    public DbSet<HallManager> HallManagers { get; set; }
    public DbSet<HallBlockedDate> HallBlockedDates { get; set; }

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
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }

    // HIGH-006: Financial Audit Log
    public DbSet<FinancialAuditLog> FinancialAuditLogs { get; set; }

    // HIGH-012: GDPR Data Retention
    public DbSet<DataRetentionRequest> DataRetentionRequests { get; set; }

    // Chat Entities
    public DbSet<ChatConversation> ChatConversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<ChatMessageReadStatus> ChatMessageReadStatuses { get; set; }
    public DbSet<ChatStatistics> ChatStatistics { get; set; }
    public DbSet<ChatRating> ChatRatings { get; set; }
    public DbSet<SupportCase> SupportCases { get; set; }

    // Organization Entities
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }

    // Purchase Orders
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    // Platform Settings
    public DbSet<PlatformSettings> PlatformSettings { get; set; }

    // Data Protection Entities
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // --- Vendor applications ------------------------------------------
        builder.Entity<VendorApplication>(entity =>
        {
            // The admin queue is filtered by status and ordered by submission.
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.ContactEmail);

            // One application per account: a second one would leave the approval
            // step unable to tell which to turn into a vendor.
            entity.HasIndex(a => a.AppUserId).IsUnique();

            entity.HasMany(a => a.Documents)
                .WithOne(d => d.VendorApplication)
                .HasForeignKey(d => d.VendorApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.VendorType)
                .WithMany()
                .HasForeignKey(a => a.VendorTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AppUser)
                .WithMany()
                .HasForeignKey(a => a.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<VendorDocument>(entity =>
        {
            // One row per document type per application: re-uploading a rejected
            // paper updates the row rather than adding a second one.
            entity.HasIndex(d => new { d.VendorApplicationId, d.DocumentType }).IsUnique();
            entity.HasIndex(d => d.Status);
        });

        // CRIT-001 FIX: Database sequence for atomic invoice number generation
        builder.HasSequence<int>("invoice_number_seq")
            .StartsAt(1)
            .IncrementsBy(1);

        // Database sequence for atomic purchase order number generation
        builder.HasSequence<int>("purchase_order_number_seq")
            .StartsAt(1)
            .IncrementsBy(1);

        // PlatformSettings configuration
        builder.Entity<PlatformSettings>(entity =>
        {
            entity.HasKey(ps => ps.Id);

            entity.Property(ps => ps.CompanyName).HasMaxLength(200);
            entity.Property(ps => ps.LegalName).HasMaxLength(200);
            entity.Property(ps => ps.VatNumber).HasMaxLength(50);
            entity.Property(ps => ps.CommercialRegistrationNumber).HasMaxLength(50);
            entity.Property(ps => ps.BuildingNumber).HasMaxLength(10);
            entity.Property(ps => ps.StreetName).HasMaxLength(100);
            entity.Property(ps => ps.District).HasMaxLength(100);
            entity.Property(ps => ps.City).HasMaxLength(100);
            entity.Property(ps => ps.PostalCode).HasMaxLength(10);
            entity.Property(ps => ps.CountryCode).HasMaxLength(2);
            entity.Property(ps => ps.Email).HasMaxLength(256);
            entity.Property(ps => ps.Phone).HasMaxLength(20);
            entity.Property(ps => ps.WhatsApp).HasMaxLength(20);
            entity.Property(ps => ps.Website).HasMaxLength(500);
            entity.Property(ps => ps.BankName).HasMaxLength(100);
            entity.Property(ps => ps.BankAccountNumber).HasMaxLength(30);
            entity.Property(ps => ps.BankIban).HasMaxLength(34);
            entity.Property(ps => ps.LogoUrl).HasMaxLength(500);
            entity.Property(ps => ps.UpdatedBy).HasMaxLength(100);

            entity.Property(ps => ps.DefaultHallCommissionRate)
                .HasColumnType("decimal(5,4)");
            entity.Property(ps => ps.DefaultVendorCommissionRate)
                .HasColumnType("decimal(5,4)");

            entity.HasIndex(ps => ps.IsActive);
        });

        // PurchaseOrder configuration
        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(po => po.Id);

            entity.HasOne(po => po.Invoice)
                .WithMany(i => i.PurchaseOrders)
                .HasForeignKey(po => po.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(po => po.Booking)
                .WithMany(b => b.PurchaseOrders)
                .HasForeignKey(po => po.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(po => po.SupplierOrganization)
                .WithMany()
                .HasForeignKey(po => po.SupplierOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique PO number
            entity.HasIndex(po => po.PurchaseOrderNumber)
                .IsUnique();

            // Indexes for querying
            entity.HasIndex(po => po.InvoiceId);
            entity.HasIndex(po => po.BookingId);
            entity.HasIndex(po => po.SupplierOrganizationId);
            entity.HasIndex(po => po.Status);
            entity.HasIndex(po => po.CreatedAt);

            // String length constraints
            entity.Property(po => po.PurchaseOrderNumber).HasMaxLength(50);
            entity.Property(po => po.SupplierType).HasMaxLength(20);
            entity.Property(po => po.SupplierName).HasMaxLength(200);
            entity.Property(po => po.SupplierVatNumber).HasMaxLength(50);
            entity.Property(po => po.SupplierCommercialRegistrationNumber).HasMaxLength(50);
            entity.Property(po => po.SupplierAddress).HasMaxLength(500);
            entity.Property(po => po.SupplierBankIban).HasMaxLength(34);
            entity.Property(po => po.SupplierBankName).HasMaxLength(100);
            entity.Property(po => po.Currency).HasMaxLength(3);
            entity.Property(po => po.Status).HasMaxLength(30);
            entity.Property(po => po.PaymentReference).HasMaxLength(200);
            entity.Property(po => po.CreatedBy).HasMaxLength(100);

            // Decimal precision for financial values
            entity.Property(po => po.GrossAmount).HasColumnType("decimal(18,2)");
            entity.Property(po => po.CommissionRate).HasColumnType("decimal(5,4)");
            entity.Property(po => po.CommissionAmount).HasColumnType("decimal(18,2)");
            entity.Property(po => po.NetAmount).HasColumnType("decimal(18,2)");
            entity.Property(po => po.TaxRate).HasColumnType("decimal(5,4)");
            entity.Property(po => po.TaxAmount).HasColumnType("decimal(18,2)");
            entity.Property(po => po.TotalAmount).HasColumnType("decimal(18,2)");

            // Optimistic concurrency
            entity.Property(po => po.RowVersion).IsRowVersion();
        });

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
                .HasFilter("\"IsPrimary\" = true")
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

        // HallBlockedDate relationships and configurations
        builder.Entity<HallBlockedDate>(entity =>
        {
            entity.HasKey(hbd => hbd.Id);

            entity.HasOne(hbd => hbd.Hall)
                .WithMany()
                .HasForeignKey(hbd => hbd.HallId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(hbd => hbd.BlockedByUser)
                .WithMany()
                .HasForeignKey(hbd => hbd.BlockedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(hbd => hbd.Reason)
                .IsRequired()
                .HasMaxLength(500);

            // Indexes for performance
            entity.HasIndex(hbd => hbd.HallId);
            entity.HasIndex(hbd => new { hbd.HallId, hbd.IsActive });
            entity.HasIndex(hbd => new { hbd.HallId, hbd.StartDate, hbd.EndDate });
        });

        // Review entity configuration
        builder.Entity<Review>(entity =>
        {
            // Manager response relationship
            entity.HasOne(r => r.ManagerResponder)
                .WithMany()
                .HasForeignKey(r => r.ManagerResponderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Flagged by user relationship
            entity.HasOne(r => r.FlaggedByUser)
                .WithMany()
                .HasForeignKey(r => r.FlaggedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(r => r.ManagerResponse)
                .HasMaxLength(1000);

            entity.Property(r => r.FlagReason)
                .HasMaxLength(500);

            entity.Property(r => r.Content)
                .HasMaxLength(1000);

            entity.Property(r => r.RejectionReason)
                .HasMaxLength(500);

            // Indexes for performance
            entity.HasIndex(r => r.HallId);
            entity.HasIndex(r => new { r.HallId, r.IsFlagged });
            entity.HasIndex(r => new { r.HallId, r.Rating });
            entity.HasIndex(r => r.CustomerId);
        });

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

            // BONUS: Optimistic concurrency via RowVersion
            entity.Property(b => b.RowVersion).IsRowVersion();
        });

        // CRIT-003 FIX: BookingPackage decimal precision for financial values
        builder.Entity<BookingPackage>(entity =>
        {
            entity.Property(bp => bp.Price)
                .HasColumnType("decimal(18,2)");
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

            // Invoice page redesign: indexes for filtered queries and statistics
            entity.HasIndex(i => i.HallId);
            entity.HasIndex(i => i.CustomerId);
            entity.HasIndex(i => i.InvoiceDate);
            entity.HasIndex(i => i.PaymentStatus);
            entity.HasIndex(i => i.IsCancelled);
            entity.HasIndex(i => new { i.HallId, i.PaymentStatus });
            entity.HasIndex(i => new { i.HallId, i.InvoiceDate });

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
            entity.Property(i => i.RefundAmount)
                .HasColumnType("decimal(18,2)");

            // BONUS: Optimistic concurrency via RowVersion
            entity.Property(i => i.RowVersion).IsRowVersion();
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

            entity.HasMany(m => m.ReadStatuses)
                .WithOne(rs => rs.Message)
                .HasForeignKey(rs => rs.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(m => m.ConversationId);
            entity.HasIndex(m => m.SentAt);
            entity.HasIndex(m => m.IsRead);
        });

        // ChatMessageReadStatus configuration - Per-user read tracking
        builder.Entity<ChatMessageReadStatus>(entity =>
        {
            entity.HasOne(rs => rs.User)
                .WithMany()
                .HasForeignKey(rs => rs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one read status per user per message
            entity.HasIndex(rs => new { rs.MessageId, rs.UserId })
                .IsUnique();

            // Index for querying unread messages for a user
            entity.HasIndex(rs => new { rs.UserId, rs.IsRead });
        });

        // ChatStatistics configuration
        builder.Entity<ChatStatistics>(entity =>
        {
            entity.HasIndex(s => s.Date)
                .IsUnique();
        });

        // ChatRating configuration - Per-user conversation ratings
        builder.Entity<ChatRating>(entity =>
        {
            entity.HasOne(r => r.Conversation)
                .WithMany(c => c.Ratings)
                .HasForeignKey(r => r.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: one rating per user per conversation
            entity.HasIndex(r => new { r.ConversationId, r.UserId })
                .IsUnique();

            // Index for querying ratings by conversation
            entity.HasIndex(r => r.ConversationId);
        });

        // SupportCase configuration
        builder.Entity<SupportCase>(entity =>
        {
            entity.HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Booking)
                .WithMany()
                .HasForeignKey(s => s.BookingId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.Hall)
                .WithMany()
                .HasForeignKey(s => s.HallId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.Vendor)
                .WithMany()
                .HasForeignKey(s => s.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-one relationship with ChatConversation
            entity.HasOne(s => s.Conversation)
                .WithOne(c => c.Case)
                .HasForeignKey<SupportCase>(s => s.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique case number
            entity.HasIndex(s => s.CaseNumber)
                .IsUnique();

            // Indexes for querying
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.CreatedByUserId);
            entity.HasIndex(s => s.CreatedAt);
        });

        // ChatConversation - ClosedBy relationship
        builder.Entity<ChatConversation>(entity =>
        {
            entity.HasOne(c => c.ClosedBy)
                .WithMany()
                .HasForeignKey(c => c.ClosedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Index for closed conversations queries
            entity.HasIndex(c => c.ClosedByUserId);
        });

        // Organization relationships
        builder.Entity<Organization>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.HasOne(o => o.Owner)
                .WithMany()
                .HasForeignKey(o => o.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(o => o.Members)
                .WithOne(m => m.Organization)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.Halls)
                .WithOne(h => h.Organization)
                .HasForeignKey(h => h.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(o => o.Vendors)
                .WithOne(v => v.Organization)
                .HasForeignKey(v => v.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(o => o.Type)
                .IsRequired()
                .HasMaxLength(50);

            // Business Registration fields
            entity.Property(o => o.CommercialRegistrationNumber)
                .HasMaxLength(50)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.VatNumber)
                .HasMaxLength(50)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.LegalName)
                .HasMaxLength(200)
                .HasDefaultValue(string.Empty);

            // Legal Address fields
            entity.Property(o => o.BuildingNumber)
                .HasMaxLength(10)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.StreetName)
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.District)
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.City)
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.PostalCode)
                .HasMaxLength(10)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.CountryCode)
                .HasMaxLength(2)
                .HasDefaultValue("SA");

            // Contact Information fields
            entity.Property(o => o.Email)
                .HasMaxLength(256)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.Phone)
                .HasMaxLength(20)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.WhatsApp)
                .HasMaxLength(20)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.Website)
                .HasMaxLength(500)
                .HasDefaultValue(string.Empty);

            // Bank Account fields
            entity.Property(o => o.BankName)
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.BankAccountNumber)
                .HasMaxLength(30)
                .HasDefaultValue(string.Empty);

            entity.Property(o => o.BankIban)
                .HasMaxLength(34)
                .HasDefaultValue(string.Empty);

            // Commission override
            entity.Property(o => o.CommissionRate)
                .HasColumnType("decimal(5,4)");

            // Metadata
            entity.Property(o => o.IsVerified)
                .HasDefaultValue(false);

            entity.HasIndex(o => o.OwnerId);
            entity.HasIndex(o => o.Type);

            // Unique filtered index: only one active organization per owner.
            // Prevents the check-then-create race condition at the database level.
            entity.HasIndex(o => o.OwnerId)
                .IsUnique()
                .HasFilter("\"IsActive\" = true")
                .HasDatabaseName("IX_Organizations_OwnerId_UniqueActive");
        });

        // OrganizationMember relationships
        builder.Entity<OrganizationMember>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.HasOne(m => m.AppUser)
                .WithMany()
                .HasForeignKey(m => m.AppUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.InvitedByUser)
                .WithMany()
                .HasForeignKey(m => m.InvitedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(m => m.Role)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(m => new { m.OrganizationId, m.AppUserId })
                .IsUnique();

            entity.HasIndex(m => m.AppUserId);
        });

        // Hall - AssignedToHallManager relationship
        builder.Entity<Hall>(entity =>
        {
            entity.HasOne(h => h.AssignedToHallManager)
                .WithMany()
                .HasForeignKey(h => h.AssignedToHallManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(h => h.OrganizationId);
            entity.HasIndex(h => h.AssignedToHallManagerId);
        });

        // Vendor - AssignedToVendorManager relationship
        builder.Entity<Vendor>(entity =>
        {
            entity.HasOne(v => v.AssignedToVendorManager)
                .WithMany()
                .HasForeignKey(v => v.AssignedToVendorManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(v => v.OrganizationId);
            entity.HasIndex(v => v.AssignedToVendorManagerId);
        });

        // HIGH-006: FinancialAuditLog configuration for financial operation audit trails
        builder.Entity<FinancialAuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.OperationType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.EntityId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.IpAddress)
                .HasMaxLength(50);

            entity.Property(a => a.UserAgent)
                .HasMaxLength(500);

            entity.Property(a => a.CorrelationId)
                .HasMaxLength(100);

            entity.Property(a => a.Description)
                .HasMaxLength(1000);

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for querying
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
            entity.HasIndex(a => a.CorrelationId);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.OperationType);
        });

        // HIGH-012: AppUser GDPR fields
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.DataRetentionPolicy)
                .HasDefaultValue(DataRetentionPolicy.Standard);

            entity.Property(u => u.IsAnonymized)
                .HasDefaultValue(false);

            entity.HasIndex(u => u.IsAnonymized);
            entity.HasIndex(u => u.DataRetentionPolicy);
            entity.HasIndex(u => u.LastActivityAt);
        });

        // HIGH-012: DataRetentionRequest configuration for GDPR data retention requests
        builder.Entity<DataRetentionRequest>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.RequestType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(r => r.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(r => r.Reason)
                .HasMaxLength(1000);

            entity.Property(r => r.RejectionReason)
                .HasMaxLength(1000);

            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.RequestedByUser)
                .WithMany()
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.ProcessedByUser)
                .WithMany()
                .HasForeignKey(r => r.ProcessedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for querying
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.RequestedAt);
            entity.HasIndex(r => new { r.UserId, r.Status });
        });

        // HIGH-003 FIX: IdempotencyKey configuration for preventing duplicate payment processing
        builder.Entity<IdempotencyKey>(entity =>
        {
            entity.HasKey(ik => ik.Id);

            entity.Property(ik => ik.Key)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(ik => ik.OperationType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(ik => ik.UserId)
                .IsRequired();

            entity.Property(ik => ik.ProcessedAt)
                .IsRequired();

            entity.Property(ik => ik.ExpiresAt)
                .IsRequired();

            // Unique index on Key to enforce uniqueness and fast lookups
            entity.HasIndex(ik => ik.Key)
                .IsUnique();

            // Index for cleanup queries (delete expired keys)
            entity.HasIndex(ik => ik.ExpiresAt);

            // Composite index for operation type and user queries
            entity.HasIndex(ik => new { ik.UserId, ik.OperationType });
        });
    }
}

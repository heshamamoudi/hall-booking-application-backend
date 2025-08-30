# Database Schema Documentation

## Entity Relationship Overview

### Core Identity Management
```sql
-- ASP.NET Core Identity Tables
AspNetUsers (AppUser)
├── Id (int, PK)
├── FirstName (nvarchar(50))
├── LastName (nvarchar(50))
├── Email (nvarchar(256))
├── PhoneNumber (nvarchar(20))
├── Active (bit)
├── Created (datetime2)
├── Updated (datetime2?)

AspNetRoles
├── Id (nvarchar(450), PK)
├── Name (nvarchar(256))
└── NormalizedName (nvarchar(256))

AspNetUserRoles
├── UserId (nvarchar(450), FK -> AspNetUsers.Id)
└── RoleId (nvarchar(450), FK -> AspNetRoles.Id)
```

### Business User Entities
```sql
-- Customer Domain
Customers
├── Id (int, PK, Identity)
├── AppUserId (int, FK -> AspNetUsers.Id)
├── Credit (decimal(18,2), default 0)
├── IsActive (bit, default 1)
├── CreatedAt (datetime2)
└── UpdatedAt (datetime2?)

-- Hall Manager Domain  
HallManagers
├── Id (int, PK, Identity)
├── AppUserId (int, FK -> AspNetUsers.Id)
├── CompanyName (nvarchar(100))
├── CommercialRegistrationNumber (nvarchar(50))
├── IsApproved (bit, default 0)
├── CreatedAt (datetime2)
└── ApprovedAt (datetime2?)

-- Vendor Manager Domain
VendorManagers
├── Id (int, PK, Identity)
├── AppUserId (int, FK -> AspNetUsers.Id)
├── CommercialRegistrationNumber (nvarchar(50)?)
├── VatNumber (nvarchar(20)?)
├── IsApproved (bit, default 0)
├── CreatedAt (datetime2)
└── ApprovedAt (datetime2?)
```

### Business Entities
```sql
-- Hall Domain
Halls
├── Id (int, PK, Identity)
├── Name (nvarchar(100))
├── Description (nvarchar(500))
├── Capacity (int)
├── PricePerHour (decimal(18,2))
├── Location (nvarchar(100))
├── Address (nvarchar(200))
├── IsActive (bit, default 1)
├── HallManagerId (int, FK -> HallManagers.Id)
├── CreatedAt (datetime2)
└── UpdatedAt (datetime2?)

HallImages
├── Id (int, PK, Identity)
├── HallId (int, FK -> Halls.Id)
├── ImageUrl (nvarchar(500))
├── Caption (nvarchar(200)?)
├── IsPrimary (bit, default 0)
└── UploadedAt (datetime2)

HallAmenities
├── Id (int, PK, Identity)
├── HallId (int, FK -> Halls.Id)
├── AmenityName (nvarchar(100))
├── Description (nvarchar(200)?)
└── IsAvailable (bit, default 1)

-- Vendor Domain
VendorTypes
├── Id (int, PK, Identity)
├── Name (nvarchar(50))
├── Description (nvarchar(200)?)
└── IsActive (bit, default 1)

Vendors
├── Id (int, PK, Identity)
├── Name (nvarchar(100))
├── Description (nvarchar(500))
├── ContactEmail (nvarchar(100))
├── ContactPhone (nvarchar(20))
├── Address (nvarchar(200)?)
├── IsActive (bit, default 1)
├── VendorTypeId (int, FK -> VendorTypes.Id)
├── VendorManagerId (int, FK -> VendorManagers.Id)
├── CreatedAt (datetime2)
└── UpdatedAt (datetime2?)

ServiceItems
├── Id (int, PK, Identity)
├── VendorId (int, FK -> Vendors.Id)
├── Name (nvarchar(100))
├── Description (nvarchar(300))
├── PricePerUnit (decimal(18,2))
├── UnitType (nvarchar(20)) -- 'person', 'hour', 'item'
├── MinimumQuantity (int, default 1)
├── IsActive (bit, default 1)
├── CreatedAt (datetime2)
└── UpdatedAt (datetime2?)
```

### Booking System
```sql
-- Main Booking Entity
Bookings
├── Id (int, PK, Identity)
├── CustomerId (int, FK -> Customers.Id)
├── HallId (int, FK -> Halls.Id)
├── EventDate (datetime2)
├── StartTime (time)
├── EndTime (time)
├── EventType (nvarchar(50))
├── GuestCount (int)
├── SpecialRequests (nvarchar(500)?)
├── Status (nvarchar(20)) -- 'Pending', 'Confirmed', 'Completed', 'Cancelled'
├── TotalAmount (decimal(18,2))
├── CreatedAt (datetime2)
├── UpdatedAt (datetime2?)
└── CompletedAt (datetime2?)

-- Vendor Services in Booking
VendorBookings
├── Id (int, PK, Identity)
├── BookingId (int, FK -> Bookings.Id)
├── VendorId (int, FK -> Vendors.Id)
├── ServiceItemId (int, FK -> ServiceItems.Id)
├── Quantity (int)
├── UnitPrice (decimal(18,2))
├── TotalPrice (decimal(18,2))
├── Status (nvarchar(20))
├── Notes (nvarchar(300)?)
├── CreatedAt (datetime2)
└── UpdatedAt (datetime2?)

-- Availability Management
VendorAvailability
├── Id (int, PK, Identity)
├── VendorId (int, FK -> Vendors.Id)
├── Date (date)
├── StartTime (time)
├── EndTime (time)
├── IsAvailable (bit, default 1)
├── MaxBookings (int, default 1)
└── Notes (nvarchar(200)?)
```

### Social Features
```sql
-- Reviews and Ratings
Reviews
├── Id (int, PK, Identity)
├── CustomerId (int, FK -> Customers.Id)
├── EntityType (nvarchar(20)) -- 'Hall', 'Vendor'
├── EntityId (int) -- Hall.Id or Vendor.Id
├── Rating (int) -- 1-5 stars
├── Comment (nvarchar(500)?)
├── IsApproved (bit, default 0)
├── CreatedAt (datetime2)
└── UpdatedAt (datetime2?)

-- Customer Favorites
Favorites
├── Id (int, PK, Identity)
├── CustomerId (int, FK -> Customers.Id)
├── EntityType (nvarchar(20)) -- 'Hall', 'Vendor'
├── EntityId (int)
└── CreatedAt (datetime2)
```

### Notifications & Communications
```sql
Notifications
├── Id (int, PK, Identity)
├── UserId (int, FK -> AspNetUsers.Id)
├── Title (nvarchar(100))
├── Message (nvarchar(500))
├── Type (nvarchar(30)) -- 'Booking', 'Payment', 'System', 'Reminder'
├── IsRead (bit, default 0)
├── CreatedAt (datetime2)
└── ReadAt (datetime2?)
```

## Indexes and Constraints

### Primary Keys and Foreign Keys
```sql
-- Customer Constraints
ALTER TABLE Customers 
ADD CONSTRAINT FK_Customers_AppUsers FOREIGN KEY (AppUserId) REFERENCES AspNetUsers(Id)

-- HallManager Constraints  
ALTER TABLE HallManagers
ADD CONSTRAINT FK_HallManagers_AppUsers FOREIGN KEY (AppUserId) REFERENCES AspNetUsers(Id)

-- VendorManager Constraints
ALTER TABLE VendorManagers  
ADD CONSTRAINT FK_VendorManagers_AppUsers FOREIGN KEY (AppUserId) REFERENCES AspNetUsers(Id)

-- Booking Constraints
ALTER TABLE Bookings
ADD CONSTRAINT FK_Bookings_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
ADD CONSTRAINT FK_Bookings_Halls FOREIGN KEY (HallId) REFERENCES Halls(Id)
```

### Performance Indexes
```sql
-- User lookup indexes
CREATE INDEX IX_Customers_AppUserId ON Customers(AppUserId)
CREATE INDEX IX_HallManagers_AppUserId ON HallManagers(AppUserId)  
CREATE INDEX IX_VendorManagers_AppUserId ON VendorManagers(AppUserId)

-- Booking performance indexes
CREATE INDEX IX_Bookings_EventDate ON Bookings(EventDate)
CREATE INDEX IX_Bookings_Status ON Bookings(Status)
CREATE INDEX IX_Bookings_Customer_Status ON Bookings(CustomerId, Status)

-- Hall search indexes
CREATE INDEX IX_Halls_Location ON Halls(Location)
CREATE INDEX IX_Halls_Capacity ON Halls(Capacity)
CREATE INDEX IX_Halls_Price ON Halls(PricePerHour)
CREATE INDEX IX_Halls_Active ON Halls(IsActive)

-- Vendor search indexes  
CREATE INDEX IX_Vendors_Type ON Vendors(VendorTypeId)
CREATE INDEX IX_Vendors_Active ON Vendors(IsActive)
CREATE INDEX IX_ServiceItems_Vendor ON ServiceItems(VendorId, IsActive)
```

### Unique Constraints
```sql
-- Prevent duplicate user associations
ALTER TABLE Customers ADD CONSTRAINT UQ_Customers_AppUserId UNIQUE(AppUserId)
ALTER TABLE HallManagers ADD CONSTRAINT UQ_HallManagers_AppUserId UNIQUE(AppUserId)
ALTER TABLE VendorManagers ADD CONSTRAINT UQ_VendorManagers_AppUserId UNIQUE(AppUserId)

-- Business registration uniqueness
ALTER TABLE HallManagers ADD CONSTRAINT UQ_HallManagers_RegNumber UNIQUE(CommercialRegistrationNumber)
ALTER TABLE VendorManagers ADD CONSTRAINT UQ_VendorManagers_RegNumber UNIQUE(CommercialRegistrationNumber)
ALTER TABLE VendorManagers ADD CONSTRAINT UQ_VendorManagers_VatNumber UNIQUE(VatNumber)

-- Prevent duplicate favorites
ALTER TABLE Favorites ADD CONSTRAINT UQ_Favorites_Customer_Entity UNIQUE(CustomerId, EntityType, EntityId)
```

## Data Validation Rules

### Business Rules Enforced at Database Level
```sql
-- Booking date must be in future
ALTER TABLE Bookings ADD CONSTRAINT CK_Bookings_FutureDate 
CHECK (EventDate >= CAST(GETDATE() as DATE))

-- End time must be after start time  
ALTER TABLE Bookings ADD CONSTRAINT CK_Bookings_TimeOrder
CHECK (EndTime > StartTime)

-- Guest count must be positive and within hall capacity
ALTER TABLE Bookings ADD CONSTRAINT CK_Bookings_GuestCount
CHECK (GuestCount > 0)

-- Rating must be between 1 and 5
ALTER TABLE Reviews ADD CONSTRAINT CK_Reviews_Rating
CHECK (Rating >= 1 AND Rating <= 5)

-- Hall capacity must be positive
ALTER TABLE Halls ADD CONSTRAINT CK_Halls_Capacity  
CHECK (Capacity > 0)

-- Price must be non-negative
ALTER TABLE Halls ADD CONSTRAINT CK_Halls_Price
CHECK (PricePerHour >= 0)

ALTER TABLE ServiceItems ADD CONSTRAINT CK_ServiceItems_Price
CHECK (PricePerUnit >= 0)

-- Credit must be non-negative  
ALTER TABLE Customers ADD CONSTRAINT CK_Customers_Credit
CHECK (Credit >= 0)
```

## Entity Framework Core Configuration

### Entity Configurations
```csharp
// Customer Configuration
modelBuilder.Entity<Customer>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Credit).HasColumnType("decimal(18,2)").HasDefaultValue(0);
    entity.Property(e => e.IsActive).HasDefaultValue(true);
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    
    entity.HasOne(e => e.AppUser)
          .WithOne()
          .HasForeignKey<Customer>(e => e.AppUserId)
          .OnDelete(DeleteBehavior.Cascade);
});

// HallManager Configuration  
modelBuilder.Entity<HallManager>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.CompanyName).HasMaxLength(100).IsRequired();
    entity.Property(e => e.CommercialRegistrationNumber).HasMaxLength(50).IsRequired();
    entity.Property(e => e.IsApproved).HasDefaultValue(false);
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    
    entity.HasIndex(e => e.AppUserId).IsUnique();
    entity.HasIndex(e => e.CommercialRegistrationNumber).IsUnique();
});

// VendorManager Configuration
modelBuilder.Entity<VendorManager>(entity =>  
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.CommercialRegistrationNumber).HasMaxLength(50);
    entity.Property(e => e.VatNumber).HasMaxLength(20);
    entity.Property(e => e.IsApproved).HasDefaultValue(false);
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    
    entity.HasIndex(e => e.AppUserId).IsUnique();
});

// Booking Configuration
modelBuilder.Entity<Booking>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
    entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    
    entity.HasOne(e => e.Customer)
          .WithMany(c => c.Bookings)
          .HasForeignKey(e => e.CustomerId)
          .OnDelete(DeleteBehavior.Restrict);
          
    entity.HasOne(e => e.Hall)
          .WithMany(h => h.Bookings)  
          .HasForeignKey(e => e.HallId)
          .OnDelete(DeleteBehavior.Restrict);
});
```

## Migration History

### Initial Migration - Identity Setup
```sql
-- 20240101000001_InitialCreate.sql
CREATE TABLE AspNetUsers (...)
CREATE TABLE AspNetRoles (...)  
CREATE TABLE AspNetUserRoles (...)
```

### Business Entities Migration
```sql
-- 20240101000002_AddBusinessEntities.sql
CREATE TABLE Customers (...)
CREATE TABLE HallManagers (...)
CREATE TABLE VendorManagers (...)
CREATE TABLE Halls (...)
CREATE TABLE Vendors (...)
CREATE TABLE VendorTypes (...)
```

### Booking System Migration  
```sql
-- 20240101000003_AddBookingSystem.sql
CREATE TABLE Bookings (...)
CREATE TABLE VendorBookings (...)
CREATE TABLE ServiceItems (...)
CREATE TABLE VendorAvailability (...)
```

### Social Features Migration
```sql
-- 20240101000004_AddSocialFeatures.sql  
CREATE TABLE Reviews (...)
CREATE TABLE Favorites (...)
CREATE TABLE Notifications (...)
```

## Performance Considerations

### Query Optimization Strategies
- Composite indexes on frequently queried column combinations
- Partial indexes for active records only
- Covering indexes for read-heavy operations
- Proper foreign key relationships for join optimization

### Scaling Considerations
- Partition large tables by date (Bookings, Notifications)
- Archive completed bookings older than 2 years
- Implement read replicas for reporting queries
- Use materialized views for dashboard statistics

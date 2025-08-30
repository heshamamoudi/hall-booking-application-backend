# Service Architecture Documentation

## Service Refactoring: Separation of Concerns

### Overview
The application has been refactored to implement strict separation between identity/authentication concerns and business domain logic across all user types.

### Architecture Pattern

#### Pure Business Services
Services that focus solely on business domain operations without authentication concerns:

```csharp
// Customer Domain
ICustomerService -> CustomerService
  - GetCustomerByIdAsync(int customerId)
  - GetAllCustomersAsync()
  - CreateCustomerAsync(Customer customer)
  - UpdateCustomerAsync(Customer customer)
  - DeleteCustomerAsync(int customerId)
  - GetCustomersByStatusAsync(bool isActive)
  - UpdateCustomerCreditAsync(int customerId, decimal amount)
  - ValidateCustomerDataAsync(Customer customer)

// HallManager Domain
IHallManagerService -> HallManagerService
  - GetHallManagerByIdAsync(int hallManagerId)
  - GetHallManagerByAppUserIdAsync(int appUserId)
  - GetAllHallManagersAsync()
  - CreateHallManagerAsync(HallManager hallManager)
  - UpdateHallManagerAsync(HallManager hallManager)
  - DeleteHallManagerAsync(int hallManagerId)
  - ApproveHallManagerAsync(int hallManagerId, bool isApproved)
  - GetPendingApprovalHallManagersAsync()
  - GetApprovedHallManagersAsync()
  - UpdateCompanyInfoAsync(int hallManagerId, string companyName, string registrationNumber)
  - IsCommercialRegistrationUniqueAsync(string registrationNumber, int? excludeId)
  - IsCompanyNameUniqueAsync(string companyName, int? excludeId)
  - GetHallManagerHallCountAsync(int hallManagerId)

// VendorManager Domain  
IVendorManagerService -> VendorManagerService
  - GetVendorManagerByIdAsync(int vendorManagerId)
  - GetVendorManagerByAppUserIdAsync(int appUserId)
  - GetAllVendorManagersAsync()
  - CreateVendorManagerAsync(VendorManager vendorManager)
  - UpdateVendorManagerAsync(VendorManager vendorManager)
  - DeleteVendorManagerAsync(int vendorManagerId)
  - ApproveVendorManagerAsync(int vendorManagerId, bool isApproved)
  - GetPendingApprovalVendorManagersAsync()
  - GetApprovedVendorManagersAsync()
  - UpdateCommercialInfoAsync(int vendorManagerId, string? registrationNumber, string? vatNumber)
  - IsCommercialRegistrationUniqueAsync(string registrationNumber, int? excludeId)
  - IsVatNumberUniqueAsync(string vatNumber, int? excludeId)
  - GetVendorManagerVendorCountAsync(int vendorManagerId)
```

#### Profile Services
Services that orchestrate operations requiring both AppUser (identity) and business domain data:

```csharp
// Customer Profile Operations
ICustomerProfileService -> CustomerProfileService
  - GetCustomerProfileAsync(string userId) -> (AppUser, Customer)?
  - UpdateCustomerProfileAsync(string userId, AppUser userData, Customer customerData)
  - DeleteCustomerProfileAsync(string userId)
  - UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
  - VerifyEmailAsync(string userId, string token)
  - SendVerificationEmailAsync(string userId)
  - GetCustomerDashboardAsync(string userId)
  - UpdatePreferencesAsync(string userId, Customer preferences)

// HallManager Profile Operations
IHallManagerProfileService -> HallManagerProfileService
  - GetHallManagerProfileAsync(string userId) -> (AppUser, HallManager)?
  - UpdateHallManagerProfileAsync(string userId, AppUser userData, HallManager hallManagerData)
  - DeleteHallManagerProfileAsync(string userId)
  - UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
  - VerifyEmailAsync(string userId, string token)
  - SendVerificationEmailAsync(string userId)
  - GetHallManagerDashboardAsync(string userId)
  - UpdateBusinessPreferencesAsync(string userId, HallManager businessData)
  - ApproveHallManagerAsync(string userId, bool isApproved)

// VendorManager Profile Operations
IVendorProfileService -> VendorProfileService
  - GetVendorProfileAsync(string userId) -> (AppUser, VendorManager)?
  - UpdateVendorProfileAsync(string userId, AppUser userData, VendorManager vendorManagerData)
  - DeleteVendorProfileAsync(string userId)
  - UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
  - VerifyEmailAsync(string userId, string token)
  - SendVerificationEmailAsync(string userId)
  - GetVendorManagerDashboardAsync(string userId)
  - UpdateBusinessPreferencesAsync(string userId, VendorManager businessData)
  - ApproveVendorManagerAsync(string userId, bool isApproved)
```

### Data Transfer Objects (DTOs)

#### Business DTOs
For pure business domain operations:

```csharp
// Customer Business DTO
CustomerBusinessDto
  - int Id
  - int AppUserId
  - decimal Credit
  - bool IsActive
  - DateTime CreatedAt
  - DateTime? UpdatedAt
  - int TotalBookings
  - decimal TotalSpent

// HallManager Business DTO
HallManagerBusinessDto
  - int Id
  - int AppUserId
  - string CompanyName
  - string CommercialRegistrationNumber
  - bool IsApproved
  - DateTime CreatedAt
  - DateTime? ApprovedAt

// VendorManager Business DTO
VendorManagerBusinessDto
  - int Id
  - int AppUserId
  - string? CommercialRegistrationNumber
  - string? VatNumber
  - bool IsApproved
  - DateTime CreatedAt
  - DateTime? ApprovedAt

// Vendor Business DTO
VendorBusinessDto
  - int Id
  - string Name
  - string Description
  - string ContactEmail
  - string ContactPhone
  - string? Address
  - bool IsActive
  - int VendorTypeId
  - string VendorTypeName
  - decimal? Rating
  - int ReviewCount
```

#### Profile DTOs
For combined AppUser + Business entity operations:

```csharp
// Customer Profile DTO
CustomerProfileDto
  // From AppUser
  - int AppUserId
  - string Email
  - string FirstName
  - string LastName
  - string PhoneNumber
  - bool EmailConfirmed
  - bool Active
  - DateTime UserCreated
  - DateTime? UserUpdated
  
  // From Customer
  - int CustomerId
  - decimal Credit
  - bool IsActive
  - DateTime CustomerCreated
  - DateTime? CustomerUpdated
  
  // Aggregated Data
  - int TotalBookings
  - int ActiveBookings
  - int CompletedBookings
  - decimal TotalSpent
  - int FavoriteHalls
  - int Reviews

// HallManager Profile DTO
HallManagerProfileDto
  // From AppUser
  - int AppUserId
  - string Email
  - string FirstName
  - string LastName
  - string PhoneNumber
  - bool EmailConfirmed
  - bool Active
  - DateTime UserCreated
  - DateTime? UserUpdated
  
  // From HallManager
  - int HallManagerId
  - string CompanyName
  - string CommercialRegistrationNumber
  - bool IsApproved
  - DateTime HallManagerCreated
  - DateTime? ApprovedAt
  
  // Aggregated Data
  - int TotalHalls
  - int ActiveHalls
  - int PendingApprovals
  - double AverageRating
  - int TotalReviews
  - int TotalBookings

// VendorManager Profile DTO
VendorManagerProfileDto
  // From AppUser
  - int AppUserId
  - string Email
  - string FirstName
  - string LastName
  - string PhoneNumber
  - bool EmailConfirmed
  - bool Active
  - DateTime UserCreated
  - DateTime? UserUpdated
  
  // From VendorManager
  - int VendorManagerId
  - string? CommercialRegistrationNumber
  - string? VatNumber
  - bool IsApproved
  - DateTime VendorManagerCreated
  - DateTime? ApprovedAt
  
  // Aggregated Data
  - int TotalVendors
  - int ActiveVendors
  - int PendingApprovals
  - double AverageRating
  - int TotalReviews
  - decimal TotalRevenue
```

### AutoMapper Configuration

#### Entity to Business DTO Mappings
```csharp
// Customer mappings
CreateMap<Customer, CustomerBusinessDto>()
CreateMap<CustomerBusinessDto, Customer>()

// HallManager mappings  
CreateMap<HallManager, HallManagerBusinessDto>()
CreateMap<HallManagerBusinessDto, HallManager>()

// VendorManager mappings
CreateMap<VendorManager, VendorManagerBusinessDto>()
CreateMap<VendorManagerBusinessDto, VendorManager>()

// Vendor mappings
CreateMap<Vendor, VendorBusinessDto>()
CreateMap<VendorBusinessDto, Vendor>()
```

#### Tuple to Profile DTO Mappings
```csharp
// Customer profile mapping
CreateMap<(AppUser AppUser, Customer Customer), CustomerProfileDto>()
    .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.AppUser.Id))
    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.AppUser.Email))
    .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.AppUser.FirstName))
    // ... additional mappings

// HallManager profile mapping
CreateMap<(AppUser AppUser, HallManager HallManager), HallManagerProfileDto>()
    .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.AppUser.Id))
    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.AppUser.Email))
    // ... additional mappings

// VendorManager profile mapping  
CreateMap<(AppUser AppUser, VendorManager VendorManager), VendorManagerProfileDto>()
    .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.AppUser.Id))
    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.AppUser.Email))
    // ... additional mappings
```

### Dependency Injection Configuration

#### Service Registration
```csharp
// Core services
services.AddScoped<IUserService, UserService>();
services.AddScoped<ITokenService, TokenService>();
services.AddScoped<IHallService, HallService>();
services.AddScoped<IVendorService, VendorService>();
services.AddScoped<IBookingService, BookingService>();
services.AddScoped<INotificationService, NotificationService>();

// Customer services
services.AddScoped<ICustomerService, CustomerService>();
services.AddScoped<ICustomerProfileService, CustomerProfileService>();

// Vendor services
services.AddScoped<IVendorManagerService, VendorManagerService>();
services.AddScoped<IVendorProfileService, VendorProfileService>();

// HallManager services
services.AddScoped<IHallManagerService, HallManagerService>();
services.AddScoped<IHallManagerProfileService, HallManagerProfileService>();
```

### Repository Pattern Implementation

#### New Repositories Added
```csharp
// HallManager Repository
IHallManagerRepository : IGenericRepository<HallManager>
  - Task<HallManager?> GetByUserIdAsync(string userId)
  - Task<bool> CommercialRegistrationExistsAsync(string registrationNumber)
  - Task<bool> CompanyNameExistsAsync(string companyName)
  - Task<IEnumerable<HallManager>> GetPendingApprovalAsync()
  - Task<IEnumerable<HallManager>> GetApprovedAsync()
```

#### Unit of Work Extension
```csharp
IUnitOfWork
{
    // Existing repositories
    IVendorRepository VendorRepository { get; }
    IVendorManagerRepository VendorManagerRepository { get; }
    ICustomerRepository CustomerRepository { get; }
    IHallRepository HallRepository { get; }
    
    // New repository
    IHallManagerRepository HallManagerRepository { get; }
    
    Task<int> Complete();
}
```

### Controller Integration

#### AdminController Refactoring
```csharp
public class AdminController : BaseApiController
{
    private readonly IHallManagerService _hallManagerService;
    private readonly IHallManagerProfileService _hallManagerProfileService;
    private readonly IVendorManagerService _vendorManagerService;
    private readonly IVendorProfileService _vendorProfileService;

    // HallManager endpoints now use dedicated services
    [HttpPost("hall-managers")]
    public async Task<ActionResult<ApiResponse<HallManagerProfileDto>>> CreateHallManager(...)
    
    [HttpPut("hall-managers/{userId}")]  
    public async Task<ActionResult<ApiResponse<HallManagerProfileDto>>> UpdateHallManager(...)
    
    [HttpDelete("hall-managers/{userId}")]
    public async Task<ActionResult<ApiResponse>> DeleteHallManager(...)
    
    [HttpGet("hall-managers")]
    public async Task<ActionResult<ApiResponse<IEnumerable<HallManagerBusinessDto>>>> GetHallManagers(...)
}
```

### Benefits of This Architecture

1. **Clear Separation of Concerns**
   - Authentication logic isolated in UserService
   - Business logic isolated in domain services
   - Profile operations handled by dedicated services

2. **Maintainability**
   - Each service has single responsibility
   - Easy to locate and modify specific functionality
   - Clear dependency relationships

3. **Testability**
   - Services can be unit tested independently
   - Mock dependencies easily
   - Clear interfaces for testing

4. **Scalability**
   - Easy to add new user types following same pattern
   - Services can be extracted to microservices later
   - Consistent architecture across domains

5. **Code Reusability**
   - Common patterns across all user types
   - Reusable DTO structures
   - Consistent mapping strategies

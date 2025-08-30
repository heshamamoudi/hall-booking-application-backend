# Hall Booking Application Backend

## 🏢 Overview

The Hall Booking Application is a comprehensive event venue management system built with .NET 8, designed to connect customers with hall owners and service vendors for seamless event planning and booking.

## 🎯 Core Business Model

### **User Types**
- **Customers**: End users who book halls and services for events
- **Hall Managers**: Venue owners who manage their halls and bookings
- **Vendor Managers**: Service providers offering event-related services
- **Admins**: System administrators with full platform control

### **Key Features**
- 🏛️ **Hall Management**: Complete venue CRUD with availability tracking
- 🛎️ **Service Management**: Vendor services like catering, photography, decoration
- 📅 **Booking System**: Real-time availability and reservation management
- 👥 **User Management**: Role-based authentication and profile management
- 💳 **Payment Integration**: Secure payment processing
- ⭐ **Review System**: Customer feedback and ratings
- 📱 **Notifications**: Real-time updates for bookings and approvals

## 🏗️ Architecture Overview

### **Clean Architecture Pattern**
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Presentation  │    │   Application    │    │      Core       │
│   (Web API)     │───▶│   (Services)     │───▶│   (Entities)    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │                         ▲
                                ▼                         │
                       ┌──────────────────┐               │
                       │ Infrastructure   │───────────────┘
                       │ (Data & External)│
                       └──────────────────┘
```

### **Project Structure**
```
HallAppBackend/
├── src/
│   ├── HallApp.Core/           # Domain entities and interfaces
│   ├── HallApp.Application/    # Business logic and services
│   ├── HallApp.Infrastructure/ # Data access and external services
│   └── HallApp.Web/           # Web API controllers and configuration
└── docs/                      # Documentation
```

## 🔧 Technology Stack

### **Backend Technologies**
- **Framework**: .NET 8.0 Web API
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity + JWT
- **Mapping**: AutoMapper
- **Validation**: FluentValidation
- **Documentation**: Swagger/OpenAPI

### **Architecture Patterns**
- **Clean Architecture**: Separation of concerns across layers
- **Repository Pattern**: Data access abstraction
- **Unit of Work Pattern**: Transaction management
- **Service Layer Pattern**: Business logic encapsulation
- **Dependency Injection**: Loose coupling and testability

## 📊 Database Schema

### **Core Entities**
```sql
-- User Management
AppUsers (Identity Management)
├── Customers (Customer profiles)
├── HallManagers (Hall owner profiles)  
└── VendorManagers (Service provider profiles)

-- Business Entities
Halls (Venues)
├── HallAvailability (Booking calendar)
└── HallImages (Venue photos)

Vendors (Service Providers)
├── ServiceItems (Available services)
├── VendorAvailability (Service calendar)
└── VendorBookings (Service reservations)

-- Booking System
Bookings (Main reservations)
├── BookingItems (Booked services)
└── Payments (Financial transactions)

-- Social Features
Reviews (Customer feedback)
Favorites (Saved venues/services)
Notifications (System alerts)
```

## 🚀 Getting Started

### **Prerequisites**
- .NET 8.0 SDK
- SQL Server (LocalDB or Server)
- Visual Studio 2022 or VS Code

### **Installation**
```bash
# Clone repository
git clone [repository-url]
cd HallAppBackend

# Restore dependencies
dotnet restore

# Update database
dotnet ef database update --project src/HallApp.Infrastructure

# Run application
dotnet run --project src/HallApp.Web
```

### **Configuration**
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HallBookingDB;Trusted_Connection=true"
  },
  "JwtSettings": {
    "Key": "your-secret-key",
    "Issuer": "HallBookingApp",
    "Audience": "HallBookingApp-Users"
  }
}
```

## 🔌 API Endpoints

### **Authentication**
```http
POST   /api/v1/auth/login          # User login
POST   /api/v1/auth/register       # User registration
POST   /api/v1/auth/refresh        # Token refresh
POST   /api/v1/auth/logout         # User logout
```

### **Hall Management**
```http
GET    /api/v1/halls              # Get all halls
GET    /api/v1/halls/{id}         # Get hall by ID
POST   /api/v1/halls              # Create new hall
PUT    /api/v1/halls/{id}         # Update hall
DELETE /api/v1/halls/{id}         # Delete hall
GET    /api/v1/halls/search       # Search halls by criteria
```

### **Booking System**
```http
GET    /api/v1/bookings           # Get user bookings
POST   /api/v1/bookings           # Create new booking
PUT    /api/v1/bookings/{id}      # Update booking
DELETE /api/v1/bookings/{id}      # Cancel booking
GET    /api/v1/bookings/{id}/status # Get booking status
```

### **Vendor Services**
```http
GET    /api/v1/vendors            # Get all vendors
GET    /api/v1/vendors/{id}       # Get vendor details
GET    /api/v1/vendors/services   # Get available services
POST   /api/v1/vendors/book       # Book vendor service
```

### **Admin Management**
```http
GET    /api/v1/admin/users        # Get all users
POST   /api/v1/admin/hall-managers # Create hall manager
PUT    /api/v1/admin/hall-managers/{id} # Update hall manager
DELETE /api/v1/admin/hall-managers/{id} # Delete hall manager
GET    /api/v1/admin/statistics   # System statistics
```

## 🏛️ Service Architecture

### **Separation of Concerns**

#### **User Management Services**
```csharp
IUserService              // Core identity & authentication
ICustomerService          // Customer business logic
ICustomerProfileService   // Customer profile operations
IHallManagerService       // Hall manager business logic
IHallManagerProfileService // Hall manager profile operations
IVendorManagerService     // Vendor manager business logic  
IVendorProfileService     // Vendor manager profile operations
```

#### **Business Domain Services**
```csharp
IHallService             // Hall venue management
IVendorService           // Vendor service management
IBookingService          // Booking and reservation logic
INotificationService     // System notifications
IPaymentService          // Payment processing
```

### **Service Responsibilities**

#### **Pure Business Services**
- Focus solely on domain entity operations
- No authentication/identity concerns
- Repository pattern for data access
- Business validation and rules

#### **Profile Services**
- Combine authentication + business data
- Handle cross-cutting operations
- User profile management
- Dashboard and reporting data

## 🔒 Security & Authentication

### **JWT Token-Based Authentication**
- Stateless authentication
- Role-based authorization
- Token refresh mechanism
- Secure password hashing

### **Authorization Levels**
```csharp
[Authorize]                    // Authenticated users only
[Authorize(Roles = "Admin")]   // Admin-only access
[Authorize(Roles = "HallManager,Admin")] // Multiple roles
```

### **Data Protection**
- HTTPS enforcement
- Input validation and sanitization
- SQL injection prevention via EF Core
- XSS protection
- CORS configuration

## 🧪 Testing Strategy

### **Test Categories**
- **Unit Tests**: Service and repository testing
- **Integration Tests**: API endpoint testing
- **Repository Tests**: Database operation testing
- **Authentication Tests**: Security feature testing

### **Test Structure**
```
Tests/
├── HallApp.UnitTests/
├── HallApp.IntegrationTests/
└── HallApp.TestUtilities/
```

## 📚 Business Logic Documentation

### **Booking Workflow**
1. **Hall Search**: Customer searches available halls by date/location
2. **Hall Selection**: Customer views hall details and availability
3. **Service Selection**: Optional vendor services added to booking
4. **Reservation Creation**: Booking created in pending status
5. **Payment Processing**: Customer completes payment
6. **Confirmation**: Booking confirmed, notifications sent
7. **Event Management**: Hall manager manages event logistics

### **Approval Workflows**
- **Hall Manager Approval**: Admin approval required for new hall managers
- **Vendor Manager Approval**: Admin approval for service providers
- **Hall Listing Approval**: New halls require admin approval
- **Service Approval**: New vendor services need verification

### **Pricing Model**
- **Hall Rental**: Base price + additional services
- **Service Fees**: Vendor-defined pricing
- **Platform Fee**: Commission on successful bookings
- **Payment Methods**: Credit card, bank transfer, digital wallets

## 🚀 Deployment

### **Environment Configuration**
```bash
# Development
dotnet run --environment Development

# Staging
dotnet run --environment Staging

# Production
dotnet run --environment Production
```

### **Docker Support**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "HallApp.Web.dll"]
```

## 🤝 Contributing

### **Development Workflow**
1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

### **Coding Standards**
- Follow C# naming conventions
- Use async/await for I/O operations
- Implement proper error handling
- Add XML documentation for public APIs
- Write unit tests for business logic

## 📝 License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## 📞 Support

For support and questions:
- **Email**: support@hallbookingapp.com
- **Documentation**: [Wiki](../../wiki)
- **Issues**: [GitHub Issues](../../issues)

## 🔄 Version History

- **v1.0.0** - Initial release with core booking functionality
- **v1.1.0** - Added vendor service management
- **v1.2.0** - Implemented user service separation and profile management
- **v1.3.0** - Enhanced admin panel and reporting features

---

*Built with ❤️ using .NET 8 and Clean Architecture principles*

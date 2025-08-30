# API Documentation

## Authentication Endpoints

### Login
```http
POST /api/v1/customer/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "Login successful",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_string",
    "user": {
      "id": 1,
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "roles": ["Customer"]
    }
  }
}
```

### Register Customer
```http
POST /api/v1/customer/register
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe", 
  "email": "john@example.com",
  "password": "Password123!",
  "phoneNumber": "+1234567890"
}
```

## Customer Endpoints

### Get Customer Profile
```http
GET /api/v1/customer/profile
Authorization: Bearer {token}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "appUserId": 1,
    "email": "john@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1234567890",
    "emailConfirmed": true,
    "active": true,
    "customerId": 1,
    "credit": 1500.00,
    "isActive": true,
    "totalBookings": 5,
    "activeBookings": 2,
    "completedBookings": 3,
    "totalSpent": 3500.00,
    "favoriteHalls": 8,
    "reviews": 3
  }
}
```

### Update Customer Profile
```http
PUT /api/v1/customer/profile
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Smith",
  "phoneNumber": "+1234567890",
  "preferences": {
    "budget": 2000,
    "preferredLocation": "Downtown"
  }
}
```

### Get Customer Dashboard
```http
GET /api/v1/customer/dashboard
Authorization: Bearer {token}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "upcomingBookings": [
      {
        "id": 123,
        "hallName": "Grand Ballroom",
        "eventDate": "2024-01-15T18:00:00Z",
        "status": "Confirmed",
        "totalAmount": 1500.00
      }
    ],
    "recentActivity": [...],
    "favoriteHalls": [...],
    "creditBalance": 1500.00,
    "totalSpent": 3500.00
  }
}
```

## Hall Management Endpoints

### Get All Halls
```http
GET /api/v1/halls?page=1&pageSize=10&location=downtown&capacity=100&priceMin=500&priceMax=2000
Authorization: Bearer {token}
```

**Query Parameters:**
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 10, max: 50)
- `location`: Filter by location
- `capacity`: Minimum capacity
- `priceMin`: Minimum price per hour
- `priceMax`: Maximum price per hour
- `available`: Check availability for specific date
- `startDate`: Start date for availability check
- `endDate`: End date for availability check

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": 1,
        "name": "Grand Ballroom",
        "description": "Elegant ballroom for weddings and events",
        "capacity": 200,
        "pricePerHour": 150.00,
        "location": "Downtown",
        "address": "123 Main St, City",
        "amenities": ["Audio/Visual", "Catering Kitchen", "Parking"],
        "images": ["image1.jpg", "image2.jpg"],
        "rating": 4.5,
        "reviewCount": 28,
        "isAvailable": true,
        "hallManager": {
          "companyName": "Elite Venues LLC",
          "contactEmail": "manager@elitevenues.com"
        }
      }
    ],
    "totalCount": 45,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5
  }
}
```

### Get Hall Details
```http
GET /api/v1/halls/{id}
Authorization: Bearer {token}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "id": 1,
    "name": "Grand Ballroom",
    "description": "Elegant ballroom perfect for weddings and corporate events",
    "capacity": 200,
    "pricePerHour": 150.00,
    "location": "Downtown",
    "address": "123 Main St, City, State 12345",
    "amenities": [
      "Professional Audio/Visual System",
      "Full Catering Kitchen",
      "200 Parking Spaces",
      "Bridal Suite",
      "Dance Floor"
    ],
    "images": [
      {
        "url": "https://example.com/images/hall1_main.jpg",
        "caption": "Main ballroom view",
        "isPrimary": true
      }
    ],
    "availability": [
      {
        "date": "2024-01-15",
        "timeSlots": [
          {
            "startTime": "09:00",
            "endTime": "13:00",
            "isAvailable": true,
            "price": 150.00
          },
          {
            "startTime": "14:00", 
            "endTime": "18:00",
            "isAvailable": false,
            "price": 150.00
          }
        ]
      }
    ],
    "reviews": [
      {
        "id": 1,
        "customerName": "Sarah Johnson",
        "rating": 5,
        "comment": "Perfect venue for our wedding!",
        "date": "2023-12-10T00:00:00Z"
      }
    ],
    "rating": 4.5,
    "reviewCount": 28,
    "hallManager": {
      "companyName": "Elite Venues LLC",
      "contactEmail": "manager@elitevenues.com",
      "phone": "+1234567890"
    }
  }
}
```

### Create Hall (Hall Manager Only)
```http
POST /api/v1/halls
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Crystal Ballroom",
  "description": "Modern ballroom with crystal chandeliers",
  "capacity": 150,
  "pricePerHour": 125.00,
  "location": "Midtown",
  "address": "456 Oak Ave, City, State 12345",
  "amenities": ["Audio/Visual", "Parking", "Kitchen"],
  "images": [
    {
      "url": "https://example.com/image1.jpg",
      "caption": "Main view",
      "isPrimary": true
    }
  ]
}
```

## Booking Endpoints

### Create Booking
```http
POST /api/v1/bookings
Authorization: Bearer {token}
Content-Type: application/json

{
  "hallId": 1,
  "eventDate": "2024-01-15",
  "startTime": "18:00",
  "endTime": "23:00",
  "eventType": "Wedding",
  "guestCount": 150,
  "specialRequests": "Need extra decorations",
  "services": [
    {
      "vendorId": 5,
      "serviceType": "Catering",
      "details": "Dinner for 150 guests",
      "price": 3000.00
    }
  ]
}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "bookingId": 12345,
    "status": "Pending",
    "hallDetails": {
      "name": "Grand Ballroom",
      "address": "123 Main St"
    },
    "eventDetails": {
      "date": "2024-01-15",
      "startTime": "18:00",
      "endTime": "23:00",
      "guestCount": 150
    },
    "pricing": {
      "hallRental": 750.00,
      "services": 3000.00,
      "subtotal": 3750.00,
      "tax": 375.00,
      "total": 4125.00
    },
    "paymentStatus": "Pending"
  }
}
```

### Get User Bookings
```http
GET /api/v1/bookings?status=active&page=1&pageSize=10
Authorization: Bearer {token}
```

**Query Parameters:**
- `status`: Filter by status (pending, confirmed, completed, cancelled)
- `page`: Page number
- `pageSize`: Items per page
- `dateFrom`: Start date filter
- `dateTo`: End date filter

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": 12345,
        "hallName": "Grand Ballroom",
        "eventDate": "2024-01-15T18:00:00Z",
        "eventType": "Wedding",
        "status": "Confirmed",
        "guestCount": 150,
        "totalAmount": 4125.00,
        "paymentStatus": "Paid",
        "services": [
          {
            "vendorName": "Gourmet Catering Co",
            "serviceType": "Catering",
            "price": 3000.00
          }
        ]
      }
    ],
    "totalCount": 8,
    "pageNumber": 1,
    "pageSize": 10
  }
}
```

### Update Booking
```http
PUT /api/v1/bookings/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "guestCount": 175,
  "specialRequests": "Updated decoration requirements",
  "services": [
    {
      "vendorId": 5,
      "serviceType": "Catering", 
      "details": "Dinner for 175 guests",
      "price": 3500.00
    }
  ]
}
```

### Cancel Booking
```http
DELETE /api/v1/bookings/{id}
Authorization: Bearer {token}
```

## Vendor Endpoints

### Get All Vendors
```http
GET /api/v1/vendors?type=catering&location=downtown&priceMin=500&priceMax=5000
Authorization: Bearer {token}
```

**Query Parameters:**
- `type`: Vendor type (catering, photography, decoration, music, etc.)
- `location`: Service area
- `priceMin`: Minimum service price
- `priceMax`: Maximum service price
- `rating`: Minimum rating
- `available`: Check availability for date

**Response:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 5,
      "name": "Gourmet Catering Co",
      "description": "Premium catering services for all events",
      "vendorType": "Catering",
      "contactEmail": "info@gourmetcatering.com",
      "contactPhone": "+1234567890",
      "address": "789 Food St, City",
      "rating": 4.7,
      "reviewCount": 156,
      "priceRange": "$$$$",
      "services": [
        {
          "id": 1,
          "name": "Wedding Dinner Package",
          "description": "3-course dinner with appetizers",
          "pricePerPerson": 85.00,
          "minimumGuests": 50
        }
      ],
      "images": ["vendor5_1.jpg", "vendor5_2.jpg"],
      "isActive": true
    }
  ]
}
```

### Book Vendor Service
```http
POST /api/v1/vendors/{vendorId}/book
Authorization: Bearer {token}
Content-Type: application/json

{
  "serviceId": 1,
  "eventDate": "2024-01-15",
  "guestCount": 150,
  "specialRequests": "Vegetarian options needed",
  "contactDetails": {
    "eventLocation": "Grand Ballroom, 123 Main St",
    "contactPerson": "John Doe",
    "contactPhone": "+1234567890"
  }
}
```

## Admin Endpoints

### Get System Statistics
```http
GET /api/v1/admin/statistics
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "users": {
      "totalCustomers": 1250,
      "totalHallManagers": 85,
      "totalVendorManagers": 156,
      "newThisMonth": 47
    },
    "bookings": {
      "totalBookings": 3420,
      "activeBookings": 284,
      "completedBookings": 2890,
      "cancelledBookings": 246,
      "revenueThisMonth": 125000.00,
      "revenueThisYear": 1580000.00
    },
    "halls": {
      "totalHalls": 95,
      "activeHalls": 87,
      "pendingApproval": 8,
      "averageOccupancy": 68.5
    },
    "vendors": {
      "totalVendors": 234,
      "activeVendors": 198,
      "pendingApproval": 12,
      "topCategories": [
        {"name": "Catering", "count": 78},
        {"name": "Photography", "count": 45},
        {"name": "Music", "count": 38}
      ]
    }
  }
}
```

### Get All Users with Roles
```http
GET /api/v1/admin/users?role=Customer&status=active&page=1&pageSize=20
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": 1,
        "email": "john@example.com",
        "firstName": "John",
        "lastName": "Doe",
        "phoneNumber": "+1234567890",
        "roles": ["Customer"],
        "isActive": true,
        "emailConfirmed": true,
        "created": "2023-06-15T10:30:00Z",
        "lastLogin": "2024-01-10T14:22:00Z"
      }
    ],
    "totalCount": 1250,
    "pageNumber": 1,
    "pageSize": 20
  }
}
```

### Create Hall Manager
```http
POST /api/v1/admin/hall-managers
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "firstName": "Michael",
  "lastName": "Johnson",
  "email": "michael@elitevenues.com",
  "phoneNumber": "+1234567890",
  "password": "SecurePass123!",
  "companyName": "Elite Venues LLC",
  "commercialRegistrationNumber": "REG-2024-001"
}
```

### Approve Hall Manager
```http
PUT /api/v1/admin/hall-managers/{userId}/approve
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "isApproved": true,
  "approvalNotes": "All documents verified successfully"
}
```

### Get Pending Approvals
```http
GET /api/v1/admin/pending-approvals
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "hallManagers": [
      {
        "id": 25,
        "firstName": "Jane",
        "lastName": "Smith",
        "email": "jane@venues.com",
        "companyName": "Premier Events",
        "registrationNumber": "REG-2024-002",
        "submittedDate": "2024-01-08T09:00:00Z",
        "documentsProvided": true
      }
    ],
    "vendorManagers": [...],
    "halls": [...],
    "vendors": [...]
  }
}
```

## Error Responses

### Standard Error Format
```json
{
  "isSuccess": false,
  "message": "Validation failed",
  "errors": [
    {
      "field": "email",
      "message": "Email is required"
    },
    {
      "field": "password", 
      "message": "Password must be at least 8 characters"
    }
  ]
}
```

### HTTP Status Codes
- `200 OK`: Request successful
- `201 Created`: Resource created successfully
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `409 Conflict`: Resource already exists
- `422 Unprocessable Entity`: Validation errors
- `500 Internal Server Error`: Server error

## Rate Limiting

### Default Limits
- **General API**: 1000 requests per hour per IP
- **Authentication**: 10 login attempts per 15 minutes per IP
- **Booking Creation**: 5 bookings per hour per user
- **Search**: 100 searches per hour per user

### Rate Limit Headers
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642704000
```

## Webhooks

### Booking Status Updates
```http
POST {webhook_url}
Content-Type: application/json

{
  "event": "booking.status_changed",
  "bookingId": 12345,
  "oldStatus": "pending",
  "newStatus": "confirmed",
  "timestamp": "2024-01-10T15:30:00Z",
  "data": {
    "hallName": "Grand Ballroom",
    "eventDate": "2024-01-15T18:00:00Z",
    "customerEmail": "john@example.com"
  }
}
```

### Payment Notifications
```http
POST {webhook_url}
Content-Type: application/json

{
  "event": "payment.completed",
  "bookingId": 12345,
  "paymentId": "pay_abc123",
  "amount": 4125.00,
  "status": "success",
  "timestamp": "2024-01-10T15:35:00Z"
}
```

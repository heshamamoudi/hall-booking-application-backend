# Business Logic Documentation

## Core Business Domains

### User Management Domain

#### Customer Lifecycle
```csharp
// Customer Registration Flow
1. User registers with basic information
2. Customer profile automatically created with default credit (0)
3. Email verification sent
4. Upon verification, account becomes active
5. Customer can browse halls and make bookings

// Customer Credit System
- Initial credit: $0.00
- Credit can be added via payment gateway
- Credit used for booking deposits and payments
- Minimum credit required for bookings: $50.00
- Refunds credited back to customer account
```

#### Hall Manager Approval Process
```csharp
// Hall Manager Registration & Approval
1. Hall Manager registers with company details
2. Profile created with IsApproved = false
3. Admin reviews registration and business documents
4. Admin approves/rejects application
5. Upon approval, HallManager can create and manage halls
6. Automatic notification sent on status change

// Business Validation Rules
- Commercial Registration Number must be unique
- Company Name must be unique within system
- Required documents: Business License, Insurance Certificate
- Approval required before hall creation allowed
```

#### Vendor Manager Approval Process
```csharp
// Vendor Manager Registration & Approval  
1. Vendor Manager registers with business information
2. Profile created with IsApproved = false
3. Commercial Registration and VAT numbers validated
4. Admin reviews and approves application
5. Upon approval, can create vendors and services
6. Ongoing monitoring for service quality

// Business Validation Rules
- Commercial Registration Number must be unique (if provided)
- VAT Number must be unique and valid format (if provided)
- At least one business identifier required (Commercial Reg OR VAT)
- Service categories must be pre-approved
```

### Hall Management Domain

#### Hall Lifecycle Management
```csharp
// Hall Creation Process
1. Hall Manager creates hall with details
2. Hall requires admin approval for listing
3. Images and amenities can be added
4. Pricing and availability configured
5. Hall becomes searchable after approval

// Hall Availability System
- Base availability: All days available by default
- Custom availability: Specific date ranges can be blocked
- Booking conflict prevention: No double bookings allowed
- Maintenance windows: Halls can be temporarily unavailable
- Seasonal pricing: Different rates for peak/off-peak periods
```

#### Pricing Strategy
```csharp
// Dynamic Pricing Model
Base Price: Hall.PricePerHour
+ Peak Time Multiplier (evenings/weekends): 1.2x - 1.5x  
+ Seasonal Adjustment (wedding season): 1.1x - 1.3x
+ Demand Surge (high booking volume): 1.05x - 1.2x
+ Long Duration Discount (8+ hours): 0.9x - 0.95x

// Minimum Booking Rules
- Minimum booking duration: 4 hours
- Maximum booking duration: 12 hours per day
- Advance booking required: 48 hours minimum
- Cancellation window: 72 hours for full refund
```

### Vendor Services Domain

#### Service Management
```csharp
// Vendor Service Categories
1. Catering Services
   - Per person pricing model
   - Minimum guest count requirements
   - Dietary restriction handling
   - Setup/cleanup time allocation

2. Photography/Videography
   - Hourly or package pricing
   - Equipment and editing included
   - Raw footage delivery options
   - Copyright and usage rights

3. Decoration Services
   - Theme-based packages
   - Custom decoration requests
   - Setup/teardown logistics
   - Damage liability coverage

4. Music/Entertainment
   - Hourly performance rates
   - Equipment rental included
   - Sound system compatibility
   - Performance genre specialization

5. Transportation
   - Vehicle type and capacity
   - Distance-based pricing
   - Wait time charges
   - Multiple pickup/drop-off points
```

#### Service Booking Integration
```csharp
// Vendor Integration with Hall Bookings
1. Customer selects hall for booking
2. System suggests compatible vendors based on:
   - Service area coverage
   - Availability on event date
   - Previous customer ratings
   - Price range compatibility
   
3. Customer adds services to booking package
4. Combined booking created with hall + services
5. Separate vendor confirmations required
6. Coordinated logistics for event day
```

### Booking System Domain

#### Booking Workflow State Machine
```csharp
public enum BookingStatus
{
    Pending,        // Initial state after creation
    Confirmed,      // Payment received, booking guaranteed
    InProgress,     // Event day, currently happening  
    Completed,      // Event finished successfully
    Cancelled,      // Cancelled by customer or system
    Refunded        // Payment returned to customer
}

// State Transitions
Pending -> Confirmed (on payment)
Pending -> Cancelled (by customer or timeout)
Confirmed -> InProgress (on event start time)
Confirmed -> Cancelled (with penalties)
InProgress -> Completed (on event end time)
Completed -> Refunded (in case of disputes)
Any -> Cancelled (by admin intervention)
```

#### Payment Processing Logic
```csharp
// Booking Payment Structure
Total Amount = Hall Cost + Service Costs + Platform Fee + Tax

Hall Cost Calculation:
- Duration (hours) × PricePerHour × Multipliers
- Peak time/seasonal adjustments applied
- Long duration discounts calculated

Service Costs:
- Sum of all selected vendor services
- Per-person services × Guest Count
- Fixed-rate services at quoted price

Platform Fees:
- Hall booking: 5% of hall cost
- Vendor services: 3% of service cost
- Minimum platform fee: $25.00
- Maximum platform fee: $500.00

Tax Calculation:
- Local tax rate applied to total
- Tax-exempt services handled separately
- Tax collected and remitted by platform
```

#### Booking Conflict Resolution
```csharp
// Availability Checking Algorithm
1. Check hall availability for requested date/time
2. Verify no existing confirmed bookings overlap
3. Account for setup/cleanup buffer time (1 hour each)
4. Check vendor availability for same date/time
5. Reserve temporary slot during payment process (15 minutes)
6. Confirm booking on successful payment
7. Release temporary reservations on payment failure

// Buffer Time Management
Setup Buffer: 1 hour before event start
- Hall access for decorations
- Vendor setup and preparation
- Sound/lighting system testing

Cleanup Buffer: 1 hour after event end  
- Guest departure time
- Vendor equipment removal
- Hall cleaning and reset
```

### Review and Rating System

#### Review Authenticity
```csharp
// Review Validation Rules
1. Only customers with completed bookings can review
2. One review per booking per entity (Hall/Vendor)
3. Reviews must be submitted within 30 days of event
4. Admin moderation for inappropriate content
5. Rating scale: 1-5 stars (integer values only)

// Review Impact on Business
- Average rating calculated in real-time
- Minimum 5 reviews before rating displayed publicly
- Low-rated entities (< 3.0) flagged for admin review  
- High-rated entities (> 4.5) eligible for premium listing
- Recent reviews weighted higher than older reviews
```

#### Rating Algorithm
```csharp
// Weighted Rating Calculation
Recent Reviews (< 30 days): Weight = 1.0
Medium Age Reviews (30-180 days): Weight = 0.8  
Older Reviews (> 180 days): Weight = 0.6

Weighted Average = Σ(Rating × Weight × Booking Value) / Σ(Weight × Booking Value)

// Booking Value Factor
Higher value bookings have slightly more weight in ratings
Value Multiplier = Min(1.5, BookingAmount / 1000)
```

### Notification System

#### Event-Driven Notifications
```csharp
// Booking Lifecycle Notifications
1. Booking Created -> Customer, Hall Manager, Vendors
2. Payment Confirmed -> All parties + Admin
3. 48hrs Before Event -> Reminder to Customer  
4. 24hrs Before Event -> Confirmation to Hall Manager
5. Event Day -> Check-in notifications
6. Event Completed -> Review request to Customer
7. Booking Cancelled -> Refund notifications

// Admin Notifications
- New user registrations requiring approval
- High-value bookings for monitoring
- Low rating alerts for quality control
- System errors and technical issues
- Monthly revenue and usage reports
```

#### Communication Channels
```csharp
// Multi-Channel Notification Delivery
1. In-App Notifications
   - Real-time push via SignalR
   - Notification center in user dashboard
   - Read/unread status tracking

2. Email Notifications  
   - Booking confirmations and receipts
   - Event reminders and updates
   - Admin approval notifications
   - Monthly statements and reports

3. SMS Notifications (Optional)
   - Critical booking updates only
   - Event day reminders
   - Emergency cancellations
   - Opt-in required for each user
```

### Revenue and Commission Model

#### Platform Revenue Streams
```csharp
// Commission Structure
Hall Bookings: 5% of booking value
- Minimum commission: $25 per booking
- Maximum commission: $500 per booking  
- Volume discounts for high-performing halls

Vendor Services: 3% of service value
- Minimum commission: $15 per service
- No maximum commission limit
- Performance bonuses for top-rated vendors

Subscription Fees (Optional):
- Premium hall listings: $99/month
- Enhanced vendor profiles: $49/month
- Priority customer support: $29/month

Payment Processing: 2.9% + $0.30 per transaction
- Standard credit card processing fee
- Pass-through cost, no markup
- Alternative payment methods may vary
```

#### Financial Settlement
```csharp
// Payout Schedule and Process
Hall Managers:
- Payouts processed weekly (Fridays)
- Funds held for 7 days after event completion
- Direct deposit to verified bank accounts
- Detailed transaction reports provided

Vendor Managers:
- Payouts processed bi-weekly
- Funds held for 14 days after service completion
- Quality assurance period for dispute resolution
- Performance metrics included in payout reports

Refund Processing:
- Customer-initiated: Processed within 24 hours
- Dispute resolution: Up to 14 business days
- Platform-initiated: Immediate processing
- Partial refunds supported for service issues
```

### Quality Assurance and Dispute Resolution

#### Quality Control Measures
```csharp
// Automated Quality Monitoring
1. Rating Trend Analysis
   - Sudden drops in ratings trigger alerts
   - Consistent low performance leads to review
   - Improvement plans required for continuation

2. Booking Pattern Analysis  
   - High cancellation rates investigated
   - No-show patterns tracked and addressed
   - Pricing anomalies flagged for review

3. Customer Complaint Tracking
   - Complaint categories and frequency analysis
   - Response time monitoring for service providers
   - Resolution satisfaction tracking
```

#### Dispute Resolution Process
```csharp
// Structured Dispute Handling
Level 1 - Direct Resolution (0-48 hours):
- Parties communicate directly through platform
- Platform provides mediation guidelines
- Most disputes resolved at this level

Level 2 - Platform Mediation (2-7 days):
- Admin team reviews evidence from both parties
- Platform makes binding decision on resolution
- Refunds/credits issued as appropriate

Level 3 - External Arbitration (7+ days):
- Complex disputes involving significant amounts
- Third-party arbitration service engaged
- Legal compliance and documentation required
- Final and binding resolution
```

### Data Analytics and Business Intelligence

#### Key Performance Indicators (KPIs)
```csharp
// Platform Metrics
- Monthly Active Users (MAU)
- Booking Conversion Rate (Browser to Booker)
- Average Booking Value (ABV)  
- Customer Lifetime Value (CLV)
- Vendor/Hall Manager Retention Rate
- Platform Revenue Growth Rate

// Quality Metrics
- Average Rating Across All Services
- Customer Satisfaction Score (CSAT)
- Net Promoter Score (NPS)
- Dispute Resolution Time
- First Response Time to Issues

// Operational Metrics
- Platform Uptime and Performance
- Payment Success Rate
- Email Delivery Rate
- Search Response Time
- Mobile App Crash Rate
```

#### Predictive Analytics
```csharp
// Demand Forecasting
- Seasonal booking pattern analysis
- Event type popularity trends
- Geographic demand distribution
- Price elasticity modeling
- Capacity utilization optimization

// Customer Behavior Analysis
- Booking abandonment prediction
- Service upselling opportunities  
- Customer churn risk identification
- Personalized recommendation engine
- Optimal pricing suggestions for providers
```

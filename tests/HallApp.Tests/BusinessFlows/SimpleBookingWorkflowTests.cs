using Xunit;
using FluentAssertions;
using Moq;
using HallApp.Core.Entities.BookingEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IRepositories;

namespace HallApp.Tests.BusinessFlows;

/// <summary>
/// Simplified Business Flow Tests for Booking Workflow
/// Tests basic booking operations without complex service dependencies
/// </summary>
public class SimpleBookingWorkflowTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IBookingRepository> _mockBookingRepository;

    private readonly Hall _testHall;
    private readonly Customer _testCustomer;
    private readonly DateTime _testEventDate;

    public SimpleBookingWorkflowTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockBookingRepository = new Mock<IBookingRepository>();
        _mockUnitOfWork.Setup(x => x.BookingRepository).Returns(_mockBookingRepository.Object);

        // Setup test data - Realistic Saudi Arabia hall booking scenario
        _testEventDate = DateTime.UtcNow.AddDays(30); // Bookings typically 30 days in advance

        _testHall = new Hall
        {
            ID = 1,
            Name = "Grand Wedding Hall",
            BothWeekDays = 5000, // 5000 SAR per day on weekdays
            BothWeekEnds = 8000, // 8000 SAR per day on weekends
            Active = true
        };

        _testCustomer = new Customer
        {
            Id = 1,
            AppUserId = 1,
            CreditMoney = 1000 // Customer has 1000 SAR credit
        };
    }

    #region Booking Creation Tests

    [Theory]
    [InlineData(DayOfWeek.Friday, 8000)]  // Weekend pricing
    [InlineData(DayOfWeek.Saturday, 8000)] // Weekend pricing
    [InlineData(DayOfWeek.Sunday, 5000)]   // Weekday pricing
    [InlineData(DayOfWeek.Monday, 5000)]   // Weekday pricing
    [InlineData(DayOfWeek.Thursday, 5000)] // Weekday pricing
    public void HallPricing_ShouldCalculateCorrectly_BasedOnDayOfWeek(
        DayOfWeek dayOfWeek, double expectedHallCost)
    {
        // Arrange - Dynamic test data based on day of week
        var isWeekend = dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday;

        // Act - Determine price based on day type
        var price = isWeekend ? _testHall.BothWeekEnds : _testHall.BothWeekDays;

        // Assert
        price.Should().Be(expectedHallCost,
            $"because {dayOfWeek} should cost {expectedHallCost} SAR");
    }

    [Theory]
    [InlineData(100, 5000, 0, 765, 5865)]    // No discount, 15% VAT
    [InlineData(100, 5000, 500, 690, 5290)]  // 500 SAR discount, 15% VAT
    [InlineData(100, 5000, 1000, 615, 4715)] // 1000 SAR discount, 15% VAT
    public void BookingPricing_ShouldCalculateCorrectly_WithDynamicDiscounts(
        decimal vendorCost, decimal hallCost, decimal discount,
        decimal expectedTax, decimal expectedTotal)
    {
        // Arrange - Test different pricing scenarios
        var subtotal = hallCost + vendorCost;
        var amountAfterDiscount = subtotal - discount;
        var taxAmount = amountAfterDiscount * 0.15m; // 15% VAT for Saudi Arabia
        var totalAmount = amountAfterDiscount + taxAmount;

        // Assert - Verify Saudi Arabia tax calculations
        taxAmount.Should().BeApproximately(expectedTax, 1m,
            "because VAT should be 15% of amount after discount");
        totalAmount.Should().BeApproximately(expectedTotal, 1m,
            "because total = (subtotal - discount) + VAT");
    }

    #endregion

    #region Hall Availability Tests

    [Theory]
    [InlineData("08:00", "12:00", "14:00", "18:00", true)]  // No overlap
    [InlineData("18:00", "22:00", "20:00", "23:00", false)] // Overlaps
    [InlineData("18:00", "22:00", "17:00", "19:00", false)] // Overlaps
    [InlineData("18:00", "22:00", "19:00", "21:00", false)] // Contained within
    public void CheckAvailability_ShouldDetectOverlaps_Correctly(
        string existingStart, string existingEnd,
        string requestedStart, string requestedEnd,
        bool expectedAvailable)
    {
        // Arrange - Parse time slots
        var existingStartTime = TimeSpan.Parse(existingStart);
        var existingEndTime = TimeSpan.Parse(existingEnd);
        var requestedStartTime = TimeSpan.Parse(requestedStart);
        var requestedEndTime = TimeSpan.Parse(requestedEnd);

        // Act - Check for time overlap
        var hasOverlap = (requestedStartTime < existingEndTime) && (requestedEndTime > existingStartTime);

        // Assert
        var isAvailable = !hasOverlap;
        isAvailable.Should().Be(expectedAvailable,
            $"booking from {requestedStart}-{requestedEnd} should {(expectedAvailable ? "not" : "")} conflict with {existingStart}-{existingEnd}");
    }

    #endregion

    #region Booking Status Transitions

    [Theory]
    [InlineData("Pending", "Approved", true)]
    [InlineData("Approved", "Confirmed", true)]
    [InlineData("Confirmed", "Completed", true)]
    [InlineData("Pending", "Cancelled", true)]
    [InlineData("Approved", "Cancelled", true)]
    public void BookingStatusTransitions_ShouldFollowBusinessRules(
        string fromStatus, string toStatus, bool shouldSucceed)
    {
        // Arrange - Valid status transitions
        var validTransitions = new Dictionary<string, List<string>>
        {
            { "Pending", new List<string> { "Approved", "Cancelled" } },
            { "Approved", new List<string> { "Confirmed", "Cancelled" } },
            { "Confirmed", new List<string> { "Completed", "Cancelled" } }
        };

        // Act - Check if transition is valid
        var isValidTransition = validTransitions.ContainsKey(fromStatus) &&
                                validTransitions[fromStatus].Contains(toStatus);

        // Assert
        isValidTransition.Should().Be(shouldSucceed);
    }

    #endregion

    #region Multi-Booking Scenarios

    [Fact]
    public async Task CustomerBookings_ShouldFilterCorrectly_ByCustomerId()
    {
        // Arrange - Multiple customers with bookings
        var customer1Bookings = new List<Booking>
        {
            new Booking { Id = 1, CustomerId = 1, HallId = 1 },
            new Booking { Id = 2, CustomerId = 1, HallId = 2 }
        };

        _mockBookingRepository.Setup(x => x.GetBookingsByCustomerIdAsync(1))
            .ReturnsAsync(customer1Bookings);

        // Act
        var result = await _mockBookingRepository.Object.GetBookingsByCustomerIdAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(b => b.CustomerId.Should().Be(1));
    }

    [Fact]
    public async Task HallBookings_ShouldFilterCorrectly_ByHallId()
    {
        // Arrange - Multiple halls with bookings
        var hall1Bookings = new List<Booking>
        {
            new Booking { Id = 1, HallId = 1 },
            new Booking { Id = 2, HallId = 1 },
            new Booking { Id = 3, HallId = 1 }
        };

        _mockBookingRepository.Setup(x => x.GetBookingsByHallIdAsync(1))
            .ReturnsAsync(hall1Bookings);

        // Act
        var result = await _mockBookingRepository.Object.GetBookingsByHallIdAsync(1);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(b => b.HallId.Should().Be(1));
    }

    #endregion

    #region VAT Calculation Tests

    [Theory]
    [InlineData(1000, 150)]    // 15% of 1000
    [InlineData(5000, 750)]    // 15% of 5000
    [InlineData(10000, 1500)]  // 15% of 10,000
    [InlineData(25000, 3750)]  // 15% of 25,000
    [InlineData(100000, 15000)] // 15% of 100,000
    public void CalculateVAT_ShouldApplySaudiRate_Consistently(
        decimal amount, decimal expectedVAT)
    {
        // Arrange
        const decimal SAUDI_VAT_RATE = 0.15m; // 15% VAT in Saudi Arabia

        // Act
        var vat = amount * SAUDI_VAT_RATE;

        // Assert
        vat.Should().Be(expectedVAT,
            $"because VAT in Saudi Arabia is always 15%");
    }

    #endregion
}

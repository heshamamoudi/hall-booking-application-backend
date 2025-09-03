#nullable enable
using System.ComponentModel.DataAnnotations;

namespace HallApp.Application.DTOs.Booking
{
    /// <summary>
    /// Request DTO for booking pricing calculation
    /// </summary>
    public class BookingPricingRequestDto
    {
        [Required]
        public int HallId { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public string SelectedGender { get; set; } = null!;

        public List<SelectedServiceDto> SelectedServices { get; set; } = new();
    }

    /// <summary>
    /// Represents a selected vendor service for pricing calculation
    /// </summary>
    public class SelectedServiceDto
    {
        [Required]
        public int VendorId { get; set; }

        [Required]
        public int ServiceItemId { get; set; }

        public int Quantity { get; set; } = 1;

        public string? SpecialInstructions { get; set; }
    }
}
#nullable restore

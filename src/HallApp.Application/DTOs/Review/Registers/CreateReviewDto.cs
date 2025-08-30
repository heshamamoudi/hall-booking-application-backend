namespace HallApp.Application.DTOs.Review.Registers;

public class CreateReviewDto
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int? HallId { get; set; }
    public int? VendorId { get; set; }
}

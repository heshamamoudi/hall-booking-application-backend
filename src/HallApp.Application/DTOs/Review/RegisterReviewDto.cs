namespace HallApp.Application.DTOs.Review
{
    public class RegisterReviewDto
    {
        public int HallId { get; set; }
        public int BookingId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
        public int CustomerId { get; set; }
    }
}

namespace HallApp.Application.DTOs.Customer;

public class FavoriteDto
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public int CustomerId { get; set; }
    public int HallId { get; set; }
}

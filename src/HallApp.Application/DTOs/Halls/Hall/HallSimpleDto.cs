namespace HallApp.Application.DTOs.Halls.Hall;

/// <summary>
/// Simplified Hall DTO without Managers to prevent circular references
/// </summary>
public class HallSimpleDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double AverageRating { get; set; }
    public int Gender { get; set; }
    public bool IsActive { get; set; }
}
